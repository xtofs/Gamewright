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

type RenderFrame = {
    commands: DrawCommand[];
    pathData: Uint8Array;
    geometry: Float32Array;
    strings: string[];
    styles: Style[];
};

const fonts = ['16px system-ui', '600 24px system-ui'];
const canvas = getRequiredElement<HTMLCanvasElement>('surface');
const context = canvas.getContext('2d');

if (!context) {
    throw new Error('The browser does not provide a 2D canvas context.');
}
const drawingContext: CanvasRenderingContext2D = context;

const imageCache = new Map<number, HTMLImageElement>();
let currentFrame: RenderFrame | undefined;

function getRequiredElement<T extends HTMLElement>(id: string): T {
    const element = document.getElementById(id);
    if (!(element instanceof HTMLElement)) {
        throw new Error(`Required page element #${id} was not found.`);
    }
    return element as T;
}

function drawFrame(frame: RenderFrame): void {
    currentFrame = frame;
    const scale = window.devicePixelRatio || 1;
    const width = canvas.clientWidth;
    const height = canvas.clientHeight;

    if (canvas.width !== Math.round(width * scale) || canvas.height !== Math.round(height * scale)) {
        canvas.width = Math.round(width * scale);
        canvas.height = Math.round(height * scale);
    }

    drawingContext.setTransform(scale, 0, 0, scale, 0, 0);
    drawingContext.clearRect(0, 0, width, height);

    for (const command of frame.commands) {
        const style = readStyle(frame, command.styleIndex);
        switch (command.type) {
            case DrawCommandType.Path:
                drawPath(frame, command, style);
                break;
            case DrawCommandType.Text:
                drawText(frame, command, style);
                break;
            case DrawCommandType.Image:
                drawImage(
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

function readStyle(frame: RenderFrame, styleIndex: number): Style {
    const style = frame.styles[styleIndex];
    if (!style) {
        throw new Error(`Frame style index ${styleIndex} is out of range.`);
    }
    return style;
}

function readGeometry(frame: RenderFrame, command: DrawCommand, relativeIndex: number): number {
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

function drawPath(frame: RenderFrame, command: DrawCommand, style: Style): void {
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
        drawingContext.fillStyle = colorToCss(style.fillColor);
        drawingContext.fill(path);
    }
    if ((command.flags & DrawCommandFlags.Stroke) !== 0) {
        drawingContext.strokeStyle = colorToCss(style.strokeColor);
        drawingContext.lineWidth = style.strokeWidth;
        drawingContext.stroke(path);
    }
}

function readPathNumber(frame: RenderFrame, end: number, index: number): number {
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

function drawText(frame: RenderFrame, command: DrawCommand, style: Style): void {
    const text = frame.strings[command.data];
    const font = fonts[style.fontIndex];
    if (text === undefined) {
        throw new Error(`Frame string index ${command.data} is out of range.`);
    }
    if (font === undefined) {
        throw new Error(`Font index ${style.fontIndex} is out of range.`);
    }

    drawingContext.fillStyle = colorToCss(style.fillColor);
    drawingContext.font = font;
    drawingContext.textAlign = (command.flags & DrawCommandFlags.AlignCenter) !== 0
        ? 'center'
        : (command.flags & DrawCommandFlags.AlignEnd) !== 0 ? 'right' : 'left';
    drawingContext.textBaseline = 'middle';
    drawingContext.fillText(text, readGeometry(frame, command, 0), readGeometry(frame, command, 1));
}

function drawImage(imageId: number, x: number, y: number, width: number, height: number): void {
    let image = imageCache.get(imageId);
    if (!image) {
        image = new Image();
        image.addEventListener('load', () => {
            if (currentFrame) {
                drawFrame(currentFrame);
            }
        }, { once: true });
        image.addEventListener('error', () => {
            console.error(`Unable to load frame image resource ${imageId}.`);
        }, { once: true });
        imageCache.set(imageId, image);
        image.src = `/images/${imageId}`;
    }

    if (image.complete && image.naturalWidth > 0) {
        drawingContext.drawImage(image, x, y, width, height);
    }
}

function colorToCss(rgba: number): string {
    const red = rgba & 0xff;
    const green = (rgba >>> 8) & 0xff;
    const blue = (rgba >>> 16) & 0xff;
    const alpha = ((rgba >>> 24) & 0xff) / 255;
    return `rgba(${red}, ${green}, ${blue}, ${alpha})`;
}

function submit(
    encodedCommands: number[],
    encodedPathData: number[],
    geometry: number[],
    strings: string[],
    encodedStyles: number[]
): void {
    if (encodedCommands.length % 8 !== 0) {
        throw new Error('Frame command buffer has an invalid length.');
    }
    if (encodedStyles.length % 16 !== 0) {
        throw new Error('Frame style buffer has an invalid length.');
    }

    const commands: DrawCommand[] = [];
    for (let offset = 0; offset < encodedCommands.length; offset += 8) {
        commands.push({
            type: encodedCommands[offset] as DrawCommandType,
            styleIndex: encodedCommands[offset + 1],
            flags: encodedCommands[offset + 2],
            geometryOffset: encodedCommands[offset + 3],
            geometryLength: encodedCommands[offset + 4],
            pathDataOffset: encodedCommands[offset + 5],
            pathDataLength: encodedCommands[offset + 6],
            data: encodedCommands[offset + 7]
        });
    }

    const styleBytes = Uint8Array.from(encodedStyles);
    const styleView = new DataView(styleBytes.buffer);
    const styles: Style[] = [];
    for (let offset = 0; offset < styleBytes.length; offset += 16) {
        styles.push({
            fillColor: styleView.getUint32(offset, true),
            strokeColor: styleView.getUint32(offset + 4, true),
            strokeWidth: styleView.getFloat32(offset + 8, true),
            fontIndex: styleView.getInt32(offset + 12, true)
        });
    }

    drawFrame({
        commands,
        pathData: Uint8Array.from(encodedPathData),
        geometry: Float32Array.from(geometry),
        strings,
        styles
    });
}

const { setModuleImports, getAssemblyExports, getConfig, runMain } = await dotnet
    .withApplicationArguments('start')
    .create();

setModuleImports('main.js', {
    dom: { submit }
});

const config = getConfig();
const exports = await getAssemblyExports(config.mainAssemblyName);
const pauseButton = getRequiredElement<HTMLButtonElement>('pause');

getRequiredElement<HTMLButtonElement>('reset').addEventListener('click', (event) => {
    exports.StopwatchSample.Reset();
    event.preventDefault();
});

pauseButton.addEventListener('click', (event) => {
    const isRunning = exports.StopwatchSample.Toggle();
    pauseButton.innerText = isRunning ? 'Pause' : 'Start';
    event.preventDefault();
});

await runMain();
