namespace DisposableExt;
public class DisposalList : IDisposableExt
{
    List<IDisposableExt> resources = [];
    public DisposeHelper DisposeHelper { get; }
    public DisposalList() => DisposeHelper = new(this);
    public void Add(IDisposableExt resource)
    {
        resources.Add(resource);
        resource.DisposeHelper.OnDispose += Remove;
    }
    public void Remove(IDisposableExt resource)
    {
        resources.Remove(resource);
    }
    public void Free()
    {
        foreach (var obj in resources)
            obj.Dispose();
    }
}