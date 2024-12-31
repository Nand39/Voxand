using System.Runtime.InteropServices;

using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

using DisposableExt;

using GLAV.Types;

using Voxand.Content;
using Voxand.Engine.Systems.Graphics.Tools.ShaderServices;
using Voxand.Engine.Systems.Graphics.Pipelines.Modules;
using Voxand.Engine.Systems.Graphics.Tools;
using Voxand.Engine.Systems.Voxels;
using Voxand.App.Map;

namespace Voxand.Engine.Systems.Graphics.Pipelines.DefaultVoxelPTRP;

public sealed class VoxelPTRP : RenderingPipeline
{
    Camera Camera;
    Texture2D luminancePT, depth_motionPT, normalPT;
    Texture2D skyTex;
    RenderTarget renderTarget;
    ShaderController pathTracingShaderController, TAAShaderController, copyTex8ShaderController, compositingShaderController;

    DisposalList lifetimeResources;
    // Stores textures and framebuffers to carry data between pipeline stages and that need to be reallocated
    // each time path tracing resolution changes
    DisposalList varyingRenderDataStorage;

    VoxelPathTracingModule voxelPathTracingModule;
    AntiAliasingBasicModule antiAliasingModule;
    CompositingModule compositingModule;
    public IAntiAliasingBasicModuleSettings antiAliasingSettings => antiAliasingModule;
    public IVoxelPathTracingSettings voxelPathTracingSettings => voxelPathTracingModule;
    public Vector2i RenderingResolution { get; set; }
    public RenderTarget RenderTarget
    {
        get => renderTarget;
        set
        {
            renderTarget = value;
            compositingModule.Output = value;
        }
    }
    public VoxelPTRP(ContentManager content, Camera camera, ChunkMap map, RenderTarget output, Vector2i renderingResolution)
    {
        lifetimeResources = new(); varyingRenderDataStorage = new();

        Camera = camera;
        RenderingResolution = renderingResolution;

        CreateVaryingRenderDataStorage(renderingResolution);

        varyingRenderDataStorage.Add(luminancePT, depth_motionPT, normalPT);

        skyTex = content.LoadTexture("Graphics/Textures/env.hdr", false);

        pathTracingShaderController = new(content, true, "Graphics.Shaders.voxelRenderComputeBrickmap.comp");
        pathTracingShaderController.Shader.Lable = "compute render shader";

        TAAShaderController = new(content, true, "Graphics.Shaders.tempAntiAliasingBasicShader.comp");
        TAAShaderController.Shader.Lable = "anti-aliasing basic shader";

        copyTex8ShaderController = new(content, true, "Graphics.Shaders.copyTex8.comp");
        copyTex8ShaderController.Shader.Lable = "copyTex8 shader";

        compositingShaderController = new(content, true,
            "Graphics.Shaders.voxelCompositing.vert",
            "Graphics.Shaders.voxelCompositing.frag");
        compositingShaderController.Shader.Lable = "compositing shader";

        voxelPathTracingModule = new VoxelPathTracingModule(
            shaderControllerPT: pathTracingShaderController,
            camera: Camera,
            mapSize: map.Dimensions,
            skyTexture: skyTex,
            luminanceOutput: luminancePT,
            depth_motionOutput: depth_motionPT,
            normalOutput: normalPT);

        antiAliasingModule = new(TAAShaderController, luminancePT);

        compositingModule = new(compositingShaderController, luminancePT, depth_motionPT, normalPT, output);

        lifetimeResources.Add(
            pathTracingShaderController, TAAShaderController, copyTex8ShaderController, compositingShaderController,
            voxelPathTracingModule, antiAliasingModule, compositingModule);
    }
    public override void Execute()
    {
        voxelPathTracingModule.Execute();
        antiAliasingModule.Execute();
        compositingModule.Execute();
    }
    public void VPT() => voxelPathTracingModule.Execute();
    public void TAA() => antiAliasingModule.Execute();
    public void CMP() => compositingModule.Execute();

    /// <summary>
    /// Sets target resolution to render the scene in. Disposes and reallocates textures and framebuffers
    /// to match <paramref name="resolution"/>.
    /// </summary>
    public void SetRenderingResolution(Vector2i resolution)
    {
        if (resolution.X % 8 + resolution.Y % 8 != 0)
            throw new ArgumentException($"Both dimensions of {nameof(resolution)} should be divisible by 8.");

        varyingRenderDataStorage.Dispose();

        CreateVaryingRenderDataStorage(resolution);

        varyingRenderDataStorage.Add(luminancePT, depth_motionPT, normalPT);

        voxelPathTracingModule.SetOutput(luminancePT, depth_motionPT, normalPT);
        antiAliasingModule.Luminance = luminancePT;
        compositingModule.SetInput(luminancePT, depth_motionPT, normalPT);
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

        luminancePT = new Texture2D();
        luminancePT.Alloc(resolution, format, nint.Zero);
        luminancePT.SetParams(ref texParams);
        luminancePT.BindTex(0);
        luminancePT.Lable = "lumPT";

        format.internalFormat = PixelInternalFormat.Rgba32f;
        format.format = PixelFormat.Rgb;

        depth_motionPT = new Texture2D();
        depth_motionPT.Alloc(resolution, format, nint.Zero);
        depth_motionPT.SetParams(ref texParams);
        depth_motionPT.BindTex(1);
        depth_motionPT.Lable = "depth_motionPT";

        format.internalFormat = PixelInternalFormat.R32i;
        format.format = PixelFormat.RedInteger;
        format.pixelType = PixelType.Int;

        normalPT = new Texture2D();
        normalPT.Alloc(resolution, format, nint.Zero);
        normalPT.SetParams(ref texParams);
        normalPT.BindTex(2);
        normalPT.Lable = "normalPT";
    }
    public void MapSizeChanged(Vector3i newSize)
    {
        voxelPathTracingModule.OnMapSizeChanged(newSize);
    }
    protected override void Free()
    {
        voxelPathTracingModule.Dispose();
        antiAliasingModule.Dispose();
        compositingModule.Dispose();
        lifetimeResources.Dispose();
        varyingRenderDataStorage.Dispose();
        skyTex.Dispose();
        GC.SuppressFinalize(this);
    }
}