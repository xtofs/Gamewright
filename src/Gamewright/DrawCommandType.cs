namespace Gamewright;

/// <summary>The kind of a draw command and the data it consumes: <c>docs/frame-spec.md</c> section 3.</summary>
public enum DrawCommandType : ushort
{
    Path = 1,
    Text = 2,
    Image = 3
}
