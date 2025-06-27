using DisposableExt;
using OpenTK.Mathematics;

namespace Voxand.Engine.Systems.Voxels;
public abstract class VoxelMap : IDisposableExt
{
    public readonly Vector3i Dimensions;
    readonly IVoxelMapPersistence persistenceModule;
    public DisposeHelper DisposeHelper { get; }

    public VoxelMap(Vector3i dimensions, IVoxelMapPersistence persistenceModule)
    {
        Dimensions = dimensions;
        this.persistenceModule = persistenceModule;
        DisposeHelper = new(this);
    }
    public abstract void SetVoxelValue(Vector3i position, uint value);
    public abstract (uint, bool) GetVoxelValue(Vector3i position);
    public abstract bool IsSolid(Vector3i position);
    public void SaveMap(string mapName) => persistenceModule.Export(mapName);
    public void LoadMap(string mapName) => persistenceModule.Import(mapName);
    public abstract RaycastResult Raycast(Vector3 origin, Vector3 dir);
    public abstract long GetMemoryUsage();
    public abstract long GetGraphicsMemoryUsage();
    protected abstract void Free();
    void IDisposableExt.Free() => Free();
    ~VoxelMap() => this.Dispose();
}
public interface ISinglePlaceable
{
    public void PlaceSingle(Vector3i position, int material);
    public void RemoveSingle(Vector3i position);
}