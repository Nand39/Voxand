using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

using GLAV.Types;

using Voxand.Engine.Graphics.Pipelines.DefaultVoxelPTRP.Helpers;
using Voxand.Engine.Graphics.Tools.ShaderServices;
using Voxand.Engine.Graphics.Tools.Exceptions;

using Buffer = GLAV.Types.Buffer;
using Voxand.Helpers;
using DisposableExt;

namespace Voxand.Engine.Graphics.Pipelines.Modules;
public sealed class VoxelPathTracingModule : RenderingPipeline
{
    ShaderController pathTracingShaderController;
    Texture2D luminanceOutput, depthOutput, normalOutput;
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
        public float randSalt = 1;
    }
    public Camera Camera { get; set; }
    public VoxelPathTracingModule(ShaderController shaderControllerPT, Camera camera, Vector3i mapSize, Texture2D skyTexInput, Texture2D luminanceOutput, Texture2D depthOutput, Texture2D normalOutput)
    {
        pathTracingShaderController = shaderControllerPT;
        pathTracingShaderController.SetUniform("luminanceTexture", 0);
        pathTracingShaderController.SetUniform("depthTexture", 1);
        pathTracingShaderController.SetUniform("normalTexture", 2);
        pathTracingShaderController.SetUniform("skyTex", 8);

        Camera = camera;
        SkyTex = skyTexInput;
        shaderInputSSBO = new();
        unsafe
        {
            shaderInputSSBO.Alloc(BufferTarget.ShaderStorageBuffer, sizeof(ShaderInputStreaming), BufferUsageHint.StreamDraw);
        }
        shaderInputSSBO.BindBufferBase(new(BufferRangeTarget.ShaderStorageBuffer, 3));

        SetOutput(luminanceOutput, depthOutput, normalOutput);
        OnMapSizeChanged(mapSize);
    }
    public unsafe override void Execute()
    {
        StreamShaderInput();

        pathTracingShaderController.Shader.Use();
        SkyTex.BindTex(8);


        LuminanceOutput.BindAsImage(0, TextureAccess.WriteOnly, SizedInternalFormat.Rgba32f);
        DepthOutput.BindAsImage(1, TextureAccess.WriteOnly, SizedInternalFormat.R32f);
        NormalOutput.BindAsImage(2, TextureAccess.WriteOnly, SizedInternalFormat.R32i);

        GL.DispatchCompute(LuminanceOutput.Size.X / 8, LuminanceOutput.Size.Y / 8, 1);

        //Vector4[] result = new Vector4[LuminanceOutput.Size.X * LuminanceOutput.Size.Y];
        //LuminanceOutput.BindTex(0);

        //fixed (Vector4* ptr = result)
        //{
        //    GL.GetnTexImage(
        //    TextureTarget.Texture2D,
        //    0,
        //    PixelFormat.Rgba,
        //    PixelType.Float,
        //    sizeof(Vector4) * result.Length,
        //    (nint)ptr);
        //}

        //ErrorCode errorCode = GL.GetError();
        //if (errorCode != ErrorCode.NoError)
        //{
        //    Console.WriteLine("Error: " + errorCode);
        //}
        //Console.WriteLine("First pixel of lumPT: " + result[0]);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void ThrowIfTextureSizesNotEqual() =>
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
        shaderInputStreaming.inverseCameraMatrix = Matrix4.Transpose(Matrix4.Invert(Camera.CreateCameraMatrix(luminanceOutput.Size.X / luminanceOutput.Size.Y)));
        shaderInputStreaming.randSalt = Util.Random.NextSingle() + 1;
        shaderInputSSBO.Store(ref shaderInputStreaming, 0);
        shaderInputSSBO.LogContent<float>();
    }
    protected override void Free() => shaderInputSSBO.Dispose();
}