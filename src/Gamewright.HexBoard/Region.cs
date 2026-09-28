namespace Gamewright.HexBoard;

public record struct Region<TGroup>(TGroup Group, ushort Edges, ushort Corners, List<int> Members) where TGroup : struct
{
    public static implicit operator (TGroup Group, ushort Edges, ushort Corners, List<int> Members)(Region<TGroup> value)
    {
        return (value.Group, value.Edges, value.Corners, value.Members);
    }

    public static implicit operator Region<TGroup>((TGroup Group, ushort Edges, ushort Corners, List<int> Members) value)
    {
        return new Region<TGroup>(value.Group, value.Edges, value.Corners, value.Members);
    }
}
