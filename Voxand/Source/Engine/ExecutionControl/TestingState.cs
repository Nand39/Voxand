using OpenTK.Windowing.Common;
using OpenTK.Graphics.OpenGL4;

using DisposableExt;
using GLAV.Types;

using Voxand.Engine.Systems.Graphics.Tools.Data;
using Voxand.Engine.Systems.Graphics.Tools.ShaderServices;

namespace Voxand.Engine.ExecutionControl;
public class TestingState(Window win) : ExecutionManager
{
    VertexArray vao;
    Texture2D texture;
    Window window = win;
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
        texture = window.Content.LoadTexture("Graphics/Textures/router.png", PixelInternalFormat.Rgba);
        texture.BindTex(0);
    }
    public override void Update(FrameEventArgs args)
    {
        
    }
    public override void Render(FrameEventArgs args)
    {
        GL.Clear(ClearBufferMask.ColorBufferBit);
        texture.BindTex(0);
        vao.Bind();
        GL.DrawArrays(PrimitiveType.Triangles, 0, 6);
    }
    public override void Unload()
    {
        vao.Dispose();
        texture.Dispose();
        Console.WriteLine("Resources was successfully unloaded");
    }
}