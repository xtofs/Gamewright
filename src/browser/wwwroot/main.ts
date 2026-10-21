import { dotnet } from './_framework/dotnet.js';

const enum DrawCommandType {
    Path = 1,
    Text = 2,
    Image = 3
}

const enum DrawCommandFlags {
    Fill = 1,
    Stroke = 2,
    Closed = 4,
    AlignCenter = 8,
    AlignEnd = 16
}

const enum PathOpcode {
    MoveTo,
    LineTo,
    QuadTo,
    CubicTo,
    ArcTo,
    ClosePath
}

type DrawCommand = {
    type: DrawCommandType;
    styleIndex: number;
    flags: number;
    geometryOffset: number;
    geometryLength: number;
    pathDataOffset: number;
    pathDataLength: number;
    data: number;
};

type Style = {
    fillColor: number;
    strokeColor: number;
    strokeWidth: number;
    fontIndex: number;
};

type Frame = {
    commands: DrawCommand[];
    pathData: Uint8Array;
    geometry: Float32Array;
    strings: string[];
    styles: Style[];
};

const fonts = ['16px system-ui', '600 24px system-ui'];
const canvas = getRequiredElement<HTMLCanvasElement>('surface');
const context = getRequiredContext(canvas);
const staticCanvas = document.createElement('canvas');
const staticContext = getRequiredContext(staticCanvas);

// Frames are authored in this coordinate space and scaled to the canvas. The host
// supplies the size (AppHost.FrameWidth/FrameHeight) before the first frame.
let frameWidth = 0;
let frameHeight = 0;

// Sprite sheet layout, supplied by the host (Gamewright.SpriteSheet) before the first frame.
let sheetColumns = 0;
let spriteSize = 0;

const spriteSheet = new Image();
spriteSheet.addEventListener('load', () => {
    staticCacheWidth = 0;
    staticCacheHeight = 0;
    renderScene();
}, { once: true });
spriteSheet.addEventListener('error', () => {
    console.error('Unable to load sprites.png.');
}, { once: true });
spriteSheet.src = 'sprites.png';
let staticFrame: Frame | undefined;
let dynamicFrame: Frame | undefined;
let staticCacheWidth = 0;
let staticCacheHeight = 0;

function getRequiredElement<T extends HTMLElement>(id: string): T {
    const element = document.getElementById(id);
    if (!(element instanceof HTMLElement)) {
        throw new Error(`Required page element #${id} was not found.`);
    }
    return element as T;
}

function getRequiredContext(target: HTMLCanvasElement): CanvasRenderingContext2D {
    const result = target.getContext('2d');
    if (!result) {
        throw new Error('The browser does not provide a 2D canvas context.');
    }
    return result;
}

// Where the frame sits on the canvas, in device pixels: one uniform scale, centred, so the
// frame never distorts whatever shape the canvas box has. Updated by resizeCanvas.
const view = { scale: 1, offsetX: 0, offsetY: 0 };

// Sizes the backing store to the displayed box and fits the frame inside it (letterbox).
function resizeCanvas(target: HTMLCanvasElement): void {
    if (frameWidth === 0 || frameHeight === 0) {
        throw new Error('The frame size was not configured.');
    }
    const ratio = window.devicePixelRatio || 1;
    const width = Math.round(target.clientWidth * ratio);
    const height = Math.round(target.clientHeight * ratio);

    if (target.width !== width || target.height !== height) {
        target.width = width;
        target.height = height;
    }
    view.scale = Math.min(width / frameWidth, height / frameHeight);
    view.offsetX = (width - frameWidth * view.scale) / 2;
    view.offsetY = (height - frameHeight * view.scale) / 2;
}

// Converts a pointer position (client CSS pixels) to frame coordinates.
function toFrameCoordinates(clientX: number, clientY: number): { x: number, y: number } {
    const bounds = canvas.getBoundingClientRect();
    const deviceX = (clientX - bounds.left - canvas.clientLeft) * canvas.width / canvas.clientWidth;
    const deviceY = (clientY - bounds.top - canvas.clientTop) * canvas.height / canvas.clientHeight;
    return {
        x: (deviceX - view.offsetX) / view.scale,
        y: (deviceY - view.offsetY) / view.scale
    };
}

// Counters for the stats panel; the per-second ones are reset each time the panel updates.
const stats = {
    renders: 0,
    renderMilliseconds: 0,
    submits: 0,
    bytes: 0,
    totalBytes: 0,
    lastStaticBytes: 0,
    lastDynamicBytes: 0
};
const textEncoder = new TextEncoder();

function frameByteLength(buffers: Uint8Array[], strings: string[]): number {
    let length = 0;
    for (const buffer of buffers) {
        length += buffer.length;
    }
    for (const text of strings) {
        length += textEncoder.encode(text).length;
    }
    return length;
}

function updateStats(): void {
    const renderMilliseconds = stats.renders > 0 ? stats.renderMilliseconds / stats.renders : 0;
    const values: Record<string, string> = {
        'stat-fps': `${stats.renders}`,
        'stat-render-ms': renderMilliseconds.toFixed(2),
        'stat-submits': `${stats.submits}`,
        'stat-rate': formatBytes(stats.bytes),
        'stat-static': formatBytes(stats.lastStaticBytes),
        'stat-dynamic': formatBytes(stats.lastDynamicBytes),
        'stat-total': formatBytes(stats.totalBytes)
    };
    for (const [id, value] of Object.entries(values)) {
        getRequiredElement<HTMLElement>(id).textContent = value;
    }
    stats.renders = 0;
    stats.renderMilliseconds = 0;
    stats.submits = 0;
    stats.bytes = 0;
}

function formatBytes(bytes: number): string {
    return bytes < 1024 ? `${bytes} B` : `${(bytes / 1024).toFixed(1)} KB`;
}

function renderScene(): void {
    const start = performance.now();
    drawScene();
    stats.renders++;
    stats.renderMilliseconds += performance.now() - start;
}

function drawScene(): void {
    resizeCanvas(canvas);
    context.setTransform(1, 0, 0, 1, 0, 0);
    context.clearRect(0, 0, canvas.width, canvas.height);
    context.setTransform(view.scale, 0, 0, view.scale, view.offsetX, view.offsetY);

    if (staticFrame) {
        if (staticCacheWidth !== canvas.width || staticCacheHeight !== canvas.height) {
            staticCanvas.width = canvas.width;
            staticCanvas.height = canvas.height;
            staticCacheWidth = canvas.width;
            staticCacheHeight = canvas.height;
            staticContext.setTransform(view.scale, 0, 0, view.scale, view.offsetX, view.offsetY);
            drawCommands(staticContext, staticFrame);
        }
        context.save();
        context.setTransform(1, 0, 0, 1, 0, 0);
        context.drawImage(staticCanvas, 0, 0);
        context.restore();
    }

    if (dynamicFrame) {
        drawCommands(context, dynamicFrame);
    }
}

function drawCommands(target: CanvasRenderingContext2D, frame: Frame): void {
    for (const command of frame.commands) {
        const style = readStyle(frame, command.styleIndex);
        switch (command.type) {
            case DrawCommandType.Path:
                drawPath(target, frame, command, style);
                break;
            case DrawCommandType.Text:
                drawText(target, frame, command, style);
                break;
            case DrawCommandType.Image:
                drawSprite(
                    target,
                    command.data,
                    readGeometry(frame, command, 0),
                    readGeometry(frame, command, 1),
                    readGeometry(frame, command, 2),
                    readGeometry(frame, command, 3)
                );
                break;
            default:
                throw new Error(`Unknown draw command type: ${command.type}`);
        }
    }
}

function readStyle(frame: Frame, styleIndex: number): Style {
    const style = frame.styles[styleIndex];
    if (!style) {
        throw new Error(`Frame style index ${styleIndex} is out of range.`);
    }
    return style;
}

function readGeometry(frame: Frame, command: DrawCommand, relativeIndex: number): number {
    if (relativeIndex < 0 || relativeIndex >= command.geometryLength
        || command.geometryOffset < 0 || command.geometryLength < 0
        || command.geometryOffset + command.geometryLength > frame.geometry.length) {
        throw new Error(`Draw command ${command.type} has incomplete geometry.`);
    }
    const value = frame.geometry[command.geometryOffset + relativeIndex];
    if (value === undefined) {
        throw new Error(`Draw command geometry range is out of bounds.`);
    }
    return value;
}

function drawPath(target: CanvasRenderingContext2D, frame: Frame, command: DrawCommand, style: Style): void {
    const path = new Path2D();
    let pathOffset = command.pathDataOffset;
    let geometryOffset = command.geometryOffset;
    const pathEnd = pathOffset + command.pathDataLength;
    const geometryEnd = geometryOffset + command.geometryLength;
    if (pathOffset < 0 || pathEnd > frame.pathData.length
        || geometryOffset < 0 || geometryEnd > frame.geometry.length) {
        throw new Error('Path geometry range is out of bounds.');
    }

    let currentX = 0;
    let currentY = 0;
    let subpathX = 0;
    let subpathY = 0;

    while (pathOffset < pathEnd) {
        const operation = frame.pathData[pathOffset++];
        switch (operation) {
            case PathOpcode.MoveTo:
                currentX = subpathX = readPathNumber(frame, geometryEnd, geometryOffset++);
                currentY = subpathY = readPathNumber(frame, geometryEnd, geometryOffset++);
                path.moveTo(currentX, currentY);
                break;
            case PathOpcode.LineTo:
                currentX = readPathNumber(frame, geometryEnd, geometryOffset++);
                currentY = readPathNumber(frame, geometryEnd, geometryOffset++);
                path.lineTo(currentX, currentY);
                break;
            case PathOpcode.QuadTo: {
                const controlX = readPathNumber(frame, geometryEnd, geometryOffset++);
                const controlY = readPathNumber(frame, geometryEnd, geometryOffset++);
                currentX = readPathNumber(frame, geometryEnd, geometryOffset++);
                currentY = readPathNumber(frame, geometryEnd, geometryOffset++);
                path.quadraticCurveTo(controlX, controlY, currentX, currentY);
                break;
            }
            case PathOpcode.CubicTo: {
                const control1X = readPathNumber(frame, geometryEnd, geometryOffset++);
                const control1Y = readPathNumber(frame, geometryEnd, geometryOffset++);
                const control2X = readPathNumber(frame, geometryEnd, geometryOffset++);
                const control2Y = readPathNumber(frame, geometryEnd, geometryOffset++);
                currentX = readPathNumber(frame, geometryEnd, geometryOffset++);
                currentY = readPathNumber(frame, geometryEnd, geometryOffset++);
                path.bezierCurveTo(
                    control1X, control1Y, control2X, control2Y, currentX, currentY
                );
                break;
            }
            case PathOpcode.ArcTo: {
                const radiusX = readPathNumber(frame, geometryEnd, geometryOffset++);
                const radiusY = readPathNumber(frame, geometryEnd, geometryOffset++);
                const rotation = readPathNumber(frame, geometryEnd, geometryOffset++);
                const largeArc = readPathNumber(frame, geometryEnd, geometryOffset++) === 1;
                const sweep = readPathNumber(frame, geometryEnd, geometryOffset++) === 1;
                const x = readPathNumber(frame, geometryEnd, geometryOffset++);
                const y = readPathNumber(frame, geometryEnd, geometryOffset++);
                drawSvgArc(path, currentX, currentY, radiusX, radiusY, rotation, largeArc, sweep, x, y);
                currentX = x;
                currentY = y;
                break;
            }
            case PathOpcode.ClosePath:
                path.closePath();
                currentX = subpathX;
                currentY = subpathY;
                break;
            default:
                throw new Error(`Unknown path bytecode operation: ${operation}`);
        }
    }

    if (geometryOffset !== geometryEnd) {
        throw new Error('Path geometry length does not match its bytecode.');
    }
    if ((command.flags & DrawCommandFlags.Closed) !== 0 && pathEnd > command.pathDataOffset
        && frame.pathData[pathEnd - 1] !== PathOpcode.ClosePath) {
        path.closePath();
    }
    if ((command.flags & DrawCommandFlags.Fill) !== 0) {
        target.fillStyle = colorToCss(style.fillColor);
        target.fill(path);
    }
    if ((command.flags & DrawCommandFlags.Stroke) !== 0) {
        target.strokeStyle = colorToCss(style.strokeColor);
        target.lineWidth = style.strokeWidth;
        target.stroke(path);
    }
}

function readPathNumber(frame: Frame, end: number, index: number): number {
    if (index >= end) {
        throw new Error('Path bytecode operation has missing coordinates.');
    }
    const value = frame.geometry[index];
    if (value === undefined) {
        throw new Error('Path geometry range is out of bounds.');
    }
    return value;
}

function drawSvgArc(
    path: Path2D,
    startX: number,
    startY: number,
    radiusX: number,
    radiusY: number,
    rotation: number,
    largeArc: boolean,
    sweep: boolean,
    endX: number,
    endY: number
): void {
    let rx = Math.abs(radiusX);
    let ry = Math.abs(radiusY);
    if ((startX === endX && startY === endY)) {
        return;
    }
    if (rx === 0 || ry === 0) {
        path.lineTo(endX, endY);
        return;
    }

    const phi = rotation * Math.PI / 180;
    const cosPhi = Math.cos(phi);
    const sinPhi = Math.sin(phi);
    const dx = (startX - endX) / 2;
    const dy = (startY - endY) / 2;
    const xPrime = cosPhi * dx + sinPhi * dy;
    const yPrime = -sinPhi * dx + cosPhi * dy;
    const lambda = xPrime * xPrime / (rx * rx) + yPrime * yPrime / (ry * ry);
    if (lambda > 1) {
        const scale = Math.sqrt(lambda);
        rx *= scale;
        ry *= scale;
    }

    const rx2 = rx * rx;
    const ry2 = ry * ry;
    const numerator = Math.max(0, rx2 * ry2 - rx2 * yPrime * yPrime - ry2 * xPrime * xPrime);
    const denominator = rx2 * yPrime * yPrime + ry2 * xPrime * xPrime;
    const factor = (largeArc === sweep ? -1 : 1) * Math.sqrt(numerator / denominator);
    const centerPrimeX = factor * rx * yPrime / ry;
    const centerPrimeY = factor * -ry * xPrime / rx;
    const centerX = cosPhi * centerPrimeX - sinPhi * centerPrimeY + (startX + endX) / 2;
    const centerY = sinPhi * centerPrimeX + cosPhi * centerPrimeY + (startY + endY) / 2;
    const ux = (xPrime - centerPrimeX) / rx;
    const uy = (yPrime - centerPrimeY) / ry;
    const vx = (-xPrime - centerPrimeX) / rx;
    const vy = (-yPrime - centerPrimeY) / ry;
    const startAngle = Math.atan2(uy, ux);
    let delta = Math.atan2(ux * vy - uy * vx, ux * vx + uy * vy);
    if (!sweep && delta > 0) delta -= Math.PI * 2;
    if (sweep && delta < 0) delta += Math.PI * 2;
    path.ellipse(centerX, centerY, rx, ry, phi, startAngle, startAngle + delta, !sweep);
}

function drawText(target: CanvasRenderingContext2D, frame: Frame, command: DrawCommand, style: Style): void {
    const text = frame.strings[command.data];
    const font = fonts[style.fontIndex];
    if (text === undefined) {
        throw new Error(`Frame string index ${command.data} is out of range.`);
    }
    if (font === undefined) {
        throw new Error(`Font index ${style.fontIndex} is out of range.`);
    }

    target.fillStyle = colorToCss(style.fillColor);
    target.font = font;
    target.textAlign = (command.flags & DrawCommandFlags.AlignCenter) !== 0
        ? 'center'
        : (command.flags & DrawCommandFlags.AlignEnd) !== 0 ? 'right' : 'left';
    target.textBaseline = 'middle';
    target.fillText(text, readGeometry(frame, command, 0), readGeometry(frame, command, 1));
}

function drawSprite(
    target: CanvasRenderingContext2D,
    spriteIndex: number,
    x: number,
    y: number,
    width: number,
    height: number
): void {
    if (sheetColumns === 0 || spriteSize === 0) {
        throw new Error('The sprite sheet layout was not configured.');
    }
    if (spriteSheet.complete && spriteSheet.naturalWidth > 0) {
        target.drawImage(
            spriteSheet,
            (spriteIndex % sheetColumns) * spriteSize,
            Math.floor(spriteIndex / sheetColumns) * spriteSize,
            spriteSize,
            spriteSize,
            x,
            y,
            width,
            height
        );
    }
}

function colorToCss(rgba: number): string {
    const red = rgba & 0xff;
    const green = (rgba >>> 8) & 0xff;
    const blue = (rgba >>> 16) & 0xff;
    const alpha = ((rgba >>> 24) & 0xff) / 255;
    return `rgba(${red}, ${green}, ${blue}, ${alpha})`;
}

const CommandSize = 26;
const StyleSize = 16;

// The byte arguments are views into WebAssembly memory that are only valid during this call,
// so everything is copied out here. Layouts match Gamewright.DrawCommand and Gamewright.Style.
function decodeFrame(
    commandView: Uint8Array,
    pathData: Uint8Array,
    geometryView: Uint8Array,
    styleView: Uint8Array,
    strings: string[]
): Frame {
    const commandBytes = commandView.slice();
    const geometryBytes = geometryView.slice();
    const styleBytes = styleView.slice();

    if (commandBytes.length % CommandSize !== 0) {
        throw new Error('Frame command buffer has an invalid length.');
    }
    if (styleBytes.length % StyleSize !== 0) {
        throw new Error('Frame style buffer has an invalid length.');
    }
    if (geometryBytes.length % 4 !== 0) {
        throw new Error('Frame geometry buffer has an invalid length.');
    }

    const commandData = new DataView(commandBytes.buffer);
    const commands: DrawCommand[] = [];
    for (let offset = 0; offset < commandBytes.length; offset += CommandSize) {
        commands.push({
            type: commandData.getUint16(offset, true) as DrawCommandType,
            styleIndex: commandData.getUint16(offset + 2, true),
            flags: commandData.getUint16(offset + 4, true),
            geometryOffset: commandData.getInt32(offset + 6, true),
            geometryLength: commandData.getInt32(offset + 10, true),
            pathDataOffset: commandData.getInt32(offset + 14, true),
            pathDataLength: commandData.getInt32(offset + 18, true),
            data: commandData.getInt32(offset + 22, true)
        });
    }

    const styleData = new DataView(styleBytes.buffer);
    const styles: Style[] = [];
    for (let offset = 0; offset < styleBytes.length; offset += StyleSize) {
        styles.push({
            fillColor: styleData.getUint32(offset, true),
            strokeColor: styleData.getUint32(offset + 4, true),
            strokeWidth: styleData.getFloat32(offset + 8, true),
            fontIndex: styleData.getInt32(offset + 12, true)
        });
    }

    return {
        commands,
        pathData: pathData.slice(),
        geometry: new Float32Array(geometryBytes.buffer),
        strings,
        styles
    };
}

function configureFrame(width: number, height: number): void {
    frameWidth = width;
    frameHeight = height;
    // The canvas box defaults to the frame's shape; a page may give it any other shape.
    canvas.style.aspectRatio = `${width} / ${height}`;
}

function configureSprites(columns: number, cellSize: number): void {
    sheetColumns = columns;
    spriteSize = cellSize;
}

function reportError(message: string): void {
    const element = getRequiredElement<HTMLElement>('error');
    element.textContent = message;
    element.hidden = false;
}

function submitFrame(
    isStatic: boolean,
    commands: Uint8Array,
    pathData: Uint8Array,
    geometry: Uint8Array,
    styles: Uint8Array,
    strings: string[]
): void {
    const byteLength = frameByteLength([commands, pathData, geometry, styles], strings);
    stats.submits++;
    stats.bytes += byteLength;
    stats.totalBytes += byteLength;
    if (isStatic) {
        stats.lastStaticBytes = byteLength;
    } else {
        stats.lastDynamicBytes = byteLength;
    }

    const frame = decodeFrame(commands, pathData, geometry, styles, strings);
    if (isStatic) {
        staticFrame = frame;
        staticCacheWidth = 0;
        staticCacheHeight = 0;
    } else {
        dynamicFrame = frame;
    }
    renderScene();
}

setInterval(updateStats, 1000);

// Redraw when the canvas box changes size, e.g. when the window is resized.
new ResizeObserver(() => {
    if (frameWidth !== 0 && (staticFrame || dynamicFrame)) {
        renderScene();
    }
}).observe(canvas);

try {
    const { setModuleImports, getAssemblyExports, getConfig, runMain } = await dotnet.create();

    setModuleImports('main.js', {
        dom: { submitFrame, configureFrame, configureSprites, reportError }
    });

    const config = getConfig();
    const exports = await getAssemblyExports(config.mainAssemblyName);

    // Clicks are sent in the frame's logical coordinate space; C# decides what they hit.
    canvas.addEventListener('pointerdown', (event) => {
        try {
            const { x, y } = toFrameCoordinates(event.clientX, event.clientY);
            exports.AppHost.Click(x, y);
        } catch (error) {
            reportError(error instanceof Error ? error.message : String(error));
        }
    });

    getRequiredElement<HTMLButtonElement>('new-game').addEventListener('click', () => {
        exports.AppHost.NewGame();
    });

    await runMain();
} catch (error) {
    reportError(error instanceof Error ? error.message : String(error));
    throw error;
}
