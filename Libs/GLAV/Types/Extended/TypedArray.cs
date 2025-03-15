using OpenTK.Graphics.OpenGL4;

namespace GLAV.Types.Extended;
public class TypedArray<T> where T : struct
{
    public Buffer Buffer { get; protected set; }

    static int itemSize;

    static unsafe TypedArray() => itemSize = sizeof(T);

    public unsafe TypedArray(BufferTarget target, int capacity, BufferUsageHint usageHint)
    {
        Buffer = new();
        Buffer.Alloc(target, capacity * sizeof(T), usageHint);
    }

    public unsafe TypedArray(T[] data, BufferTarget target, BufferUsageHint usageHint)
    {
        Buffer = new();
        Buffer.Alloc(target, data, data.Length * itemSize, usageHint);
    }
    public unsafe TypedArray(Span<T> data, BufferTarget target, BufferUsageHint usageHint)
    {
        Buffer = new();
        Buffer.Alloc(target, data, data.Length * itemSize, usageHint);
    }

    public unsafe T this[int index]
    {
        get
        {
            return Buffer.Retrieve<T>(index * itemSize);
        }
        set
        {
            Buffer.Store(ref value, index * itemSize);
        }
    }

    public unsafe void Store(Span<T> data, int offset)
    {
        Buffer.Store(data, offset * itemSize);
    }
}