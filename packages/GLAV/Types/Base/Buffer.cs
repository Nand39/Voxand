using System.Runtime.CompilerServices;

using OpenTK.Graphics.OpenGL4;

using GLAV.Systems;
using System.Text;

namespace GLAV.Types;
public readonly struct BufferBindingInfo(BufferRangeTarget target, int index)
{
    public readonly BufferRangeTarget target = target;
    public readonly int index = index;
}

public class Buffer : GLResource
{
    public int Size { get; protected set; } = 0;
    public BufferTarget bufferTarget { get; protected set; }
    public BufferBindingInfo bindingInfo { get; protected set; }
    public Buffer()
    {
        Handle.resourceType = GLResourceType.Buffer;
        Handle.id = GL.GenBuffer();
    }

    public void Alloc(BufferTarget bufferTarget, int size, BufferUsageHint usageHint)
    {
        Size = size;
        Use(bufferTarget);
        GL.BufferData(bufferTarget, size, IntPtr.Zero, usageHint);
    }
    public void Alloc<T>(BufferTarget bufferTarget, ref T data, int size, BufferUsageHint usageHint) 
        where T : struct
    {
        Size = size;
        Use(bufferTarget);
        GL.BufferData(this.bufferTarget, size, ref data, usageHint);
    }
    public unsafe void Alloc<T>(BufferTarget bufferTarget, ref T[] data, int size, BufferUsageHint usageHint) 
        where T : struct
    {
        Size = size;
        Use(bufferTarget);
        GL.BufferData(this.bufferTarget, size, data, usageHint);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Use() => GLRegistry.Instance.BindBuffer(bufferTarget, Handle.id);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Use(BufferTarget target) 
    {
        bufferTarget = target;
        Use();
    }
    public unsafe void Store(int offsetInBytes, nint dataPtr, int size)
    {
        Use();
        GL.BufferSubData(bufferTarget, offsetInBytes, size, dataPtr);
    }
    public unsafe void Store<T>(ref T data, int offsetInBytes)
        where T : struct
    {
        Use();
        GL.BufferSubData(bufferTarget, offsetInBytes, sizeof(T), ref data);
    }
    public unsafe void Store<T>(T[] data, int readingOffsetInBytes, int sizeInBytes, int writingOffsetInBytes)
        where T : struct
    {
        Use();
        fixed (T* arrayStart = &data[0])
            GL.BufferSubData(bufferTarget, writingOffsetInBytes, sizeInBytes, (nint)arrayStart + readingOffsetInBytes);
    }
    public unsafe void LogContent<Format>() where Format : struct
    {
        int itemSize = sizeof(Format);
        int count = Size / itemSize;
        string contentLog = GetContentLog<Format>(0, count);
        Console.WriteLine(contentLog);

        int bytesOmitted = Size - (count * itemSize);
        if (bytesOmitted > 0)
            Console.WriteLine($"{bytesOmitted} bytes omitted!");
    }
    public string GetContentLog<Format>(int startOffsetInBytes, int count) where Format : struct
    {
        int itemSize; unsafe { itemSize = sizeof(Format); }
        Format[] data = Retrieve<Format>(startOffsetInBytes, count);

        string log = "";
        StringBuilder stringBuilder = new();
        stringBuilder.Append($"Content of {Lable} from {startOffsetInBytes} to {startOffsetInBytes + count * itemSize} ({count} items)\n");
        for (int i = 0; i < data.Length; i++)
        {
            stringBuilder.Append($"{data[i]}\n");
        }
        stringBuilder.Append("End of buffer content log.");
        return stringBuilder.ToString();
    }
    public void CopyTo(Buffer destinationBuffer, int readingOffsetInBytes, int writingOffsetInBytes, int size)
    {
        BufferTarget sourceOriginalTarget = bufferTarget;
        BufferTarget destinationOriginalTarget = destinationBuffer.bufferTarget;

        Use(BufferTarget.CopyReadBuffer);
        destinationBuffer.Use(BufferTarget.CopyWriteBuffer);

        GL.CopyBufferSubData(BufferTarget.CopyReadBuffer, BufferTarget.CopyWriteBuffer, readingOffsetInBytes, writingOffsetInBytes, size);

        Use(sourceOriginalTarget);
        destinationBuffer.Use(destinationOriginalTarget);
    }

    public unsafe T[] Retrieve<T>(int readingOffsetInBytes, int count) where T : struct
    {
        Use();
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
        int size = sizeof(T);
        if (readingOffsetInBytes + size > Size)
            throw new ArgumentOutOfRangeException(
                paramName: nameof(readingOffsetInBytes),
                actualValue: readingOffsetInBytes,
                message: $"{nameof(readingOffsetInBytes)} + size of the type to retrieve ({typeof(T)}; size = {size}) " +
                         $"should not exceed buffer bounds. Tried accessing bytes from {readingOffsetInBytes} to {readingOffsetInBytes + size} out of {Size}.");

        T output = new T();
        Use();
        GL.GetBufferSubData(bufferTarget, readingOffsetInBytes, size, (nint)(&output));
        return output;
    }
    public unsafe T Retrieve<T>(int y, int z, int x, int dimX, int dimZ) where T : struct
    {
        int index = y * dimZ * dimX + z * dimX + x;
        return Retrieve<T>(index * sizeof(T));
    }

    public void BindEntireBuffer(BufferBindingInfo bindingInfo)
    {
        this.bindingInfo = bindingInfo;
        GL.BindBufferBase(bindingInfo.target, bindingInfo.index, Handle.id);
    }

    protected override void Free(bool hasContext)
    {
        if (hasContext)
        {
            GL.DeleteBuffer(Handle.id);
            return;
        }
        GLRegistry.Instance.ScheduleAction(() => GL.DeleteBuffer(Handle.id));
    }
}