using OpenTK.Mathematics;

namespace Sparkvox.src.Features.Voxels.Map;
public interface IVoxelMapVerticalChunks
{
    bool IsChunkLoaded(Vector2i position);
    void StartLoadingChunkIfUnloaded(Vector2i position);
}