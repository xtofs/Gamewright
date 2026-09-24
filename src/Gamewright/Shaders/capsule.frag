#version 330 core

in vec2 fragmentPosition;
in float fragmentHalfLength;
in float fragmentRadius;
in vec4 fragmentFillColor;
in vec4 fragmentStrokeColor;
in float fragmentStrokeWidth;

out vec4 outputColor;

void main()
{
    vec2 offset = vec2(max(abs(fragmentPosition.x) - fragmentHalfLength, 0.0), fragmentPosition.y);
    float distanceValue = length(offset) - fragmentRadius;
    float fillAlpha = fillAlphaFromDistance(distanceValue);
    float strokeAlpha = fragmentStrokeWidth > 0.0
        ? strokeAlphaFromDistance(distanceValue, fragmentStrokeWidth)
        : 0.0;
    vec4 color = mix(fragmentFillColor, fragmentStrokeColor, strokeAlpha);
    color.a *= max(fillAlpha, strokeAlpha);
    outputColor = color;
}
