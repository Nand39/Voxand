using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;

using DisposableExt;
using GLAV.Types;
using GLAV.Systems;

using Voxand.Content;
using Voxand.Engine.Graphics.GLUtil;
using Voxand.Helpers;

using Buffer = GLAV.Types.Buffer;

namespace Voxand.Engine.Graphics;
using Voxand.Engine.Graphics.Tools.ShaderServices;
public class VoxandRendererActive : IDisposable
{
    public ShaderController voxelRenderComputeShader;
    public ShaderController compositeShaderInfo;
    public ShaderController postprocessingShaderInfo;
    public ShaderController TAAShaderInfo;

    VertexArray screenRectVAO;
    public RendererActiveSettings settings;
    Vector2 renderScale;
    public Vector2i renderResolution;
    bool disposed = false;

    Buffer computeInputSSBO;
    Buffer directionalLights;

    public bool agressiveTAA = false;

    float renderResolutionScaler = 0.8f;

    ComputeRenderInputStd140 computeInput;

    RenderTechniques technique;

    Vector2i computeWorkGroupSize = (8, 8);

    public Texture2D skyLightTex;

    public Camera camera;
    public VoxandRendererActive(ContentManager content) : base()
    {
        settings = new();
        InitializeSettings(Vector2i.One);

        compositeShaderInfo = new(content, true,
            "Graphics.Shaders.voxelCompositing.vert",
            "Graphics.Shaders.voxelCompositing.frag");
        compositeShaderInfo.Shader.GLLable = "compositing shader";
        compositeShaderInfo.Shader.Lable = "compositing shader";

        postprocessingShaderInfo = new(content, true,
            "Graphics.Shaders.voxelPostprocessingShader.vert",
            "Graphics.Shaders.voxelPostprocessingShader.frag");
        postprocessingShaderInfo.Shader.GLLable = "postprocessing shader";
        compositeShaderInfo.Shader.Lable = "postprocessing shader";

        TAAShaderInfo = new(content, true,
            "Graphics.Shaders.tempAntiAliasingShader.vert",
            "Graphics.Shaders.tempAntiAliasingShader.frag");
        TAAShaderInfo.Shader.GLLable = "anti-aliasing shader";
        compositeShaderInfo.Shader.Lable = "anti-aliasing shader";

        voxelRenderComputeShader = new(content, true, 
            "Graphics.Shaders.VoxelRender.voxelRenderComputeBrickmap.comp");
        voxelRenderComputeShader.Shader.GLLable = "compute render shader";
        compositeShaderInfo.Shader.Lable = "compute render shader";

        TAAShaderInfo.SetUniform("intensity", 0.4f);

        computeInput = new ComputeRenderInputStd140();

        computeInputSSBO = new Buffer();
        computeInputSSBO.Alloc(BufferTarget.ShaderStorageBuffer, ref computeInput, ComputeRenderInputStd140.sizeInBytes, BufferUsageHint.StreamDraw);
        computeInputSSBO.BindBufferBase(new(BufferRangeTarget.ShaderStorageBuffer, 3));
        computeInputSSBO.GLLable = "compute input";


        V_PositionUV[] viewRectVertices = [
            new(new(-1, -1, 0), new(0, 0)),
            new(new(-1, 1, 0),  new(0, 1)),
            new(new(1, 1, 0),   new(1, 1)),
            new(new(-1, -1, 0), new(0, 0)),
            new(new(1, 1, 0),   new(1, 1)),
            new(new(1, -1, 0),  new(1, 0)),
        ];
        screenRectVAO = new VertexArray();
        screenRectVAO.Alloc(ref viewRectVertices, V_PositionUV.vertexInfo, BufferUsageHint.StaticDraw);

        skyLightTex = content.LoadTexture("Graphics/Textures/env.hdr", false);
    }
    public void Composite()
    {
        // Temporal anti-aliasing
        settings.TAAFramebuffer.BindFramebuffer(FramebufferTarget.Framebuffer);

        TAAShaderInfo.Shader.Use();
        TAAShaderInfo.SetUniform("tex1", 0);
        TAAShaderInfo.SetUniform("tex2", 1);
        settings.VoxelLuminanceTexture.BindTex(0);
        settings.TAALuminanceTexture.BindTex(1);

        if (agressiveTAA)
            TAAShaderInfo.SetUniform("intensity", 0.99f);
        else
            TAAShaderInfo.SetUniform("intensity", 0.4f);


        screenRectVAO.Bind();
        GL.DrawArrays(PrimitiveType.Triangles, 0, 6);

        //Compositing
        settings.CompositingFramebuffer.BindFramebuffer(FramebufferTarget.Framebuffer);
        GL.Clear(ClearBufferMask.ColorBufferBit);

        compositeShaderInfo.Shader.Use();

        compositeShaderInfo.SetUniform("luminance", 0);
        compositeShaderInfo.SetUniform("normal", 1);
        compositeShaderInfo.SetUniform("depth", 2);

        settings.TAALuminanceTexture.BindTex(0);
        settings.VoxelNormalTexture.BindTex(1);
        settings.VoxelDepthTexture.BindTex(2);

        screenRectVAO.Bind();
        GL.DrawArrays(PrimitiveType.Triangles, 0, 6);
    }

    public void Postprocess(Texture2D textureToDisplay)
    {
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        GL.Clear(ClearBufferMask.ColorBufferBit);

        postprocessingShaderInfo.Shader.Use();
        postprocessingShaderInfo.SetUniform("tex", 0);

        textureToDisplay.BindTex(0);

        screenRectVAO.Bind();
        GL.DrawArrays(PrimitiveType.Triangles, 0, 6);
    }

    public unsafe void RenderVoxelsCompute()
    {
        voxelRenderComputeShader.Shader.Use();
        skyLightTex.BindTex(8);
        voxelRenderComputeShader.SetUniform("skyTex", 8);

        fixed (ComputeRenderInputStd140* dataPtr = &computeInput)
        {
            computeInput.cameraPosition = camera.position;
            computeInput.inverseCameraMatrix = Matrix4.Transpose(Matrix4.Invert(camera.CreateCameraMatrix(
                settings.VoxelLuminanceTexture.Size.X / settings.VoxelLuminanceTexture.Size.Y)));

            computeInput.randSalt = Util.Random.NextSingle() + 1;

            computeInputSSBO.Bind();
            computeInputSSBO.Store(ref computeInput, 0);
        }

        GL.DispatchCompute(renderResolution.X / 8, renderResolution.Y / 8, 1);
    }
    public void SetTechnique(RenderTechniques technique)
    {
        this.technique = technique;
        SetRenderResolution(renderResolution, Util.ClientSize);
        Console.WriteLine($"Set to: {technique}");
    }
    public void OnResize(ResizeEventArgs args)
    {
        Vector2i resolution = new Vector2i((int)(args.Size.X * renderResolutionScaler) & -8, (int)(args.Size.Y * renderResolutionScaler) & -8);
        Console.WriteLine("new resolution: " + resolution);
        SetRenderResolution(resolution, args.Size);
    }

    void InitializeSettings(Vector2i resolution)
    {
        Console.WriteLine($"rendering output resolution set to {resolution}");

        TexParam[] parameters;
        TextureFormat format;

        parameters =
            [
            new(TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest),
            new(TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest),
            new(TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge),
            new(TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge),
            ];

        format = new TextureFormat()
        {
            internalFormat = PixelInternalFormat.Rgba32f,
            format = PixelFormat.Rgba,
            pixelType = PixelType.Float
        };

        #region Voxel render textures

        settings.VoxelLuminanceTexture = new Texture2D();
        settings.VoxelLuminanceTexture.Alloc(resolution, format, nint.Zero);
        settings.VoxelLuminanceTexture.SetParams(ref parameters);
        settings.VoxelLuminanceTexture.BindTex(0);

        format.internalFormat = PixelInternalFormat.R32f;
        format.format = PixelFormat.Red;

        settings.VoxelDepthTexture = new Texture2D();
        settings.VoxelDepthTexture.Alloc(resolution, format, nint.Zero);
        settings.VoxelDepthTexture.SetParams(ref parameters);
        settings.VoxelDepthTexture.BindTex(1);

        format.internalFormat = PixelInternalFormat.R32i;
        format.format = PixelFormat.RedInteger;
        format.pixelType = PixelType.Int;

        settings.VoxelNormalTexture = new Texture2D();
        settings.VoxelNormalTexture.Alloc(resolution, format, nint.Zero);
        settings.VoxelNormalTexture.SetParams(ref parameters);
        settings.VoxelNormalTexture.BindTex(2);

        // Bind new textures for compute shaders
        settings.VoxelLuminanceTexture.BindAsImage(0, TextureAccess.WriteOnly, SizedInternalFormat.Rgba32f);
        settings.VoxelDepthTexture.BindAsImage(1, TextureAccess.WriteOnly, SizedInternalFormat.R32f);
        settings.VoxelNormalTexture.BindAsImage(2, TextureAccess.WriteOnly, SizedInternalFormat.R32i);

        #endregion

        #region Compositing

        // TAA texture

        format.internalFormat = PixelInternalFormat.Rgb32f;
        format.format = PixelFormat.Rgb;
        format.pixelType = PixelType.Float;

        settings.TAALuminanceTexture = new Texture2D();
        settings.TAALuminanceTexture.Alloc(resolution, format, nint.Zero);
        settings.TAALuminanceTexture.SetParams(ref parameters);

        // Compositing result

        parameters =
            [
            new(TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear),
            new(TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest),
            ];

        format.internalFormat = PixelInternalFormat.Rgba32f;
        format.format = PixelFormat.Rgba;

        settings.CompositingResultTexture = new Texture2D();
        settings.CompositingResultTexture.Alloc(resolution, format, nint.Zero);
        settings.CompositingResultTexture.SetParams(ref parameters);

        #endregion

        #region Framebuffers

        FramebufferAttachmentInfo[] attachments;

        // TAA framebuffer
        attachments =
            [new(settings.TAALuminanceTexture, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D)];

        settings.TAAFramebuffer = new Framebuffer();
        settings.TAAFramebuffer.Create(ref attachments);

        // Compositing framebuffer
        attachments =
            [new(settings.CompositingResultTexture, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D)];

        settings.CompositingFramebuffer = new Framebuffer();
        settings.CompositingFramebuffer.Create(ref attachments);

        #endregion
    }
    public bool SetRenderResolution(Vector2i newResolution, Vector2i screenResolution)
    {
        // Rendering with compute shaders only support resolution that is multiple of 8 on both dimensions.
        if ((newResolution.X % computeWorkGroupSize.X) > 0 || (newResolution.Y % computeWorkGroupSize.Y) > 0) 
            return false;

        renderResolution = newResolution;
        renderScale = (Vector2)newResolution / screenResolution;

        // Disposing old framebuffers and corresponding texture attachments


        settings.VoxelLuminanceTexture.Dispose();
        settings.VoxelDepthTexture.Dispose();
        settings.VoxelNormalTexture.Dispose();

        settings.TAALuminanceTexture.Dispose();
        settings.TAAFramebuffer.Dispose();

        settings.CompositingResultTexture.Dispose();
        settings.CompositingFramebuffer.Dispose();

        // Creating new texture attachments

        InitializeSettings(newResolution);

        // Resend render scale uniforms

        TAAShaderInfo.SetUniform("renderScale", renderScale);
        //GL.Uniform2(TAAShaderInfo.GetUniformLocation(TAAShaderUniforms.renderScale), renderScale);
        compositeShaderInfo.SetUniform("renderScale", renderScale);
        //GL.Uniform2(compositeShaderInfo.GetUniformLocation(CompositeShaderUniforms.), renderScale);
        
        GLRegistry.UseProgram(0);

        return true;
    }

    public void SetMapSize(Vector3i size)
    {
        voxelRenderComputeShader.SetUniform("mapSize", size);
        voxelRenderComputeShader.SetUniform("voxelBrickmapSize", new Vector3i(size.X >> 2, size.Y >> 2, size.Z >> 2));
    }

    public void Dispose()
    {
        if (disposed) return;

        GC.SuppressFinalize(this);

        Console.WriteLine("Renderer was successfully disposed");
    }

    ~VoxandRendererActive()
    {
        Dispose();
    }
}