namespace Dandan;

using System.Numerics;

// Hand fanned around a pivot point below the cards
public sealed class FanLayout : ICardLayout
{
    public Vector2 Pivot { get; init; }
    public float Radius { get; init; } = 400f;
    public float SpreadRadians { get; init; } = 0.6f;

    public Vector2 CardSize { get; init; } = new(100, 140);

    public CardPlacement GetPlacement(int slot, int count)
    {
        var t = count <= 1 ? 0f : (float)slot / (count - 1) - 0.5f;
        var angle = t * SpreadRadians;
        // y-down screen coordinates: angle 0 puts the card straight above the pivot
        var center = Pivot + new Vector2(MathF.Sin(angle), -MathF.Cos(angle)) * Radius;
        return new(center, angle);
    }
}
