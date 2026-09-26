#version 330 core

layout(location = 0) in vec2 unitPosition;
layout(location = 1) in vec4 destination;
layout(location = 2) in vec4 uvRectangle;
layout(location = 3) in vec4 tint;

out vec2 fragmentUv;
out vec4 fragmentTint;

void main()
{
    vec2 normalizedPosition = unitPosition * 0.5 + 0.5;
    vec2 worldPosition = destination.xy + normalizedPosition * destination.zw;
    fragmentUv = mix(uvRectangle.xy, uvRectangle.zw, normalizedPosition);
    fragmentTint = tint;
    gl_Position = projection * vec4(worldPosition, 0.0, 1.0);
}
