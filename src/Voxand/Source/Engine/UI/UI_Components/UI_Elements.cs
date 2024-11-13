using System.Numerics;

using ImGuiNET;

namespace Voxand.UI.Components;

public abstract class UI_Element
{
    public Dictionary<string, int> Data = [];
    public virtual void Display() { }
}
public class UI_Button(string label, Vector2 size, int pushID = 0) : UI_Element
{
    public string label = label;
    public int pushID = pushID;
    public Vector2 size = size;
    public Vector4 color = Vector4.One;
    public Vector4 hoverColor = Vector4.One;
    public Vector4 activeColor = Vector4.One;

    public event Action<Dictionary<string, int>> OnClick;
    public override void Display()
    {
        ImGui.PushStyleColor(ImGuiCol.Button, color); 
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, hoverColor); 
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, activeColor);

        ImGui.PushID(pushID);
        if (ImGui.Button(label, size)) Click();
        ImGui.PopID();

        ImGui.PopStyleColor();
    }

    public virtual void Click()
    {
        OnClick?.Invoke(Data);
    }
}
public class UI_ColorButton(Vector2 size, int pushID, int key) : UI_Button(string.Empty, size, pushID)
{
    public int Key = key;
    public bool selected = false;
    public uint outlineColor;
    public float outlineThickness;

    public event Action<Dictionary<string, int>, int, bool> OnClick;
    public override void Display()
    {
        ImGui.PushStyleColor(ImGuiCol.Button, color);
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, hoverColor);
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, activeColor);

        ImGui.PushID(pushID);
        if (ImGui.Button(string.Empty, size)) 
        {
            selected = !selected;
            Click();
        }
        ImGui.PopID();
        ImGui.PopStyleColor();

        if (selected)
        {
            ImGui.GetWindowDrawList().AddRect(
                p_min: ImGui.GetItemRectMin(),
                p_max: ImGui.GetItemRectMax(),
                col: outlineColor,
                rounding: 0,
                flags: ImDrawFlags.None,
                thickness: outlineThickness);
        }
    }

    public override void Click()
    {
        OnClick?.Invoke(Data, Key, selected);
    }
}