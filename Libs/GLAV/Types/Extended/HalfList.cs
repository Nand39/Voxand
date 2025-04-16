using OpenTK.Graphics.OpenGL4;

using DisposableExt;

namespace GLAV.Types.Extended;
public unsafe class HalfList<T> : IDisposableExt where T : struct
{
    public TypedArray<T> array;
    readonly BufferUsageHint usageHint;
    readonly BufferTarget originalBufferTarget;
    public DisposeHelper DisposeHelper { get; } 

    public readonly int itemSize;
    Func<int, int> growthFunction;

    public int Capacity => array.Length;
    public int Count { get; protected set; } = 0;
    public string Label { get => array.Buffer.Lable; set => array.Buffer.Lable = value; }

    public HalfList(BufferTarget target, int initialCapacity, BufferUsageHint usageHint, Func<int, int> growthFunction)
    {
        if (initialCapacity < 1)
            throw new ArgumentOutOfRangeException(nameof(initialCapacity));

        DisposeHelper = new(this);

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
        
        array.Buffer.CopyTo(newArray.Buffer, 0, 0, array.Buffer.Size);

        newArray.Buffer.ReplaceBindingsOf(array.Buffer);

        array.Dispose();
        array = newArray;
    }

    public void BindAsShaderStorage(BufferRangeTarget target, int binding) => array.BindAsShaderStorage(target, binding);
    public void BindAsShaderStorage(BufferRangeTarget target, int binding, int start, int count) => array.BindAsShaderStorage(target, binding, start, count);

    void IDisposableExt.Free()
    {
        array.Dispose();
        GC.SuppressFinalize(this);
    }
    ~HalfList() => this.Dispose();
}