namespace Dandan;

using System.Diagnostics.CodeAnalysis;
using System.Numerics;

public sealed class CardHitTester(ICardLayout layout)
{
    private readonly ICardLayout _layout = layout;

    public float CornerRadius { get; init; } = 8f;

    /// <summary>Finds the topmost card under the point and returns its layout slot.</summary>
    /// <param name="drawOrder">Card layout slots in the order they were drawn, back to front.</param>
    public bool TryHitTest(IReadOnlyList<int> drawOrder, Vector2 point, [MaybeNullWhen(false)] out int resultSlot)
    {
        for (var drawIndex = drawOrder.Count - 1; drawIndex >= 0; drawIndex--)
        {
            var slot = drawOrder[drawIndex];
            if (Contains(_layout.GetPlacement(slot, drawOrder.Count), point))
            {
                resultSlot = slot;
                return true;
            }
        }
        resultSlot = -1;
        return false;
    }

    private bool Contains(CardPlacement p, Vector2 point, float tolerance = 0f)
    {
        var local = Vector2.Transform(point - p.Center,
                                      Matrix3x2.CreateRotation(-p.Rotation));
        return SdRoundedBox(local, _layout.CardSize / 2, CornerRadius) <= tolerance;
    }

    /// <summary>
    /// Signed distance from p (card-local, origin at center) to a rounded rectangle.
    /// </summary>
    /// <returns></returns>
    // Negative inside, zero on the edge, positive outside.
    static float SdRoundedBox(Vector2 p, Vector2 halfSize, float radius)
    {
        // q is the point's position relative to the inner rectangle, which is the card shrunk by the corner radius.
        // Max(q, 0).Length() is the distance to that inner rectangle when the point is outside it. It covers both the straight edges and the corner arcs.
        // Min(Max(q.X, q.Y), 0) gives the correct negative distance for points inside, so the sign and magnitude stay exact everywhere.
        // Subtracting radius inflates the inner rectangle back into the rounded card.
        var q = Vector2.Abs(p) - halfSize + new Vector2(radius);
        return Vector2.Max(q, Vector2.Zero).Length()
             + MathF.Min(MathF.Max(q.X, q.Y), 0f)
             - radius;
    }
}
