using OpenTK.Graphics.OpenGL4;

using GLAV.Types;

using Voxand.Engine.Graphics.Meshes.Primitives;
using Voxand.Engine.Graphics.Pipelines.DefaultVoxelPTRP.Helpers;
using Voxand.Engine.Graphics.Tools.ShaderServices;
using Voxand.Helpers;
using OpenTK.Mathematics;
using GLAV.Systems;

namespace Voxand.Engine.Graphics.Pipelines.Modules;

public sealed class PostprocessingModule : RenderingPipeline
{
    ShaderController shaderController;
    Texture2D textureInput;
    Vector2i outputResolution;
    public Vector2i OutputResolution
    {
        get => outputResolution;
        set => outputResolution = value;
    }
    Texture2D TextureInput
    {
        get => textureInput;
        set
        {
            VoxelPTRPHelper.ThrowIfTextureInvalid(value);
            textureInput = value;
        }
    }
    public PostprocessingModule(ShaderController shaderController, Texture2D textureInput, Vector2i outputResolution)
    {
        this.shaderController = shaderController;
        TextureInput = textureInput;
        OutputResolution = outputResolution;
    }
    public override void Execute()
    {
        GLRegistry.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        GL.Clear(ClearBufferMask.ColorBufferBit);
        GL.Viewport(0, 0, OutputResolution.X, OutputResolution.Y);

        shaderController.Shader.Use();
        shaderController.SetUniform("tex", 0);

        TextureInput.BindTex(0);

        Primitives.ScreenQuad.Bind();
        GL.DrawArrays(PrimitiveType.Triangles, 0, 6);
    }

    protected override void Free() { }
}