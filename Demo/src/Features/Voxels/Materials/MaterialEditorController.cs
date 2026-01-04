using Sparkvox.src.Services;
using Voxand.Engine.Systems.Scripts;
using Voxand.Engine.Systems.Services.Voxels;
using Voxand.Engine.Systems.UI.Windows;

namespace Sparkvox.src.Features.Voxels.Materials;
public class MaterialEditorController : Script
{
    public required IVoxelMaterialEditor MaterialEditor { init => voxelMaterialEditor = value; }

    IVoxelMaterialEditor voxelMaterialEditor;
    IVoxelPalette voxelPalette;

    int editedMaterialIndex = 0;
    public int EditedMaterialIndex
    {
        get => editedMaterialIndex;
        set
        {
            editedMaterialIndex = value;
            voxelMaterialEditor.SetEditedMaterial(voxelPalette.GetMaterial(editedMaterialIndex));
        }
    }
    public override void Initialize()
    {
        voxelPalette = ServiceLocator.GetService<IVoxelPalette>();
        ServiceLocator.AddReplacementCallback<IVoxelPalette>((palette) => voxelPalette = palette);

        voxelMaterialEditor.OnMaterialEdited += (material) => voxelPalette.SetMaterial(EditedMaterialIndex, material);
    }
}