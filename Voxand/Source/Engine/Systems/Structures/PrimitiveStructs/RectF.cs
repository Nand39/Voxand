using OpenTK.Mathematics;

namespace Voxand.Engine.Systems.Structures.PrimitiveStructs;
public struct RectF
{
    public Vector2 position;
    public Vector2 size;

    public float Left => position.X;
    public float Right => position.X + size.X;
    public float Bottom => position.Y;
    public float Top => position.Y + size.Y;
    public float Width => size.X;
    public float Height => size.Y;

    public RectF(float x, float y, float width, float height)
    {
        position.X = x;
        position.Y = y;
        size.X = width;
        size.Y = height;
    }

    public RectF(Vector2 position, Vector2 size)
    {
        this.position = position;
        this.size = size;
    }

    public bool Contains(Vector2 point)
    {
        return point.X >= position.X && point.X <= position.X + size.X &&
               point.Y >= position.Y && point.Y <= position.Y + size.Y;
    }
    public bool Contains(Vector2i point)
    {
        return point.X >= position.X && point.X <= position.X + size.X &&
               point.Y >= position.Y && point.Y <= position.Y + size.Y;
    }
}