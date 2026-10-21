import { dotnet } from './_framework/dotnet.js';
const fonts = ['16px system-ui', '600 24px system-ui'];
const canvas = getRequiredElement('surface');
const context = canvas.getContext('2d');
if (!context) {
    throw new Error('The browser does not provide a 2D canvas context.');
}
const drawingContext = context;
const staticCanvas = document.createElement('canvas');
const staticContext = staticCanvas.getContext('2d');
if (!staticContext) {
    throw new Error('The browser does not provide a 2D offscreen canvas context.');
}
const staticDrawingContext = staticContext;
// Frames are authored in this coordinate space and scaled to the canvas.
const LogicalWidth = 640;
const LogicalHeight = 620;
// Sprite sheet layout: a grid of square cells; sprite indices count row by row.
const SheetColumns = 7;
const SpriteSize = 200;
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
let staticFrame;
let dynamicFrame;
let staticCacheWidth = 0;
let staticCacheHeight = 0;
function getRequiredElement(id) {
    const element = document.getElementById(id);
    if (!(element instanceof HTMLElement)) {
        throw new Error(`Required page element #${id} was not found.`);
    }
    return element;
}
// Sizes the backing store to the displayed size and returns the logical-to-pixel scale.
function resizeCanvas(target) {
    const ratio = window.devicePixelRatio || 1;
    const width = Math.round(target.clientWidth * ratio);
    const height = Math.round(target.clientWidth * ratio * LogicalHeight / LogicalWidth);
    if (target.width !== width || target.height !== height) {
        target.width = width;
        target.height = height;
    }
    return width / LogicalWidth;
}
function renderScene() {
    const scale = resizeCanvas(canvas);
    drawingContext.setTransform(1, 0, 0, 1, 0, 0);
    drawingContext.clearRect(0, 0, canvas.width, canvas.height);
    drawingContext.setTransform(scale, 0, 0, scale, 0, 0);
    if (staticFrame) {
        if (staticCacheWidth !== canvas.width || staticCacheHeight !== canvas.height) {
            staticCanvas.width = canvas.width;
            staticCanvas.height = canvas.height;
            staticCacheWidth = canvas.width;
            staticCacheHeight = canvas.height;
            staticDrawingContext.setTransform(scale, 0, 0, scale, 0, 0);
            drawCommands(staticDrawingContext, staticFrame);
        }
        drawingContext.save();
        drawingContext.setTransform(1, 0, 0, 1, 0, 0);
        drawingContext.drawImage(staticCanvas, 0, 0);
        drawingContext.restore();
    }
    if (dynamicFrame) {
        drawCommands(drawingContext, dynamicFrame);
    }
}
function drawCommands(target, frame) {
    for (const command of frame.commands) {
        const style = readStyle(frame, command.styleIndex);
        switch (command.type) {
            case 1 /* DrawCommandType.Path */:
                drawPath(target, frame, command, style);
                break;
            case 2 /* DrawCommandType.Text */:
                drawText(target, frame, command, style);
                break;
            case 3 /* DrawCommandType.Image */:
                drawSprite(target, command.data, readGeometry(frame, command, 0), readGeometry(frame, command, 1), readGeometry(frame, command, 2), readGeometry(frame, command, 3));
                break;
            default:
                throw new Error(`Unknown draw command type: ${command.type}`);
        }
    }
}
function readStyle(frame, styleIndex) {
    const style = frame.styles[styleIndex];
    if (!style) {
        throw new Error(`Frame style index ${styleIndex} is out of range.`);
    }
    return style;
}
function readGeometry(frame, command, relativeIndex) {
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
function drawPath(target, frame, command, style) {
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
            case 0 /* PathOpcode.MoveTo */:
                currentX = subpathX = readPathNumber(frame, geometryEnd, geometryOffset++);
                currentY = subpathY = readPathNumber(frame, geometryEnd, geometryOffset++);
                path.moveTo(currentX, currentY);
                break;
            case 1 /* PathOpcode.LineTo */:
                currentX = readPathNumber(frame, geometryEnd, geometryOffset++);
                currentY = readPathNumber(frame, geometryEnd, geometryOffset++);
                path.lineTo(currentX, currentY);
                break;
            case 2 /* PathOpcode.QuadTo */: {
                const controlX = readPathNumber(frame, geometryEnd, geometryOffset++);
                const controlY = readPathNumber(frame, geometryEnd, geometryOffset++);
                currentX = readPathNumber(frame, geometryEnd, geometryOffset++);
                currentY = readPathNumber(frame, geometryEnd, geometryOffset++);
                path.quadraticCurveTo(controlX, controlY, currentX, currentY);
                break;
            }
            case 3 /* PathOpcode.CubicTo */: {
                const control1X = readPathNumber(frame, geometryEnd, geometryOffset++);
                const control1Y = readPathNumber(frame, geometryEnd, geometryOffset++);
                const control2X = readPathNumber(frame, geometryEnd, geometryOffset++);
                const control2Y = readPathNumber(frame, geometryEnd, geometryOffset++);
                currentX = readPathNumber(frame, geometryEnd, geometryOffset++);
                currentY = readPathNumber(frame, geometryEnd, geometryOffset++);
                path.bezierCurveTo(control1X, control1Y, control2X, control2Y, currentX, currentY);
                break;
            }
            case 4 /* PathOpcode.ArcTo */: {
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
            case 5 /* PathOpcode.ClosePath */:
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
    if ((command.flags & 4 /* DrawCommandFlags.Closed */) !== 0 && pathEnd > command.pathDataOffset
        && frame.pathData[pathEnd - 1] !== 5 /* PathOpcode.ClosePath */) {
        path.closePath();
    }
    if ((command.flags & 1 /* DrawCommandFlags.Fill */) !== 0) {
        target.fillStyle = colorToCss(style.fillColor);
        target.fill(path);
    }
    if ((command.flags & 2 /* DrawCommandFlags.Stroke */) !== 0) {
        target.strokeStyle = colorToCss(style.strokeColor);
        target.lineWidth = style.strokeWidth;
        target.stroke(path);
    }
}
function readPathNumber(frame, end, index) {
    if (index >= end) {
        throw new Error('Path bytecode operation has missing coordinates.');
    }
    const value = frame.geometry[index];
    if (value === undefined) {
        throw new Error('Path geometry range is out of bounds.');
    }
    return value;
}
function drawSvgArc(path, startX, startY, radiusX, radiusY, rotation, largeArc, sweep, endX, endY) {
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
    if (!sweep && delta > 0)
        delta -= Math.PI * 2;
    if (sweep && delta < 0)
        delta += Math.PI * 2;
    path.ellipse(centerX, centerY, rx, ry, phi, startAngle, startAngle + delta, !sweep);
}
function drawText(target, frame, command, style) {
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
    target.textAlign = (command.flags & 8 /* DrawCommandFlags.AlignCenter */) !== 0
        ? 'center'
        : (command.flags & 16 /* DrawCommandFlags.AlignEnd */) !== 0 ? 'right' : 'left';
    target.textBaseline = 'middle';
    target.fillText(text, readGeometry(frame, command, 0), readGeometry(frame, command, 1));
}
function drawSprite(target, spriteIndex, x, y, width, height) {
    if (spriteSheet.complete && spriteSheet.naturalWidth > 0) {
        target.drawImage(spriteSheet, (spriteIndex % SheetColumns) * SpriteSize, Math.floor(spriteIndex / SheetColumns) * SpriteSize, SpriteSize, SpriteSize, x, y, width, height);
    }
}
function colorToCss(rgba) {
    const red = rgba & 0xff;
    const green = (rgba >>> 8) & 0xff;
    const blue = (rgba >>> 16) & 0xff;
    const alpha = ((rgba >>> 24) & 0xff) / 255;
    return `rgba(${red}, ${green}, ${blue}, ${alpha})`;
}
function decodeFrame(encodedCommands, encodedPathData, geometry, strings, encodedStyles) {
    if (encodedCommands.length % 8 !== 0) {
        throw new Error('Frame command buffer has an invalid length.');
    }
    if (encodedStyles.length % 16 !== 0) {
        throw new Error('Frame style buffer has an invalid length.');
    }
    const commands = [];
    for (let offset = 0; offset < encodedCommands.length; offset += 8) {
        commands.push({
            type: encodedCommands[offset],
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
    const styles = [];
    for (let offset = 0; offset < styleBytes.length; offset += 16) {
        styles.push({
            fillColor: styleView.getUint32(offset, true),
            strokeColor: styleView.getUint32(offset + 4, true),
            strokeWidth: styleView.getFloat32(offset + 8, true),
            fontIndex: styleView.getInt32(offset + 12, true)
        });
    }
    return {
        commands,
        pathData: Uint8Array.from(encodedPathData),
        geometry: Float32Array.from(geometry),
        strings,
        styles
    };
}
function submitStatic(commands, pathData, geometry, strings, styles) {
    staticFrame = decodeFrame(commands, pathData, geometry, strings, styles);
    staticCacheWidth = 0;
    staticCacheHeight = 0;
    renderScene();
}
function submitDynamic(commands, pathData, geometry, strings, styles) {
    dynamicFrame = decodeFrame(commands, pathData, geometry, strings, styles);
    renderScene();
}
const { setModuleImports, getAssemblyExports, getConfig, runMain } = await dotnet
    .withApplicationArguments('start')
    .create();
setModuleImports('main.js', {
    dom: { submitStatic, submitDynamic }
});
const config = getConfig();
const exports = await getAssemblyExports(config.mainAssemblyName);
const pauseButton = getRequiredElement('pause');
getRequiredElement('reset').addEventListener('click', (event) => {
    exports.StopwatchSample.Reset();
    event.preventDefault();
});
pauseButton.addEventListener('click', (event) => {
    const isRunning = exports.StopwatchSample.Toggle();
    pauseButton.innerText = isRunning ? 'Pause' : 'Start';
    event.preventDefault();
});
await runMain();
