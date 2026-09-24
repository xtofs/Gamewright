#version 330 core

in vec2 fragmentPosition;
in vec2 fragmentHalfExtent;
in vec4 fragmentCornerRadii;
in vec4 fragmentFillColor;
in vec4 fragmentStrokeColor;
in float fragmentStrokeWidth;

out vec4 outputColor;

float roundedBoxDistance(vec2 position, vec2 halfExtent, vec4 radii)
{
    float radius = position.x > 0.0
        ? (position.y > 0.0 ? radii.x : radii.y)
        : (position.y > 0.0 ? radii.w : radii.z);
    vec2 offset = abs(position) - halfExtent + vec2(radius);
    return min(max(offset.x, offset.y), 0.0) + length(max(offset, 0.0)) - radius;
}

void main()
{
    float distanceValue = roundedBoxDistance(fragmentPosition, fragmentHalfExtent, fragmentCornerRadii);
    float fillAlpha = fillAlphaFromDistance(distanceValue);
    float strokeAlpha = fragmentStrokeWidth > 0.0
        ? strokeAlphaFromDistance(distanceValue, fragmentStrokeWidth)
        : 0.0;
    vec4 color = mix(fragmentFillColor, fragmentStrokeColor, strokeAlpha);
    color.a *= max(fillAlpha, strokeAlpha);
    outputColor = color;
}
