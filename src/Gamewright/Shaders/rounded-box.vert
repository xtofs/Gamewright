#version 330 core

layout(location = 0) in vec2 unitPosition;
layout(location = 1) in vec2 center;
layout(location = 2) in vec2 halfExtent;
layout(location = 3) in vec4 cornerRadii;
layout(location = 4) in vec4 fillColor;
layout(location = 5) in vec4 strokeColor;
layout(location = 6) in float strokeWidth;

out vec2 fragmentPosition;
out vec2 fragmentHalfExtent;
out vec4 fragmentCornerRadii;
out vec4 fragmentFillColor;
out vec4 fragmentStrokeColor;
out float fragmentStrokeWidth;

void main()
{
    float expansion = strokeWidth * 0.5 + 1.0;
    vec2 expandedHalfExtent = halfExtent + vec2(expansion);
    fragmentPosition = unitPosition * expandedHalfExtent;
    fragmentHalfExtent = halfExtent;
    fragmentCornerRadii = cornerRadii;
    fragmentFillColor = fillColor;
    fragmentStrokeColor = strokeColor;
    fragmentStrokeWidth = strokeWidth;
    gl_Position = projection * vec4(center + fragmentPosition, 0.0, 1.0);
}
