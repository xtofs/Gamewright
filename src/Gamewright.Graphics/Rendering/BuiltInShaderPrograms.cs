namespace Gamewright.Graphics.Rendering;

using Gamewright.Graphics.Generated;
using Silk.NET.OpenGL;

internal sealed class BuiltInShaderPrograms : IDisposable
{
    private bool disposed;

    public BuiltInShaderPrograms(GL gl)
    {
        RoundedBox = Create(
            gl,
            RoundedBoxShaderBindings.VertexSource,
            RoundedBoxShaderBindings.FragmentSource,
            RoundedBoxShaderBindings.FrameBlockName,
            RoundedBoxShaderBindings.FrameBinding);
        Capsule = Create(
            gl,
            CapsuleShaderBindings.VertexSource,
            CapsuleShaderBindings.FragmentSource,
            CapsuleShaderBindings.FrameBlockName,
            CapsuleShaderBindings.FrameBinding);
        Glyph = Create(
            gl,
            GlyphShaderBindings.VertexSource,
            GlyphShaderBindings.FragmentSource,
            GlyphShaderBindings.FrameBlockName,
            GlyphShaderBindings.FrameBinding);
        Sprite = Create(
            gl,
            SpriteShaderBindings.VertexSource,
            SpriteShaderBindings.FragmentSource,
            SpriteShaderBindings.FrameBlockName,
            SpriteShaderBindings.FrameBinding);
        Arrow = Create(
            gl,
            ArrowShaderBindings.VertexSource,
            ArrowShaderBindings.FragmentSource,
            ArrowShaderBindings.FrameBlockName,
            ArrowShaderBindings.FrameBinding);
        Triangle = Create(
            gl,
            TriangleShaderBindings.VertexSource,
            TriangleShaderBindings.FragmentSource,
            TriangleShaderBindings.FrameBlockName,
            TriangleShaderBindings.FrameBinding);

        Glyph.Use();
        Glyph.SetInt(GlyphShaderBindings.UniformAtlasName, 0);
        Sprite.Use();
        Sprite.SetInt(SpriteShaderBindings.UniformAtlasName, 0);
    }

    public ShaderProgram RoundedBox { get; }

    public ShaderProgram Capsule { get; }

    public ShaderProgram Glyph { get; }

    public ShaderProgram Sprite { get; }

    public ShaderProgram Arrow { get; }

    public ShaderProgram Triangle { get; }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        Triangle.Dispose();
        Arrow.Dispose();
        Sprite.Dispose();
        Glyph.Dispose();
        Capsule.Dispose();
        RoundedBox.Dispose();
        disposed = true;
    }

    private static ShaderProgram Create(
        GL gl,
        string vertexSource,
        string fragmentSource,
        string frameBlockName,
        int frameBinding)
    {
        return new ShaderProgram(
            gl,
            vertexSource,
            fragmentSource,
            [new UniformBlockBinding(frameBlockName, checked((uint)frameBinding))]);
    }
}
