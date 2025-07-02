using System.Runtime.CompilerServices;

using OpenTK.Mathematics;

using GLAV;

using Voxand.Engine.Systems.Voxels;
using Voxand.Helpers;

namespace Voxand.App.Voxels.Map.Generation;
public class BrickmapGenerator : MapGenerator
{
    public VoxelStructure map;
    NoiseAdapter noiseLarge, noiseSmall;
    Vector2 cellSize;
    Vector2 secondaryCellSize;
    int height;
    const int SECONDARY_NOISE_FREQUENCY = 5;
    public Vector2 CellSize
    {
        get => cellSize;
        set
        {
            if (cellSize != value)
            {
                cellSize = value;
                secondaryCellSize = (Vector2i)(cellSize / SECONDARY_NOISE_FREQUENCY);
                RegenerateNoise();
            }
        }
    }
    public BrickmapGenerator(VoxelStructure map, Vector2 cellSize, int height)
    {
        this.map = map;
        CellSize = cellSize;
        this.height = height;
        RegenerateNoise();
    }
    public void RegenerateNoise()
    {
        Vector2i gridSize = (Vector2i)((Vector2)map.Dimensions.Xz / cellSize) + Vector2i.One * 2;
        noiseLarge = new(gridSize, cellSize);
        gridSize = (Vector2i)((Vector2)map.Dimensions.Xz / secondaryCellSize) + Vector2i.One * 2;
        noiseSmall = new(gridSize, secondaryCellSize);
    }
    public override void GenerateAll()
    {
        Vector2i chunks = new(map.Dimensions.X >> 2, map.Dimensions.Z >> 2);
        for (int z = 0; z < chunks.Y; z++)
        {
            for (int x = 0; x < chunks.X; x++)
            {
                int xc = x;
                int zc = z;
                Task.Run(() => GenerateChunk(new(xc, 0, zc)));
            }
        }
    }
    public unsafe override void GenerateChunk(Vector3i position)
    {
        int[,] heights = new int[4, 4];
        int maxHeight = 0;

        Vector3i voxPos = Vector3i.Zero;

        for (voxPos.Z = position.Z << 2; voxPos.Z < (position.Z << 2) + 4; voxPos.Z++)
        {
            for (voxPos.X = position.X << 2; voxPos.X < (position.X << 2) + 4; voxPos.X++)
            {
                float noiseVal = noiseLarge.Sample(new(voxPos.X + 0.5f, voxPos.Z + 0.5f)) * 0.5f + 0.25f;
                noiseVal += noiseSmall.Sample(new(voxPos.X + 0.5f, voxPos.Z + 0.5f)) * 0.15f + 0.25f;

                int localHeight = (int)(noiseVal * height);
                localHeight = localHeight > map.Dimensions.Y ? map.Dimensions.Y : localHeight;
                heights[voxPos.X & 3, voxPos.Z & 3] = localHeight;
                maxHeight = localHeight > maxHeight ? localHeight : maxHeight;
            }
        }

        int numberOfBricks = (maxHeight >> 2) + 1;
        VoxelBrickValues* brickValues = stackalloc VoxelBrickValues[numberOfBricks];
        VoxelBrickOccupancy* brickOccupancies = stackalloc VoxelBrickOccupancy[numberOfBricks];

        for (voxPos.Z = 0; voxPos.Z < 4; voxPos.Z++)
        {
            for (voxPos.X = 0; voxPos.X < 4; voxPos.X++)
            {
                voxPos.Y = 0;
                int height = heights[voxPos.X, voxPos.Z];
                for (; voxPos.Y < height - 5; voxPos.Y++)
                {
                    PlaceVoxel(brickValues, brickOccupancies, voxPos, 0);
                }
                for (; voxPos.Y < height - 2; voxPos.Y++)
                {
                    PlaceVoxel(brickValues, brickOccupancies, voxPos, 1);
                }
                height -= Util.Random.Next(0, 2);
                for (; voxPos.Y < height; voxPos.Y++)
                {
                    PlaceVoxel(brickValues, brickOccupancies, voxPos, 2);
                }
            }
        }

        for (int i = 0; i < numberOfBricks; i++)
        {
            if (brickOccupancies[i].Bitmask != 0)
                ScheduleBrick(new(position.X, i, position.Z), brickValues[i], brickOccupancies[i]);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    unsafe void PlaceVoxel(VoxelBrickValues* brickValues, VoxelBrickOccupancy* brickOccupancy, Vector3i position, uint material)
    {
        int brickIndex = position.Y >> 2;
        Vector3i localPosition = new(position.X, position.Y & 3, position.Z);
        brickValues[brickIndex].SetVoxelValue(localPosition, material);
        brickOccupancy[brickIndex].SetVoxelBit(localPosition, true);
    }
    void ScheduleBrick(Vector3i pos, VoxelBrickValues values, VoxelBrickOccupancy occupancy)
    {
        GLRegistry.Instance.ScheduleAction(() =>
        {
            ((VoxelBrickmap)map).SetBrick(pos, values, occupancy);
        });
    }
}