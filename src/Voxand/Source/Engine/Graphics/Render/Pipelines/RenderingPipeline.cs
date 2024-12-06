using DisposableExt;

namespace Voxand.Engine.Graphics.Pipelines;
public abstract class RenderingPipeline : IDisposableExt
{
    public DisposeHelper DisposeHelper { get; }
    public RenderingPipeline()
    {
        DisposeHelper = new(this);
    }
    public abstract void Execute();
    void IDisposableExt.Free() => Free();
    protected abstract void Free();
    ~RenderingPipeline() => this.Dispose();
}