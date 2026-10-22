# Frame format

A `Frame` is the drawing language between an app and a renderer. An app records one with a
`Canvas` (or `FrameBuilder`), and each host — the browser renderer in `main.ts`, the Avalonia
`GameControl`, any future one — reads it and draws. This document says what a frame contains,
how much data each command and path operation consumes, and what a renderer must do with it.
The producers are in `src/Gamewright` (`FrameBuilder`, `PathBuilder`, `SvgPathParser`).

Words in capitals (MUST, MAY) are meant as in RFC 2119.

## 1. Conventions

- **Coordinates** are `float`s in _frame units_. The origin is the top left, x grows right and y
  grows down. Every coordinate and size MUST be finite; producers reject anything else.
- A renderer scales frame units to its surface (see `Letterbox`). Line widths, font sizes and
  radii are frame units too, so they scale with everything else.
- **Order.** An app has two frames. The static frame is drawn first, then the dynamic frame on
  top. Within a frame, commands are drawn in order, later ones over earlier ones.
- **Color** is a `uint` packed `R | G<<8 | B<<16 | A<<24` (little endian bytes R, G, B, A),
  straight (not premultiplied) alpha, 255 = opaque.
- **Little endian** is used for every multi-byte value sent between C# and JavaScript.

## 2. The parts of a frame

| Part       | C# type         | Wire form (browser host) | Holds                                           |
| ---------- | --------------- | ------------------------ | ----------------------------------------------- |
| Commands   | `DrawCommand[]` | 26 bytes per command     | the draw calls, in order                        |
| PathData   | `byte[]`        | bytes                    | path opcodes of all path commands, back to back |
| Geometry   | `float[]`       | `float32` values         | the numbers of all commands, back to back       |
| Paints     | `Paint[]`       | 16 bytes per paint       | how paths are painted                           |
| TextStyles | `TextStyle[]`   | 8 bytes per style        | color and font of text                          |
| Strings    | `string[]`      | string array             | text to draw                                    |

Commands refer into the other parts by offset and length. A command's ranges MUST lie inside the
buffers, and styles are shared: many commands may use the same table entry.

### Byte layouts

`DrawCommand` (26 bytes, packed):

| Offset | Size | Field          | Meaning                                                               |
| ------ | ---- | -------------- | --------------------------------------------------------------------- |
| 0      | u16  | Type           | 1 Path, 2 Text, 3 Image (sprite)                                      |
| 2      | u16  | StyleIndex     | index into Paints (Path) or TextStyles (Text); 0 and unused for Image |
| 4      | u16  | Flags          | see §3                                                                |
| 6      | i32  | GeometryOffset | first float of this command in Geometry                               |
| 10     | i32  | GeometryLength | number of floats                                                      |
| 14     | i32  | PathDataOffset | first opcode in PathData (Path only, else 0)                          |
| 18     | i32  | PathDataLength | number of opcodes (Path only, else 0)                                 |
| 22     | i32  | Data           | Text: index into Strings. Image: sprite index. Path: 0                |

`Paint` (16 bytes): `u32 FillRgba` @0, `u32 StrokeRgba` @4, `f32 StrokeWidth` @8, `u32 Mode` @12.
`TextStyle` (8 bytes): `u32 ColorRgba` @0, `i32 Font` @4.

## 3. Draw commands

Flags (`DrawCommandFlags`): `Closed = 1`, `AlignCenter = 2`, `AlignEnd = 4`. Bits not listed are
unused and MUST be 0.

| Command       | StyleIndex | Flags                                  | Geometry                                 | PathData                              | Data         |
| ------------- | ---------- | -------------------------------------- | ---------------------------------------- | ------------------------------------- | ------------ |
| **Path** (1)  | Paints     | `Closed`                               | exactly the operands of its opcodes (§4) | the opcodes, `PathDataLength` of them | 0            |
| **Text** (2)  | TextStyles | `AlignCenter` or `AlignEnd` (not both) | 2 floats: x, y                           | none                                  | string index |
| **Image** (3) | unused (0) | none                                   | 4 floats: x, y, width, height            | none                                  | sprite index |

**Text.** Draws `Strings[Data]` in the text style, on one line. `x` is the left edge for no
alignment flag, the centre for `AlignCenter` and the right edge for `AlignEnd`. `y` is the
_vertical middle of the text_, defined as the middle of the em square: the baseline sits
`(ascent − descent) / 2` below `y`, with ascent and descent taken from the font at the drawn size.
Text is filled with the style's color, no outline. A text command with both alignment flags is
invalid; renderers MAY treat it as centred.

**Image.** Draws one cell of the app's sprite sheet (the `SpriteSheetLayout` the app declares)
into the rectangle, scaled to fit it exactly. The cell for sprite index `i` is column
`i % Columns`, row `i / Columns`, each `CellSize` pixels square. A renderer whose image has not
loaded yet draws nothing and MAY draw again when it has.

## 4. Path operations

A Path command's geometry is the operands of its opcodes in order, with no padding.

| Opcode    | Value | Operands (floats)                                 | Count |
| --------- | ----- | ------------------------------------------------- | ----- |
| MoveTo    | 0     | x, y                                              | 2     |
| LineTo    | 1     | x, y                                              | 2     |
| QuadTo    | 2     | controlX, controlY, x, y                          | 4     |
| CubicTo   | 3     | control1X, control1Y, control2X, control2Y, x, y  | 6     |
| ArcTo     | 4     | radiusX, radiusY, rotation, largeArc, sweep, x, y | 7     |
| ClosePath | 5     | none                                              | 0     |

`GeometryLength` MUST equal the sum of the operand counts of the opcodes. A renderer MUST reject a
frame where it does not, or where an opcode is unknown or its operands run past the geometry.

Producers always start a path with MoveTo and never produce an empty path. A path with no
opcodes draws nothing. The behaviour of a path that does not start with MoveTo is undefined.

**Subpaths and the current point.**

- MoveTo starts a new subpath at (x, y) and makes it the current point. The previous subpath is
  left open, not closed.
- LineTo, QuadTo, CubicTo and ArcTo draw from the current point to their end point (x, y), which
  becomes the current point. QuadTo has one control point and CubicTo two.
- ClosePath draws a straight line from the current point back to the start of the subpath and ends
  it. The current point becomes that start point. ClosePath with no open subpath does nothing.
- An operation other than MoveTo after a ClosePath starts a new subpath at the start point of the
  one just closed.
- The `Closed` flag means the last subpath ends closed. Producers set it exactly when the last
  opcode is ClosePath, so it adds nothing for a renderer that handles ClosePath. If it is set and
  the last subpath is still open, a renderer closes it.

**ArcTo** is an SVG elliptical arc from the current point to (x, y), as in SVG 1.1 §F.6:

- `rotation` is the x-axis rotation of the ellipse in degrees.
- `largeArc` and `sweep` are exactly 0 or 1. `sweep` 1 means the arc is drawn in the
  positive-angle direction, which is clockwise on screen because y points down.
- Radii are taken as absolute values. If they are too small to reach the end point they are scaled
  up uniformly until the arc fits.
- If (x, y) equals the current point the arc draws nothing and the current point does not move.
- If either radius is 0 the arc is a straight line to (x, y).

## 5. Painting

**Paint** (`PaintMode`) says how a path is painted. Fill and stroke use the same path.

| Mode           | Value | What is drawn                                                      |
| -------------- | ----- | ------------------------------------------------------------------ |
| None           | 0     | invalid for a path (the `default` value); a renderer MAY reject it |
| Fill           | 1     | fill with `FillRgba`                                               |
| Stroke         | 2     | stroke with `StrokeRgba` at `StrokeWidth`                          |
| FillThenStroke | 3     | fill, then stroke over it                                          |
| StrokeThenFill | 4     | stroke, then fill over it                                          |

- Filling uses the non-zero winding rule, and every open subpath is filled as if closed.
- A stroke is centred on the path edge and is `StrokeWidth` frame units wide, which is greater
  than 0. Joins are mitre, caps are butt, the mitre limit is 10, and there is no dash pattern.
- Because a stroke is centred on the edge, stroking after the fill shows the full width, while
  stroking first leaves only the outer half visible once the fill covers the inner half.

**Fonts.** `TextStyle.Font` is a `FontIndex`. Body is 16 frame units, regular weight. Heading is
24 frame units, semi-bold (weight 600). The family is the host's default UI font. Any other value
is invalid.

## 6. What a renderer must reject

A renderer MUST fail, and not draw a partial or wrong result, for: an unknown command type, an
unknown opcode, a style index outside its table, a string index outside `Strings`, a command range
outside its buffer, a path whose geometry length does not match its opcodes, an unknown paint mode
or an unknown font.

## 7. Implementations

| Role                         | Where                                                                       |
| ---------------------------- | --------------------------------------------------------------------------- |
| Producers                    | `src/Gamewright`: `Canvas` → `FrameBuilder`, `PathBuilder`, `SvgPathParser` |
| Browser renderer             | `src/Chess.Browser/wwwroot/main.ts` (reads the buffers sent by `AppHost`)   |
| Avalonia renderer            | `src/Gamewright.Avalonia/GameControl.cs`                                    |
| Wire layouts pinned by tests | `tests/Gamewright.Tests/ProtocolTests.cs` s sent by `AppHost`)              |
| Avalonia renderer            | `src/Gamewright.Avalonia/GameControl.cs`                                    |
| Wire layouts pinned by tests | `tests/Gamewright.Tests/ProtocolTests.cs`                                   |
