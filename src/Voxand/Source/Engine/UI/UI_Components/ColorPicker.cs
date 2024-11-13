using ImGuiNET;
using System.Numerics;

namespace Voxand.UI.Components;

public class ColorPicker(string label) : UI_Element
{
    public Vector3 color;
    string label = label;

    public event Action<Vector3> OnColorChange;
    public override void Display()
    {
        if (ImGui.ColorEdit3(label, ref color))
        {
            OnColorChange?.Invoke(color);
        }
    }
}