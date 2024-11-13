#version 430 core

in vec2 uv;
out vec4 outColor;

uniform sampler2D tex1;
uniform sampler2D tex2;

uniform float intensity;

void main() 
{
    ivec2 frag = ivec2(uv * textureSize(tex1, 0));
    outColor = mix(texelFetch(tex1, frag, 0), texelFetch(tex2, frag, 0), intensity);
}
