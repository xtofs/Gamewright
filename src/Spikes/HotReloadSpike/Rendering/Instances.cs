namespace HotReloadSpike.Rendering;

using System.Numerics;
using System.Runtime.InteropServices;

// Per-instance vertex data. Field order must match the attribute lists in RenderContext.

[StructLayout(LayoutKind.Sequential)]
internal struct RoundedBoxInstance
{
    public Vector2 Center;
    public Vector2 HalfExtent;
    public float CornerRadius;
    public Vector4 FillColor;
    public Vector4 StrokeColor;
    public float StrokeWidth;
}

[StructLayout(LayoutKind.Sequential)]
internal struct CapsuleInstance
{
    public Vector2 Start;
    public Vector2 End;
    public float Thickness;
    public Vector4 Color;
}

[StructLayout(LayoutKind.Sequential)]
internal struct TriangleInstance
{
    public Vector2 V0;
    public Vector2 V1;
    public Vector2 V2;
    public Vector4 Color;
}

[StructLayout(LayoutKind.Sequential)]
internal struct SpriteInstance
{
    public Vector4 Destination;
    public Vector4 Uv;
    public Vector4 Tint;
}
