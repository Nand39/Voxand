using OpenTK.Graphics.OpenGL4;

using DisposableExt;
using GLAV.Types.Extended;

using Voxand.Engine.Systems.General.Events;
using Voxand.Engine.Systems.Voxels.VoxelMaterialServices;

namespace Voxand.Engine.Systems.Voxels;
public class VoxelPalette : IDisposableExt
{
    readonly VoxelMaterial[] materialPalette;
    readonly TypedArray<VoxelMaterialInternal> gpuMaterialPalette;
    public int MaterialCount => materialPalette.Length;
    int bufferBinding;
    public int Binding
    {
        get => bufferBinding;
        set
        {
            bufferBinding = value;
            gpuMaterialPalette.BindAsShaderStorage(BufferRangeTarget.ShaderStorageBuffer, value);
        }
    }
    public DisposeHelper DisposeHelper { get; }
    public EventDispatcher Events { get; protected set; } = new("voxelPalette_events");

    public VoxelPalette(VoxelMaterial[] materials, int binding)
    {
        VoxelMaterialInternal[] internalMaterials = Array.ConvertAll(materials, material => new VoxelMaterialInternal(material));

        gpuMaterialPalette = new(internalMaterials, BufferTarget.ShaderStorageBuffer, BufferUsageHint.DynamicDraw);
        materialPalette = materials;
        Binding = binding;

        DisposeHelper = new(this);
        Events.AddEvent("material_modified");
    }
    public void SetMaterial(VoxelMaterial newMaterial, int index)
    {
        materialPalette[index] = newMaterial;
        gpuMaterialPalette[index] = new VoxelMaterialInternal(newMaterial);
        Events.Invoke("material_modified", index);
    }
    public void SetMaterial(object args)
    {
        (VoxelMaterial newMaterial, int index) materialEditedArgs = ((VoxelMaterial, int))args;
        SetMaterial(materialEditedArgs.newMaterial, materialEditedArgs.index);
    }
    public VoxelMaterial GetMaterial(int index) => materialPalette[index];
    void IDisposableExt.Free()
    {
        gpuMaterialPalette.Dispose();
        GC.SuppressFinalize(this);
    }

    ~VoxelPalette() => this.Dispose();
}