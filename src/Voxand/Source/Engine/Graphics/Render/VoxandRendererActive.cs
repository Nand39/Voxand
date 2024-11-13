using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;

using GLAV.Types;
using GLAV.Systems;
using GLAV.Helpers;
using DisposableExt;

using Voxand.Content;
using Voxand.Engine.Graphics.GLUtil;
using Voxand.Helpers;
using Voxand.Helpers.ExtensionMethods.GLAVExtensions;

using Buffer = GLAV.Types.Buffer;
using GLAVUtils = GLAV.Helpers.Util.Util;
namespace Voxand.Engine.Graphics;
public class VoxandRendererActive : IDisposable
{
    public ShaderInfo<VoxelShaderUniforms> voxelShaderInfo;
    public ShaderInfo<CompositeShaderUniforms> compositeShaderInfo;
    public ShaderInfo<PostprocessingShaderUniforms> postprocessingShaderInfo;
    public ShaderInfo<TAAShaderUniforms> TAAShaderInfo;
    public ShaderInfo<FlatShaderUniforms> flatShaderInfo;

    VertexArray screenRectVAO;
    public RendererActiveSettings settings;
    DrawBuffersEnum[] voxelRenderAttachments = 
        [
        DrawBuffersEnum.ColorAttachment0,
        DrawBuffersEnum.ColorAttachment1,
        DrawBuffersEnum.ColorAttachment2,
        DrawBuffersEnum.ColorAttachment3
        ];
    Vector2 renderScale;
    public Vector2i renderResolution;
    bool disposed = false;

    Shader voxelRenderComputeShader;
    Buffer computeInputSSBO;
    Buffer directionalLights;

    public bool agressiveTAA = false;

    float renderResolutionScaler = 0.5f;

    ComputeRenderInputStd140 computeInput;

    RenderTechniques technique;

    Vector2i computeWorkGroupSize = (8, 8);

    Texture2D skyboxTex;
    public VoxandRendererActive(ContentManager content) : base()
    {
        settings = new();
        InitializeSettings(Vector2i.One);

        Shader voxelRenderShader = new Shader();
        voxelRenderShader.Create(content, true, 
            "Graphics.Shaders.VoxelRender.voxelRender.vert",
            "Graphics.Shaders.VoxelRender.voxelRender.frag");

        Shader compositingShader = new Shader();
        compositingShader.Create(content, true,
            "Graphics.Shaders.voxelCompositing.vert",
            "Graphics.Shaders.voxelCompositing.frag");

        Shader postprocessingShader = new Shader();
        postprocessingShader.Create(content, true,
            "Graphics.Shaders.voxelPostprocessingShader.vert",
            "Graphics.Shaders.voxelPostprocessingShader.frag");

        Shader tempAntiAliasingShader = new Shader();
        tempAntiAliasingShader.Create(content, true,
            "Graphics.Shaders.tempAntiAliasingShader.vert",
            "Graphics.Shaders.tempAntiAliasingShader.frag");

        Shader flatRenderShader = new Shader();
        flatRenderShader.Create(content, true,
            "Graphics.Shaders.VoxelRender.voxelRender.vert",
            "Graphics.Shaders.VoxelRender.voxelRenderFlat.frag");

        voxelShaderInfo = new(voxelRenderShader);
        compositeShaderInfo = new(compositingShader);
        postprocessingShaderInfo = new(postprocessingShader);
        TAAShaderInfo = new(tempAntiAliasingShader);
        flatShaderInfo = new(flatRenderShader);

        voxelShaderInfo.SetUniform(VoxelShaderUniforms.ambientLighting, new Vector3(0.6f, 0.8f, 0.9f));
        TAAShaderInfo.SetUniform(TAAShaderUniforms.intensity, 0.36f);

        voxelRenderComputeShader = new Shader();
        voxelRenderComputeShader.Create(content, true, "Graphics.Shaders.VoxelRender.voxelRenderComputeBrickmap.comp");
        computeInput = new ComputeRenderInputStd140()
        {
            ambientLighting = new Vector3(0.6f, 0.8f, 0.9f),
        };
        computeInputSSBO = new Buffer();
        computeInputSSBO.Alloc(BufferTarget.ShaderStorageBuffer, ref computeInput, ComputeRenderInputStd140.sizeInBytes, BufferUsageHint.StreamDraw);
        computeInputSSBO.BindBufferBase(new(BufferRangeTarget.ShaderStorageBuffer, 3));
        GLAVUtils.LabelResource(computeInputSSBO.Handle, "compute input");
        

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
    }
    public void RenderVoxels(float deltaTime)
    {
        switch(technique)
        {
            case RenderTechniques.PathTracingFragment: RenderVoxelsFragment(); break;
            case RenderTechniques.PathTracingCompute: RenderVoxelsCompute(deltaTime); break;
        }
    }
    void RenderVoxelsFragment()
    {
        settings.VoxelRenderFramebuffer.BindFramebuffer(FramebufferTarget.Framebuffer);
        GL.Clear(ClearBufferMask.ColorBufferBit);

        voxelShaderInfo.Shader.Use();

        Matrix4 cameraMat = Matrix4.Invert(Camera.CreateCameraMatrix());
        GL.Uniform3(voxelShaderInfo.GetUniformLocation(VoxelShaderUniforms.cameraPosition), Camera.position);
        GL.Uniform1(voxelShaderInfo.GetUniformLocation(VoxelShaderUniforms.randSalt), Util.Random.NextSingle() + 1);
        GL.UniformMatrix4(voxelShaderInfo.GetUniformLocation(VoxelShaderUniforms.inverseCameraMatrix), true, ref cameraMat);

        GL.DrawBuffers(voxelRenderAttachments.Length, voxelRenderAttachments);

        screenRectVAO.Bind();
        GL.DrawArrays(PrimitiveType.Triangles, 0, 6);
    }
    public void Composite()
    {
        // Temporal anti-aliasing
        settings.TAAFramebuffer.BindFramebuffer(FramebufferTarget.Framebuffer);

        TAAShaderInfo.Shader.Use();
        GL.Uniform1(TAAShaderInfo.GetUniformLocation(TAAShaderUniforms.tex1), 0);
        GL.Uniform1(TAAShaderInfo.GetUniformLocation(TAAShaderUniforms.tex2), 1);
        settings.VoxelLuminanceTexture.BindToUnit(TextureUnit.Texture0);
        settings.TAALuminanceTexture.BindToUnit(TextureUnit.Texture1);
        if (agressiveTAA) 
            GL.Uniform1(TAAShaderInfo.GetUniformLocation(TAAShaderUniforms.intensity), 0.95f);
        else 
            GL.Uniform1(TAAShaderInfo.GetUniformLocation(TAAShaderUniforms.intensity), 0.4f);


        screenRectVAO.Bind();
        GL.DrawArrays(PrimitiveType.Triangles, 0, 6);

        //Compositing
        settings.CompositingFramebuffer.BindFramebuffer(FramebufferTarget.Framebuffer);
        GL.Clear(ClearBufferMask.ColorBufferBit);

        compositeShaderInfo.Shader.Use();

        GL.Uniform1(compositeShaderInfo.GetUniformLocation(CompositeShaderUniforms.albedo), 0);
        GL.Uniform1(compositeShaderInfo.GetUniformLocation(CompositeShaderUniforms.luminance), 1);
        GL.Uniform1(compositeShaderInfo.GetUniformLocation(CompositeShaderUniforms.normal), 2);
        GL.Uniform1(compositeShaderInfo.GetUniformLocation(CompositeShaderUniforms.depth), 3);
        settings.VoxelAlbedoTexture.BindToUnit(TextureUnit.Texture0);
        settings.TAALuminanceTexture.BindToUnit(TextureUnit.Texture1);
        settings.VoxelNormalTexture.BindToUnit(TextureUnit.Texture2);
        settings.VoxelDepthTexture.BindToUnit(TextureUnit.Texture3);

        screenRectVAO.Bind();
        GL.DrawArrays(PrimitiveType.Triangles, 0, 6);
    }

    public void Postprocess(Texture2D textureToDisplay)
    {
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        GL.Clear(ClearBufferMask.ColorBufferBit);

        postprocessingShaderInfo.Shader.Use();
        GL.Uniform1(postprocessingShaderInfo.GetUniformLocation(PostprocessingShaderUniforms.tex), 0);
        textureToDisplay.BindToUnit(TextureUnit.Texture0);

        screenRectVAO.Bind();
        GL.DrawArrays(PrimitiveType.Triangles, 0, 6);
    }
    public void RenderFlat()
    {
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        GL.Clear(ClearBufferMask.ColorBufferBit);

        flatShaderInfo.Shader.Use();

        Matrix4 cameraMat = Matrix4.Invert(Camera.CreateCameraMatrix());
        GL.Uniform3(flatShaderInfo.GetUniformLocation(FlatShaderUniforms.cameraPosition), Camera.position);
        GL.UniformMatrix4(flatShaderInfo.GetUniformLocation(FlatShaderUniforms.inverseCameraMatrix), true, ref cameraMat);

        screenRectVAO.Bind();
        GL.DrawArrays(PrimitiveType.Triangles, 0, 6);
    }

    unsafe void RenderVoxelsCompute(float deltaTime)
    {
        voxelRenderComputeShader.Use();

        fixed (ComputeRenderInputStd140* dataPtr = &computeInput)
        {
            computeInput.cameraPosition = Camera.position;
            computeInput.inverseCameraMatrix = Matrix4.Transpose(Matrix4.Invert(Camera.CreateCameraMatrix()));
            computeInput.randSalt = Util.Random.NextSingle() + 1;
            computeInput.time += deltaTime;
            //computeInput.time %= MathF.Tau;

            computeInputSSBO.Bind();
            computeInputSSBO.Store(ref computeInput, 0);
        }

        GL.DispatchCompute(renderResolution.X / 8, renderResolution.Y / 8, 1);
    }
    public enum VoxelShaderUniforms
    {
        mapSize,
        cameraPosition,
        inverseCameraMatrix,
        renderScale,
        randSalt,
        ambientLighting,
    }
    public enum TAAShaderUniforms
    {
        tex1,
        tex2,
        intensity,
        renderScale,
    }
    public enum CompositeShaderUniforms
    {
        albedo,
        luminance,
        normal,
        depth,
        renderScale,
    }
    public enum PostprocessingShaderUniforms
    {
        tex
    }
    public enum FlatShaderUniforms
    {
        mapSize,
        cameraPosition,
        inverseCameraMatrix,
        renderScale,
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

        settings.VoxelAlbedoTexture = new Texture2D();
        settings.VoxelAlbedoTexture.Alloc(resolution, format, nint.Zero);
        settings.VoxelAlbedoTexture.SetParams(ref parameters);
        settings.VoxelAlbedoTexture.BindToUnit(TextureUnit.Texture0);

        settings.VoxelLuminanceTexture = new Texture2D();
        settings.VoxelLuminanceTexture.Alloc(resolution, format, nint.Zero);
        settings.VoxelLuminanceTexture.SetParams(ref parameters);
        settings.VoxelLuminanceTexture.BindToUnit(TextureUnit.Texture1);

        format.internalFormat = PixelInternalFormat.R32f;
        format.format = PixelFormat.Red;

        settings.VoxelDepthTexture = new Texture2D();
        settings.VoxelDepthTexture.Alloc(resolution, format, nint.Zero);
        settings.VoxelDepthTexture.SetParams(ref parameters);
        settings.VoxelDepthTexture.BindToUnit(TextureUnit.Texture2);

        format.internalFormat = PixelInternalFormat.R32i;
        format.format = PixelFormat.RedInteger;
        format.pixelType = PixelType.Int;

        settings.VoxelNormalTexture = new Texture2D();
        settings.VoxelNormalTexture.Alloc(resolution, format, nint.Zero);
        settings.VoxelNormalTexture.SetParams(ref parameters);
        settings.VoxelNormalTexture.BindToUnit(TextureUnit.Texture3);

        // Bind new textures for compute shaders
        settings.VoxelAlbedoTexture.BindAsImage(0, TextureAccess.WriteOnly, SizedInternalFormat.Rgba32f);
        settings.VoxelLuminanceTexture.BindAsImage(1, TextureAccess.WriteOnly, SizedInternalFormat.Rgba32f);
        settings.VoxelDepthTexture.BindAsImage(2, TextureAccess.WriteOnly, SizedInternalFormat.R32f);
        settings.VoxelNormalTexture.BindAsImage(3, TextureAccess.WriteOnly, SizedInternalFormat.R32i);

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

        // Path tracing framebuffer
        attachments =
            [
            new(settings.VoxelAlbedoTexture, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D),
            new(settings.VoxelLuminanceTexture, FramebufferAttachment.ColorAttachment1, TextureTarget.Texture2D),
            new(settings.VoxelDepthTexture, FramebufferAttachment.ColorAttachment2, TextureTarget.Texture2D),
            new(settings.VoxelNormalTexture, FramebufferAttachment.ColorAttachment3, TextureTarget.Texture2D),
            ];

        settings.VoxelRenderFramebuffer = new Framebuffer();
        settings.VoxelRenderFramebuffer.Create(ref attachments);

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


        settings.VoxelAlbedoTexture.Dispose();
        settings.VoxelLuminanceTexture.Dispose();
        settings.VoxelDepthTexture.Dispose();
        settings.VoxelNormalTexture.Dispose();
        settings.VoxelRenderFramebuffer.Dispose();

        settings.TAALuminanceTexture.Dispose();
        settings.TAAFramebuffer.Dispose();

        settings.CompositingResultTexture.Dispose();
        settings.CompositingFramebuffer.Dispose();

        // Creating new texture attachments

        InitializeSettings(newResolution);

        // Resend render scale uniforms


        voxelShaderInfo.Shader.Use();
        GL.Uniform2(voxelShaderInfo.GetUniformLocation(VoxelShaderUniforms.renderScale), renderScale);
        flatShaderInfo.Shader.Use();
        GL.Uniform2(flatShaderInfo.GetUniformLocation(FlatShaderUniforms.renderScale), renderScale);
        TAAShaderInfo.Shader.Use();
        GL.Uniform2(TAAShaderInfo.GetUniformLocation(TAAShaderUniforms.renderScale), renderScale);
        compositeShaderInfo.Shader.Use();
        GL.Uniform2(compositeShaderInfo.GetUniformLocation(CompositeShaderUniforms.renderScale), renderScale);
        GLRegistry.UseProgram(0);

        return true;
    }

    public void SetMapSize(Vector3i size)
    {
        voxelShaderInfo.Shader.Use();
        GL.Uniform3(voxelShaderInfo.GetUniformLocation(VoxelShaderUniforms.mapSize), size);
        flatShaderInfo.Shader.Use();
        GL.Uniform3(flatShaderInfo.GetUniformLocation(FlatShaderUniforms.mapSize), size);

        computeInput.mapSize = size;
        computeInput.voxelBrickmapSize = new Vector3i(size.X >> 2, size.Y >> 2, size.Z >> 2);
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