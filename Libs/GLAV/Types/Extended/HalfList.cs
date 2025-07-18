using OpenTK.Graphics.OpenGL4;

using DisposableExt;
using System.Collections.Generic;
using System.Drawing;

namespace GLAV.Types.Extended;
public unsafe class HalfList<T> : IDisposableExt where T : struct
{
    public TypedArray<T> array;
    readonly BufferUsageHint usageHint;
    readonly BufferTarget originalBufferTarget;
    public DisposeState DisposeState { get; } 

    public readonly int itemSize;
    Func<int, int> growthFunction;

    public int Capacity => array.Length;
    public int Count { get; protected set; } = 0;
    public string Label { get => array.Buffer.Label; set => array.Buffer.Label = value; }

    public HalfList(BufferTarget target, int initialCapacity, BufferUsageHint usageHint, Func<int, int> growthFunction)
    {
        if (initialCapacity < 1)
            throw new ArgumentOutOfRangeException(nameof(initialCapacity));

        DisposeState = new(this);

        itemSize = sizeof(T);
        originalBufferTarget = target;
        this.usageHint = usageHint;
        this.growthFunction = growthFunction;

        array = new TypedArray<T>(target, initialCapacity, usageHint);
    }

    public T this[int index]
    {
        set => array[index] = value;
        get => array[index];
    }

    public int Add(T item)
    {
        if (Count == Capacity)
            Grow(growthFunction(Capacity));

        array[Count] = item;
        Count++;
        return Count - 1;
    }

    void Grow(int newCapacity)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(newCapacity, Capacity, $"{nameof(newCapacity)} cannot be less than the current list capacity.");

        TypedArray<T> newArray = new(originalBufferTarget, newCapacity, usageHint);
        newArray.Label = array.Label;
        
        array.Buffer.CopyTo(newArray.Buffer, 0, 0, Count * itemSize);

        newArray.Buffer.ReplaceBindingsOf(array.Buffer);

        array.Dispose();
        array = newArray;
    }

    public void BindAsShaderStorage(BufferRangeTarget target, int binding) => array.BindAsShaderStorage(target, binding);
    public void BindAsShaderStorage(BufferRangeTarget target, int binding, int start, int count) => array.BindAsShaderStorage(target, binding, start, count);
    public void WriteOrAdd(Span<T> data, int index)
    {
        if (index > Count)
            throw new ArgumentOutOfRangeException($"{nameof(index)} may not be greater than {nameof(Count)}. Only allowed to write over already added items or next to them.");

        if (index + data.Length > Count)
            Grow(growthFunction(Capacity));

        Count = Math.Max(index + data.Length, Count);

        array.Store(data, index);
    }

    void IDisposableExt.Free()
    {
        array.Dispose();
        GC.SuppressFinalize(this);
    }
    ~HalfList() => this.Dispose();
}