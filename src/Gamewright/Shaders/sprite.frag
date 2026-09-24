#version 330 core

in vec2 fragmentUv;
in vec4 fragmentTint;

uniform sampler2D atlas;

out vec4 outputColor;

void main()
{
    outputColor = texture(atlas, fragmentUv) * fragmentTint;
}
