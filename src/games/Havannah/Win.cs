namespace Havannah;

/// <summary>The three ways to win a game of Havannah.</summary>
public enum WinKind
{
    /// <summary>Connects any two of the six corner cells of the board.</summary>
    Bridge,
    /// <summary>Connects any three edges of the board; corner cells are not part of an edge.</summary>
    Fork,
    /// <summary>A loop around one or more cells, whoever occupies them.</summary>
    Ring,
}

/// <summary>A detected win: its kind, the player and the hexes that demonstrate it.</summary>
/// <param name="Cells">The witness: a bridge's path, a fork's junction and three branches, or a ring's inner boundary.</param>
public readonly record struct Win<T>(WinKind Kind, T Group, IReadOnlyList<Hex> Cells) where T : struct;
