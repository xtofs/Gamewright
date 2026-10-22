namespace Havannah;

using Gamewright;

/// <summary>
/// Colors from the OKLab color space (https://en.wikipedia.org/wiki/Oklab_color_space), used to pick
/// board colors that differ evenly in perceived lightness and hue.
/// </summary>
internal static class OklabColorSpace
{
    /// <summary>Converts an OKLab color to sRGB.</summary>
    public static Color FromOkLab(float lightness, float a, float b, byte alpha = 255)
    {
        // OKLab -> l'm's' (inverse of the second matrix), undo the cube root, LMS -> linear sRGB.
        float l = Cube(lightness + 0.3963377774f * a + 0.2158037573f * b);
        float m = Cube(lightness - 0.1055613458f * a - 0.0638541728f * b);
        float s = Cube(lightness - 0.0894841775f * a - 1.2914855480f * b);

        float red = 4.0767416621f * l - 3.3077115913f * m + 0.2309699292f * s;
        float green = -1.2684380046f * l + 2.6097574011f * m - 0.3413193965f * s;
        float blue = -0.0041960863f * l - 0.7034186147f * m + 1.7076147010f * s;

        return new Color(ToByte(red), ToByte(green), ToByte(blue), alpha);
    }

    /// <summary>
    /// Four colors forming a regular tetrahedron in OKLab, centered on the gray axis, so any two differ
    /// equally. The edge length is 2·<paramref name="s"/>; <paramref name="thetaDegrees"/> rotates it about the
    /// lightness axis and so chooses the hues. The defaults give #857B7A, #758081, #82847B and #83828A.
    /// </summary>
    public static Color[] GrayTetrahedron(
        float centerLightness = 0.59987f,
        float s = 0.013f,
        float thetaDegrees = 24f,
        byte alpha = 255)
    {
        var h = s / MathF.Sqrt(2f);
        var theta = thetaDegrees * MathF.PI / 180f;
        var (sin, cos) = MathF.SinCos(theta);

        // Unrotated vertices as (a, b, lightness offset): (±s, 0, -h) and (0, ±s, +h).
        (float A, float B, float DL)[] vertices = [(s, 0, -h), (-s, 0, -h), (0, s, h), (0, -s, h)];

        return [.. vertices.Select(v => FromOkLab(
            centerLightness + v.DL,
            v.A * cos - v.B * sin,
            v.A * sin + v.B * cos,
            alpha))];
    }

    private static float Cube(float x) => x * x * x;

    private static byte ToByte(float linear)
    {
        var gamma = linear <= 0.0031308f
            ? 12.92f * linear
            : 1.055f * MathF.Pow(linear, 1f / 2.4f) - 0.055f;
        return (byte)MathF.Round(Math.Clamp(gamma, 0f, 1f) * 255f);
    }
}
