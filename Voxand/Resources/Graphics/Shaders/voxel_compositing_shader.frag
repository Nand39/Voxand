#version 430 core

in vec2 uv;

out vec4 outColor;

uniform sampler2D directIllum_depthTex;
uniform sampler2D indirectIllumTex;
uniform sampler2D normalCompound_motionTex;

ivec2 targetTextureSize;

const int blurKernelSize = 3;

vec3 toneMap_IDKWHAT(vec3 color, float max_white_l);
vec3 toneMap_ExtendedReinhard(vec3 color, float whitePoint);
vec3 denoiseLuminance(ivec2 targetTexel);

vec3 fogColor = vec3(0.75, 1.3, 1.9);

void main() 
{
    ivec2 texelCoord = ivec2(uv * textureSize(directIllum_depthTex, 0));
    
    vec4 directIllumDepth = texelFetch(directIllum_depthTex, texelCoord, 0);
    vec4 indirectIllum = texelFetch(indirectIllumTex, texelCoord, 0);

    vec3 lum = directIllumDepth.xyz + indirectIllum.xyz;

    float fogIntensity = clamp(directIllumDepth.w * 0.0001, 0, 0.8);
    lum = mix(lum, fogColor, fogIntensity);

    vec3 toneMappedLum = toneMap_IDKWHAT(lum, 6);
    vec3 gammaCorrectedLum = vec3(pow(toneMappedLum.r, 0.4545), pow(toneMappedLum.g, 0.4545), pow(toneMappedLum.b, 0.4545));

    outColor = vec4(gammaCorrectedLum, 1);
}

vec3 toneMap_IDKWHAT(vec3 color, float max_white_l)
{
    float l_old = dot(color, vec3(0.2126, 0.7152, 0.0722));
    float numerator = l_old * (1.0 + (l_old / (max_white_l * max_white_l)));
    float l_new = numerator / (1.0 + l_old);
    return color * (l_new / l_old);
}

vec3 toneMap_ExtendedReinhard(vec3 color, float whitePoint)
{
    return clamp(color * (1 + color / (whitePoint * whitePoint)) / (color + 1), vec3(0), vec3(1));
}