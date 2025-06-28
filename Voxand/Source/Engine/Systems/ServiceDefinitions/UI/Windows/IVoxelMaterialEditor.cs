using Voxand.Engine.Systems.Voxels.VoxelMaterialServices;

namespace Voxand.Engine.Systems.UI.Windows;
public interface IVoxelMaterialEditor
{
    event Action<VoxelMaterial>? OnMaterialEdited;
    void SetEditedMaterial(VoxelMaterial material);
}