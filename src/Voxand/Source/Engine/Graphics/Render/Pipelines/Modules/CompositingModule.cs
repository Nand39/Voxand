using System.Runtime.CompilerServices;

using OpenTK.Graphics.OpenGL4;

using GLAV.Types;

using DisposableExt;

using Voxand.Engine.Graphics.Meshes.Primitives;
using Voxand.Engine.Graphics.Pipelines.DefaultVoxelPTRP.Helpers;
using Voxand.Engine.Graphics.Tools.ShaderServices;
using Voxand.Engine.Graphics.Tools.Exceptions;
using GLAV.Systems;

namespace Voxand.Engine.Graphics.Pipelines.Modules;
public sealed class CompositingModule : RenderingPipeline
{
    ShaderController compositingShaderController;
    Framebuffer compositingFramebuffer;
    Texture2D luminanceInput, depthInput, normalInput;
    Texture2D compositingOutput;
    public Texture2D LuminanceInput
    {
        get => luminanceInput;
        set
        {
            VoxelPTRPHelper.ThrowIfTextureInvalid(value);
            luminanceInput = value;
            ThrowIfTextureSizesNotEqual();
        }
    }
    public Texture2D DepthInput
    {
        get => depthInput;
        set
        {
            VoxelPTRPHelper.ThrowIfTextureInvalid(value);
            depthInput = value;
            ThrowIfTextureSizesNotEqual();
        }
    }
    public Texture2D NormalInput
    {
        get => normalInput;
        set
        {
            VoxelPTRPHelper.ThrowIfTextureInvalid(value);
            normalInput = value;
            ThrowIfTextureSizesNotEqual();
        }
    }
    public Texture2D CompositingOutput
    {
        get => compositingOutput;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            compositingOutput = value;
            compositingFramebuffer.Attach(new(compositingOutput, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D));
        }
    }
    public CompositingModule(ShaderController shaderController, Texture2D luminanceInput,
        Texture2D depthInput, Texture2D normalInput, Texture2D compositingOutput)
    {
        compositingShaderController = shaderController;
        compositingShaderController.SetUniform("luminance", 0);
        compositingShaderController.SetUniform("depth", 1);
        compositingShaderController.SetUniform("normal", 2);

        SetInput(luminanceInput, depthInput, normalInput);
        ArgumentNullException.ThrowIfNull(compositingOutput);
        this.compositingOutput = compositingOutput;

        compositingFramebuffer = new Framebuffer();
        FramebufferAttachmentInfo[] attachments =
            { new(compositingOutput, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D) };
        compositingFramebuffer.Create(ref attachments);

    }
    public override void Execute()
    {
        GLRegistry.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        GL.Clear(ClearBufferMask.ColorBufferBit);
        //GL.Viewport(0, 0, CompositingOutput.Size.X, CompositingOutput.Size.Y);

        compositingShaderController.Shader.Use();

        LuminanceInput.BindTex(0);
        DepthInput.BindTex(1);
        NormalInput.BindTex(2);

        Primitives.ScreenQuad.Bind();
        GL.DrawArrays(PrimitiveType.Triangles, 0, 6);
        //GL.Viewport(0, 0, Util.ClientSize.X, Util.ClientSize.Y);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void ThrowIfTextureSizesNotEqual() =>
        ExceptionConstructor.ThrowIfTextureSizeNotEqual(luminanceInput, depthInput, normalInput);
    public void SetInput(Texture2D luminanceInput, Texture2D depthInput, Texture2D normalInput)
    {
        VoxelPTRPHelper.ThrowIfAnyTextureInvalid(luminanceInput, depthInput, normalInput);
        this.luminanceInput = luminanceInput;
        this.depthInput = depthInput;
        this.normalInput = normalInput;
    }
    protected override void Free() => compositingFramebuffer.Dispose();
}