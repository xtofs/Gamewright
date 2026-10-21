namespace Gamewright;

/// <summary>A board square. File 0 is the a-file; rank 0 is the top row, where Black starts.</summary>
public readonly record struct Square(int File, int Rank)
{
    public bool IsOnBoard => File is >= 0 and < ChessBoard.Size && Rank is >= 0 and < ChessBoard.Size;

    /// <summary>Algebraic name such as "e4".</summary>
    public override string ToString() =>
        IsOnBoard ? $"{(char)('a' + File)}{ChessBoard.Size - Rank}" : $"({File}, {Rank})";
}
