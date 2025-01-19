using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

using GLAV.Types;
using GLAV.Helpers.Public.Exceptions;

using Voxand.Engine.Systems.Graphics.Pipelines.DefaultVoxelPTRP.Helpers;
using Voxand.Engine.Systems.Graphics.Tools.ShaderServices;

using Buffer = GLAV.Types.Buffer;
using Voxand.Helpers;
using DisposableExt;
using Voxand.Helpers.ExtensionMethods;

namespace Voxand.Engine.Systems.Graphics.Pipelines.Modules;
public interface IVoxelPathTracingSettings
{
    int Samples { get; set; }
}
public sealed class VoxelPathTracingModule : RenderingPipeline, IVoxelPathTracingSettings
{
    ShaderController pathTracingShaderController;
    Texture2D luminance_depthOutput, normalCompound_motionOutput;
    Buffer shaderInputSSBO;
    ShaderInputStreaming shaderInputStreaming = new();
    int samples;
    int cycle = 0;
    public Camera Camera { get; set; }
    public Texture2D SkyTex { get; set; }
    public Texture2D Luminance_depthOutput
    {
        get => luminance_depthOutput;
        set
        {
            VoxelPTRPHelper.ThrowIfTextureInvalid(value);
            luminance_depthOutput = value;
            ExceptionConstructor.ThrowIfTextureSizeNotEqual(Luminance_depthOutput, NormalCompound_motionOutput);
        }
    }
    public Texture2D NormalCompound_motionOutput
    {
        get => normalCompound_motionOutput;
        set
        {
            VoxelPTRPHelper.ThrowIfTextureInvalid(value);
            normalCompound_motionOutput = value;
            ExceptionConstructor.ThrowIfTextureSizeNotEqual(Luminance_depthOutput, NormalCompound_motionOutput);
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
    public int Samples
    {
        get => samples;
        set
        {
            samples = value;
            pathTracingShaderController.SetUniform("samples", samples);
        }
    }

    public VoxelPathTracingModule(ShaderController shaderControllerPT, Camera camera, Vector3i mapSize, Texture2D skyTexture, Texture2D luminance_depthOutput, Texture2D normalCompound_motionOutput)
    {
        pathTracingShaderController = shaderControllerPT;
        pathTracingShaderController.SetUniform("luminance_depthImg", 0);
        pathTracingShaderController.SetUniform("normalCompound_motionImg", 1);
        pathTracingShaderController.SetUniform("skyTex", 2);
        
        Samples = 1;

        Camera = camera;
        SkyTex = skyTexture;
        shaderInputSSBO = new();
        unsafe
        {
            shaderInputSSBO.Alloc(BufferTarget.ShaderStorageBuffer, sizeof(ShaderInputStreaming), BufferUsageHint.StreamDraw);
        }
        shaderInputSSBO.BindEntireBuffer(new(BufferRangeTarget.ShaderStorageBuffer, 3));

        SetOutput(luminance_depthOutput, normalCompound_motionOutput);
        OnMapSizeChanged(mapSize);
    }
    public unsafe override void Execute()
    {
        StreamShaderInput();

        pathTracingShaderController.Shader.Use();

        SkyTex.BindTex(2);
        Luminance_depthOutput.BindAsImage(0, TextureAccess.ReadWrite, SizedInternalFormat.Rgba32f);
        NormalCompound_motionOutput.BindAsImage(1, TextureAccess.ReadWrite, SizedInternalFormat.Rgba32f);

        GL.DispatchCompute(Luminance_depthOutput.Size.X / 8, Luminance_depthOutput.Size.Y / 8, 1);

        cycle++; 
        cycle &= 1;
        pathTracingShaderController.SetUniform("cycle", cycle);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetOutput(Texture2D luminance_depthOutput, Texture2D normalCompound_motionOutput)
    {
        ExceptionConstructor.ThrowIfTextureSizeNotEqual(luminance_depthOutput, normalCompound_motionOutput);
        VoxelPTRPHelper.ThrowIfAnyTextureInvalid(luminance_depthOutput, normalCompound_motionOutput);
        this.luminance_depthOutput = luminance_depthOutput;
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
        Matrix4 cameraMat = Camera.CreateCameraMatrix(luminance_depthOutput.Size.Ratio());
        shaderInputStreaming.prevCameraMatrix = shaderInputStreaming.cameraMatrix;
        shaderInputStreaming.cameraMatrix = cameraMat;
        shaderInputStreaming.invCameraMatrix = Matrix4.Invert(cameraMat);

        shaderInputStreaming.prevCameraPosition = shaderInputStreaming.cameraPosition;
        shaderInputStreaming.cameraPosition = Camera.position;
        shaderInputStreaming.randSalt = Util.Random.NextSingle() + 1;
        shaderInputSSBO.Store(ref shaderInputStreaming, 0);
    }
    protected override void Free() => shaderInputSSBO.Dispose();
}