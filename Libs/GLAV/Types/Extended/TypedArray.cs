using DisposableExt;
using OpenTK.Graphics.OpenGL4;

namespace GLAV.Types.Extended;
public class TypedArray<T> : IDisposableExt where T : struct
{
    static int itemSize;
    internal Buffer Buffer { get; private protected set; }
    public DisposeHelper DisposeHelper { get; }

    public int Length { get; private set; } 

    public string Label { get => Buffer.Label; set => Buffer.Label = value; }

    static unsafe TypedArray() => itemSize = sizeof(T);
    TypedArray() => DisposeHelper = new(this);

    #region Allocation
    public TypedArray(BufferTarget target, int capacity, BufferUsageHint usageHint)
        : this()
    {
        Buffer = new();
        Buffer.Alloc(target, capacity * itemSize, usageHint);
        Length = capacity;
    }
    public TypedArray(T[] data, BufferTarget target, BufferUsageHint usageHint)
        : this()
    {
        Buffer = new();
        Buffer.Alloc(target, data, data.Length * itemSize, usageHint);
        Length = data.Length;
    }
    public TypedArray(Span<T> data, BufferTarget target, BufferUsageHint usageHint)
        : this()
    {
        Buffer = new();
        Buffer.Alloc(target, data, data.Length * itemSize, usageHint);
        Length = data.Length;
    }
    #endregion

    public T this[int index]
    {
        get => Buffer.Retrieve<T>(index * itemSize);
        set => Buffer.Store(ref value, index * itemSize);
    }

    public void Store(Span<T> data, int offset) => Buffer.Store(data, offset * itemSize);
    public T[] Retrieve(int offset, int count) => Buffer.Retrieve<T>(offset * itemSize, count);

    public void BindAsShaderStorage(BufferRangeTarget target, int binding) => Buffer.BindAsShaderStorage(target, new(binding));
    public void BindAsShaderStorage(BufferRangeTarget target, int binding, int start, int count)
    {
        Buffer.BindAsShaderStorage(target, new(binding, start * itemSize, count * itemSize));
    }

    public void Free() => Buffer.Dispose();
}