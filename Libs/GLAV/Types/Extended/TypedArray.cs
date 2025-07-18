using DisposableExt;
using OpenTK.Graphics.OpenGL4;

namespace GLAV.Types.Extended;
public class TypedArray<T> : IDisposableExt where T : struct
{
    static int itemSize;
    internal Buffer Buffer { get; private protected set; }
    public DisposeState DisposeState { get; }

    public int Length { get; private set; } 

    public string Label { get => Buffer.Label; set => Buffer.Label = value; }

    static unsafe TypedArray() => itemSize = sizeof(T);
    TypedArray() => DisposeState = new(this);

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

    public void Store(Span<T> data, int index) => Buffer.Store(data, index * itemSize);
    public T[] Retrieve(int offset, int count) => Buffer.Retrieve<T>(offset * itemSize, count);

    public void BindAsShaderStorage(BufferRangeTarget target, int binding) => Buffer.BindAsShaderStorage(target, new(binding));
    public void BindAsShaderStorage(BufferRangeTarget target, int binding, int start, int count)
    {
        Buffer.BindAsShaderStorage(target, new(binding, start * itemSize, count * itemSize));
    }

    void IDisposableExt.Free() => Buffer.Dispose();

    ~TypedArray() => this.Dispose();
}