using DisposableExt;

namespace Voxand.Engine.Systems.Graphics.Pipelines;
public abstract class RenderingPipeline : IDisposableExt
{
    public DisposeState DisposeState { get; }
    public RenderingPipeline()
    {
        DisposeState = new(this);
    }
    public abstract void Execute();
    void IDisposableExt.Free() => Free();
    protected abstract void Free();
    ~RenderingPipeline() => this.Dispose();
}