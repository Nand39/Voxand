namespace DisposableExt;
public class DisposalList : IDisposableExt
{
    List<IDisposableExt> resources = [];
    public DisposeState DisposeState { get; }
    public DisposalList() => DisposeState = new(this);
    public void Add(IDisposableExt resource)
    {
        ArgumentNullException.ThrowIfNull(resource);
        resources.Add(resource);
        resource.DisposeState.OnDispose += Remove;
    }
    public void Add(params IDisposableExt[] resources)
    {
        foreach (var resource in resources)
            Add(resource);
    }
    public void Remove(IDisposableExt resource)
    {
        resources.Remove(resource);
        resource.DisposeState.OnDispose -= Remove;
    }
    public void Remove(params IDisposableExt[] resources)
    {
        foreach (var resource in resources)
            Remove(resource);
    }
    void IDisposableExt.Free()
    {
        for (; resources.Count > 0;)
            resources[0].Dispose();
    }
    ~DisposalList() => this.Dispose();
}