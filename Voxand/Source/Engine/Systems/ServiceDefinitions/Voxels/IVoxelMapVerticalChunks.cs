using OpenTK.Mathematics;

namespace Voxand.Engine.Systems.Services.Voxels;
interface IVoxelMapVerticalChunks
{
    bool IsChunkLoaded(Vector2i position);
    void StartLoadingChunkIfUnloaded(Vector2i position);
}