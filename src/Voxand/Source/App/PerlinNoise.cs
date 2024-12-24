using OpenTK.Mathematics;
using System.Diagnostics;
using Voxand.Helpers;
using Voxand.Helpers.ExtensionMethods;

public class PerlinNoise
{
    Vector2[,] vectorGrid;
    Vector2i boundaries;
    public PerlinNoise(Vector2i gridSize)
    {
        boundaries = gridSize - Vector2i.One;
        vectorGrid = new Vector2[gridSize.X, gridSize.Y];
        for (int y = 0; y < gridSize.Y; y++)
        {
            for (int x = 0; x < gridSize.X; x++)
            {
                float angle = Util.Random.NextSingle() * (MathF.PI * 2);
                vectorGrid[x, y] = new(MathF.Cos(angle), MathF.Sin(angle));
            }
        }
    }
    public float Sample(Vector2 point)
    {
        Vector2i node00Index = (Vector2i)point;
        if (node00Index.X < 0 || node00Index.X >= boundaries.X || node00Index.Y < 0 || node00Index.Y >= boundaries.Y)
            return -1;

        Vector2 node00Pos = node00Index;
        Vector2 node11Pos = node00Pos + Vector2.One;
        Vector2 offset00, offset10, offset01, offset11;
        offset00 = point - node00Pos;
        offset11 = point - node11Pos;
        offset10 = new(point.X - node11Pos.X, point.Y - node00Pos.Y);
        offset01 = new(point.X - node00Pos.X, point.Y - node11Pos.Y);
        float dot00 = Vector2.Dot(offset00, vectorGrid[node00Index.X, node00Index.Y]);
        float dot11 = Vector2.Dot(offset11, vectorGrid[node00Index.X + 1, node00Index.Y + 1]);
        float dot10 = Vector2.Dot(offset10, vectorGrid[node00Index.X + 1, node00Index.Y]);
        float dot01 = Vector2.Dot(offset01, vectorGrid[node00Index.X, node00Index.Y + 1]);

        Vector2 pointOffset = point - node00Pos;
        float weightX = Smoothstep(pointOffset.X);
        float weightY = Smoothstep(pointOffset.Y);

        float horizontal0 = Lerp(dot00, dot10, weightX);
        float horizontal1 = Lerp(dot01, dot11, weightX);

        float val = Lerp(horizontal0, horizontal1, weightY);

        if (val == float.NaN)
            Debugger.Break();

        return val;
    }
    static float Smoothstep(float t)
    {
        return t * t * t * (t * (t * 6 - 15) + 10);
    }
    static float Lerp(float a, float b, float t)
    {
        return a + (b - a) * t;
    }
}