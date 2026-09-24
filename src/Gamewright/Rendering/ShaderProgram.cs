namespace Gamewright.Rendering;

using Silk.NET.OpenGL;

internal sealed class ShaderProgram : IDisposable
{
    private readonly GL gl;
    private bool disposed;

    public ShaderProgram(
        GL gl,
        string vertexSource,
        string fragmentSource,
        IReadOnlyList<UniformBlockBinding> uniformBlocks)
    {
        this.gl = gl;
        var vertexShader = Compile(ShaderType.VertexShader, vertexSource);
        var fragmentShader = Compile(ShaderType.FragmentShader, fragmentSource);

        try
        {
            Handle = gl.CreateProgram();
            gl.AttachShader(Handle, vertexShader);
            gl.AttachShader(Handle, fragmentShader);
            gl.LinkProgram(Handle);
            gl.GetProgram(Handle, ProgramPropertyARB.LinkStatus, out var linkStatus);
            if (linkStatus == 0)
            {
                throw new ShaderCompilationException("Shader program linking failed", gl.GetProgramInfoLog(Handle));
            }

            foreach (var block in uniformBlocks)
            {
                var blockIndex = gl.GetUniformBlockIndex(Handle, block.Name);
                if (blockIndex == uint.MaxValue)
                {
                    throw new ShaderCompilationException($"Uniform block '{block.Name}' is not active", string.Empty);
                }

                gl.UniformBlockBinding(Handle, blockIndex, block.Binding);
            }
        }
        catch
        {
            if (Handle != 0)
            {
                gl.DeleteProgram(Handle);
            }

            throw;
        }
        finally
        {
            gl.DeleteShader(vertexShader);
            gl.DeleteShader(fragmentShader);
        }
    }

    public uint Handle { get; private set; }

    public void Use()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        gl.UseProgram(Handle);
    }

    public void SetInt(string uniformName, int value)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        var location = gl.GetUniformLocation(Handle, uniformName);
        if (location < 0)
        {
            throw new InvalidOperationException($"Uniform '{uniformName}' is not active in shader program {Handle}.");
        }

        gl.Uniform1(location, value);
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        gl.DeleteProgram(Handle);
        Handle = 0;
        disposed = true;
    }

    private uint Compile(ShaderType type, string source)
    {
        var shader = gl.CreateShader(type);
        gl.ShaderSource(shader, source);
        gl.CompileShader(shader);
        gl.GetShader(shader, ShaderParameterName.CompileStatus, out var compileStatus);
        if (compileStatus != 0)
        {
            return shader;
        }

        var log = gl.GetShaderInfoLog(shader);
        gl.DeleteShader(shader);
        throw new ShaderCompilationException($"{type} compilation failed", log);
    }
}

internal readonly record struct UniformBlockBinding(string Name, uint Binding);

public sealed class ShaderCompilationException(string message, string compilerLog)
    : Exception(string.IsNullOrWhiteSpace(compilerLog) ? message : $"{message}:{Environment.NewLine}{compilerLog}");
