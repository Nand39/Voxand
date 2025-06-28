using OpenTK.Mathematics;

using DisposableExt;

using Voxand.App.Voxels.Map.Generation;
using Voxand.Engine.Systems.Voxels;

namespace Voxand.Engine.Systems.Services.Voxels;
interface IVoxelMap : IDisposableExt
{
    Vector3i Dimensions { get; }
    VoxelStructure RawStructure { get; }
    MapGenerator MapGenerator { get; }
}