using OpenTK.Graphics.OpenGL4;

using GLAV.Types;

using DisposableExt;

using Voxand.Engine.Graphics.Meshes.Primitives;
using Voxand.Engine.Graphics.Tools.ShaderServices;
using Voxand.Engine.Graphics.Tools.Exceptions;

namespace Voxand.Engine.Graphics.Pipelines.Modules;
public sealed class AntiAliasingModule : RenderingPipeline
{
    Texture2D luminanceTAA, luminancePT;
    Framebuffer TAAOutputFramebuffer;
    ShaderController TAAShaderController;
    public AntiAliasingModule(ShaderController shaderControllerTAA, Texture2D luminanceInput, Texture2D luminanceOutput)
    {
        if (luminanceInput == luminanceOutput)
            throw new ArgumentException($"{nameof(luminanceInput)} and {nameof(luminanceOutput)} should be distinct textures.");

        ExceptionConstructor.ThrowIfTextureSizeNotEqual(luminanceInput, luminanceOutput);

        luminancePT = luminanceInput;
        luminanceTAA = luminanceOutput;

        TAAShaderController = shaderControllerTAA;
        TAAShaderController.SetUniform("intensity", 0.5f);
        TAAShaderController.SetUniform("tex1", 0);
        TAAShaderController.SetUniform("tex2", 1);

        TAAOutputFramebuffer = new Framebuffer();
        FramebufferAttachmentInfo[] attachments =
            { new(luminanceTAA, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D) };
        TAAOutputFramebuffer.Create(ref attachments);
    }
    public override void Execute()
    {
        TAAOutputFramebuffer.BindFramebuffer(FramebufferTarget.Framebuffer);

        TAAShaderController.Shader.Use();

        luminancePT.BindTex(0);
        luminanceTAA.BindTex(1);

        Primitives.ScreenQuad.Bind();
        GL.DrawArrays(PrimitiveType.Triangles, 0, 6);
    }
    public void SetInputOutput(Texture2D luminanceInput, Texture2D luminanceOutput)
    {
        ExceptionConstructor.ThrowIfTextureSizeNotEqual(luminanceInput, luminanceOutput);

        luminancePT = luminanceInput;
        luminanceTAA = luminanceOutput;
    }
    protected override void Free() => TAAOutputFramebuffer.Dispose();
}