using OpenTK.Mathematics;
using Voxand.Helpers;

namespace Voxand.Engine.Systems.Voxels;
public abstract class VoxelMap : IDisposable
{
    public readonly Vector3i Dimensions;
    readonly IVoxelMapPersistence persistenceModule;

    bool disposed = false;
    public VoxelMap(Vector3i dimensions, IVoxelMapPersistence persistenceModule)
    {
        Dimensions = dimensions;
        this.persistenceModule = persistenceModule;
    }
    public abstract void SetVoxelValue(Vector3i position, uint value);
    public abstract (uint, bool) GetVoxelValue(Vector3i position);
    public abstract bool IsSolid(Vector3i position);
    public void SaveMap(string mapName) => persistenceModule.Export(mapName);
    public void LoadMap(string mapName) => persistenceModule.Import(mapName);
    public abstract DDAOut Raycast(Vector3 origin, Vector3 dir);
    public abstract void Free();
    public abstract long GetMemoryUsage();
    public abstract long GetGraphicsMemoryUsage();
    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        Free();
        GC.SuppressFinalize(this);
    }
    ~VoxelMap() => Dispose();
}