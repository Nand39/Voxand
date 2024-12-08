using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

using GLAV.Data;
using GLAV.Types;
using Voxand.Content;

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

namespace Voxand.Engine.Graphics
{
    public class ShaderUniformCacheEnum<E> where E : Enum
    {
        protected int[] uniformLocations = new int[Enum.GetNames(typeof(E)).Length];
        public Shader Shader { get; }
        public ShaderUniformCacheEnum(Shader shader)
        {
            Shader = shader;
            UpdateUniformLocations();
        }
        void UpdateUniformLocations()
        {
            string[] uniformNames = Enum.GetNames(typeof(E));
            for (int i = 0; i < uniformLocations.Length; i++)
                uniformLocations[i] = GL.GetUniformLocation(Shader.Handle.id, uniformNames[i]);
        }
        public int GetUniformLocation(E uniformKey) => uniformLocations[Convert.ToInt32(uniformKey)];
    }

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

    public enum RenderTechniques
    {
        PathTracingFragment,
        PathTracingCompute,
    }
}

namespace Voxand.Engine.Graphics.GLUtil
{
    #region Vertex Data
    public struct V_PositionUV
    {
        public Vector3 position;
        public Vector2 uv;

        public static VertexInfo vertexInfo = new(
            attribs: [
                new VertexAttributeData(0, 3, 0, VertexAttribPointerType.Float),
                new VertexAttributeData(1, 2, 3 * sizeof(float), VertexAttribPointerType.Float),
            ]);

        public V_PositionUV(Vector3 position, Vector2 uv)
        {
            this.position = position;
            this.uv = uv;
        }
    }
    #endregion
}