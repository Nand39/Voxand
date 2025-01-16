using System.Runtime.InteropServices;

using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

using DisposableExt;

using GLAV.Types;

using Voxand.Content;
using Voxand.Engine.Systems.Graphics.Tools.ShaderServices;
using Voxand.Engine.Systems.Graphics.Pipelines.Modules;
using Voxand.Engine.Systems.Graphics.Tools;
using Voxand.App.Map;

namespace Voxand.Engine.Systems.Graphics.Pipelines.DefaultVoxelPTRP;

public sealed class VoxelPTRP : RenderingPipeline
{
    Camera Camera;
    Texture2D luminance_depthPT, normalCompound_motionPT;
    Texture2D skyTex;
    RenderTarget renderTarget;
    ShaderController pathTracingShaderController, TAAShaderController, compositingShaderController;

    DisposalList lifetimeResources;
    // Stores textures and framebuffers to carry data between pipeline stages and that need to be reallocated
    // each time path tracing resolution changes
    DisposalList varyingRenderDataStorage;

    VoxelPathTracingModule voxelPathTracingModule;
    AntiAliasingBasicModule antiAliasingModule;
    CompositingModule compositingModule;
    public IAntiAliasingBasicModuleSettings antiAliasingSettings => antiAliasingModule;
    public IVoxelPathTracingSettings voxelPathTracingSettings => voxelPathTracingModule;
    public Vector2i RenderingResolution { get; private set; }
    public RenderTarget RenderTarget
    {
        get => renderTarget;
        set
        {
            renderTarget = value;
            compositingModule.RenderTarget = value;
        }
    }
    public VoxelPTRP(ContentManager content, Camera camera, ChunkMap map, RenderTarget output, Vector2i renderingResolution)
    {
        lifetimeResources = new(); varyingRenderDataStorage = new();

        Camera = camera;
        RenderingResolution = renderingResolution;

        CreateVaryingRenderDataStorage(renderingResolution);

        varyingRenderDataStorage.Add(luminance_depthPT, normalCompound_motionPT);

        skyTex = content.LoadTexture("Graphics/Textures/env.hdr");

        pathTracingShaderController = new(content, false, "Graphics/Shaders/voxel_path_tracing_shader.comp");
        pathTracingShaderController.Shader.Lable = "* voxel PT shader";

        TAAShaderController = new(content, false, "Graphics/Shaders/taa_shader.comp");
        TAAShaderController.Shader.Lable = "* TAA shader";

        compositingShaderController = new(content, false,
            "Graphics/Shaders/voxel_compositing_shader.vert",
            "Graphics/Shaders/voxel_compositing_shader.frag");
        compositingShaderController.Shader.Lable = "* Compositing shader";

        voxelPathTracingModule = new VoxelPathTracingModule(
            shaderControllerPT: pathTracingShaderController,
            camera: Camera,
            mapSize: map.Dimensions,
            skyTexture: skyTex,
            luminance_depthOutput: luminance_depthPT,
            normalCompound_motionOutput: normalCompound_motionPT);

        antiAliasingModule = new(TAAShaderController, luminance_depthPT, normalCompound_motionPT);

        compositingModule = new(compositingShaderController, luminance_depthPT, normalCompound_motionPT, output);

        lifetimeResources.Add(
            pathTracingShaderController, TAAShaderController, compositingShaderController,
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

        varyingRenderDataStorage.Add(luminance_depthPT, normalCompound_motionPT);

        voxelPathTracingModule.SetOutput(luminance_depthPT, normalCompound_motionPT);
        antiAliasingModule.SetInputOutput(luminance_depthPT, normalCompound_motionPT);
        compositingModule.SetInput(luminance_depthPT, normalCompound_motionPT);

        RenderingResolution = resolution;
    }
    
    void CreateVaryingRenderDataStorage(Vector2i resolution)
    {
        Console.WriteLine($"Rendering resolution set to {resolution}");

        TexParam[] texParams;
        TextureFormat format;

        texParams = TexParam.defaultTexParams;

        format = new TextureFormat()
        {
            internalFormat = PixelInternalFormat.Rgba32f,
            format = PixelFormat.Rgba,
            pixelType = PixelType.Float
        };

        luminance_depthPT = new Texture2D();
        luminance_depthPT.Alloc(resolution, format, nint.Zero);
        luminance_depthPT.SetParams(texParams);
        luminance_depthPT.BindTex(0);
        luminance_depthPT.Lable = "lum_depthPT";

        format.internalFormat = PixelInternalFormat.Rgba32f;
        format.format = PixelFormat.Rgb;

        normalCompound_motionPT = new Texture2D();
        normalCompound_motionPT.Alloc(resolution, format, nint.Zero);
        normalCompound_motionPT.SetParams(texParams);
        normalCompound_motionPT.BindTex(1);
        normalCompound_motionPT.Lable = "normal_motionPT";
    }
    
    public void MapSizeChanged(Vector3i newSize)
    {
        voxelPathTracingModule.OnMapSizeChanged(newSize);
    }

    protected override void Free()
    {
        lifetimeResources.Dispose();
        varyingRenderDataStorage.Dispose();
        skyTex.Dispose();
        GC.SuppressFinalize(this);
    }
}