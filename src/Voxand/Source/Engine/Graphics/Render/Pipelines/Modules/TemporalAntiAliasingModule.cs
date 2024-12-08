using OpenTK.Graphics.OpenGL4;

using GLAV.Types;

using DisposableExt;

using Voxand.Engine.Graphics.Meshes.Primitives;
using Voxand.Engine.Graphics.Tools.ShaderServices;
using Voxand.Engine.Graphics.Tools.Exceptions;
using Voxand.Engine.Graphics.Tools;

namespace Voxand.Engine.Graphics.Pipelines.Modules;
public sealed class AntiAliasingModule : RenderingPipeline
{
    Texture2D luminanceOutput, luminanceInput;
    RenderTarget renderTarget;
    ShaderController TAAShaderController;
    public Texture2D LuminanceInput
    {
        get => luminanceInput;
        set
        {
            ExceptionConstructor.ThrowIfTextureSizeNotEqual(value, luminanceOutput);
            luminanceInput = value;
        }
    }
    public Texture2D LuminanceOutput
    {
        get => luminanceOutput;
        set
        {
            ExceptionConstructor.ThrowIfTextureSizeNotEqual(value, luminanceInput);
            luminanceOutput = value;
        }
    }
    public AntiAliasingModule(ShaderController shaderControllerTAA, Texture2D luminanceInput, Texture2D luminanceOutput)
    {
        if (luminanceInput == luminanceOutput)
            throw new ArgumentException($"{nameof(luminanceInput)} and {nameof(luminanceOutput)} should be distinct textures.");

        ExceptionConstructor.ThrowIfTextureSizeNotEqual(luminanceInput, luminanceOutput);

        this.luminanceInput = luminanceInput;
        this.luminanceOutput = luminanceOutput;

        FramebufferAttachmentInfo attachment =
            new(LuminanceOutput, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D);
        renderTarget = new(LuminanceOutput.Size, attachment);

        TAAShaderController = shaderControllerTAA;
        TAAShaderController.SetUniform("intensity", 0.5f);
        TAAShaderController.SetUniform("tex1", 0);
        TAAShaderController.SetUniform("tex2", 1);
    }
    public override void Execute()
    {
        renderTarget.Use();
        TAAShaderController.Shader.Use();

        luminanceInput.BindTex(0);
        luminanceOutput.BindTex(1);

        Primitives.ScreenQuad.Bind();
        GL.DrawArrays(PrimitiveType.Triangles, 0, 6);
    }
    public void SetInputOutput(Texture2D luminanceInput, Texture2D luminanceOutput)
    {
        ExceptionConstructor.ThrowIfTextureSizeNotEqual(luminanceInput, luminanceOutput);

        this.luminanceInput = luminanceInput;
        this.luminanceOutput = luminanceOutput;
        FramebufferAttachmentInfo attachment = new(LuminanceOutput, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D);
        //renderTarget.Dispose();
        renderTarget = new(LuminanceOutput.Size, attachment);
    }
    protected override void Free() => renderTarget.Dispose();
}