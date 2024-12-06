#version 430 core

in vec2 uv;
out vec4 outColor;

uniform sampler2D tex;

void main() 
{
    outColor = texture2D(tex, uv);
    //outColor = vec4(uv, 0, 1);
}
