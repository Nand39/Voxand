using DisposableExt;
using OpenTK.Mathematics;
using Voxand.App.Voxels.Map.Generation;
using Voxand.Engine.Systems.Services.Voxels;
using Voxand.Engine.Systems.Voxels;
using Voxand.Helpers.ExtensionMethods;

namespace Voxand.App.Voxels.Map;

public class ChunkMap : IVoxelMap, IVoxelMapVerticalChunks, IDisposableExt
{
    VoxelBrickmap voxelMap;
    public VoxelStructure RawStructure => voxelMap;
    public MapGenerator MapGenerator { get; set; }
    public Vector3i Dimensions { get; private set; }

    public DisposeHelper DisposeHelper { get; }

    bool[,] chunkPresence;
    public ChunkMap(Vector2i numberOfChunks, Vector2i chunkSize, int height, VoxelBrickmap brickmap, BrickmapGenerator mapGenerator)
    {
        if (height <= 0)
            throw new ArgumentOutOfRangeException($"{nameof(height)} should be greater than zero.");
        if ((height & 3) > 0)
            throw new ArgumentException($"{nameof(height)} should be divisible by 4.");

        Dimensions = new(numberOfChunks.X * chunkSize.X, height, numberOfChunks.Y * chunkSize.Y);

        voxelMap = brickmap;
        MapGenerator = mapGenerator;

        chunkPresence = new bool[numberOfChunks.X, numberOfChunks.Y];

        DisposeHelper = new(this);
    }

    public bool IsChunkLoaded(Vector2i position) => chunkPresence[position.X, position.Y];
    public void StartLoadingChunkIfUnloaded(Vector2i position)
    {
        if (!position.Inbounds(Vector2i.Zero, voxelMap.Dimensions.Xz))
            throw new ArgumentOutOfRangeException(nameof(position), "Cannot load chunk outside of the map.");

        if (!IsChunkLoaded(position))
            Task.Run(() => GenerateChunk(position));
    }
    void GenerateChunk(Vector2i position)
    {
        MapGenerator.GenerateChunk(new Vector3i(position.X, 0, position.Y));
        chunkPresence[position.X, position.Y] = true;
    }

    void IDisposableExt.Free()
    {
        voxelMap.Dispose();
    }
}