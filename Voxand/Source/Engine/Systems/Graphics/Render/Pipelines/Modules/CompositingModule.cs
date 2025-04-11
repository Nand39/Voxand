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
    Texture2D directIllum_depthInput, indirectIllumInput, normalCompound_motionInput;
    RenderTarget renderTarget;
    public Texture2D DirectIllum_depthInput
    {
        get => directIllum_depthInput;
        set
        {
            VoxelPTRPHelper.ThrowIfTextureInvalid(value);
            directIllum_depthInput = value;
            ExceptionConstructor.ThrowIfTextureSizeNotEqual(directIllum_depthInput, indirectIllumInput, normalCompound_motionInput);
        }
    }
    public Texture2D IndirectIllumInput
    {
        get => indirectIllumInput;
        set
        {
            VoxelPTRPHelper.ThrowIfTextureInvalid(value);
            indirectIllumInput = value;
            ExceptionConstructor.ThrowIfTextureSizeNotEqual(directIllum_depthInput, indirectIllumInput, normalCompound_motionInput);
        }
    }
    public Texture2D NormalCompound_motionInput
    {
        get => normalCompound_motionInput;
        set
        {
            VoxelPTRPHelper.ThrowIfTextureInvalid(value);
            normalCompound_motionInput = value;
            ExceptionConstructor.ThrowIfTextureSizeNotEqual(directIllum_depthInput, indirectIllumInput, normalCompound_motionInput);
        }
    }
    public RenderTarget RenderTarget
    {
        get => renderTarget;
        set => renderTarget = value;
    }
    public CompositingModule(ShaderController shaderController, Texture2D directIllum_depthInput, Texture2D indirectIllumInput,
        Texture2D normalCompound_motionInput, RenderTarget output)
    {
        compositingShaderController = shaderController;
        compositingShaderController.SetUniform("directIllum_depthTex", 0);
        compositingShaderController.SetUniform("indirectIllumTex", 2);
        compositingShaderController.SetUniform("normalCompound_motionTex", 1);

        SetInput(directIllum_depthInput, indirectIllumInput, normalCompound_motionInput);
        renderTarget = output;
    }
    public override void Execute()
    {
        renderTarget.Use();
        GL.Clear(ClearBufferMask.ColorBufferBit);

        compositingShaderController.Shader.Use();

        directIllum_depthInput.BindTex(0);
        indirectIllumInput.BindTex(2);
        normalCompound_motionInput.BindTex(1);

        Primitives.ScreenQuadVertexAttribs.Use();
        GL.DrawArrays(PrimitiveType.Triangles, 0, 6);
    }
    public void SetInput(Texture2D directIllum_depthInput, Texture2D indirectIllumInput, Texture2D normalCompound_motionInput)
    {
        VoxelPTRPHelper.ThrowIfAnyTextureInvalid(directIllum_depthInput, indirectIllumInput, normalCompound_motionInput);
        this.directIllum_depthInput = directIllum_depthInput;
        this.indirectIllumInput = indirectIllumInput;
        this.normalCompound_motionInput = normalCompound_motionInput;
    }
    protected override void Free() { }
}