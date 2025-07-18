using ImGuiNET;
using OpenTK.Mathematics;

namespace Voxand.UI.Components;
public class FloatPicker(string label) : UI_Element
{
    string label = label;

    float value;
    public float Value { get => value; set => this.value = value; }

    public bool IsHovered { get; private set; }

    public event Action<float>? OnValueChange;
    public override void Display()
    {
        bool valueChanged = ImGui.InputFloat(label, ref value, 0.01f, 1f, "%.2f");

        IsHovered = ImGui.IsItemHovered();

        if (valueChanged)
            OnValueChange?.Invoke(value);
    }
}
public class IntPicker(string label) : UI_Element
{
    string label = label;
    int value;
    public int Value { get => value; set => this.value = value; }

    public bool IsHovered { get; private set; }

    public event Action<int>? OnValueChange;
    public override void Display()
    {
        bool valueChanged = ImGui.InputInt(label, ref value);

        IsHovered = ImGui.IsItemHovered();

        if (valueChanged)
            OnValueChange?.Invoke(Value);
    }
}
public class Vector3iPicker : UI_Element
{
    public Vector3i Value
    {
        get => value;
        set
        {
            this.value = value;
            pickerX.Value = value.X;
            pickerY.Value = value.Y;
            pickerZ.Value = value.Z;
        }
    }

    public bool IsHovered => pickerX.IsHovered || pickerY.IsHovered || pickerZ.IsHovered;

    string label;
    Vector3i value;
    IntPicker pickerX = new("x"), pickerY = new("y"), pickerZ = new("z");

    public event Action<Vector3i> OnValueChange;

    public Vector3iPicker(string label)
    {
        this.label = label;
        pickerX.OnValueChange += (x) => { value.X = x; OnValueChange?.Invoke(value); };
        pickerY.OnValueChange += (y) => { value.Y = y; OnValueChange?.Invoke(value); };
        pickerZ.OnValueChange += (z) => { value.Z = z; OnValueChange?.Invoke(value); };
    }
    public override void Display()
    {
        if (label != string.Empty)
            ImGui.Text(label);

        pickerX.Display();
        pickerY.Display(); 
        pickerZ.Display();
    }
}