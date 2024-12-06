using System.Runtime.InteropServices;

using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

using DisposableExt;

using GLAV.Types;

using Voxand.Engine.Graphics.Tools.ShaderServices;
using Voxand.Helpers;
using Voxand.Content;
using Voxand.Engine.Graphics.Tools.Exceptions;
using Voxand.Engine.Graphics.Meshes.Primitives;

using Buffer = GLAV.Types.Buffer;
using System.Runtime.CompilerServices;
using Voxand.Engine.Systems.Voxels;
using Voxand.Engine.Graphics.Pipelines.DefaultVoxelPTRP.Helpers;

namespace Voxand.Engine.Graphics.Pipelines.DefaultVoxelPTRP;

public sealed class VoxelPTRP : RenderingPipeline
{
    Camera Camera;
    Texture2D luminancePT, depthPT, normalPT, luminanceTAA, compositingResult;
    ShaderController pathTracingShaderController, TAAShaderController, compositingShaderController, postprocessingShaderController;

    DisposalList lifetimeResources;
    // Stores textures and framebuffers to carry data between pipeline stages and that need to be reallocated
    // each time path tracing resolution changes
    DisposalList varyingRenderDataStorage;

    Vector2i renderResolution;

    VoxelPathTracingModule voxelPathTracer;
    AntiAliasingModule antiAliasingModule;
    CompositingModule compositingModule;
    PostprocessingModule postprocessingModule;
    public Vector2i PathTracingResolution
    {
        get => renderResolution;
        set => renderResolution = value;
    }
    public VoxelPTRP(ContentManager content, Camera camera, VoxelMap map, Vector2i pathTracingResolution)
    {
        lifetimeResources = new(); varyingRenderDataStorage = new();

        Camera = camera;
        PathTracingResolution = pathTracingResolution;

        CreateVaryingRenderDataStorage(pathTracingResolution);

        varyingRenderDataStorage.Add(luminancePT,
                                     depthPT,
                                     normalPT,
                                     luminanceTAA,
                                     compositingResult);

        pathTracingShaderController = new(content, true,
            "Graphics.Shaders.VoxelRender.voxelRenderComputeBrickmap.comp");
        pathTracingShaderController.Shader.GLLable = "compute render shader";
        pathTracingShaderController.Shader.Lable = "compute render shader";

        TAAShaderController = new(content, true,
            "Graphics.Shaders.tempAntiAliasingShader.vert",
            "Graphics.Shaders.tempAntiAliasingShader.frag");
        TAAShaderController.Shader.GLLable = "anti-aliasing shader";
        TAAShaderController.Shader.Lable = "anti-aliasing shader";
        TAAShaderController.SetUniform("tex1", 0);
        TAAShaderController.SetUniform("tex2", 1);

        compositingShaderController = new(content, true,
            "Graphics.Shaders.voxelCompositing.vert",
            "Graphics.Shaders.voxelCompositing.frag");
        compositingShaderController.Shader.GLLable = "compositing shader";
        compositingShaderController.Shader.Lable = "compositing shader";

        postprocessingShaderController = new(content, true,
            "Graphics.Shaders.voxelPostprocessingShader.vert",
            "Graphics.Shaders.voxelPostprocessingShader.frag");
        postprocessingShaderController.Shader.GLLable = "postprocessing shader";
        postprocessingShaderController.Shader.Lable = "postprocessing shader";

        voxelPathTracer = new VoxelPathTracingModule(
            shaderControllerPT: pathTracingShaderController,
            camera: Camera,
            mapSize: map.Dimensions,
            luminanceOutput: luminancePT,
            depthOutput: depthPT,
            normalOutput: normalPT);

        antiAliasingModule = new(TAAShaderController, luminancePT, luminanceTAA);

        compositingModule = new(compositingShaderController, luminanceTAA, depthPT, normalPT, compositingResult);

        postprocessingModule = new(postprocessingShaderController, compositingResult);

        lifetimeResources.Add(
            pathTracingShaderController,
            TAAShaderController,
            compositingShaderController,
            postprocessingShaderController,
            voxelPathTracer,
            antiAliasingModule);
    }
    public override void Execute()
    {
        ExecutePathTracing();
        ExecuteTemporalAntiAliasing();
        ExecuteCompositing();a // this stage seems to execute inaccurately
        ExecutePostprocessing();
    }
    void ExecutePathTracing()
    {
        voxelPathTracer.Execute();
    }
    void ExecuteTemporalAntiAliasing()
    {
        antiAliasingModule.Execute();
    }
    void ExecuteCompositing()
    {
        compositingModule.Execute();
    }
    void ExecutePostprocessing()
    {
        postprocessingModule.Execute();
    }

    /// <summary>
    /// Sets target resolution to render the scene in. Disposes and reallocates textures and framebuffers 
    /// to match <paramref name="resolution"/>.
    /// </summary>
    public void SetRenderingResolution(Vector2i resolution)
    {
        varyingRenderDataStorage.Free();

        CreateVaryingRenderDataStorage(resolution);

        varyingRenderDataStorage.Add(
        luminancePT,
        depthPT,
        normalPT,
        luminanceTAA,
        compositingResult);

        voxelPathTracer.SetOutput(luminancePT, depthPT, normalPT);
        antiAliasingModule.SetInputOutput(luminancePT, luminanceTAA);
        compositingModule.SetInput(luminanceTAA, depthPT, normalPT);
        compositingModule.CompositingOutput = compositingResult;
    }
    void CreateVaryingRenderDataStorage(Vector2i resolution)
    {
        Console.WriteLine($"Rendering output resolution set to {resolution}");

        TexParam[] texParams;
        TextureFormat format;

        texParams = TexParam.defaultTexParams;

        format = new TextureFormat()
        {
            internalFormat = PixelInternalFormat.Rgba32f,
            format = PixelFormat.Rgba,
            pixelType = PixelType.Float
        };

        #region PT

        luminancePT = new Texture2D();
        luminancePT.Alloc(resolution, format, nint.Zero);
        luminancePT.SetParams(ref texParams);
        luminancePT.BindTex(0);

        format.internalFormat = PixelInternalFormat.R32f;
        format.format = PixelFormat.Red;

        depthPT = new Texture2D();
        depthPT.Alloc(resolution, format, nint.Zero);
        depthPT.SetParams(ref texParams);
        depthPT.BindTex(1);

        format.internalFormat = PixelInternalFormat.R32i;
        format.format = PixelFormat.RedInteger;
        format.pixelType = PixelType.Int;

        normalPT = new Texture2D();
        normalPT.Alloc(resolution, format, nint.Zero);
        normalPT.SetParams(ref texParams);
        normalPT.BindTex(2);

        // Bind new textures as images for compute shaders
        luminancePT.BindAsImage(0, TextureAccess.WriteOnly, SizedInternalFormat.Rgba32f);
        depthPT.BindAsImage(1, TextureAccess.WriteOnly, SizedInternalFormat.R32f);
        normalPT.BindAsImage(2, TextureAccess.WriteOnly, SizedInternalFormat.R32i);

        #endregion

        #region TAA

        format.internalFormat = PixelInternalFormat.Rgb32f;
        format.format = PixelFormat.Rgb;
        format.pixelType = PixelType.Float;

        luminanceTAA = new Texture2D();
        luminanceTAA.Alloc(resolution, format, nint.Zero);
        luminanceTAA.SetParams(ref texParams);

        #endregion

        #region Compositing

        texParams =
            [
            new(TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear),
        new(TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest),
        ];

        format.internalFormat = PixelInternalFormat.Rgba32f;
        format.format = PixelFormat.Rgba;

        compositingResult = new Texture2D();
        compositingResult.Alloc(resolution, format, nint.Zero);
        compositingResult.SetParams(ref texParams);

        #endregion
    }
    protected override void Free()
    {
        voxelPathTracer.Dispose();
        antiAliasingModule.Dispose();
        compositingModule.Dispose();
        postprocessingModule.Dispose();
        lifetimeResources.Dispose();
        varyingRenderDataStorage.Dispose();
        GC.SuppressFinalize(this);
    }
}
public sealed class VoxelPathTracingModule : RenderingPipeline
{
    ShaderController pathTracingShaderController;
    Texture2D luminanceOutput, depthOutput, normalOutput;
    public Texture2D LuminanceOutput
    {
        get => luminanceOutput;
        set
        {
            VoxelPTRPHelper.ThrowIfTextureInvalid(value);
            luminanceOutput = value;
            ThrowIfTextureSizesNotEqual();
        }
    }
    public Texture2D DepthOutput
    {
        get => depthOutput;
        set
        {
            VoxelPTRPHelper.ThrowIfTextureInvalid(value);
            depthOutput = value;
            ThrowIfTextureSizesNotEqual();
        }
    }
    public Texture2D NormalOutput
    {
        get => normalOutput;
        set
        {
            VoxelPTRPHelper.ThrowIfTextureInvalid(value);
            normalOutput = value;
            ThrowIfTextureSizesNotEqual();
        }
    }

    Buffer shaderInputSSBO;
    ShaderInputStreaming shaderInputStreaming = new();

    [StructLayout(LayoutKind.Sequential)]
    struct ShaderInputStreaming
    {
        public ShaderInputStreaming() { }

        public Matrix4 inverseCameraMatrix = Matrix4.Identity;
        public Vector3 cameraPosition = default;
        public float randSalt = 0;
    }
    public Camera Camera { get; set; }
    public VoxelPathTracingModule(ShaderController shaderControllerPT, Camera camera, Vector3i mapSize, Texture2D luminanceOutput, Texture2D depthOutput, Texture2D normalOutput)
    {
        pathTracingShaderController = shaderControllerPT;
        pathTracingShaderController.SetUniform("luminanceTexture", 0);
        pathTracingShaderController.SetUniform("depthTexture", 1);
        pathTracingShaderController.SetUniform("normalTexture", 2);
        Camera = camera;
        shaderInputSSBO = new();
        unsafe
        {
            shaderInputSSBO.Alloc(BufferTarget.ShaderStorageBuffer, sizeof(ShaderInputStreaming), BufferUsageHint.StreamDraw);
        }

        SetOutput(luminanceOutput, depthOutput, normalOutput);
        OnMapSizeChanged(mapSize);
    }
    public override void Execute()
    {
        StreamShaderInput();

        pathTracingShaderController.Shader.Use();

        LuminanceOutput.BindAsImage(0, TextureAccess.WriteOnly, SizedInternalFormat.Rgba32f);
        DepthOutput.BindAsImage(1, TextureAccess.WriteOnly, SizedInternalFormat.R32f);
        NormalOutput.BindAsImage(2, TextureAccess.WriteOnly, SizedInternalFormat.R32i);
        GL.DispatchCompute(LuminanceOutput.Size.X / 8, LuminanceOutput.Size.Y / 8, 1);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] void ThrowIfTextureSizesNotEqual() =>
        ExceptionConstructor.ThrowIfTextureSizeNotEqual(luminanceOutput, depthOutput, normalOutput);
    public void SetOutput(Texture2D luminanceOutput, Texture2D depthOutput, Texture2D normalOutput)
    {
        ExceptionConstructor.ThrowIfTextureSizeNotEqual(luminanceOutput, depthOutput, normalOutput);
        VoxelPTRPHelper.ThrowIfAnyTextureInvalid(luminanceOutput, depthOutput, normalOutput);
        this.luminanceOutput = luminanceOutput;
        this.depthOutput = depthOutput;
        this.normalOutput = normalOutput;
    }
    public void OnMapSizeChanged(Vector3i newSize)
    {
        pathTracingShaderController.SetUniform("mapSize", newSize);
        Vector3i brickmapSize = new Vector3i(newSize.X >> 2, newSize.Y >> 2, newSize.Z >> 2);
        pathTracingShaderController.SetUniform("voxelBrickmapSize", brickmapSize);
    }
    void StreamShaderInput()
    {
        shaderInputStreaming.cameraPosition = Camera.position;
        shaderInputStreaming.inverseCameraMatrix = Matrix4.Invert(Camera.CreateCameraMatrix(luminanceOutput.Size.X / luminanceOutput.Size.Y));
        shaderInputStreaming.randSalt = Util.Random.NextSingle() + 1;
        shaderInputSSBO.Store(ref shaderInputStreaming, 0);
    }
    protected override void Free() => shaderInputSSBO.Dispose();
}

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
        TAAShaderController.SetUniform("intensity", 0.4f);
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

public sealed class CompositingModule : RenderingPipeline
{
    ShaderController compositingShaderController;
    Framebuffer compositingFramebuffer;
    Texture2D compositingOutput;

    Texture2D luminanceInput, depthInput, normalInput;
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
        compositingFramebuffer.BindFramebuffer(FramebufferTarget.Framebuffer);
        GL.Clear(ClearBufferMask.ColorBufferBit);

        compositingShaderController.Shader.Use();

        LuminanceInput.BindTex(0);
        DepthInput.BindTex(1);
        NormalInput.BindTex(2);

        Primitives.ScreenQuad.Bind();
        GL.DrawArrays(PrimitiveType.Triangles, 0, 6);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] void ThrowIfTextureSizesNotEqual() =>
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

public sealed class PostprocessingModule : RenderingPipeline
{
    ShaderController shaderController;
    Texture2D textureInput;
    Texture2D TextureInput
    {
        get => textureInput;
        set
        {
            VoxelPTRPHelper.ThrowIfTextureInvalid(value);
            textureInput = value;
        }
    }
    public PostprocessingModule(ShaderController shaderController, Texture2D textureInput)
    {
        this.shaderController = shaderController;
        TextureInput = textureInput;
    }
    public override void Execute()
    {
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        GL.Clear(ClearBufferMask.ColorBufferBit);

        shaderController.Shader.Use();
        shaderController.SetUniform("tex", 0);

        TextureInput.BindTex(0);

        Primitives.ScreenQuad.Bind();
        GL.DrawArrays(PrimitiveType.Triangles, 0, 6);
    }

    protected override void Free() { }
}