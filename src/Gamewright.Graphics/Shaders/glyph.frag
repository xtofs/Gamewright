#version 330 core

in vec2 fragmentUv;
in vec4 fragmentTint;

uniform sampler2D atlas;

out vec4 outputColor;

void main()
{
    float coverage = texture(atlas, fragmentUv).a;
    outputColor = vec4(fragmentTint.rgb, fragmentTint.a * coverage);
}
