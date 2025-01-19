namespace DisposableExt;
public class DisposalList : IDisposableExt
{
    List<IDisposableExt> resources = [];
    public DisposeHelper DisposeHelper { get; }
    public DisposalList() => DisposeHelper = new(this);
    public void Add(IDisposableExt resource)
    {
        ArgumentNullException.ThrowIfNull(resource);
        resources.Add(resource);
        resource.DisposeHelper.OnDispose += Remove;
    }
    public void Add(params IDisposableExt[] resources)
    {
        foreach (var resource in resources)
            Add(resource);
    }
    public void Remove(IDisposableExt resource)
    {
        resources.Remove(resource);
        resource.DisposeHelper.OnDispose -= Remove;
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