namespace HexLib;

using System.Runtime.CompilerServices;

// interface ICellContent : IEquatable<ICellContent> { char Symbol { get; } }
public interface ICellContent : IEquatable<ICellContent>
{
    public abstract char Symbol { get; }
}

