namespace Gamewright;

/// <summary>Per command flags; which commands use which: <c>docs/frame-spec.md</c> section 3.</summary>
[Flags]
public enum DrawCommandFlags : ushort
{
    None = 0,

    /// <summary>The path ends closed, even if its last operation is not a close.</summary>
    Closed = 1,
    AlignCenter = 2,
    AlignEnd = 4
}
