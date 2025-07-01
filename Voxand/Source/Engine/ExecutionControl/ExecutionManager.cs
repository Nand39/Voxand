using OpenTK.Windowing.Common;

namespace Voxand.Engine.ExecutionControl;
public abstract class ExecutionManager()
{
    public abstract void Load(Window win);
    public abstract void Update(FrameEventArgs args);
    public abstract void Render(FrameEventArgs args);
    public abstract void Unload();
    public virtual void OnResize(ResizeEventArgs args) { }
}