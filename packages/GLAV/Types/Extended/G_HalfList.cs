using OpenTK.Graphics.OpenGL4;

using DisposableExt;

namespace GLAV.Types;
public unsafe class G_HalfList<T> : IDisposableExt where T : struct
{
    public Buffer buffer;
    readonly BufferUsageHint usageHint;
    readonly int bindingIndex;
    bool named = false;
    string bufferName = string.Empty;
    public DisposeHelper DisposeHelper { get; } 

    public readonly int ItemSize;
    public int Capacity { get; protected set; } = 0;
    public int Count { get; protected set; } = 0;
    public G_HalfList(BufferTarget target, BufferRangeTarget rangeTarget, int bindingIndex, int initialCapacity, int itemSize, BufferUsageHint usageHint)
    {
        if (initialCapacity < 1)
            throw new ArgumentOutOfRangeException(nameof(initialCapacity));
        Capacity = initialCapacity;
        ItemSize = itemSize;
        this.usageHint = usageHint;
        this.bindingIndex = bindingIndex;

        buffer = new Buffer();
        buffer.Alloc(target, initialCapacity * ItemSize, usageHint);
        buffer.BindBufferBase(new(rangeTarget, bindingIndex));

        DisposeHelper = new(this);
    }
    public G_HalfList(BufferTarget target, BufferRangeTarget rangeTarget, int bindingIndex, int initialCapacity, int itemSize, BufferUsageHint usageHint, string lable)
        : this(target, rangeTarget, bindingIndex, initialCapacity, itemSize, usageHint)
    {
        bufferName = lable;
        buffer.GLLable = lable;
        named = true;
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
        {
            Grow(GetNextCapacity());
        }

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
        if (named) 
            newBuffer.GLLable = bufferName;

        Console.WriteLine($"OLD: {buffer.Handle.id}; NEW: {newBuffer.Handle.id}");
        
        buffer.CopyTo(newBuffer, 0, 0, buffer.Size - 1);

        newBuffer.BindBufferBase(buffer.destination);

        buffer.Dispose();
        buffer = newBuffer;

        Capacity = newCapacity;

        Console.WriteLine("list expanded to " + Capacity);
    }
    int GetNextCapacity() => Capacity < 1000 ? (int)MathF.Ceiling((2 - Capacity * 0.0005f) * Capacity) : Capacity + 500;
    void IDisposableExt.Free()
    {
        buffer.Dispose();
        GC.SuppressFinalize(this);
    }
    ~G_HalfList() => this.Dispose();
}