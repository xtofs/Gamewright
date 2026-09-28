#version 330 core

layout(location = 0) in vec2 unitPosition;
layout(location = 1) in vec2 startPoint;
layout(location = 2) in vec2 controlPoint;
layout(location = 3) in vec2 endPoint;
layout(location = 4) in float thickness;
layout(location = 5) in vec4 color;

out vec2 fragmentPosition;
out vec2 fragmentStart;
out vec2 fragmentControl;
out vec2 fragmentEnd;
out float fragmentRadius;
out vec4 fragmentColor;

void main()
{
    // the curve lies inside the convex hull of its control points,
    // so their bounding box grown by the radius (plus a pixel for antialiasing) covers the stroke
    float radius = thickness * 0.5;
    float expansion = radius + 1.0;
    vec2 minimum = min(min(startPoint, controlPoint), endPoint) - expansion;
    vec2 maximum = max(max(startPoint, controlPoint), endPoint) + expansion;
    vec2 worldPosition = mix(minimum, maximum, unitPosition * 0.5 + 0.5);

    fragmentPosition = worldPosition;
    fragmentStart = startPoint;
    fragmentControl = controlPoint;
    fragmentEnd = endPoint;
    fragmentRadius = radius;
    fragmentColor = color;
    gl_Position = projection * vec4(worldPosition, 0.0, 1.0);
}
