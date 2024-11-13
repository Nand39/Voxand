using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

using GLAV.Data;
using GLAV.Types;

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
    public class ShaderInfo<E> where E : Enum
    {
        protected int[] uLocs = new int[Enum.GetNames(typeof(E)).Length];
        protected readonly Shader shader;
        public Shader Shader
        {
            get => shader;
        }
        public ShaderInfo(Shader shader)
        {
            this.shader = shader;
            string[] uniformNames = Enum.GetNames(typeof(E));
            for (int i = 0; i < uLocs.Length; i++)
                uLocs[i] = GL.GetUniformLocation(Shader.Handle.id, uniformNames[i]);
        }
        public void SetUniform<T>(E uniformKey, T value) where T : struct
        {
            Shader.SetUniform(GetUniformLocation(uniformKey), value);
        }
        public int GetUniformLocation(E uniformKey) => uLocs[Convert.ToInt32(uniformKey)];
    }
    public struct RendererActiveSettings()
    {
        public Framebuffer VoxelRenderFramebuffer;
        public Texture2D VoxelAlbedoTexture;
        public Texture2D VoxelLuminanceTexture;
        public Texture2D VoxelNormalTexture;
        public Texture2D VoxelDepthTexture;

        public Framebuffer TAAFramebuffer;
        public Texture2D TAALuminanceTexture;

        public Framebuffer CompositingFramebuffer;
        public Texture2D CompositingResultTexture;
    }
    
    public enum CommonVoxelShaderUniforms
    {
        mapSize,
        cameraPosition,
        inverseCameraMatrix,
        renderScale,
        randSalt,
        ambientLighting,
    }
    public enum CommonTAAShaderUniforms
    {
        tex1,
        tex2,
        intensity,
        renderScale,
    }
    public enum CommonCompositeShaderUniforms
    {
        albedo,
        luminance,
        normal,
        depth,
        renderScale,
    }
    public enum CommonPostprocessingShaderUniforms
    {
        tex
    }
    public enum CommonFlatShaderUniforms
    {
        mapSize,
        cameraPosition,
        inverseCameraMatrix,
        renderScale,
    }

    [StructLayout(LayoutKind.Sequential)]
    struct ComputeRenderInputStd140
    {
        unsafe public static int sizeInBytes = sizeof(ComputeRenderInputStd140);
        public static int streamDataSize = 20 * sizeof(float);

        public Matrix4 inverseCameraMatrix = Matrix4.Identity;

        public Vector3 cameraPosition = default;
        public float randSalt = 0;

        public Vector3 ambientLighting = default;
        public float time = 0;

        public Vector3i mapSize = default;
        float padding1 = 0;

        public Vector3i voxelBrickmapSize = default;
        float padding2 = 0;

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