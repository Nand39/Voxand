using OpenTK.Mathematics;

using DisposableExt;
using Voxand.Engine.Systems.Voxels;

namespace Voxand.Engine.Systems.Services.Voxels;
public interface IVoxelMap : IDisposableExt
{
    Vector3i Dimensions { get; }
    VoxelStructure RawStructure { get; }
}