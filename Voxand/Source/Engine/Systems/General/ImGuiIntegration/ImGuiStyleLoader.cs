using ImGuiNET;
using System.Text.Json;
using Voxand.Engine.Systems.Services.UI.ImGuiIntegration;

namespace Voxand.UI.ImGuiIntegration;
public class ImGuiStyleLoader : IImGuiStyleLoader
{
    JsonSerializerOptions serializerOptions;
    public ImGuiStyleLoader() => serializerOptions = new() { IncludeFields = true, WriteIndented = true };
    public unsafe void SetJSON(string json)
    {
        ImGuiStyle style = JsonSerializer.Deserialize<ImGuiStyle>(json, serializerOptions);
        *ImGui.GetStyle().NativePtr = style;
    }
    public unsafe string SerializeJSON(bool readable)
    {
        ImGuiStyle style = *ImGui.GetStyle().NativePtr;
        return JsonSerializer.Serialize(style, serializerOptions);
    }
}