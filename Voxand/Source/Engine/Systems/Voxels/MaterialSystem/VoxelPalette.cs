using OpenTK.Graphics.OpenGL4;

using DisposableExt;
using GLAV.Types.Extended;

using Voxand.Engine.Systems.Voxels.VoxelMaterialServices;
using Voxand.Engine.Systems.Services.Voxels;

namespace Voxand.Engine.Systems.Voxels;
public class VoxelPalette : IVoxelPalette, IDisposableExt
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
    public DisposeState DisposeState { get; }

    public event Action<int, VoxelMaterial>? MaterialModified;

    public VoxelPalette(VoxelMaterial[] materials, int binding)
    {
        VoxelMaterialInternal[] internalMaterials = Array.ConvertAll(materials, material => new VoxelMaterialInternal(material));

        gpuMaterialPalette = new(internalMaterials, BufferTarget.ShaderStorageBuffer, BufferUsageHint.DynamicDraw);
        materialPalette = materials;
        Binding = binding;

        DisposeState = new(this);
    }
    public void SetMaterial(int index, VoxelMaterial newMaterial)
    {
        materialPalette[index] = newMaterial;
        gpuMaterialPalette[index] = new VoxelMaterialInternal(newMaterial);
        Console.WriteLine($"set to: {newMaterial.baseColor}; {newMaterial.baseColorVariance}; {newMaterial.emissionColor}; {newMaterial.emissionIntensity}");
        MaterialModified?.Invoke(index, newMaterial);
    }
    public VoxelMaterial GetMaterial(int index) => materialPalette[index];
    void IDisposableExt.Free()
    {
        gpuMaterialPalette.Dispose();
        GC.SuppressFinalize(this);
    }

    ~VoxelPalette() => this.Dispose();
}