using DisposableExt;

namespace Voxand.Engine.Graphics;
public abstract class RenderingPipeline : IDisposableExt
{
    protected DisposalList resources = new();
    public DisposeHelper DisposeHelper { get; }
    public RenderingPipeline()
    {
        DisposeHelper = new(this);
    }
    public abstract void Execute();
    public abstract void Free();
    ~RenderingPipeline() => this.Dispose();
}