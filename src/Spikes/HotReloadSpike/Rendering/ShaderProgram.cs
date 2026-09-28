namespace HotReloadSpike.Rendering;

using System.Numerics;
using Silk.NET.OpenGL;

internal sealed unsafe class ShaderProgram : IDisposable
{
    private readonly GL _gl;
    private readonly int _projectionLocation;

    public ShaderProgram(GL gl, string vertexSource, string fragmentSource)
    {
        _gl = gl;
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
                throw new InvalidOperationException($"Shader program linking failed:{Environment.NewLine}{gl.GetProgramInfoLog(Handle)}");
            }
        }
        finally
        {
            gl.DeleteShader(vertexShader);
            gl.DeleteShader(fragmentShader);
        }

        _projectionLocation = gl.GetUniformLocation(Handle, "projection");
    }

    public uint Handle { get; }

    public void Use() => _gl.UseProgram(Handle);

    public void SetProjection(Matrix4x4 projection)
    {
        _gl.UseProgram(Handle);
        _gl.UniformMatrix4(_projectionLocation, 1, false, (float*)&projection);
    }

    public void Dispose() => _gl.DeleteProgram(Handle);

    private uint Compile(ShaderType type, string source)
    {
        var shader = _gl.CreateShader(type);
        _gl.ShaderSource(shader, source);
        _gl.CompileShader(shader);
        _gl.GetShader(shader, ShaderParameterName.CompileStatus, out var compileStatus);
        if (compileStatus != 0)
        {
            return shader;
        }

        var log = _gl.GetShaderInfoLog(shader);
        _gl.DeleteShader(shader);
        throw new InvalidOperationException($"{type} compilation failed:{Environment.NewLine}{log}");
    }
}
