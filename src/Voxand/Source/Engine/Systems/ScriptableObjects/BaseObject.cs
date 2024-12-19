using DisposableExt;
using OpenTK.Windowing.Common;
using Voxand.Engine.ExecutionControl;
using Voxand.Helpers.Exceptions;

namespace Voxand.Engine.Systems.ScriptableObjects;

public abstract class BaseObject : IDisposableExt
{
    public DisposeHelper DisposeHelper { get; }

    protected IWindowState WindowState => Window.Instance.TryAccessExecutionManager<IWindowStateSupported>().WindowState;
    protected IEngineState EngineState => Window.Instance.TryAccessExecutionManager<IEngineStateSupported>().EngineState;
    protected IObjectRegistry ObjectRegistry => Window.Instance.TryAccessExecutionManager<IObjectRegistrySupported>().ObjectRegistry;
    /// <summary>
    /// A shorthand for the window object.
    /// </summary>
    protected Window Win => WindowState.Window;

    public BaseObject() => DisposeHelper = new(this);
    public virtual void Initialize() { }
    public virtual void Update(FrameEventArgs args) { }
    protected virtual void Free() { }
    void IDisposableExt.Free() => Free();
}