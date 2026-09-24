namespace Gamewright;

using System.Numerics;
using Silk.NET.Maths;

public static class Vector2DExtensions
{
    extension(Vector2D<int> vector)
    {
        public Vector2 ToVector2()
            => new(vector.X, vector.Y);

        public Rect ToRect()
            => new Rect(0, 0, vector.X, vector.Y);

        public Vector2D<int> Clamp(Vector2D<int> min, Vector2D<int> max)
            => new(Math.Clamp(vector.X, min.X, max.X), Math.Clamp(vector.Y, min.Y, max.Y));
    }
}
