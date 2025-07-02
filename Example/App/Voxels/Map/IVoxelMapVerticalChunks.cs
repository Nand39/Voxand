using OpenTK.Mathematics;

namespace Voxand.App.Voxels.Map;
public interface IVoxelMapVerticalChunks
{
    bool IsChunkLoaded(Vector2i position);
    void StartLoadingChunkIfUnloaded(Vector2i position);
}