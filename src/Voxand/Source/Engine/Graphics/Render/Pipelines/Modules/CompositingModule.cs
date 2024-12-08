using System.Runtime.CompilerServices;

using OpenTK.Graphics.OpenGL4;

using GLAV.Types;

using Voxand.Engine.Graphics.Meshes.Primitives;
using Voxand.Engine.Graphics.Pipelines.DefaultVoxelPTRP.Helpers;
using Voxand.Engine.Graphics.Tools.ShaderServices;
using Voxand.Engine.Graphics.Tools.Exceptions;
using Voxand.Engine.Graphics.Tools;

namespace Voxand.Engine.Graphics.Pipelines.Modules;
public sealed class CompositingModule : RenderingPipeline
{
    ShaderController compositingShaderController;
    Texture2D luminanceInput, depthInput, normalInput;
    IRenderTarget renderTarget;
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
    public IRenderTarget Output
    {
        get => renderTarget;
        set => renderTarget = value;
    }
    public CompositingModule(ShaderController shaderController, Texture2D luminanceInput,
        Texture2D depthInput, Texture2D normalInput, IRenderTarget output)
    {
        compositingShaderController = shaderController;
        compositingShaderController.SetUniform("luminance", 0);
        compositingShaderController.SetUniform("depth", 1);
        compositingShaderController.SetUniform("normal", 2);

        SetInput(luminanceInput, depthInput, normalInput);
        renderTarget = output;
    }
    public override void Execute()
    {
        renderTarget.Use();
        GL.Clear(ClearBufferMask.ColorBufferBit);

        compositingShaderController.Shader.Use();

        LuminanceInput.BindTex(0);
        DepthInput.BindTex(1);
        NormalInput.BindTex(2);

        Primitives.ScreenQuad.Bind();
        GL.DrawArrays(PrimitiveType.Triangles, 0, 6);
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
    protected override void Free() { }
}