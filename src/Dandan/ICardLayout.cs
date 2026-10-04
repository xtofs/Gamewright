namespace Dandan;

using System.Numerics;

public interface ICardLayout
{
    Vector2 CardSize { get; }

    CardPlacement GetPlacement(int slot, int count);
}
