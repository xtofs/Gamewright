namespace Gamewright;

/// <summary>
/// A handle to an image a host loads. The <paramref name="Path"/> is relative to the host's
/// resources: the page's base URL in a browser, the app's resources on the desktop. A frame only
/// names the image; each host loads it the first time a frame draws it.
/// </summary>
public readonly record struct Image(string Path)
{
    public string Path { get; } = RequirePath(Path);

    private static string RequirePath(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path, nameof(Path));
        return path;
    }
}
