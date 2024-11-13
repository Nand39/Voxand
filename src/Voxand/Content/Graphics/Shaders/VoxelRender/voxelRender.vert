#version 430 core

layout (location = 0) in vec3 aPosition;
layout (location = 1) in vec2 aTexCoord;

uniform vec2 renderScale;

out vec2 fragPosition;
out vec2 trueUV;

void main() 
{
    fragPosition = vec2(aPosition.x, aPosition.y);
    trueUV = aPosition.xy;
    vec2 far = vec2(renderScale.x * 2 - 1, renderScale.y * 2 - 1);
    gl_Position = vec4(aPosition.x > 0 ? far.x : aPosition.x, 
                       aPosition.y > 0 ? far.y : aPosition.y, 0, 1);
}