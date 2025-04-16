using OpenTK.Graphics.OpenGL4;

using DisposableExt;
using GLAV.Types.Extended;

using Voxand.Engine.Systems.General.Events;
using Voxand.Helpers.ExtensionMethods;

namespace Voxand.Engine.Systems.Voxels;

public class VoxelPalette : IDisposableExt
{
    readonly VoxelMaterial[] materialPalette;
    readonly TypedArray<VoxelMaterial> gpuMaterialPalette;
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

    VoxelPalette()
    {
        DisposeHelper = new(this);
        Events.AddEvent("material_modified");
    }
    public VoxelPalette(VoxelMaterial[] materials, int binding) 
        : this()
    {
        VoxelMaterial[] transformedMaterials = new VoxelMaterial[materials.Length];
        for (int i = 0; i < transformedMaterials.Length; i++)
        {
            transformedMaterials[i] = materials[i];
            transformedMaterials[i].color = transformedMaterials[i].color.Pow(2.2f) / MathF.PI;
        }

        gpuMaterialPalette = new(transformedMaterials, BufferTarget.ShaderStorageBuffer, BufferUsageHint.DynamicDraw);
        materialPalette = materials;
        Binding = binding;
    }
    public void SetMaterial(VoxelMaterial newMaterial, int index)
    {
        materialPalette[index] = newMaterial;
        newMaterial.color = newMaterial.color.Pow(2.2f) / MathF.PI;
        gpuMaterialPalette[index] = newMaterial;
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
        gpuMaterialPalette.Dispose();
        GC.SuppressFinalize(this);
    }
}