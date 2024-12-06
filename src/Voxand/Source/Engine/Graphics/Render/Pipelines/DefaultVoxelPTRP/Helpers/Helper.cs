using System.Runtime.CompilerServices;

using GLAV.Types;

using Voxand.Engine.Graphics.Tools.Exceptions;

namespace Voxand.Engine.Graphics.Pipelines.DefaultVoxelPTRP.Helpers;

public static class VoxelPTRPHelper
{
    /// <summary>
    /// Checks texture for validity in the context of <see cref="VoxelPTRP"/>. 
    /// Checks for null reference and throws <see cref="ArgumentException"/> if dimensions of a texture are not divisible by 8.
    /// </summary> 
    /// <exception cref="ArgumentNullException"/> 
    /// <exception cref="ArgumentException"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfTextureInvalid(Texture2D texture)
    {
        ArgumentNullException.ThrowIfNull(texture);
        ExceptionConstructor.ThrowIfTextureSizeNotDivisible(texture, 8);
    }

    /// <summary>
    /// For each texture calls <see cref="ThrowIfTextureInvalid(Texture2D)"/>.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfAnyTextureInvalid(params Texture2D[] textures)
    {
        foreach (Texture2D texture in textures)
            ThrowIfTextureInvalid(texture);
    }
}