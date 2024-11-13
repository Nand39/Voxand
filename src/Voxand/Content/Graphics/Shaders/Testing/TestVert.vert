#version 430 core

layout (location = 0) in vec3 aPosition;
layout (location = 1) in vec2 aTexCoord;

out vec2 uv;

void main() 
{
    uv = aTexCoord; 
    gl_Position = vec4(aPosition, 1);
}