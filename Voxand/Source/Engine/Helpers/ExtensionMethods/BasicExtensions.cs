using System.Runtime.InteropServices;

namespace Voxand.Helpers.ExtensionMethods;
public static class BasicExtensions
{
    public static void ReplaceFirst<T>(this T[] array, T target, T replacement)
    {
        int index = Array.IndexOf(array, target);
        if (index != -1)
        {
            array[index] = replacement;
        }
    }

    public static void Write<T>(this BinaryWriter bw, T val) 
        where T : struct => bw.Write(MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref val, 1)));

    public static unsafe T Read<T>(this BinaryReader br) where T : struct
    {
        int size = sizeof(T);
        Span<byte> buffer = size < 512 ? stackalloc byte[size] : new byte[size];
        int bytesRead = br.Read(buffer);
        if (bytesRead < size)
            throw new EndOfStreamException($"Not enough bytes to read struct of type {typeof(T).Name}. Expected {size}, got {bytesRead}.");
        return MemoryMarshal.Read<T>(buffer);
    }
}