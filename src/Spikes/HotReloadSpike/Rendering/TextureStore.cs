namespace HotReloadSpike.Rendering;

using HotReloadSpike.Resources;
using Silk.NET.OpenGL;

/// <summary>
/// Render-thread side of resource loading: owns the GL textures. Uploads whatever the game thread
/// queued, maps <see cref="TextureId"/>s to GL handles, and deletes everything on dispose.
/// </summary>
public sealed unsafe class TextureStore : IDisposable
{
    private readonly GL _gl;
    private readonly TextureUploadQueue _uploads;
    private readonly Dictionary<TextureId, uint> _handles = [];

    public TextureStore(GL gl, TextureUploadQueue uploads)
    {
        _gl = gl;
        _uploads = uploads;
    }

    /// <summary>Uploads all pending textures. Call before replaying a frame.</summary>
    public void ProcessUploads()
    {
        while (_uploads.TryDequeue(out var upload))
        {
            _handles[upload.Id] = Create(upload);
        }
    }

    public bool TryGet(TextureId id, out uint handle)
        => _handles.TryGetValue(id, out handle);

    public void Dispose()
    {
        foreach (var handle in _handles.Values)
        {
            _gl.DeleteTexture(handle);
        }

        _handles.Clear();
    }

    private uint Create(TextureUpload upload)
    {
        var handle = _gl.GenTexture();
        _gl.BindTexture(TextureTarget.Texture2D, handle);
        _gl.PixelStore(PixelStoreParameter.UnpackAlignment, 1);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
        fixed (byte* pixels = upload.Rgba)
        {
            _gl.TexImage2D(
                TextureTarget.Texture2D, 0, InternalFormat.Rgba8, (uint)upload.Width, (uint)upload.Height, 0,
                PixelFormat.Rgba, PixelType.UnsignedByte, pixels);
        }

        return handle;
    }
}
