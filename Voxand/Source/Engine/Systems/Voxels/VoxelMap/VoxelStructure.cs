using DisposableExt;
using OpenTK.Mathematics;

namespace Voxand.Engine.Systems.Voxels;
public abstract class VoxelStructure : IDisposableExt
{
    protected Vector3i dimensions;
    public Vector3i Dimensions => dimensions; 
    public DisposeState DisposeState { get; }

    public VoxelStructure(Vector3i dimensions)
    {
        this.dimensions = dimensions;
        DisposeState = new(this);
    }
    public abstract void SetVoxelValue(Vector3i position, uint value);
    public abstract (uint, bool) GetVoxelValue(Vector3i position);
    public abstract bool IsSolid(Vector3i position);
    public abstract RaycastResult Raycast(Vector3 origin, Vector3 dir);
    public abstract long GetMemoryUsage();
    public abstract long GetGraphicsMemoryUsage();
    protected abstract void Free();
    void IDisposableExt.Free() => Free();
    ~VoxelStructure() => this.Dispose();
}
public interface ISinglePlaceable
{
    public void PlaceSingle(Vector3i position, int material);
    public void RemoveSingle(Vector3i position);
}