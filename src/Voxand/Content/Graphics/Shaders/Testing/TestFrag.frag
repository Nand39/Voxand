#version 430 core

in vec2 uv;
out vec4 outColor;

layout (binding = 0) uniform sampler2D texture0;

void main()
{
    outColor = texture2D(texture0, uv);
}