namespace Voxand.Helpers;

public static class FrameTime
{
    static double preciseDelta;
    public static double PreciseDelta
    {
        get => preciseDelta;
        private set
        {
            preciseDelta = value;
            Delta = (float)value;
        }
    }
    public static float Delta { get; private set; }
}