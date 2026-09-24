#version 330 core

layout(location = 0) in vec2 unitPosition;
layout(location = 1) in vec2 tip;
layout(location = 2) in vec2 base;
layout(location = 3) in float halfWidth;
layout(location = 4) in vec4 color;

out vec4 fragmentColor;

void main()
{
    vec2 axis = base - tip;
    float axisLen = length(axis);
    vec2 tangent = axisLen > 0.0 ? axis / axisLen : vec2(0.0, 1.0);
    vec2 normal = vec2(-tangent.y, tangent.x);

    vec2 leftBase  = base - normal * halfWidth;
    vec2 rightBase = base + normal * halfWidth;

    // Map the four unit quad corners to three triangle vertices:
    //   [-1,-1] -> tip
    //   [ 1,-1] -> rightBase
    //   [ 1, 1] -> leftBase
    //   [-1, 1] -> leftBase  (degenerate; collapses second triangle to zero area)
    float xSide = step(0.0, unitPosition.x);
    float ySide = step(0.0, unitPosition.y);
    vec2 worldPosition = mix(
        mix(tip,       leftBase,  ySide),
        mix(rightBase, leftBase,  ySide),
        xSide);

    fragmentColor = color;
    gl_Position = projection * vec4(worldPosition, 0.0, 1.0);
}
