using OpenTK.Mathematics;

using Voxand.App.MathR.Noise;

namespace Voxand.App.Voxels.Map.Generation;

public abstract class MapGenerator
{
    public abstract void GenerateChunk(Vector3i position);
    public abstract void GenerateAll();
}

struct NoiseAdapter
{
    Vector2 cellSize;
    Vector2 scaler;
    public PerlinNoise Noise { get; private set; }
    public Vector2 CellSize
    {
        get => cellSize;
        set
        {
            cellSize = value;
            scaler = new Vector2(1 / cellSize.X, 1 / cellSize.Y);
        }
    }
    public NoiseAdapter(Vector2i numberOfCells, Vector2 cellSize)
    {
        Noise = new PerlinNoise(numberOfCells);
        scaler = new Vector2(1 / cellSize.X, 1 / cellSize.Y);
    }
    public float Sample(Vector2 position) => Noise.Sample(position * scaler);
}