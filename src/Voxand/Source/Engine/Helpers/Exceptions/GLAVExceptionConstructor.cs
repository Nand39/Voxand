using OpenTK.Mathematics;

using GLAV.Types;

namespace Voxand.Helpers.Exceptions.GLAVExceptions;
public static class ExceptionConstructor
{
    public static void ThrowIfTextureSizeNotEqual(Texture2D tex1, Texture2D tex2)
    {
        if (tex1.Size != tex2.Size)
            throw new ArgumentException($"Textures {tex1} and {tex2} are not of the same size.");
    }
    /// <summary>
    /// Throws <see cref="ArgumentException"/> if dimensions of any <see cref="Texture2D"/> in <paramref name="textures"/> 
    /// are not equal to the dimensions of the first texture in the array. Requires at least 2 textures.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown if length of <paramref name="textures"/> array is less that two.
    /// </exception>
    public static void ThrowIfTextureSizeNotEqual(params Texture2D[] textures)
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
    public static void ThrowIfTextureSizeNotDivisible(Texture2D tex, int number)
    {
        if ((tex.Size.X + tex.Size.Y) % number != 0)
            throw new ArgumentException($"The size {tex.Size} of the texture {tex} is not multiple of {number}.");
    }
}