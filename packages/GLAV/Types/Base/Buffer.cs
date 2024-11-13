using System.Runtime.CompilerServices;

using OpenTK.Graphics.OpenGL4;

using GLAV.Systems;

namespace GLAV.Types;
public readonly struct BufferDestination(BufferRangeTarget target, int index)
{
    public readonly BufferRangeTarget target = target;
    public readonly int index = index;
}

public class Buffer : GLResource
{
    public int Size { get; protected set; } = 0;
    public BufferTarget bufferTarget { get; protected set; }
    public BufferDestination destination { get; protected set; }
    public Buffer()
    {
        Handle.resourceType = GLResourceType.Buffer;
        Handle.id = GL.GenBuffer();
    }

    public void Alloc(BufferTarget bufferTarget, int size, BufferUsageHint usageHint)
    {
        Size = size;
        Bind(bufferTarget);
        GL.BufferData(bufferTarget, size, IntPtr.Zero, usageHint);
    }
    public void Alloc<T>(BufferTarget bufferTarget, ref T data, int size, BufferUsageHint usageHint) 
        where T : struct
    {
        Size = size;
        Bind(bufferTarget);
        GL.BufferData(this.bufferTarget, size, ref data, usageHint);
    }
    public unsafe void Alloc<T>(BufferTarget bufferTarget, ref T[] data, int size, BufferUsageHint usageHint) 
        where T : struct
    {
        Size = size;
        Bind(bufferTarget);
        GL.BufferData(this.bufferTarget, size, data, usageHint);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Bind() => GLRegistry.BindBuffer(bufferTarget, Handle.id);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Bind(BufferTarget target) 
    {
        bufferTarget = target;
        Bind();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe void Store<T>(ref T[] data, int readingOffsetIndex, int count, int writingOffsetInBytes)
        where T : struct
    {
        Bind();
        fixed (T* readStart = &data[readingOffsetIndex])
            GL.BufferSubData(bufferTarget, writingOffsetInBytes, count * sizeof(T), (nint)readStart);
    }
    public unsafe void Store<T>(ref T data, int offsetInBytes)
        where T : struct
    {
        Bind();
        GL.BufferSubData(bufferTarget, offsetInBytes, sizeof(T), ref data);
    }
    public unsafe void Store(nint dataPtr, int offsetInBytes, int size)
    {
        Bind();
        GL.BufferSubData(bufferTarget, offsetInBytes, size, dataPtr);
    }
    public unsafe void LogContent<Format>() where Format : struct
    {
        int itemSize = sizeof(Format);
        int count = Size / itemSize;
        LogContent<Format>(0, count);
        int bytesOmitted = Size - (count * itemSize);
        if (bytesOmitted > 0)
            Console.WriteLine($"! {bytesOmitted} bytes omitted");
    }
    public void LogContent<Format>(int startOffsetInBytes, int count) where Format : struct
    {
        int itemSize; unsafe { itemSize = sizeof(Format); }
        Format[] data = Retrieve<Format>(startOffsetInBytes, count);

        Console.WriteLine($"# Content of {Lable} from {startOffsetInBytes} to {startOffsetInBytes + count * itemSize} ({count} items)");
        for (int i = 0; i < data.Length; i++)
        {
            Console.WriteLine(data[i]);
        }
        Console.WriteLine("# End of buffer content log");
    }
    public void CopyTo(Buffer destinationBuffer, int readingOffsetInBytes, int writingOffsetInBytes, int size)
    {
        BufferTarget sourceOriginalTarget = bufferTarget;
        BufferTarget destinationOriginalTarget = destinationBuffer.bufferTarget;

        Bind(BufferTarget.CopyReadBuffer);
        destinationBuffer.Bind(BufferTarget.CopyWriteBuffer);

        GL.CopyBufferSubData(BufferTarget.CopyReadBuffer, BufferTarget.CopyWriteBuffer, readingOffsetInBytes, writingOffsetInBytes, size);

        Bind(sourceOriginalTarget);
        destinationBuffer.Bind(destinationOriginalTarget);
    }

    public unsafe T[] Retrieve<T>(int readingOffsetInBytes, int count) where T : struct
    {
        Bind();
        int size = sizeof(T) * count;
        T[] output = new T[count];
        fixed (T* outputPtr = output)
        {
            GL.GetBufferSubData(bufferTarget, readingOffsetInBytes, size, (nint)outputPtr);
        }
        return output;
    }
    public unsafe T Retrieve<T>(int readingOffsetInBytes) where T : struct
    {
        Bind();
        int size = sizeof(T);
        T output = new T();
        GL.GetBufferSubData(bufferTarget, readingOffsetInBytes, size, (nint)(&output));
        return output;
    }
    public unsafe T Retrieve<T>(int y, int z, int x, int dimX, int dimZ) where T : struct
    {
        int index = y * dimZ * dimX + z * dimX + x;
        return Retrieve<T>(index * sizeof(T));
    }

    public void BindBufferBase(BufferDestination destination)
    {
        this.destination = destination;
        GL.BindBufferBase(destination.target, destination.index, Handle.id);
    }

    public override void Free()
    {
        GL.DeleteBuffer(Handle.id);
    }
}