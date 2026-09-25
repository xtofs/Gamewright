namespace Gamewright;

using System.Numerics;

public static class VectorExtensions
{
    extension(Vector2 size)
    {
        public Rect ToRect() => new Rect(0, 0, size.X, size.Y);
    }
}
