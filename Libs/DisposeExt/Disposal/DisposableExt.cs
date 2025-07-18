using System.Collections;
using System.Runtime.CompilerServices;

namespace DisposableExt;
public interface IDisposableExt : IDisposable
{
    DisposeState DisposeState { get; }

    void IDisposable.Dispose()
    {
        if (DisposeState.TryMarkDisposedAndNotify())
            Free();
    }

    protected void Free();
}

public class DisposeState(IDisposableExt target)
{
    IDisposableExt disposable = target;
    internal int disposeState = 0;

    public bool Disposed => disposeState == 1;
    public event Action<IDisposableExt>? OnDispose;
    
    internal bool TryMarkDisposedAndNotify()
    {
        if (Interlocked.Exchange(ref disposeState, 1) == 0)
        {
            OnDispose?.Invoke(disposable);
            return true;
        }
        return false;
    }
}

public static class ImplicitCastExtension
{
    /// <summary>
    /// Allows for implicit casting of concrete types to <see cref="IDisposableExt"/> to call its explicitly implemented <see cref="IDisposable.Dispose"/> and improve readability.
    /// <br/>Before: ((<see cref="IDisposableExt"/>)obj).Dispose()<br/> After: obj.Dispose()
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Dispose(this IDisposableExt obj) => obj.Dispose();
}