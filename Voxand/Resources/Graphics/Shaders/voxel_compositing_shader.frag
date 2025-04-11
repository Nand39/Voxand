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

    vec3 directIllum = directIllumDepth.xyz + indirectIllum.xyz;//lumDepth.w == -1 ? vec3(0, 1, 1) : lumDepth.xyz;
    
//    float fogIntensity = texelFetch(depth_motion, frag, 0).x;
//    float fogShift = fogIntensity - 50;
//    fogIntensity = fogShift < 0 ? 0 : clamp((fogShift) * 0.009, 0, 1);
//    lum = mix(lum, fogColor, fogIntensity);

    vec3 toneMappedLum = toneMap_IDKWHAT(directIllum, 6);
    vec3 gammaCorrectedLum = vec3(pow(toneMappedLum.r, 0.4545), pow(toneMappedLum.g, 0.4545), pow(toneMappedLum.b, 0.4545));
//    vec2 motion = texture(depth_motion, uv).gb;
//    vec2 reprojection = uv + motion;
//    vec4 motionColoring = vec4(motion * 2, 0, 0);
//    if ()
//    {
//        motionColoring = vec4(0, 0, 1, 0);
//    }
//    outColor = vec4(gammaCorrectedLum, 1) * 0.5 + motionColoring;
    outColor = vec4(gammaCorrectedLum, 1);
    
    //outColor = vec4(clamp(lum, vec3(0), vec3(1)), 1);

    //outColor = vec4(denoiseLuminance(frag) * texelFetch(albedo, frag, 0).xyz, 1);
    //outColor = vec4(texelFetch(normal, frag, 0).r * 0.2, 0, 0, 1);
    
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