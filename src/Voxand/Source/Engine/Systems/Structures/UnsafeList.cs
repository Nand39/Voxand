using System.Runtime.InteropServices;

namespace Voxand.Engine.Systems.Structures;
public unsafe class UnsafeList<T> : IDisposable where T : struct
{
    T* arrayPtr;
    bool disposed = false;
    int size;

    public readonly int ItemSize;
    public int Capacity { get; protected set; } = 0;
    public int Count { get; protected set; } = 0;

    public T* this[int index]
    {
        get => arrayPtr + index;
        set => arrayPtr[index] = *value;
    }
    public UnsafeList(int initialCapacity)
    {
        if (initialCapacity < 1)
            throw new ArgumentOutOfRangeException(nameof(initialCapacity));
        Capacity = initialCapacity;
        ItemSize = sizeof(T);
        size = initialCapacity * ItemSize;
        GC.AddMemoryPressure(size);
        arrayPtr = (T*)Marshal.AllocHGlobal(size);
    }

    public int Add(T item)
    {
        int itemCount = Count;
        if (itemCount + 1 > Capacity)
        {
            Grow(GetHigherCapacity());
        }

        arrayPtr[itemCount] = item;
        Count++;
        return itemCount;
    }
    public void TrimExcess()
    {
        if (Capacity == Count) return;
        Shrink(Count);
    }
    void Grow(int higherCapacity)
    {
        if (higherCapacity <= Capacity)
            throw new ArgumentOutOfRangeException($"Cannot reallocate list: {nameof(higherCapacity)} must be greater than {nameof(Capacity)}");

        int newSize = higherCapacity * ItemSize;
        int d_size = newSize - size;

        T* newArrayPtr = (T*)Marshal.AllocHGlobal(newSize);
        Buffer.MemoryCopy(arrayPtr, newArrayPtr, newSize, size);
        Marshal.FreeHGlobal((nint)arrayPtr);
        GC.AddMemoryPressure(d_size);
        
        Capacity = higherCapacity;
        size = newSize;
        arrayPtr = newArrayPtr;
    }
    void Shrink(int lowerCapacity)
    {
        if (lowerCapacity >= Capacity)
            throw new ArgumentOutOfRangeException($"Cannot reallocate list: {nameof(lowerCapacity)} must be lesser than {nameof(Capacity)}");

        int newSize = lowerCapacity * ItemSize;
        size = Capacity * ItemSize;
        int d_size = size - newSize;
        Capacity = lowerCapacity;
        size = newSize;

        T* newArrayPtr = (T*)Marshal.AllocHGlobal(newSize);
        Buffer.MemoryCopy(arrayPtr, newArrayPtr, newSize, newSize);
        Marshal.FreeHGlobal((nint)arrayPtr);
        GC.RemoveMemoryPressure(d_size);
    }
    int GetHigherCapacity() => Capacity < 1000 ? (int)MathF.Ceiling((2 - Capacity * 0.0005f) * Capacity) : Capacity + 500;
    public void Dispose()
    {
        if (disposed) return;
        Marshal.FreeHGlobal((nint)arrayPtr);
        GC.RemoveMemoryPressure(Capacity * ItemSize);
        GC.SuppressFinalize(this);
    }
    ~UnsafeList() => Dispose();
}