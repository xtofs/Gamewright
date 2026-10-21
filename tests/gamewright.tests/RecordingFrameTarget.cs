using System.Globalization;
using Gamewright;

namespace Gamewright.Tests;

/// <summary>An <see cref="IFrameTarget"/> that writes each call down as a short line of text.</summary>
internal sealed class RecordingFrameTarget : IFrameTarget
{
    public List<string> Calls { get; } = [];

    private static string F(float value) => value.ToString(CultureInfo.InvariantCulture);

    public void BeginPath() => Calls.Add("begin");

    public void MoveTo(float x, float y) => Calls.Add($"M {F(x)} {F(y)}");

    public void LineTo(float x, float y) => Calls.Add($"L {F(x)} {F(y)}");

    public void QuadTo(float controlX, float controlY, float x, float y) =>
        Calls.Add($"Q {F(controlX)} {F(controlY)} {F(x)} {F(y)}");

    public void CubicTo(float control1X, float control1Y, float control2X, float control2Y, float x, float y) =>
        Calls.Add($"C {F(control1X)} {F(control1Y)} {F(control2X)} {F(control2Y)} {F(x)} {F(y)}");

    public void ArcTo(float radiusX, float radiusY, float rotation, bool largeArc, bool sweep, float x, float y) =>
        Calls.Add($"A {F(radiusX)} {F(radiusY)} {F(rotation)} {(largeArc ? 1 : 0)} {(sweep ? 1 : 0)} {F(x)} {F(y)}");

    public void ClosePath() => Calls.Add("Z");

    public void FillPath(uint rgba) => Calls.Add($"fill {rgba:x8}");

    public void StrokePath(uint rgba, float width) => Calls.Add($"stroke {rgba:x8} {F(width)}");

    public void DrawText(string text, float x, float y, TextStyle style, TextAlignment alignment) =>
        Calls.Add($"text \"{text}\" {F(x)} {F(y)} {style.ColorRgba:x8} font{style.Font} {alignment}");

    public void DrawSprite(int spriteIndex, float x, float y, float width, float height) =>
        Calls.Add($"sprite {spriteIndex} {F(x)} {F(y)} {F(width)} {F(height)}");

    public static List<string> Play(Frame frame)
    {
        var target = new RecordingFrameTarget();
        FramePlayer.Play(frame, target);
        return target.Calls;
    }
}
