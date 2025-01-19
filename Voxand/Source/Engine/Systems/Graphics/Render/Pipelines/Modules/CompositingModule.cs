using System.Runtime.CompilerServices;

using OpenTK.Graphics.OpenGL4;

using GLAV.Types;
using GLAV.Helpers.Public.Exceptions;

using Voxand.Engine.Systems.Graphics.Meshes.Primitives;
using Voxand.Engine.Systems.Graphics.Pipelines.DefaultVoxelPTRP.Helpers;
using Voxand.Engine.Systems.Graphics.Tools.ShaderServices;
using Voxand.Engine.Systems.Graphics.Tools;

namespace Voxand.Engine.Systems.Graphics.Pipelines.Modules;
public sealed class CompositingModule : RenderingPipeline
{
    ShaderController compositingShaderController;
    Texture2D luminance_depthInput, normalCompound_motionInput;
    RenderTarget renderTarget;
    public Texture2D Luminance_depthInput
    {
        get => luminance_depthInput;
        set
        {
            VoxelPTRPHelper.ThrowIfTextureInvalid(value);
            luminance_depthInput = value;
            ExceptionConstructor.ThrowIfTextureSizeNotEqual(luminance_depthInput, normalCompound_motionInput);
        }
    }
    public Texture2D NormalCompound_motionInput
    {
        get => normalCompound_motionInput;
        set
        {
            VoxelPTRPHelper.ThrowIfTextureInvalid(value);
            normalCompound_motionInput = value;
            ExceptionConstructor.ThrowIfTextureSizeNotEqual(luminance_depthInput, normalCompound_motionInput);
        }
    }
    public RenderTarget RenderTarget
    {
        get => renderTarget;
        set => renderTarget = value;
    }
    public CompositingModule(ShaderController shaderController, Texture2D luminance_depthInput,
        Texture2D normalCompound_motionInput, RenderTarget output)
    {
        compositingShaderController = shaderController;
        compositingShaderController.SetUniform("luminance_depthTex", 0);
        compositingShaderController.SetUniform("normalCompound_motionTex", 1);

        SetInput(luminance_depthInput, normalCompound_motionInput);
        renderTarget = output;
    }
    public override void Execute()
    {
        renderTarget.Use();
        GL.Clear(ClearBufferMask.ColorBufferBit);

        compositingShaderController.Shader.Use();

        luminance_depthInput.BindTex(0);
        normalCompound_motionInput.BindTex(1);

        Primitives.ScreenQuad.Bind();
        GL.DrawArrays(PrimitiveType.Triangles, 0, 6);
    }
    public void SetInput(Texture2D luminance_depthInput, Texture2D normalCompound_motionInput)
    {
        VoxelPTRPHelper.ThrowIfAnyTextureInvalid(luminance_depthInput, normalCompound_motionInput);
        this.luminance_depthInput = luminance_depthInput;
        this.normalCompound_motionInput = normalCompound_motionInput;
    }
    protected override void Free() { }
}