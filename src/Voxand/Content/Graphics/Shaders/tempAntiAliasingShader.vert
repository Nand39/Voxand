#version 430 core

layout (location = 0) in vec3 aPosition;
layout (location = 1) in vec2 aTexCoord;

uniform vec2 renderScale;

out vec2 uv;

void main() 
{
    uv = aTexCoord;
    vec2 far = vec2(renderScale.x * 2 - 1, renderScale.y * 2 - 1);
    gl_Position = vec4(aPosition.x > 0 ? far.x : aPosition.x, 
                       aPosition.y > 0 ? far.y : aPosition.y, 0, 1);
}