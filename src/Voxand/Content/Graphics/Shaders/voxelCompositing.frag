#version 430 core

in vec2 uv;

out vec4 outColor;

uniform sampler2D albedo;
uniform sampler2D luminance;
uniform sampler2D depth;
uniform isampler2D normal;

ivec2 targetTextureSize;

const int blurKernelSize = 3;

vec3 toneMap(vec3 color, float max_white_l);
vec3 denoiseLuminance(ivec2 targetTexel);

void main() 
{
    targetTextureSize = textureSize(albedo, 0);
    ivec2 frag = ivec2(uv * targetTextureSize);
    //outColor = vec4(denoiseLuminance(frag) * texelFetch(albedo, frag, 0).xyz, 1);
    outColor = vec4(texelFetch(luminance, frag, 0).xyz * texelFetch(albedo, frag, 0).xyz, 1);
    //outColor = vec4(texelFetch(luminance, frag, 0).xyz, 1);
    //outColor = vec4(texelFetch(albedo, frag, 0).xyz, 1);
    //outColor = vec4(texelFetch(depth, frag, 0).r * 0.03, 0, 0, 1);
    //outColor = vec4(texelFetch(normal, frag, 0).r * 0.2, 0, 0, 1);
}

vec3 toneMap(vec3 color, float max_white_l)
{
    float l_old = dot(color, vec3(0.2126, 0.7152, 0.0722));
    float numerator = l_old * (1.0 + (l_old / (max_white_l * max_white_l)));
    float l_new = numerator / (1.0 + l_old);
    return color * (l_new / l_old);
}

vec3 denoiseLuminance(ivec2 targetTexel)
{
    ivec2 maxTexel = ivec2(targetTexel.x + blurKernelSize, targetTexel.y + blurKernelSize);
    vec3 sum = vec3(0);
    ivec2 texel;
    int CN = texelFetch(normal, targetTexel, 0).r;
    float CD = texelFetch(depth, targetTexel, 0).r;
    int pixCount = 0;

    for (texel.x = targetTexel.x - blurKernelSize; texel.x <= maxTexel.x; texel.x++)
    {
        for (texel.y = targetTexel.y - blurKernelSize; texel.y <= maxTexel.y; texel.y++)
        {
            if (texelFetch(normal, texel, 0).r != CN)
            {
                continue;
            }
            if (abs(texelFetch(depth, texel, 0).r - CD) > 0.9)
            {
                continue;
            }
            sum += texelFetch(luminance, texel, 0).xyz;
            pixCount++;
        }
    }
    //return pixCount < 24 ? vec3(1, 1, 1) : vec3(0.2, 0.2, 0.2);
    return sum / pixCount;
}
