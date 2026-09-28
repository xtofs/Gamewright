namespace HotReloadSpike.Rendering;

/// <summary>
/// Inline GLSL for the spike. Simplified from Gamewright.Graphics' rounded-box, capsule and
/// triangle shaders, with a plain <c>projection</c> uniform instead of the Frame UBO.
/// </summary>
internal static class Shaders
{
    private const string Alpha = """
        float fillAlphaFromDistance(float distanceValue)
        {
            float antialiasWidth = fwidth(distanceValue);
            return 1.0 - smoothstep(-antialiasWidth, antialiasWidth, distanceValue);
        }

        float strokeAlphaFromDistance(float distanceValue, float strokeWidth)
        {
            float antialiasWidth = fwidth(distanceValue);
            float halfStrokeWidth = strokeWidth * 0.5;
            return 1.0 - smoothstep(halfStrokeWidth - antialiasWidth, halfStrokeWidth + antialiasWidth, abs(distanceValue));
        }
        """;

    public const string RoundedBoxVertex = """
        #version 330 core
        layout(location = 0) in vec2 unitPosition;
        layout(location = 1) in vec2 center;
        layout(location = 2) in vec2 halfExtent;
        layout(location = 3) in float cornerRadius;
        layout(location = 4) in vec4 fillColor;
        layout(location = 5) in vec4 strokeColor;
        layout(location = 6) in float strokeWidth;

        uniform mat4 projection;

        out vec2 fragmentPosition;
        out vec2 fragmentHalfExtent;
        out float fragmentCornerRadius;
        out vec4 fragmentFillColor;
        out vec4 fragmentStrokeColor;
        out float fragmentStrokeWidth;

        void main()
        {
            float expansion = strokeWidth * 0.5 + 1.0;
            fragmentPosition = unitPosition * (halfExtent + vec2(expansion));
            fragmentHalfExtent = halfExtent;
            fragmentCornerRadius = cornerRadius;
            fragmentFillColor = fillColor;
            fragmentStrokeColor = strokeColor;
            fragmentStrokeWidth = strokeWidth;
            gl_Position = projection * vec4(center + fragmentPosition, 0.0, 1.0);
        }
        """;

    public const string RoundedBoxFragment = """
        #version 330 core
        in vec2 fragmentPosition;
        in vec2 fragmentHalfExtent;
        in float fragmentCornerRadius;
        in vec4 fragmentFillColor;
        in vec4 fragmentStrokeColor;
        in float fragmentStrokeWidth;

        out vec4 outputColor;

        """ + Alpha + """

        void main()
        {
            vec2 offset = abs(fragmentPosition) - fragmentHalfExtent + vec2(fragmentCornerRadius);
            float distanceValue = min(max(offset.x, offset.y), 0.0) + length(max(offset, 0.0)) - fragmentCornerRadius;
            float fillAlpha = fillAlphaFromDistance(distanceValue);
            float strokeAlpha = fragmentStrokeWidth > 0.0 ? strokeAlphaFromDistance(distanceValue, fragmentStrokeWidth) : 0.0;
            vec4 color = mix(fragmentFillColor, fragmentStrokeColor, strokeAlpha);
            color.a *= max(fillAlpha, strokeAlpha);
            outputColor = color;
        }
        """;

    public const string CapsuleVertex = """
        #version 330 core
        layout(location = 0) in vec2 unitPosition;
        layout(location = 1) in vec2 startPoint;
        layout(location = 2) in vec2 endPoint;
        layout(location = 3) in float thickness;
        layout(location = 4) in vec4 color;

        uniform mat4 projection;

        out vec2 fragmentPosition;
        out float fragmentHalfLength;
        out float fragmentRadius;
        out vec4 fragmentColor;

        void main()
        {
            vec2 segment = endPoint - startPoint;
            float segmentLength = length(segment);
            vec2 tangent = segmentLength > 0.0 ? segment / segmentLength : vec2(1.0, 0.0);
            vec2 normal = vec2(-tangent.y, tangent.x);
            float radius = thickness * 0.5;
            float expansion = 1.0;
            fragmentHalfLength = segmentLength * 0.5;
            fragmentRadius = radius;
            fragmentPosition = vec2(
                unitPosition.x * (fragmentHalfLength + radius + expansion),
                unitPosition.y * (radius + expansion));
            fragmentColor = color;
            vec2 center = (startPoint + endPoint) * 0.5;
            vec2 world = center + tangent * fragmentPosition.x + normal * fragmentPosition.y;
            gl_Position = projection * vec4(world, 0.0, 1.0);
        }
        """;

    public const string CapsuleFragment = """
        #version 330 core
        in vec2 fragmentPosition;
        in float fragmentHalfLength;
        in float fragmentRadius;
        in vec4 fragmentColor;

        out vec4 outputColor;

        """ + Alpha + """

        void main()
        {
            vec2 offset = vec2(max(abs(fragmentPosition.x) - fragmentHalfLength, 0.0), fragmentPosition.y);
            float distanceValue = length(offset) - fragmentRadius;
            vec4 color = fragmentColor;
            color.a *= fillAlphaFromDistance(distanceValue);
            outputColor = color;
        }
        """;

    public const string TriangleVertex = """
        #version 330 core
        layout(location = 0) in vec2 unitPosition;
        layout(location = 1) in vec2 v0;
        layout(location = 2) in vec2 v1;
        layout(location = 3) in vec2 v2;
        layout(location = 4) in vec4 color;

        uniform mat4 projection;

        out vec4 fragmentColor;

        void main()
        {
            // quad corners -> triangle vertices; the second quad triangle collapses to zero area
            float xSide = step(0.0, unitPosition.x);
            float ySide = step(0.0, unitPosition.y);
            vec2 world = mix(mix(v0, v2, ySide), mix(v1, v2, ySide), xSide);
            fragmentColor = color;
            gl_Position = projection * vec4(world, 0.0, 1.0);
        }
        """;

    public const string TriangleFragment = """
        #version 330 core
        in vec4 fragmentColor;
        out vec4 outputColor;

        void main()
        {
            outputColor = fragmentColor;
        }
        """;

    public const string SpriteVertex = """
        #version 330 core
        layout(location = 0) in vec2 unitPosition;
        layout(location = 1) in vec4 destination;
        layout(location = 2) in vec4 uvRectangle;
        layout(location = 3) in vec4 tint;

        uniform mat4 projection;

        out vec2 fragmentUv;
        out vec4 fragmentTint;

        void main()
        {
            vec2 normalizedPosition = unitPosition * 0.5 + 0.5;
            fragmentUv = mix(uvRectangle.xy, uvRectangle.zw, normalizedPosition);
            fragmentTint = tint;
            gl_Position = projection * vec4(destination.xy + normalizedPosition * destination.zw, 0.0, 1.0);
        }
        """;

    // the sampler uniform defaults to texture unit 0, which is where RenderContext binds
    public const string SpriteFragment = """
        #version 330 core
        in vec2 fragmentUv;
        in vec4 fragmentTint;

        uniform sampler2D atlas;

        out vec4 outputColor;

        void main()
        {
            outputColor = texture(atlas, fragmentUv) * fragmentTint;
        }
        """;
}
