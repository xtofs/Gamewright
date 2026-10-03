#version 330 core

layout(location = 0) in vec2 unitPosition;
layout(location = 1) in vec4 destination;
layout(location = 2) in vec4 uvRectangle;
layout(location = 3) in vec4 tint;
layout(location = 4) in float rotationRadians;

out vec2 fragmentUv;
out vec4 fragmentTint;

void main()
{
    vec2 normalizedPosition = unitPosition * 0.5 + 0.5;
    vec2 localPosition = (normalizedPosition - vec2(0.5)) * destination.zw;
    float cosine = cos(rotationRadians);
    float sine = sin(rotationRadians);
    vec2 rotatedPosition = vec2(
        cosine * localPosition.x - sine * localPosition.y,
        sine * localPosition.x + cosine * localPosition.y);
    vec2 worldPosition = destination.xy + destination.zw * 0.5 + rotatedPosition;
    fragmentUv = mix(uvRectangle.xy, uvRectangle.zw, normalizedPosition);
    fragmentTint = tint;
    gl_Position = projection * vec4(worldPosition, 0.0, 1.0);
}
