using OpenTK.Graphics.OpenGL4;

using GLAV.Types;
using GLAV.Helpers.Public.Exceptions;

using DisposableExt;

using Voxand.Engine.Systems.Graphics.Tools.ShaderServices;
using Voxand.Engine.Systems.Graphics.Tools.Utility;
using Voxand.Engine.Systems.Services.Graphics;

namespace Voxand.Engine.Systems.Graphics.Pipelines.Modules;
public sealed class AntiAliasingModule : RenderingPipeline, IRendererAntiAliasing
{
    ShaderController shaderController;
    MutableTexture2D luminanceAccum, luminanceBuffer;
    public MutableTexture2D LuminanceToAccumulate { get; private set; }
    public MutableTexture2D NormalCompound_motionTexture { get; private set; }

    float intensity = 0.5f;
    public float Intensity
    {
        get => intensity;
        set
        {
            intensity = value;
            shaderController.SetUniform("intensity", Math.Clamp(intensity, 0f, 1f));
        }
    }
    float prevIntensity;
    bool isResetPushed;
    public AntiAliasingModule(ShaderController shaderControllerTAA, MutableTexture2D luminanceToAccumulate, MutableTexture2D normalCompound_motionTexture)
    {
        ExceptionConstructor.ThrowIfTextureSizeNotDivisible(luminanceToAccumulate, 8);

        NormalCompound_motionTexture = normalCompound_motionTexture;
        LuminanceToAccumulate = luminanceToAccumulate;
        CreateBufferTextures();

        shaderController = shaderControllerTAA;
        shaderController.SetUniform("intensity", intensity);
        shaderController.SetUniform("lum_depthTex", 0);
        shaderController.SetUniform("lum_depthAccumTex", 1);
        shaderController.SetUniform("normalCompound_motionTex", 2);
        shaderController.SetUniform("outputImg", 0);
        shaderController.SetUniform("normalCompound_motionImg", 1);
    }
    public override void Execute()
    {
        if (isResetPushed)
        {
            prevIntensity = intensity;
            Intensity = 0f;
        }

        shaderController.Shader.Use();

        GL.MemoryBarrier(MemoryBarrierFlags.ShaderImageAccessBarrierBit);
        LuminanceToAccumulate.BindTex(0);
        luminanceAccum.BindTex(1);
        NormalCompound_motionTexture.BindTex(2);
        luminanceBuffer.BindAsImage(0, TextureAccess.WriteOnly);
        NormalCompound_motionTexture.BindAsImage(1, TextureAccess.WriteOnly);

        GL.DispatchCompute(LuminanceToAccumulate.Size.X / 8, LuminanceToAccumulate.Size.Y / 8, 1);
        GL.MemoryBarrier(MemoryBarrierFlags.ShaderImageAccessBarrierBit);
        ComputeUtility.Instance.CopyTex8(luminanceBuffer, luminanceAccum, SizedInternalFormat.Rgba32f, true);
        ComputeUtility.Instance.CopyTex8(luminanceBuffer, LuminanceToAccumulate, SizedInternalFormat.Rgba32f, true);

        if (isResetPushed)
        {
            isResetPushed = false;
            Intensity = prevIntensity;
        }
    }

    public void SetInputOutput(MutableTexture2D luminanceToAccumulate, MutableTexture2D normalCompound_motionTexture)
    {
        ExceptionConstructor.ThrowIfTextureSizeNotDivisible(luminanceToAccumulate, 8);
        ExceptionConstructor.ThrowIfTextureSizeNotEqual(luminanceToAccumulate, normalCompound_motionTexture);

        NormalCompound_motionTexture = normalCompound_motionTexture;
        LuminanceToAccumulate = luminanceToAccumulate;
        luminanceAccum.Dispose();
        luminanceBuffer.Dispose();
        CreateBufferTextures();
    }

    void CreateBufferTextures()
    {
        luminanceAccum = new();
        luminanceAccum.Alloc(LuminanceToAccumulate.Size, LuminanceToAccumulate.StorageFormat, new(PixelFormat.Rgba, PixelType.UnsignedByte), nint.Zero);
        luminanceAccum.SetParam(new(TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear));
        luminanceAccum.SetParam(new(TextureParameterName.TextureMinFilter, (int)TextureMagFilter.Linear));
        luminanceAccum.Label = "luminance accum texture";

        luminanceBuffer = new();
        luminanceBuffer.Alloc(LuminanceToAccumulate.Size, LuminanceToAccumulate.StorageFormat, new(PixelFormat.Rgba, PixelType.UnsignedByte), nint.Zero);
        luminanceBuffer.SetParam(new(TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest));
        luminanceBuffer.SetParam(new(TextureParameterName.TextureMinFilter, (int)TextureMagFilter.Nearest));
        luminanceBuffer.Label = "luminance anti-aliasing buffer texture";
    }

    public void ResetAccumulated() => isResetPushed = true;

    protected override void Free() => luminanceAccum.Dispose();
}