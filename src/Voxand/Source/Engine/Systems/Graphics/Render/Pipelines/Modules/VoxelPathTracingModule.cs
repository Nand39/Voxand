using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

using GLAV.Types;

using Voxand.Engine.Systems.Graphics.Pipelines.DefaultVoxelPTRP.Helpers;
using Voxand.Engine.Systems.Graphics.Tools.ShaderServices;
using Voxand.Helpers.Exceptions.GLAVExceptions;

using Buffer = GLAV.Types.Buffer;
using Voxand.Helpers;
using DisposableExt;

namespace Voxand.Engine.Systems.Graphics.Pipelines.Modules;
public sealed class VoxelPathTracingModule : RenderingPipeline
{
    ShaderController pathTracingShaderController;
    Texture2D luminanceOutput, depth_motionOutput, normalOutput;
    public Texture2D SkyTex { get; set; }
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
    public Texture2D Depth_motionOutput
    {
        get => depth_motionOutput;
        set
        {
            VoxelPTRPHelper.ThrowIfTextureInvalid(value);
            depth_motionOutput = value;
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

        public Matrix4 cameraMatrix = Matrix4.Identity;
        public Matrix4 invCameraMatrix = Matrix4.Identity;
        public Matrix4 prevInvCameraMatrix = Matrix4.Identity;
        public Vector3 cameraPosition = default;
        public float randSalt = 1;
    }
    public Camera Camera { get; set; }
    public VoxelPathTracingModule(ShaderController shaderControllerPT, Camera camera, Vector3i mapSize, Texture2D skyTexture, Texture2D luminanceOutput, Texture2D depth_motionOutput, Texture2D normalOutput)
    {
        pathTracingShaderController = shaderControllerPT;
        pathTracingShaderController.SetUniform("luminanceTexture", 0);
        pathTracingShaderController.SetUniform("depthTexture", 1);
        pathTracingShaderController.SetUniform("normalTexture", 2);
        pathTracingShaderController.SetUniform("skyTex", 8);

        Camera = camera;
        SkyTex = skyTexture;
        shaderInputSSBO = new();
        unsafe
        {
            shaderInputSSBO.Alloc(BufferTarget.ShaderStorageBuffer, sizeof(ShaderInputStreaming), BufferUsageHint.StreamDraw);
        }
        shaderInputSSBO.BindEntireBuffer(new(BufferRangeTarget.ShaderStorageBuffer, 3));

        SetOutput(luminanceOutput, depth_motionOutput, normalOutput);
        OnMapSizeChanged(mapSize);
    }
    public unsafe override void Execute()
    {
        StreamShaderInput();

        pathTracingShaderController.Shader.Use();
        SkyTex.BindTex(8);


        LuminanceOutput.BindAsImage(0, TextureAccess.WriteOnly, SizedInternalFormat.Rgba32f);
        Depth_motionOutput.BindAsImage(1, TextureAccess.WriteOnly, SizedInternalFormat.Rgba32f);
        NormalOutput.BindAsImage(2, TextureAccess.WriteOnly, SizedInternalFormat.R32i);

        GL.DispatchCompute(LuminanceOutput.Size.X / 8, LuminanceOutput.Size.Y / 8, 1);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void ThrowIfTextureSizesNotEqual() =>
        ExceptionConstructor.ThrowIfTextureSizeNotEqual(luminanceOutput, depth_motionOutput, normalOutput);
    public void SetOutput(Texture2D luminanceOutput, Texture2D depthOutput, Texture2D normalOutput)
    {
        ExceptionConstructor.ThrowIfTextureSizeNotEqual(luminanceOutput, depthOutput, normalOutput);
        VoxelPTRPHelper.ThrowIfAnyTextureInvalid(luminanceOutput, depthOutput, normalOutput);
        this.luminanceOutput = luminanceOutput;
        this.depth_motionOutput = depthOutput;
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
        Matrix4 cameraMat = Camera.CreateCameraMatrix((float)LuminanceOutput.Size.X / LuminanceOutput.Size.Y);
        shaderInputStreaming.cameraMatrix = cameraMat;
        shaderInputStreaming.prevInvCameraMatrix = shaderInputStreaming.invCameraMatrix;
        shaderInputStreaming.invCameraMatrix = Matrix4.Invert(cameraMat);

        shaderInputStreaming.cameraPosition = Camera.position;
        shaderInputStreaming.randSalt = Util.Random.NextSingle() + 1;
        shaderInputSSBO.Store(ref shaderInputStreaming, 0);
    }
    protected override void Free() => shaderInputSSBO.Dispose();
}