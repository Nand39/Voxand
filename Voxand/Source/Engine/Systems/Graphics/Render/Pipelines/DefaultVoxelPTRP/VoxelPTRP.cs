using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

using DisposableExt;

using GLAV.Types;

using Voxand.Content;
using Voxand.Engine.Systems.Graphics.Tools.ShaderServices;
using Voxand.Engine.Systems.Graphics.Pipelines.Modules;
using Voxand.Engine.Systems.Graphics.Tools;
using Voxand.App.Voxels.Map;
using Voxand.Engine.Systems.Services.Graphics;

namespace Voxand.Engine.Systems.Graphics.Pipelines.DefaultVoxelPTRP;

public sealed class VoxelPTRP : RenderingPipeline, IRendererAntiAliasingUsage
{
    Camera Camera;
    Texture2D directIllumination_depthPT, indirectIlluminationPT, normalCompound_motionPT;
    Texture2D skyTex;
    RenderTarget renderTarget;
    ShaderController pathTracingShaderController, TAAShaderController, compositingShaderController;

    DisposalList lifetimeResources;
    // Stores textures and framebuffers to carry data between pipeline stages and that need to be reallocated
    // each time path tracing resolution changes
    DisposalList varyingRenderDataStorage;

    VoxelPathTracingModule voxelPathTracingModule;
    AntiAliasingModule antiAliasingModule;
    CompositingModule compositingModule;

    public bool UseAntiAliasing { get; set; } = true;
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
    public VoxelPTRP(ContentManager content, Camera camera, ChunkMap map, RenderTarget output, Vector2i renderingResolution, out IRendererAntiAliasing antiAliasing, out IRendererPathTracing pathTracing)
    {
        lifetimeResources = new(); varyingRenderDataStorage = new();

        Camera = camera;
        RenderingResolution = renderingResolution;

        CreateVaryingRenderDataStorage(renderingResolution);

        varyingRenderDataStorage.Add(directIllumination_depthPT, indirectIlluminationPT, normalCompound_motionPT);

        skyTex = content.LoadTexture("Graphics/Textures/env.hdr", PixelInternalFormat.Rgba32f);
        skyTex.Label = "Sky texture";

        pathTracingShaderController = new(content, "Graphics/Shaders/voxel_path_tracing_shader.comp");
        pathTracingShaderController.Shader.Label = "* voxel PT shader";

        TAAShaderController = new(content, "Graphics/Shaders/taa_shader.comp");
        TAAShaderController.Shader.Label = "* TAA shader";

        compositingShaderController = new(content,
            "Graphics/Shaders/voxel_compositing_shader.vert",
            "Graphics/Shaders/voxel_compositing_shader.frag");
        compositingShaderController.Shader.Label = "* Compositing shader";

        voxelPathTracingModule = new VoxelPathTracingModule(
            shaderControllerPT: pathTracingShaderController,
            camera: Camera,
            mapSize: map.Dimensions,
            skyTexture: skyTex,
            directIllumination_depthOutput: directIllumination_depthPT,
            indirectIlluminationOutput: indirectIlluminationPT,
            normalCompound_motionOutput: normalCompound_motionPT);

        antiAliasingModule = new(TAAShaderController, indirectIlluminationPT, normalCompound_motionPT);

        pathTracing = voxelPathTracingModule;
        antiAliasing = antiAliasingModule;

        compositingModule = new(compositingShaderController, directIllumination_depthPT, indirectIlluminationPT, normalCompound_motionPT, output);

        lifetimeResources.Add(
            pathTracingShaderController, TAAShaderController, compositingShaderController,
            voxelPathTracingModule, antiAliasingModule, compositingModule);
    }
    public override void Execute()
    {
        voxelPathTracingModule.Execute();
        if (UseAntiAliasing) antiAliasingModule.Execute();
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

        varyingRenderDataStorage.Add(directIllumination_depthPT, normalCompound_motionPT);

        voxelPathTracingModule.SetOutput(directIllumination_depthPT, indirectIlluminationPT, normalCompound_motionPT);
        antiAliasingModule.SetInputOutput(indirectIlluminationPT, normalCompound_motionPT);
        compositingModule.SetInput(directIllumination_depthPT, indirectIlluminationPT, normalCompound_motionPT);

        RenderingResolution = resolution;
    }
    
    void CreateVaryingRenderDataStorage(Vector2i resolution)
    {
        Console.WriteLine($"Rendering resolution set to {resolution}");

        directIllumination_depthPT = new Texture2D();
        directIllumination_depthPT.Alloc(resolution, PixelInternalFormat.Rgba32f);
        directIllumination_depthPT.Label = "directIllum_depthPT";

        indirectIlluminationPT = new Texture2D();
        indirectIlluminationPT.Alloc(resolution, PixelInternalFormat.Rgba32f);
        indirectIlluminationPT.Label = "indirectIllumPT";

        normalCompound_motionPT = new Texture2D();
        normalCompound_motionPT.Alloc(resolution, PixelInternalFormat.Rgba32f);
        normalCompound_motionPT.Label = "normal_motionPT";
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