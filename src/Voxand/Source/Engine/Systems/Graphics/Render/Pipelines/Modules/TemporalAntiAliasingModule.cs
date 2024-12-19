using OpenTK.Graphics.OpenGL4;

using GLAV.Types;

using DisposableExt;

using Voxand.Engine.Systems.Graphics.Tools.ShaderServices;
using Voxand.Helpers.Exceptions.GLAVExceptions;

namespace Voxand.Engine.Systems.Graphics.Pipelines.Modules;
public sealed class AntiAliasingBasicModule : RenderingPipeline
{
    ShaderController shaderController;
    Texture2D luminanceAccum, luminanceOutput;
    public Texture2D Luminance
    {
        get => luminanceOutput;
        set
        {
            luminanceOutput = value;
            luminanceAccum.Dispose();
            CreateAccumTextures();
        }
    }
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
    public AntiAliasingBasicModule(ShaderController shaderControllerTAA, Texture2D luminanceToAccumulate)
    {
        ExceptionConstructor.ThrowIfTextureSizeNotDivisible(luminanceToAccumulate, 8);

        luminanceOutput = luminanceToAccumulate;
        CreateAccumTextures();

        shaderController = shaderControllerTAA;
        shaderController.SetUniform("intensity", intensity);
        shaderController.SetUniform("newTex", 0);
        shaderController.SetUniform("oldTex", 0);
    }
    public override void Execute()
    {
        shaderController.Shader.Use();

        Luminance.BindAsImage(0, TextureAccess.ReadWrite, SizedInternalFormat.Rgba32f);
        luminanceAccum.BindAsImage(1, TextureAccess.ReadWrite, SizedInternalFormat.Rgba32f);

        GL.DispatchCompute(Luminance.Size.X / 8, Luminance.Size.Y / 8, 1);
    }
    void CreateAccumTextures()
    {
        luminanceAccum = new();
        luminanceAccum.Alloc(Luminance.Size, Luminance.Format, nint.Zero);
        luminanceAccum.SetParam(new(TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest));
        luminanceAccum.SetParam(new(TextureParameterName.TextureMinFilter, (int)TextureMagFilter.Nearest));
        luminanceAccum.Lable = "luminance accum texture";
    }
    protected override void Free() => luminanceAccum.Dispose();
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