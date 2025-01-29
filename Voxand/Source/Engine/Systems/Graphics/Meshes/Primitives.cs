using OpenTK.Graphics.OpenGL4;

using GLAV.Types;

using Voxand.Engine.Systems.Graphics.Tools.Data;

namespace Voxand.Engine.Systems.Graphics.Meshes.Primitives;
public class Primitives
{
    public static readonly V_PositionUV[] screenQuadVertices =
    {
        new(new(-1, -1, 0), new(0, 0)),
        new(new(-1, 1, 0),  new(0, 1)),
        new(new(1, 1, 0),   new(1, 1)),
        new(new(-1, -1, 0), new(0, 0)),
        new(new(1, 1, 0),   new(1, 1)),
        new(new(1, -1, 0),  new(1, 0)),
    };

    protected static VertexArray screenQuadVAOInstance;
    public static VertexArray ScreenQuad
    {
        get
        {
            if (screenQuadVAOInstance != null)
                return screenQuadVAOInstance;
            screenQuadVAOInstance = new();
            V_PositionUV[] vertices = screenQuadVertices;
            screenQuadVAOInstance.Alloc(ref vertices, V_PositionUV.vertexInfo, BufferUsageHint.StaticDraw);
            return screenQuadVAOInstance;
        }
    }
}