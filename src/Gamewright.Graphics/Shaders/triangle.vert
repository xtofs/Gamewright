#version 330 core

layout(location = 0) in vec2 unitPosition;
layout(location = 1) in vec2 v0;
layout(location = 2) in vec2 v1;
layout(location = 3) in vec2 v2;
layout(location = 4) in vec4 color;

out vec4 fragmentColor;

void main()
{
    // Map the four unit quad corners to three triangle vertices:
    //   [-1,-1] -> v0
    //   [ 1,-1] -> v1
    //   [ 1, 1] -> v2
    //   [-1, 1] -> v2  (degenerate; collapses second triangle to zero area)
    float xSide = step(0.0, unitPosition.x);
    float ySide = step(0.0, unitPosition.y);
    vec2 worldPosition = mix(
        mix(v0, v2, ySide),
        mix(v1, v2, ySide),
        xSide);

    fragmentColor = color;
    gl_Position = projection * vec4(worldPosition, 0.0, 1.0);
}
