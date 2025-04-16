using System.Runtime.CompilerServices;

using OpenTK.Mathematics;

using GLAV;

using Voxand.Engine.Systems.Voxels;
using Voxand.Helpers;

namespace Voxand.App.Map.Generation;
public class BrickmapGenerator : MapGenerator
{
    public VoxelMap map;
    NoiseAdapter noiseLarge, noiseSmall, mudNoise;
    Vector2 cellSize;
    int height;
    public Vector2 CellSize
    {
        get => cellSize;
        set
        {
            if (cellSize != value)
            {
                cellSize = value;
                RegenerateNoise();
            }
        }
    }
    public BrickmapGenerator(VoxelMap map, Vector2 cellSize, int height)
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
        noiseSmall = new(gridSize * 3, cellSize / 3);
        mudNoise = new(gridSize * 4, cellSize / 4);
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
                float noiseVal = noiseLarge.Sample(new(voxPos.X + 0.5f, voxPos.Z + 0.5f)) * 0.5f + 0.5f;
                noiseVal += noiseSmall.Sample(new(voxPos.X + 0.5f, voxPos.Z + 0.5f)) * 0.25f + 0.25f;

                int localHeight = (int)(noiseVal * height);
                heights[voxPos.X & 3, voxPos.Z & 3] = localHeight;
                maxHeight = localHeight > maxHeight ? localHeight : maxHeight;
            }
        }

        int numberOfBricks = (maxHeight >> 2) + 1;
        VoxelBrickmap.VoxelBrick* bricks = stackalloc VoxelBrickmap.VoxelBrick[numberOfBricks];

        for (voxPos.Z = 0; voxPos.Z < 4; voxPos.Z++)
        {
            for (voxPos.X = 0; voxPos.X < 4; voxPos.X++)
            {
                voxPos.Y = 0;
                int height = heights[voxPos.X, voxPos.Z];
                for (; voxPos.Y < height - 5; voxPos.Y++)
                {
                    PlaceVoxel(bricks, voxPos, 0);
                }
                for (; voxPos.Y < height - 2; voxPos.Y++)
                {
                    PlaceVoxel(bricks, voxPos, 1);
                }
                float mudVal = mudNoise.Sample(new(voxPos.X, voxPos.Z));
                if (mudVal > -0.2f)
                {
                    height -= Util.Random.Next(0, 2);
                    for (; voxPos.Y < height; voxPos.Y++)
                    {
                        PlaceVoxel(bricks, voxPos, 2);
                    }
                }
            }
        }

        for (int i = 0; i < numberOfBricks; i++)
            ScheduleBrick(new(position.X, i, position.Z), bricks[i]);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    unsafe void PlaceVoxel(VoxelBrickmap.VoxelBrick* bricks, Vector3i position, uint material)
    {
        int brickIndex = position.Y >> 2;
        Vector3i localPosition = new(position.X, position.Y & 3, position.Z);
        bricks[brickIndex].SetVoxelValue(localPosition, material);
        bricks[brickIndex].SetVoxelBit(localPosition, true);
    }
    void ScheduleBrick(Vector3i pos, VoxelBrickmap.VoxelBrick brick)
    {
        GLRegistry.Instance.ScheduleAction(() =>
        {
            ((VoxelBrickmap)map).CreateOrModifyBrick(pos, ref brick);
        });
    }
}