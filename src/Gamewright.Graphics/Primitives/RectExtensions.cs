namespace Gamewright.Graphics;

using System.Numerics;

public static class RectExtensions
{
    extension(Rect rect)
    {
        /// <summary>
        /// Returns the rectangle that fits centered within the given rectangle while maintaining the specified aspect ratio.
        /// </summary>
        public Rect GetMaxOfRatio(float ratio = 1f)
        {
            var width = rect.Width;
            var height = rect.Height;
            if (width / height > ratio)
            {
                width = height * ratio;
            }
            else
            {
                height = width / ratio;
            }
            // center the rectangle within the original rect
            var offsetX = (rect.Width - width) / 2;
            var offsetY = (rect.Height - height) / 2;
            return new Rect(rect.X + offsetX, rect.Y + offsetY, width, height);
        }

        public Rect Inset(float amount)
            => new Rect(rect.X + amount, rect.Y + amount, rect.Width - 2 * amount, rect.Height - 2 * amount);

        public Rect Inset(Vector2 amount)
            => new Rect(rect.X + amount.X, rect.Y + amount.Y, rect.Width - 2 * amount.X, rect.Height - 2 * amount.Y);

        /// <summary>
        /// Returns <paramref name="fraction"/> of the shorter side, for line widths, insets and corner radii
        /// that should grow and shrink with the rectangle they decorate.
        /// </summary>
        public float Fraction(float fraction) => fraction * MathF.Min(rect.Width, rect.Height);

        public Vector2 TopLeft
        {
            get => new Vector2(rect.X, rect.Y);
        }
    }
}
