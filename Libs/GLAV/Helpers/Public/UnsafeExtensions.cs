using GLAV.Types.Extended;

namespace GLAV.Helpers.Public.Extensions.Unsafe;

public static class ListExtensions
{
    public static void Write<T>(this HalfList<T> list, nint dataPtr, int index, int offset, int size) where T : struct
    {
        list.array.Buffer.Store(index * list.itemSize + offset, dataPtr, size);
    }
}