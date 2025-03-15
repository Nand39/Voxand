using OpenTK.Graphics.OpenGL4;

using DisposableExt;

namespace GLAV.Types.Extended;
public unsafe class G_HalfList<T> : IDisposableExt where T : struct
{
    public Buffer buffer;
    readonly BufferUsageHint usageHint;
    public DisposeHelper DisposeHelper { get; } 

    public readonly int ItemSize;
    Func<int, int> growthFunction;
    public int Capacity { get; protected set; } = 0;
    public int Count { get; protected set; } = 0;
    public G_HalfList(BufferTarget target, BufferRangeTarget rangeTarget, int bindingIndex, int initialCapacity, int itemSize, BufferUsageHint usageHint, Func<int, int> growthFunction)
    {
        if (initialCapacity < 1)
            throw new ArgumentOutOfRangeException(nameof(initialCapacity));
        
        Capacity = initialCapacity;
        ItemSize = itemSize;
        this.usageHint = usageHint;
        this.growthFunction = growthFunction;

        buffer = new Buffer();
        buffer.Alloc(target, initialCapacity * ItemSize, usageHint);
        buffer.BindAsShaderStorage(new(rangeTarget, bindingIndex));

        DisposeHelper = new(this);
    }
    public G_HalfList(BufferTarget target, BufferRangeTarget rangeTarget, int bindingIndex, int initialCapacity, int itemSize, BufferUsageHint usageHint, Func<int, int> growthFunction, string lable)
        : this(target, rangeTarget, bindingIndex, initialCapacity, itemSize, usageHint, growthFunction)
    {
        buffer.Lable = lable;
    }

    public T this[int index]
    {
        set
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, Count);
            ArgumentOutOfRangeException.ThrowIfNegative(index);
            buffer.Store(ref value, index * ItemSize);
        }
        get
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, Count);
            ArgumentOutOfRangeException.ThrowIfNegative(index);
            return buffer.Retrieve<T>(index * ItemSize);
        }
    }

    public int Add(T item)
    {
        int itemCount = Count;
        if (itemCount + 1 > Capacity)
            Grow(growthFunction(Capacity));

        buffer.Store(ref item, itemCount * ItemSize);
        Count++;
        return itemCount;
    }
    public void Write(nint dataPtr, int index, int offset, int size)
    {
        buffer.Store(index * ItemSize + offset, dataPtr, size);
    }
    void Grow(int newCapacity)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(newCapacity, Capacity, $"{nameof(newCapacity)} cannot be less than the current list capacity.");

        Buffer newBuffer = new Buffer();
        newBuffer.Alloc(buffer.bufferTarget, newCapacity * ItemSize, usageHint);
        newBuffer.Lable = buffer.Lable;
        
        buffer.CopyTo(newBuffer, 0, 0, buffer.Size - 1);

        newBuffer.BindAsShaderStorage(buffer.bindingInfo);

        buffer.Dispose();
        buffer = newBuffer;

        Capacity = newCapacity;
    }
    void IDisposableExt.Free()
    {
        buffer.Dispose();
        GC.SuppressFinalize(this);
    }
    ~G_HalfList() => this.Dispose();
}