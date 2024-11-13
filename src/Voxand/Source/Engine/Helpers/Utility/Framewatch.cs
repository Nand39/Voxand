using OpenTK.Windowing.Common;
using Voxand.Engine;

namespace Voxand.Helpers.Utility;

public class FrameTimeData
{
    public double FPS { get; set; }
    public TimeSpan MinTime { get; set; }
    public TimeSpan MaxTime { get; set; }
    public TimeSpan AvgTime { get; set; }
}
public class Framewatch
{
    public double Interval { get; }
    public double Elapsed { get; protected set; }
    public int OutputPrecision { get; set; } = 1;
    public int Frames { get; protected set; }
    public double FPS { get; protected set; }
    public double MinTime { get; protected set; }
    public double MaxTime { get; protected set; }
    public double AvgTime { get; protected set; }
    public FrameTimeData FrameTimeData { get; protected set; }
    public event Action? OnElapsed;
    public Framewatch(FrameTimeData output, double interval)
    {
        Interval = interval;
        FrameTimeData = output;
    }
    public void Tick(FrameEventArgs args)
    {
        if (Elapsed < Interval)
        {
            Elapsed += args.Time;
            MaxTime = args.Time > MaxTime ? args.Time : MaxTime;
            MinTime = args.Time < MinTime ? args.Time : MinTime;
            Frames++;
        }
        else
        {
            FrameTimeData.FPS = Math.Round(Frames / Elapsed, OutputPrecision);
            FrameTimeData.AvgTime = new((long)(Elapsed / Frames * TimeSpan.TicksPerSecond));
            FrameTimeData.MinTime = new((long)(MinTime * TimeSpan.TicksPerSecond));
            FrameTimeData.MaxTime = new((long)(MaxTime * TimeSpan.TicksPerSecond));

            Elapsed = 0;
            Frames = 0;
            MinTime = double.MaxValue;
            MaxTime = double.MinValue;

            OnElapsed?.Invoke();
        }
    }
}