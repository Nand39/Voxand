using GLAV.Types;
using OpenTK.Mathematics;

namespace GLAV.Helpers.Public.Exceptions;
public static class ExceptionConstructor
{
    /// <summary>
    /// Throws <see cref="ArgumentException"/> if the dimensions of <paramref name="tex1"/> do not match <paramref name="tex2"/>.
    /// </summary>
    public static void ThrowIfTextureSizeNotEqual(MutableTexture2D tex1, MutableTexture2D tex2)
    {
        if (tex1.Size != tex2.Size)
            throw new ArgumentException($"Textures {tex1} and {tex2} are not of the same size.");
    }
    /// <summary>
    /// Throws <see cref="ArgumentException"/> if dimensions of any <see cref="MutableTexture2D"/> in <paramref name="textures"/> 
    /// are not equal to the dimensions of the first texture in the array. Requires at least 2 textures.
    /// </summary>
    /// <exception cref="InvalidOperationException"/>
    public static void ThrowIfTextureSizeNotEqual(params MutableTexture2D[] textures)
    {
        if (textures is null || textures.Length < 2)
            throw new InvalidOperationException("Must receive at least two textures to compare sizes.");

        Vector2i targetSize = textures[0].Size;
        for (int i = 0; i < textures.Length; i++)
        {
            if (textures[i].Size != targetSize)
                throw new ArgumentException($"Textures {textures[0]} and {textures[i]} are not of the same size.");
        }
    }
    /// <summary>
    /// Throws <see cref="ArgumentException"/> if any dimension of <paramref name="tex"/> 
    /// is not divisible by <paramref name="number"/>
    /// </summary>
    public static void ThrowIfTextureSizeNotDivisible(MutableTexture2D tex, int number)
    {
        if ((tex.Size.X + tex.Size.Y) % number != 0)
            throw new ArgumentException($"The size {tex.Size} of the texture {tex} is not multiple of {number}.");
    }
}