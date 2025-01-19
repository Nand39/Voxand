using System.Runtime.CompilerServices;

namespace DisposableExt;
public interface IDisposableExt
{
    public DisposeHelper DisposeHelper { get; }
    public void Dispose()
    {
        lock (DisposeHelper.Locker)
        {
            if (!DisposeHelper.Disposed)
            {
                DisposeHelper.MarkAndNotify();
                Free();
            }
        }
    }
    protected void Free();
}
public class DisposeHelper(IDisposableExt target)
{
    IDisposableExt disposable = target;
    public event Action<IDisposableExt>? OnDispose;
    public bool Disposed { get; protected set; } = false;
    public object Locker { get; } = new();
    public void MarkAndNotify()
    {
        Disposed = true;
        OnDispose?.Invoke(disposable);
    }
}
public static class ImplicitCastExtension
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static void Dispose(this IDisposableExt obj) => obj.Dispose();
}