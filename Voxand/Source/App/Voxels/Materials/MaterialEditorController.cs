using Voxand.Engine.Systems.Common;
using Voxand.Engine.Systems.Scripts;
using Voxand.Engine.Systems.Services.Voxels;
using Voxand.Engine.Systems.UI.Windows;

namespace Voxand.App.Voxels.Materials;
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
        voxelPalette = EngineServices.GetService<IVoxelPalette>();
        EngineServices.AddReplacementCallback<IVoxelPalette>((palette) => voxelPalette = palette);

        voxelMaterialEditor.OnMaterialEdited += (material) => voxelPalette.SetMaterial(EditedMaterialIndex, material);
    }
}