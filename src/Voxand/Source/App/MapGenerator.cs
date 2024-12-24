using OpenTK.Mathematics;
using System.Diagnostics;
using Voxand.Engine.Systems.Voxels;
using Voxand.Helpers;

public class MapGenerator
{
    public VoxelMap map;
    Vector2 invScale;
    public MapGenerator(VoxelMap map)
    {
        this.map = map;
    }
    public void Generate(Vector2i scale)
    {
        invScale = new(1f / scale.X, 1f / scale.Y);
        Vector2i gridSize = (Vector2i)((Vector2)map.Dimensions.Xz * invScale) + Vector2i.One * 2;
        PerlinNoise noise = new(gridSize);
        PerlinNoise noise1 = new(gridSize * 2);

        for (int z = 0; z < map.Dimensions.Z; z++)
        {
            for (int x = 0; x < map.Dimensions.X; x++)
            {
                Vector3i voxPos = new(x, 0, z);
                float noiseVal = noise.Sample(new(voxPos.X * invScale.X, voxPos.Z * invScale.Y)) * 0.5f + 0.5f;
                noiseVal += noise1.Sample(new(voxPos.X * invScale.X * 2, voxPos.Z * invScale.Y * 2)) * 0.25f + 0.25f;

                int height = (int)(noiseVal * 80);
                uint material;
                for (; voxPos.Y < height - 3; voxPos.Y++)
                {
                    material = (uint)Util.Random.Next(0, 2);
                    ((VoxelBrickmap)map).SetVoxelValueAndBit(voxPos, material, true);
                }
                for (; voxPos.Y < height; voxPos.Y++)
                {
                    material = (uint)Util.Random.Next(3, 5);
                    ((VoxelBrickmap)map).SetVoxelValueAndBit(voxPos, material, true);
                }
                material = (uint)Util.Random.Next(6, 8);
                ((VoxelBrickmap)map).SetVoxelValueAndBit(voxPos, material, true);
            }
        }
    }
}