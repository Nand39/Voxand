using ImGuiNET;
using System.Numerics;

namespace Voxand.UI.Components;

public class FloatPicker(string label) : UI_Element
{
    string label = label;
    public float value;

    public event Action<float> OnValueChange;
    public override void Display()
    {
        if (ImGui.InputFloat(label, ref value, 0.01f, 1f, "%.2f"))
            OnValueChange?.Invoke(value);
    }
}
public class IntPicker(string label) : UI_Element
{
    string label = label;
    public int value;

    public event Action<int> OnValueChange;
    public override void Display()
    {
        if (ImGui.InputInt(label, ref value))
            OnValueChange?.Invoke(value);
    }
}