namespace Voxand.Engine.Systems.Voxels;
public interface IVoxelMapPersistence
{
    public void Export(Stream stream);
    public void Import(Stream stream);
    public event Action? OnMapImported;
    public event Action? OnMapExported;
}