using OpenTK.Graphics.OpenGL4;

using GLAV.Types;

using Buffer = GLAV.Types.Buffer;

using Voxand.Helpers;

namespace Voxand.Engine.Systems.Voxels;

public class VoxelPalette 
{
    VoxelMaterial[] materialPalette =
        [
        new(new(0.8f, 0.8f, 0.8f), new(0, 0, 0)),
        new(Util.Hex2Vec("#376e47"), new(0, 0, 0)),
        new(Util.Hex2Vec("#f5f2a6"), new(0, 0, 0)),
        new(Util.Hex2Vec("#78664e"), new(0, 0, 0)),

        new(new(0.8f, 0.8f, 0.8f), new(2, 1.85f, 1)),
        new(new(0.8f, 0.8f, 0.8f), new(1, 0, 0)),
        new(new(0.8f, 0.8f, 0.8f), new(0, 1, 0)),
        new(new(0.8f, 0.8f, 0.8f), new(0, 0, 1)),
        ];
    Buffer materialPaletteSSBO;
    public int PaletteLength { get { return materialPalette.Length; } }

    public VoxelPalette()
    {
        materialPaletteSSBO = new();
        materialPaletteSSBO.Alloc(BufferTarget.ShaderStorageBuffer, ref materialPalette, materialPalette.Length * VoxelMaterial.sizeInBytes, BufferUsageHint.DynamicDraw);
        materialPaletteSSBO.BindBufferBase(new(BufferRangeTarget.ShaderStorageBuffer, 2));
    }
    public void SetMaterial(int index, VoxelMaterial newMaterial)
    {
        materialPalette[index] = newMaterial;
        materialPaletteSSBO.Store(ref newMaterial, index * VoxelMaterial.sizeInBytes);
    }
    public VoxelMaterial GetMaterial(int index)
    {
        return materialPalette[index];
    }
}