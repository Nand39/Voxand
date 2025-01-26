using DisposableExt;
using OpenTK.Graphics.OpenGL4;
using Voxand.Engine.Systems.General.Events;
using Buffer = GLAV.Types.Buffer;

namespace Voxand.Engine.Systems.Voxels;

public class VoxelPalette : IDisposableExt
{
    readonly VoxelMaterial[] materialPalette;
    readonly Buffer materialPaletteSSBO;
    public int MaterialCount => materialPalette.Length;
    int bufferBinding;
    public int Binding
    {
        get => bufferBinding;
        set
        {
            bufferBinding = value;
            materialPaletteSSBO.BindAsShaderStorage(new(BufferRangeTarget.ShaderStorageBuffer, value));
        }
    }
    public DisposeHelper DisposeHelper { get; }
    public EventDispatcher Events { get; protected set; } = new("voxelPalette_events");

    public VoxelPalette(int materialCount, int binding)
    {
        DisposeHelper = new(this);
        materialPalette = new VoxelMaterial[materialCount];
        materialPaletteSSBO = new();
        materialPaletteSSBO.Lable = "voxel palette buffer";
        materialPaletteSSBO.Alloc(BufferTarget.ShaderStorageBuffer, MaterialCount * VoxelMaterial.sizeInBytes, BufferUsageHint.DynamicDraw);
        Binding = binding;
        Events.AddEvent("material_modified");
    }
    public VoxelPalette(VoxelMaterial[] materials, int binding) : this(materials.Length, binding)
    {
        materialPaletteSSBO.Store(materials, 0, materials.Length * VoxelMaterial.sizeInBytes, 0);
        materialPalette = materials;
    }
    public void SetMaterial(VoxelMaterial newMaterial, int index)
    {
        materialPalette[index] = newMaterial;
        materialPaletteSSBO.Store(ref newMaterial, index * VoxelMaterial.sizeInBytes);
        Events.Invoke("material_modified", index);
    }
    public void SetMaterial(object args)
    {
        (VoxelMaterial newMaterial, int index) materialEditedArgs = ((VoxelMaterial, int))args;
        SetMaterial(materialEditedArgs.newMaterial, materialEditedArgs.index);
    }
    public VoxelMaterial GetMaterial(int index)
    {
        return materialPalette[index];
    }
    void IDisposableExt.Free()
    {
        materialPaletteSSBO.Dispose();
        GC.SuppressFinalize(this);
    }
}