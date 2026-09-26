#version 330 core

layout(location = 0) in vec2 unitPosition;
layout(location = 1) in vec2 startPoint;
layout(location = 2) in vec2 endPoint;
layout(location = 3) in float thickness;
layout(location = 4) in vec4 fillColor;
layout(location = 5) in vec4 strokeColor;
layout(location = 6) in float strokeWidth;

out vec2 fragmentPosition;
out float fragmentHalfLength;
out float fragmentRadius;
out vec4 fragmentFillColor;
out vec4 fragmentStrokeColor;
out float fragmentStrokeWidth;

void main()
{
    vec2 segment = endPoint - startPoint;
    float segmentLength = length(segment);
    vec2 tangent = segmentLength > 0.0 ? segment / segmentLength : vec2(1.0, 0.0);
    vec2 normal = vec2(-tangent.y, tangent.x);
    float radius = thickness * 0.5;
    float expansion = strokeWidth * 0.5 + 1.0;
    vec2 center = (startPoint + endPoint) * 0.5;
    fragmentHalfLength = segmentLength * 0.5;
    fragmentRadius = radius;
    fragmentPosition = vec2(
        unitPosition.x * (fragmentHalfLength + radius + expansion),
        unitPosition.y * (radius + expansion));
    fragmentFillColor = fillColor;
    fragmentStrokeColor = strokeColor;
    fragmentStrokeWidth = strokeWidth;
    vec2 worldPosition = center + tangent * fragmentPosition.x + normal * fragmentPosition.y;
    gl_Position = projection * vec4(worldPosition, 0.0, 1.0);
}
