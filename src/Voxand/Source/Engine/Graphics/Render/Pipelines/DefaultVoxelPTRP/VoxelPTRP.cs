using System.Runtime.InteropServices;

using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

using DisposableExt;

using GLAV.Types;

using Voxand.Engine.Graphics.Tools.ShaderServices;
using Voxand.Engine.Graphics.Pipelines.Modules;
using Voxand.Engine.Systems.Voxels;
using Voxand.Content;
using Voxand.Helpers;

namespace Voxand.Engine.Graphics.Pipelines.DefaultVoxelPTRP;

public sealed class VoxelPTRP : RenderingPipeline
{
    Camera Camera;
    Texture2D luminancePT, depthPT, normalPT, luminanceTAA, compositingResult;
    Texture2D skyTex;
    ShaderController pathTracingShaderController, TAAShaderController, compositingShaderController, postprocessingShaderController;

    DisposalList lifetimeResources;
    // Stores textures and framebuffers to carry data between pipeline stages and that need to be reallocated
    // each time path tracing resolution changes
    DisposalList varyingRenderDataStorage;

    Vector2i renderResolution;

    VoxelPathTracingModule voxelPathTracingModule;
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

        skyTex = content.LoadTexture("Graphics/Textures/env.hdr", false);

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

        voxelPathTracingModule = new VoxelPathTracingModule(
            shaderControllerPT: pathTracingShaderController,
            camera: Camera,
            mapSize: map.Dimensions,
            skyTexInput: skyTex,
            luminanceOutput: luminancePT,
            depthOutput: depthPT,
            normalOutput: normalPT);

        antiAliasingModule = new(TAAShaderController, luminancePT, luminanceTAA);

        compositingModule = new(compositingShaderController, luminanceTAA, depthPT, normalPT, compositingResult);

        postprocessingModule = new(postprocessingShaderController, compositingResult, Util.ClientSize);

        lifetimeResources.Add(
            pathTracingShaderController,
            TAAShaderController,
            compositingShaderController,
            postprocessingShaderController,
            voxelPathTracingModule,
            antiAliasingModule);
    }
    public override void Execute()
    {
        voxelPathTracingModule.Execute();
        antiAliasingModule.Execute();
        compositingModule.Execute();
        postprocessingModule.Execute();
    }
    public void VPT() => voxelPathTracingModule.Execute();
    public void TAA() => antiAliasingModule.Execute();
    public void CMP() => compositingModule.Execute();
    public void IPP() => postprocessingModule.Execute();

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

        voxelPathTracingModule.SetOutput(luminancePT, depthPT, normalPT);
        antiAliasingModule.SetInputOutput(luminancePT, luminanceTAA);
        compositingModule.SetInput(luminanceTAA, depthPT, normalPT);
        compositingModule.CompositingOutput = compositingResult;
    }
    public void SetFinalResolution(Vector2i resolution)
    {
        postprocessingModule.OutputResolution = resolution;
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
        luminancePT.GLLable = "lumPT";

        format.internalFormat = PixelInternalFormat.R32f;
        format.format = PixelFormat.Red;

        depthPT = new Texture2D();
        depthPT.Alloc(resolution, format, nint.Zero);
        depthPT.SetParams(ref texParams);
        depthPT.BindTex(1);
        depthPT.GLLable = "depthPT";

        format.internalFormat = PixelInternalFormat.R32i;
        format.format = PixelFormat.RedInteger;
        format.pixelType = PixelType.Int;

        normalPT = new Texture2D();
        normalPT.Alloc(resolution, format, nint.Zero);
        normalPT.SetParams(ref texParams);
        normalPT.BindTex(2);
        normalPT.GLLable = "normalPT";

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
        luminanceTAA.GLLable = "lumTAA";

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
        compositingResult.GLLable = "compositingOutput";

        #endregion
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
        postprocessingModule.Dispose();
        lifetimeResources.Dispose();
        varyingRenderDataStorage.Dispose();
        skyTex.Dispose();
        GC.SuppressFinalize(this);
    }
}