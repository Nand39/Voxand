using DisposableExt;
using Voxand.Engine.ExecutionControl;

namespace Voxand.Engine.Systems.Scripts;

public abstract class Script : IDisposableExt
{
    public DisposeHelper DisposeHelper { get; }
    public Script() => DisposeHelper = new(this);
    public virtual void Initialize() { }
    public virtual void Update() { }
    protected virtual void Free() { }
    void IDisposableExt.Free() => Free();
}