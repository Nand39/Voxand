using OpenTK.Graphics.OpenGL4;

using GLAV.Types;

using Voxand.Engine.Systems.Graphics.Tools.Data;
using GLAV.Types.Extended;

namespace Voxand.Engine.Systems.Graphics.Meshes.Primitives;
public class Primitives
{
    public static readonly V_PosUV[] screenQuadVertices =
    {
        new(new(-1, -1, 0), new(0, 0)),
        new(new(-1, 1, 0),  new(0, 1)),
        new(new(1, 1, 0),   new(1, 1)),
        new(new(-1, -1, 0), new(0, 0)),
        new(new(1, 1, 0),   new(1, 1)),
        new(new(1, -1, 0),  new(1, 0)),
    };

    static TypedArray<V_PosUV>? screenQuadVertexArray;

    protected static VertexAttributeSet? screenQuadVertexAttribs;
    public static VertexAttributeSet ScreenQuadVertexAttribs
    {
        get
        {
            if (screenQuadVertexAttribs is not null)
                return screenQuadVertexAttribs;

            screenQuadVertexAttribs = new();

            screenQuadVertexArray = new(screenQuadVertices, BufferTarget.ArrayBuffer, BufferUsageHint.StaticDraw);
            screenQuadVertexAttribs.AddAttributes<V_PosUV>(screenQuadVertexArray.Buffer);

            return screenQuadVertexAttribs;
        }
    }
}