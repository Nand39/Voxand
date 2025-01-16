using OpenTK.Graphics.OpenGL4;

using GLAV.Types;
using GLAV.Helpers.Public.Exceptions;

using DisposableExt;

using Voxand.Engine.Systems.Graphics.Tools.ShaderServices;
using Voxand.Engine.Systems.Graphics.Tools.Utility;

namespace Voxand.Engine.Systems.Graphics.Pipelines.Modules;
public interface IAntiAliasingBasicModuleSettings
{
    float Intensity { get; set; }
}
public sealed class AntiAliasingBasicModule : RenderingPipeline, IAntiAliasingBasicModuleSettings
{
    ShaderController shaderController;
    Texture2D luminance_depthAccum, luminance_depthBuffer;
    public Texture2D Luminance_depthToAccumulate { get; private set; }
    public Texture2D NormalCompound_motionTexture { get; private set; }

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
    public AntiAliasingBasicModule(ShaderController shaderControllerTAA, Texture2D luminance_depthToAccumulate, Texture2D normalCompound_motionTexture)
    {
        ExceptionConstructor.ThrowIfTextureSizeNotDivisible(luminance_depthToAccumulate, 8);

        NormalCompound_motionTexture = normalCompound_motionTexture;
        Luminance_depthToAccumulate = luminance_depthToAccumulate;
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
        shaderController.Shader.Use();

        GL.MemoryBarrier(MemoryBarrierFlags.ShaderImageAccessBarrierBit);
        Luminance_depthToAccumulate.BindTex(0);
        luminance_depthAccum.BindTex(1);
        NormalCompound_motionTexture.BindTex(2);
        luminance_depthBuffer.BindAsImage(0, TextureAccess.WriteOnly, SizedInternalFormat.Rgba32f);
        NormalCompound_motionTexture.BindAsImage(1, TextureAccess.WriteOnly, SizedInternalFormat.Rgba32f);

        GL.DispatchCompute(Luminance_depthToAccumulate.Size.X / 8, Luminance_depthToAccumulate.Size.Y / 8, 1);
        GL.MemoryBarrier(MemoryBarrierFlags.ShaderImageAccessBarrierBit);
        ComputeUtility.Instance.CopyTex8(luminance_depthBuffer, luminance_depthAccum, SizedInternalFormat.Rgba32f, true);
        ComputeUtility.Instance.CopyTex8(luminance_depthBuffer, Luminance_depthToAccumulate, SizedInternalFormat.Rgba32f, true);
    }

    public void SetInputOutput(Texture2D luminanceToAccumulate, Texture2D normalCompound_motionTexture)
    {
        ExceptionConstructor.ThrowIfTextureSizeNotDivisible(luminanceToAccumulate, 8);
        ExceptionConstructor.ThrowIfTextureSizeNotEqual(luminanceToAccumulate, normalCompound_motionTexture);

        NormalCompound_motionTexture = normalCompound_motionTexture;
        Luminance_depthToAccumulate = luminanceToAccumulate;
        luminance_depthAccum.Dispose();
        luminance_depthBuffer.Dispose();
        CreateBufferTextures();
    }

    void CreateBufferTextures()
    {
        luminance_depthAccum = new();
        luminance_depthAccum.Alloc(Luminance_depthToAccumulate.Size, Luminance_depthToAccumulate.Format, nint.Zero);
        luminance_depthAccum.SetParam(new(TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear));
        luminance_depthAccum.SetParam(new(TextureParameterName.TextureMinFilter, (int)TextureMagFilter.Linear));
        luminance_depthAccum.Lable = "luminance accum texture";

        luminance_depthBuffer = new();
        luminance_depthBuffer.Alloc(Luminance_depthToAccumulate.Size, Luminance_depthToAccumulate.Format, nint.Zero);
        luminance_depthBuffer.SetParam(new(TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest));
        luminance_depthBuffer.SetParam(new(TextureParameterName.TextureMinFilter, (int)TextureMagFilter.Nearest));
        luminance_depthBuffer.Lable = "luminance anti-aliasing buffer texture";
    }
    protected override void Free() => luminance_depthAccum.Dispose();
}

public sealed class AntiAliasingModule : RenderingPipeline
{
    ShaderController shaderControllerTAA, shaderControllerCopyTex8;
    Texture2D luminanceAccumulator, luminanceBuffer, luminanceOutput;
    public Texture2D Luminance
    {
        get => luminanceOutput; 
        set
        {
            luminanceOutput = value;
            luminanceAccumulator.Dispose();
            luminanceBuffer.Dispose();
            CreateAccumTextures();
        }
    }
    public Texture2D Depth_motionInput { get; set; }
    public AntiAliasingModule(ShaderController shaderControllerTAA, ShaderController shaderControllerCopyTex8, Texture2D depth_motionInput, Texture2D luminanceToAccumulate)
    {
        ExceptionConstructor.ThrowIfTextureSizeNotDivisible(luminanceToAccumulate, 8);

        Depth_motionInput = depth_motionInput;
        luminanceOutput = luminanceToAccumulate;
        CreateAccumTextures();

        this.shaderControllerCopyTex8 = shaderControllerCopyTex8;
        this.shaderControllerCopyTex8.SetUniform("source", 0);

        this.shaderControllerTAA = shaderControllerTAA;
        this.shaderControllerTAA.SetUniform("intensity", 0.9f);
        this.shaderControllerTAA.SetUniform("lumNewImg", 0);
        this.shaderControllerTAA.SetUniform("lumOldImg", 1);
        this.shaderControllerTAA.SetUniform("depth_motionTex", 2);
    }
    public override void Execute()
    {
        shaderControllerTAA.Shader.Use();

        Luminance.BindTex(0);
        luminanceAccumulator.BindTex(1);
        Depth_motionInput.BindTex(2);
        luminanceBuffer.BindAsImage(0, TextureAccess.ReadWrite, SizedInternalFormat.Rgba32f);

        GL.DispatchCompute(Luminance.Size.X / 8, Luminance.Size.Y / 8, 1);
        GL.MemoryBarrier(MemoryBarrierFlags.ShaderImageAccessBarrierBit);

        shaderControllerCopyTex8.Shader.Use();
        luminanceBuffer.BindTex(0);
        Luminance.BindAsImage(0, TextureAccess.WriteOnly, SizedInternalFormat.Rgba32f);
        GL.DispatchCompute(Luminance.Size.X / 8, Luminance.Size.Y / 8, 1);
        GL.MemoryBarrier(MemoryBarrierFlags.ShaderImageAccessBarrierBit);
        luminanceAccumulator.BindAsImage(0, TextureAccess.WriteOnly, SizedInternalFormat.Rgba32f);
        GL.DispatchCompute(Luminance.Size.X / 8, Luminance.Size.Y / 8, 1);
        GL.MemoryBarrier(MemoryBarrierFlags.ShaderImageAccessBarrierBit);
    }
    void CreateAccumTextures()
    {
        luminanceAccumulator = new();
        luminanceAccumulator.Alloc(Luminance.Size, Luminance.Format, nint.Zero);
        luminanceAccumulator.SetParam(new(TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest));
        luminanceAccumulator.SetParam(new(TextureParameterName.TextureMinFilter, (int)TextureMagFilter.Nearest));
        luminanceAccumulator.Lable = "accum";

        luminanceBuffer = new();
        luminanceBuffer.Alloc(Luminance.Size, Luminance.Format, nint.Zero);
        luminanceBuffer.SetParam(new(TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest));
        luminanceBuffer.SetParam(new(TextureParameterName.TextureMinFilter, (int)TextureMagFilter.Nearest));
        luminanceBuffer.Lable = "accumBuffer";
    }
    protected override void Free()
    {
        luminanceAccumulator.Dispose();
        luminanceBuffer.Dispose();
    }
}