namespace Voxand.Engine.Systems.Voxels;
public interface IVoxelMapPersistence
{
    public void Export(string mapName);
    public void Import(string mapName);
}

public class VoxelBrickmapDefaultPersistenceModule : IVoxelMapPersistence
{
    public void Export(string mapName)
    {
        throw new NotImplementedException();
    }

    public void Import(string mapName)
    {
        throw new NotImplementedException();
    }
}