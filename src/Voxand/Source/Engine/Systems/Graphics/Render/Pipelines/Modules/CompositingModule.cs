using System.Runtime.CompilerServices;

using OpenTK.Graphics.OpenGL4;

using GLAV.Types;

using Voxand.Engine.Systems.Graphics.Meshes.Primitives;
using Voxand.Engine.Systems.Graphics.Pipelines.DefaultVoxelPTRP.Helpers;
using Voxand.Engine.Systems.Graphics.Tools.ShaderServices;
using Voxand.Engine.Systems.Graphics.Tools;
using Voxand.Helpers.Exceptions.GLAVExceptions;

namespace Voxand.Engine.Systems.Graphics.Pipelines.Modules;
public sealed class CompositingModule : RenderingPipeline
{
    ShaderController compositingShaderController;
    Texture2D luminanceInput, depth_motionInput, normalInput;
    RenderTarget renderTarget;
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
    public Texture2D Depth_motionInput
    {
        get => depth_motionInput;
        set
        {
            VoxelPTRPHelper.ThrowIfTextureInvalid(value);
            depth_motionInput = value;
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
    public RenderTarget Output
    {
        get => renderTarget;
        set => renderTarget = value;
    }
    public CompositingModule(ShaderController shaderController, Texture2D luminanceInput,
        Texture2D depth_motionInput, Texture2D normalInput, RenderTarget output)
    {
        compositingShaderController = shaderController;
        compositingShaderController.SetUniform("luminance", 0);
        compositingShaderController.SetUniform("depth_motion", 1);
        compositingShaderController.SetUniform("normal", 2);

        SetInput(luminanceInput, depth_motionInput, normalInput);
        renderTarget = output;
    }
    public override void Execute()
    {
        renderTarget.Use();
        GL.Clear(ClearBufferMask.ColorBufferBit);

        compositingShaderController.Shader.Use();

        LuminanceInput.BindTex(0);
        Depth_motionInput.BindTex(1);
        NormalInput.BindTex(2);

        Primitives.ScreenQuad.Bind();
        GL.DrawArrays(PrimitiveType.Triangles, 0, 6);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void ThrowIfTextureSizesNotEqual() =>
        ExceptionConstructor.ThrowIfTextureSizeNotEqual(luminanceInput, depth_motionInput, normalInput);
    public void SetInput(Texture2D luminanceInput, Texture2D depth_motionInput, Texture2D normalInput)
    {
        VoxelPTRPHelper.ThrowIfAnyTextureInvalid(luminanceInput, depth_motionInput, normalInput);
        this.luminanceInput = luminanceInput;
        this.depth_motionInput = depth_motionInput;
        this.normalInput = normalInput;
    }
    protected override void Free() { }
}