namespace Voxand.Helpers.UtilityTypes;
public class IntervalCounter
{
    public float ElapsedTime { get; set; } = 0;
    public required float Interval { get; set; }

    public int Tick(float deltaTime)
    {
        ElapsedTime += deltaTime;
        if (ElapsedTime > Interval)
        {
            int intervalsPassed = (int)(ElapsedTime / Interval);
            ElapsedTime -= intervalsPassed * Interval;
            return intervalsPassed;
        }
        return 0;
    }

    public void Reset() => ElapsedTime = 0;
}