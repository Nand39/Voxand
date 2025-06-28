using OpenTK.Mathematics;

namespace Voxand.Engine.Systems.Common;

public class Pose
{
    public Vector3 position;
    public Vector3 rotation;
    public Pose() { }
    public Pose(Vector3 position)
    {
        this.position = position;
    }
    public Pose(Vector3 position, Vector3 rotation)
    {
        this.position = position;
        this.rotation = rotation;
    }
}