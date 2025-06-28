namespace Voxand.Engine.Systems.Services.UI.Windows;
public interface IVoxelMaterialSelector
{
    event Action<int>? OnMaterialSelected;
}