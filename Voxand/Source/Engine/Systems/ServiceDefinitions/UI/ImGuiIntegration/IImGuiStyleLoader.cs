namespace Voxand.Engine.Systems.Services.UI.ImGuiIntegration;

public interface IImGuiStyleLoader
{
    void SetJSON(string json);
    string SerializeJSON(bool readable);
}