using OpenTK.Windowing.Common;
using OpenTK.Graphics.OpenGL4;

using DisposableExt;
using GLAV.Types;

using Voxand.Engine.GameStates;
using Voxand.Engine.Graphics.GLUtil;

namespace Voxand;
public class TestingState(Voxand game) : GameState(game)
{
    VertexArray vao;
    public override void Load()
    {
        GL.ClearColor(0.2f, 0.3f, 0.3f, 1);

        V_PositionUV[] viewRectVertices = [
            new(new(-1, -1, 0), new(0, 0)),
            new(new(-1, 1, 0),  new(0, 1)),
            new(new(1, 1, 0),   new(1, 1)),
            new(new(-1, -1, 0), new(0, 0)),
            new(new(1, 1, 0),   new(1, 1)),
            new(new(1, -1, 0),  new(1, 0)),
        ];
        vao = new();
        vao.Alloc(ref viewRectVertices, V_PositionUV.vertexInfo, BufferUsageHint.StaticDraw);
    }
    public override void Update(FrameEventArgs args)
    {
        
    }
    public override void Render(FrameEventArgs args)
    {
        
    }
    public override void Unload()
    {
        vao.Dispose();
        Console.WriteLine("Resources was successfully unloaded");
    }
}