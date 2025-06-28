using DisposableExt;

using Voxand.Engine.Systems.Voxels.VoxelMaterialServices;

namespace Voxand.Engine.Systems.Services.Voxels;
public interface IVoxelPalette : IDisposableExt
{
    int MaterialCount { get; }
    void SetMaterial(int index, VoxelMaterial newMaterial);
    VoxelMaterial GetMaterial(int index);
    event Action<int, VoxelMaterial>? MaterialModified;
}