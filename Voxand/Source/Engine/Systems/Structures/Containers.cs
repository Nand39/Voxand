using System.Runtime.InteropServices;

using OpenTK.Mathematics;

using GLAV.Data;

namespace Voxand.Engine.Systems.Voxels
{
    public readonly struct VoxelMaterial(Vector3 color, Vector3 emission)
    {
        public readonly static byte sizeInBytes = sizeof(float) * 8;

        public readonly Vector3 color = color;
        readonly float padding1;
        public readonly Vector3 emission = emission;
        readonly float padding2;
    }
}

namespace Voxand.Engine.Systems.Graphics
{
    [StructLayout(LayoutKind.Sequential)]
    struct ComputeRenderInputStd140
    {
        public static unsafe int sizeInBytes = sizeof(ComputeRenderInputStd140);
        public static int streamDataSize = 20 * sizeof(float);

        public Matrix4 inverseCameraMatrix = Matrix4.Identity;

        public Vector3 cameraPosition = default;
        public float randSalt = 1;

        public ComputeRenderInputStd140() { }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct DirectionalLightStd140
    {
        public Vector3 direction;
        float padding1;
        public Vector3 color;
        float padding2;
    }
}

namespace Voxand.Engine.Systems.Graphics.Tools.Data
{
    [StructLayout(LayoutKind.Sequential)]
    public struct V_PosUV(Vector3 position, Vector2 uv)
    {
        [VertexData(location: 0)] public Vector3 position = position;
        [VertexData(location: 1)] public Vector2 uv = uv;
    }
}