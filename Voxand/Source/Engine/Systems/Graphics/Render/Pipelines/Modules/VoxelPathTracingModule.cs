using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

using DisposableExt;
using GLAV.Types;
using GLAV.Helpers.Public.Exceptions;

using Buffer = GLAV.Types.Buffer;

using Voxand.Engine.Systems.Graphics.Pipelines.DefaultVoxelPTRP.Helpers;
using Voxand.Helpers;
using Voxand.Helpers.ExtensionMethods;
using Voxand.Engine.Systems.Graphics.Tools.ShaderServices;
using Voxand.Engine.Systems.Services.Graphics;

namespace Voxand.Engine.Systems.Graphics.Pipelines.Modules;
public sealed class VoxelPathTracingModule : RenderingPipeline, IRendererPathTracing
{
    ShaderController pathTracingShaderController;
    MutableTexture2D directIllum_depthOutput, indirectIllumOutput, normalCompound_motionOutput;
    Buffer shaderInputSSBO;
    ShaderInputStreaming shaderInputStreaming = new();
    int cycle = 0;
    public Camera Camera { get; set; }
    public MutableTexture2D SkyTex { get; set; }
    public MutableTexture2D DirectIllumination_depthOutput
    {
        get => directIllum_depthOutput;
        set
        {
            VoxelPTRPHelper.ThrowIfTextureInvalid(value);
            ExceptionConstructor.ThrowIfTextureSizeNotEqual(DirectIllumination_depthOutput, IndirectIlluminationOutput, NormalCompound_motionOutput);
            directIllum_depthOutput = value;
        }
    }
    public MutableTexture2D IndirectIlluminationOutput
    {
        get => indirectIllumOutput;
        set
        {
            VoxelPTRPHelper.ThrowIfTextureInvalid(value);
            ExceptionConstructor.ThrowIfTextureSizeNotEqual(DirectIllumination_depthOutput, IndirectIlluminationOutput, NormalCompound_motionOutput);
            indirectIllumOutput = value;
        }
    }
    public MutableTexture2D NormalCompound_motionOutput
    {
        get => normalCompound_motionOutput;
        set
        {
            VoxelPTRPHelper.ThrowIfTextureInvalid(value);
            ExceptionConstructor.ThrowIfTextureSizeNotEqual(DirectIllumination_depthOutput, NormalCompound_motionOutput);
            normalCompound_motionOutput = value;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    struct ShaderInputStreaming
    {
        public ShaderInputStreaming() { }

        public Matrix4 cameraMatrix = Matrix4.Identity;
        public Matrix4 invCameraMatrix = Matrix4.Identity;
        public Matrix4 prevCameraMatrix = Matrix4.Identity;
        public Vector3 cameraPosition = default;
        public float randSalt = 1;
        public Vector3 prevCameraPosition = default;
    }
    public UniformAccessor<int> Samples { get; private set; }
    public UniformAccessor<Vector3> SunDirection { get; private set; }

    public VoxelPathTracingModule(ShaderController shaderControllerPT, Camera camera, Vector3i mapSize, MutableTexture2D skyTexture, MutableTexture2D directIllumination_depthOutput, MutableTexture2D indirectIlluminationOutput, MutableTexture2D normalCompound_motionOutput)
    {
        pathTracingShaderController = shaderControllerPT;
        pathTracingShaderController.SetUniform("directIllum_depthImg", 0);
        pathTracingShaderController.SetUniform("indirectIllumImg", 2);
        pathTracingShaderController.SetUniform("normalCompound_motionImg", 1);
        pathTracingShaderController.SetUniform("skyTex", 2);
        
        Samples = pathTracingShaderController.GetUniformAccessor<int>("samples");
        SunDirection = pathTracingShaderController.GetUniformAccessor<Vector3>("sunDirection");
        
        Samples.Set(1);
        SunDirection.Set(new(0.904f, 0.361f, 0.226f));

        Camera = camera;
        SkyTex = skyTexture;
        shaderInputSSBO = new();
        unsafe
        {
            shaderInputSSBO.Alloc(BufferTarget.ShaderStorageBuffer, sizeof(ShaderInputStreaming), BufferUsageHint.StreamDraw);
        }
        shaderInputSSBO.BindAsShaderStorage(BufferRangeTarget.ShaderStorageBuffer, new(4));

        SetOutput(directIllumination_depthOutput, indirectIlluminationOutput, normalCompound_motionOutput);
        OnMapSizeChanged(mapSize);
    }
    public unsafe override void Execute()
    {
        StreamShaderInput();

        pathTracingShaderController.Shader.Use();

        //SkyTex.BindTex(2);
        DirectIllumination_depthOutput.BindAsImage(0, TextureAccess.ReadWrite);
        IndirectIlluminationOutput.BindAsImage(2, TextureAccess.ReadWrite);
        NormalCompound_motionOutput.BindAsImage(1, TextureAccess.ReadWrite);

        GL.DispatchCompute(DirectIllumination_depthOutput.Size.X / 8, DirectIllumination_depthOutput.Size.Y / 8, 1);

        cycle++; 
        cycle &= 1;
        pathTracingShaderController.SetUniform("cycle", cycle);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetOutput(MutableTexture2D directIllumination_depthOutput, MutableTexture2D indirectIlluminationOutput, MutableTexture2D normalCompound_motionOutput)
    {
        ExceptionConstructor.ThrowIfTextureSizeNotEqual(directIllumination_depthOutput, indirectIlluminationOutput, normalCompound_motionOutput);
        VoxelPTRPHelper.ThrowIfAnyTextureInvalid(directIllumination_depthOutput, indirectIlluminationOutput, normalCompound_motionOutput);
        directIllum_depthOutput = directIllumination_depthOutput;
        indirectIllumOutput = indirectIlluminationOutput;
        this.normalCompound_motionOutput = normalCompound_motionOutput;
    }
    public void OnMapSizeChanged(Vector3i newSize)
    {
        pathTracingShaderController.SetUniform("mapSize", newSize);
        Vector3i brickmapSize = newSize.BitshiftRight(2);
        pathTracingShaderController.SetUniform("voxelBrickmapSize", brickmapSize);
    }
    void StreamShaderInput()
    {
        Matrix4 cameraMat = Camera.CreateCameraMatrix(directIllum_depthOutput.Size.Ratio());
        shaderInputStreaming.prevCameraMatrix = shaderInputStreaming.cameraMatrix;
        shaderInputStreaming.cameraMatrix = cameraMat;
        shaderInputStreaming.invCameraMatrix = Matrix4.Invert(cameraMat);

        shaderInputStreaming.prevCameraPosition = shaderInputStreaming.cameraPosition;
        shaderInputStreaming.cameraPosition = Camera.Pose.position;
        shaderInputStreaming.randSalt = Util.Random.NextSingle() + 1;
        shaderInputSSBO.Store(ref shaderInputStreaming, 0);
    }
    protected override void Free() => shaderInputSSBO.Dispose();
}