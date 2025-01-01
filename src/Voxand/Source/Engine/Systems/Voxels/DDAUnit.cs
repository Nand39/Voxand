using OpenTK.Mathematics;
using System.Runtime.CompilerServices;

namespace Voxand.Engine.Systems.Voxels;
public struct DDAContext
{
    public Vector3i voxelPosition;
    public Vector3 timeToCross;
    public Vector3 nextIntersectionTime;
    public Vector3i gridStep;
    public int lastAxis;
    public float lastDepth;
}
public struct DDAOut
{
    public bool hit;
    public Vector3 hitPos;
    public Vector3i voxelHitPos;
    public int normal;
}

public class DDAUnit()
{
    DDAContext data = new();
    public Vector3i CurrentVoxelPos => data.voxelPosition;
    public float LastHitDepth => data.lastDepth;
    public Vector3 NextIntersectionTime => data.nextIntersectionTime;
    public int LastAxis => data.lastAxis;
    public Vector3 TimeToCross => data.timeToCross;

    public void Begin(Vector3 origin, Vector3 dir)
    {
        Begin(origin, dir, (Vector3i)origin);
    }
    public void Begin(Vector3 origin, Vector3 dir, Vector3i targetStart)
    {
        data = new();
        data.voxelPosition = targetStart;
        data.gridStep = new(dir.X > 0 ? 1 : -1, dir.Y > 0 ? 1 : -1, dir.Z > 0 ? 1 : -1);
        data.timeToCross = new(MathF.Abs(1 / dir.X), MathF.Abs(1 / dir.Y), MathF.Abs(1 / dir.Z));
        data.nextIntersectionTime = new(
            dir.X < 0 ? origin.X - data.voxelPosition.X : data.voxelPosition.X + 1 - origin.X,
            dir.Y < 0 ? origin.Y - data.voxelPosition.Y : data.voxelPosition.Y + 1 - origin.Y,
            dir.Z < 0 ? origin.Z - data.voxelPosition.Z : data.voxelPosition.Z + 1 - origin.Z);
        data.nextIntersectionTime *= data.timeToCross;
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public void Step()
    {
        data.lastAxis = data.nextIntersectionTime.X <= data.nextIntersectionTime.Y
                ? data.nextIntersectionTime.X <= data.nextIntersectionTime.Z ? 0 : 2
                : data.nextIntersectionTime.Y <= data.nextIntersectionTime.Z ? 1 : 2;

        if (data.lastAxis == 0)
        {
            data.voxelPosition.X += data.gridStep.X;
            data.lastDepth = data.nextIntersectionTime.X;
            data.nextIntersectionTime.X += data.timeToCross.X;
        }
        else if(data.lastAxis == 1)
        {
            data.voxelPosition.Y += data.gridStep.Y;
            data.lastDepth = data.nextIntersectionTime.Y;
            data.nextIntersectionTime.Y += data.timeToCross.Y;
        }
        else
        {
            data.voxelPosition.Z += data.gridStep.Z;
            data.lastDepth = data.nextIntersectionTime.Z;
            data.nextIntersectionTime.Z += data.timeToCross.Z;
        }
    }
}