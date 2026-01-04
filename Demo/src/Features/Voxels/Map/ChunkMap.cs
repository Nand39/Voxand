using System.Text;

using OpenTK.Mathematics;

using DisposableExt;
using Voxand.Engine.Systems.Services.Voxels;
using Voxand.Engine.Systems.Voxels;
using Voxand.Helpers.ExtensionMethods;
using Sparkvox.src.Features.Voxels.Map.Generation;

namespace Sparkvox.src.Features.Voxels.Map;
public class ChunkMap : IVoxelMap, IVoxelMapVerticalChunks, IDisposableExt, IVoxelMapPersistence
{
    VoxelBrickmap voxelMap;
    public VoxelStructure RawStructure => voxelMap;
    public MapGenerator MapGenerator { get; set; }
    public Vector3i Dimensions { get; private set; }
    public Vector2i ChunkSize { get; private set; }
    public DisposeState DisposeState { get; }

    bool[,] chunkPresence;

    public event Action? OnMapImported;
    public event Action? OnMapExported;

    public ChunkMap(Vector2i numberOfChunks, Vector2i chunkSize, int heightInVoxels, VoxelBrickmap brickmap, BrickmapGenerator mapGenerator)
    {
        if (heightInVoxels <= 0)
            throw new ArgumentOutOfRangeException($"{nameof(heightInVoxels)} should be greater than zero.");
        if ((heightInVoxels & 3) > 0)
            throw new ArgumentException($"{nameof(heightInVoxels)} should be divisible by 4.");

        Dimensions = new(numberOfChunks.X * chunkSize.X, heightInVoxels, numberOfChunks.Y * chunkSize.Y);
        ChunkSize = chunkSize;

        voxelMap = brickmap;
        MapGenerator = mapGenerator;

        chunkPresence = new bool[numberOfChunks.X, numberOfChunks.Y];

        DisposeState = new(this);
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

    public unsafe void Export(Stream stream)
    {
        BinaryWriter writer = new BinaryWriter(stream, Encoding.UTF8, true);
        writer.Write(ChunkSize);
        writer.Write(Dimensions);

        fixed (bool* chunkPresencePtr = chunkPresence)
        {
            ReadOnlySpan<byte> chunkPresenceSpan = new(chunkPresencePtr, chunkPresence.Length * sizeof(bool));
            writer.Write(chunkPresenceSpan);
        }

        voxelMap.Export(stream);
    }

    public unsafe void Import(Stream stream)
    {
        BinaryReader reader= new BinaryReader(stream, Encoding.UTF8, true);
        ChunkSize = reader.Read<Vector2i>();
        Dimensions = reader.Read<Vector3i>();
        Vector2i numChunks = Dimensions.Xz / ChunkSize;

        chunkPresence = new bool[numChunks.X, numChunks.Y];

        fixed (bool* chunkPresencePtr = chunkPresence)
        {
            Span<byte> chunkPresenceSpan = new(chunkPresencePtr, chunkPresence.Length * sizeof(bool));
            reader.Read(chunkPresenceSpan);
        }

        voxelMap.Import(stream);
    }

    void IDisposableExt.Free()
    {
        voxelMap.Dispose();
    }
}