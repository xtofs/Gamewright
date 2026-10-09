namespace Mills;

using System.Runtime.InteropServices;

public class MillsBoard
{
    private readonly Piece?[,] _board = new Piece?[3, 8];

    public Piece? this[Coordinate coord]
    {
        get => _board[coord.Ring, coord.Index];
        set => _board[coord.Ring, coord.Index] = value;
    }

    public Piece ActivePlayer
    {
        get
        {
            var flat = MemoryMarshal.CreateSpan(ref _board[0, 0], _board.Length);
            (var w, var b) = (0, 0);
            for (var i = 0; i < flat.Length; i++)
            {
                var cell = flat[i];
                switch (cell)
                {
                    case Piece.White:
                        w++;
                        break;
                    case Piece.Black:
                        b++;
                        break;
                }
            }
            return w <= b ? Piece.White : Piece.Black;
        }
    }

    /// <summary>
    /// Determines whether the piece at the specified coordinate is part of a mill.
    /// </summary>
    /// <param name="coord"></param>
    /// <returns></returns>
    public bool IsPartOfMill(Coordinate coord)
    {
        if (_board[coord.Ring, coord.Index] is not Piece piece)
        {
            return false;
        }
        var isCorner = coord.Index % 2 != 0;
        if (isCorner)
        {
            // corner cells can only be part of a mill if the two predecessors or the two successors are of the same piece.
            var pred1 = coord with { Index = (coord.Index + 7) % 8 };
            var pred2 = coord with { Index = (coord.Index + 6) % 8 };
            if (this[pred1] == piece && this[pred2] == piece)
            {
                return true;
            }
            var succ1 = coord with { Index = (coord.Index + 1) % 8 };
            var succ2 = coord with { Index = (coord.Index + 2) % 8 };
            if (this[succ1] == piece && this[succ2] == piece)
            {
                return true;
            }
        }
        else
        {
            // non-corner cells can be part of a mill if 
            // - the two adjacent cells in the same ring are of the same piece.
            // - the two cells with the same index in the other two rings are of the same piece.
            var pred = coord with { Index = (coord.Index + 8) % 8 };
            var succ = coord with { Index = (coord.Index + 1) % 8 };
            if (this[pred] == piece && this[succ] == piece)
            {
                return true;
            }
            var otherRing1 = coord with { Ring = (coord.Ring + 1) % 3 };
            var otherRing2 = coord with { Ring = (coord.Ring + 2) % 3 };
            if (this[otherRing1] == piece && this[otherRing2] == piece)
            {
                return true;
            }
        }

        return false;
    }
}
