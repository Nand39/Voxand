using DisposableExt;
using System.Runtime.InteropServices;

namespace Voxand.Engine.Systems.Structures;

public unsafe class UnmanagedList<T> : IDisposableExt where T : unmanaged
{
    T* arrayPtr;
    int size;

    public readonly int ItemSize;
    public int Capacity { get; protected set; } = 0;
    public int Count { get; protected set; } = 0;

    public DisposeState DisposeState { get; }

    Func<int, int> growthFunction;

    public T* this[int index]
    {
        get => arrayPtr + index;
        set => arrayPtr[index] = *value;
    }
    public UnmanagedList(int initialCapacity, Func<int, int> growthFunction)
    {
        if (initialCapacity < 1)
            throw new ArgumentOutOfRangeException(nameof(initialCapacity), initialCapacity, $"{initialCapacity} may not be less than one item.");
        Capacity = initialCapacity;
        ItemSize = sizeof(T);
        size = initialCapacity * ItemSize;
        GC.AddMemoryPressure(size);
        arrayPtr = (T*)NativeMemory.Alloc((nuint)initialCapacity, (nuint)sizeof(T));
        this.growthFunction = growthFunction;
        DisposeState = new(this);
    }

    public int Add(T item)
    {
        int itemCount = Count;
        if (itemCount + 1 > Capacity)
        {
            Grow(growthFunction(Capacity));
        }

        arrayPtr[itemCount] = item;
        Count++;
        return itemCount;
    }

    public void WriteOrAdd(Span<T> data, int index)
    {
        if (index > Count)
            throw new ArgumentOutOfRangeException($"{nameof(index)} may not be greater than {nameof(Count)}. Only allowed to write over already added items or next to them.");

        if (index + data.Length > Count)
            Grow(growthFunction(Capacity));

        Count = Math.Max(index + data.Length, Count);

        Span<T> destination = new(arrayPtr + index, data.Length);
        data.CopyTo(destination);
    }

    void Grow(int higherCapacity)
    {
        if (higherCapacity <= Capacity)
            throw new ArgumentException("New capacity must be greater than old capacity.", nameof(higherCapacity));

        int newSize = higherCapacity * ItemSize;
        int d_size = newSize - size;

        T* newArrayPtr = (T*)NativeMemory.Realloc(arrayPtr, (nuint)newSize);

        if (newArrayPtr is null)
            throw new OutOfMemoryException();

        GC.AddMemoryPressure(d_size);

        Capacity = higherCapacity;
        size = newSize;
        arrayPtr = newArrayPtr;
    }

    void IDisposableExt.Free()
    {
        if (arrayPtr is not null)
        {
            NativeMemory.Free(arrayPtr);
            GC.RemoveMemoryPressure((long)Capacity * ItemSize);
            arrayPtr = null;
        }
        GC.SuppressFinalize(this);
    }

    ~UnmanagedList() => this.Dispose();
}