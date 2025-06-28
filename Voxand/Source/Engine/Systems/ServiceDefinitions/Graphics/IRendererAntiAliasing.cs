namespace Voxand.Engine.Systems.Services.Graphics;
public interface IRendererAntiAliasing
{
    float Intensity { get; set; }
    void ResetAccumulated();
}