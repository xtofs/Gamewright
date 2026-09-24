namespace Gamewright;

using Silk.NET.OpenGL;
using StbImageSharp;

public sealed unsafe class Texture2D : IDisposable
{
    private readonly GL _gl;
    private bool _disposed;

    public Texture2D(GL gl, uint width, uint height, ReadOnlySpan<byte> rgbaPixels)
    {
        ArgumentNullException.ThrowIfNull(gl);
        if (width == 0 || height == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width), "Texture dimensions must be positive.");
        }

        if (rgbaPixels.Length != checked(width * height * 4))
        {
            throw new ArgumentException("RGBA data length must equal width * height * 4.", nameof(rgbaPixels));
        }

        this._gl = gl;
        Width = width;
        Height = height;
        Handle = gl.GenTexture();
        Bind();
        gl.PixelStore(PixelStoreParameter.UnpackAlignment, 1);
        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
        fixed (byte* pixels = rgbaPixels)
        {
            gl.TexImage2D(
                TextureTarget.Texture2D,
                0,
                InternalFormat.Rgba8,
                width,
                height,
                0,
                PixelFormat.Rgba,
                PixelType.UnsignedByte,
                pixels);
        }
    }

    public static Texture2D FromFile(GL gl, string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("Texture file was not found.", path);
        }

        using var stream = File.OpenRead(path);
        var image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
        return new Texture2D(gl, (uint)image.Width, (uint)image.Height, image.Data);
    }

    public uint Width { get; }

    public uint Height { get; }

    internal uint Handle { get; private set; }

    internal void Bind()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _gl.ActiveTexture(TextureUnit.Texture0);
        _gl.BindTexture(TextureTarget.Texture2D, Handle);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _gl.DeleteTexture(Handle);
        Handle = 0;
        _disposed = true;
    }
}
