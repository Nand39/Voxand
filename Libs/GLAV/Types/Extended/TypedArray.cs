using DisposableExt;
using OpenTK.Graphics.OpenGL4;

namespace GLAV.Types.Extended;
public class TypedArray<T> : IDisposableExt where T : struct
{
    internal Buffer Buffer { get; private protected set; }

    public DisposeHelper DisposeHelper { get; }

    static int itemSize;

    static unsafe TypedArray() => itemSize = sizeof(T);

    TypedArray() => DisposeHelper = new(this);

    #region Allocation
    public TypedArray(BufferTarget target, int capacity, BufferUsageHint usageHint)
        : this()
    {
        Buffer = new();
        Buffer.Alloc(target, capacity * itemSize, usageHint);
    }
    public TypedArray(T[] data, BufferTarget target, BufferUsageHint usageHint)
        : this()
    {
        Buffer = new();
        Buffer.Alloc(target, data, data.Length * itemSize, usageHint);
    }
    public TypedArray(Span<T> data, BufferTarget target, BufferUsageHint usageHint)
        : this()
    {
        Buffer = new();
        Buffer.Alloc(target, data, data.Length * itemSize, usageHint);
    }
    #endregion

    public T this[int index]
    {
        get => Buffer.Retrieve<T>(index * itemSize);
        set => Buffer.Store(ref value, index * itemSize);
    }

    public void Store(Span<T> data, int offset) => Buffer.Store(data, offset * itemSize);
    public T[] Retrieve(int offset, int count) => Buffer.Retrieve<T>(offset * itemSize, count);

    public void Free() => Buffer.Dispose();
}