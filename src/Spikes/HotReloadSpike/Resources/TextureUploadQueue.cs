namespace HotReloadSpike.Resources;

using System.Collections.Concurrent;

/// <summary>RGBA pixels decoded on the game thread, waiting to be uploaded by the render thread.</summary>
public sealed record TextureUpload(TextureId Id, int Width, int Height, byte[] Rgba);

/// <summary>
/// Hands decoded textures from the game thread to the render thread. The game thread enqueues an
/// upload before it publishes any frame that uses it, and the render thread drains the queue
/// before replaying a frame, so a command never references a texture that isn't uploaded yet.
/// </summary>
public sealed class TextureUploadQueue
{
    private readonly ConcurrentQueue<TextureUpload> _uploads = new();

    public void Enqueue(TextureUpload upload) => _uploads.Enqueue(upload);

    public bool TryDequeue(out TextureUpload upload) => _uploads.TryDequeue(out upload!);
}
