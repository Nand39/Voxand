using OpenTK.Windowing.Common;
using Voxand.Engine.Graphics;
using Voxand.UI;

namespace Voxand.Engine.GameStates;
public abstract class GameState(Voxand game)
{
    protected Voxand main = game;

    public abstract void Load();
    public abstract void Update(FrameEventArgs args);
    public abstract void Render(FrameEventArgs args);
    public abstract void Unload();
    public virtual void OnResize(ResizeEventArgs args) { }
}