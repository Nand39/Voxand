using System.Numerics;

using ImGuiNET;

namespace Voxand.UI.Components;

public abstract class UI_Element
{
    public Dictionary<string, object> Data { get; protected set; } = [];
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

    public event Action<Dictionary<string, object>> OnClick;
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
public class UI_ColorButton(Vector2 size, int pushID) : UI_Button(string.Empty, size, pushID)
{
    public bool Enabled { get; protected set; } = false;
    public uint outlineColor;
    public float outlineThickness;

    public event Action<Dictionary<string, object>, bool> OnClick;
    public event Action<Dictionary<string, object>> OnEnable;
    public event Action<Dictionary<string, object>> OnDisable;
    public override void Display()
    {
        ImGui.PushStyleColor(ImGuiCol.Button, color);
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, hoverColor);
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, activeColor);

        ImGui.PushID(pushID);
        if (ImGui.Button(string.Empty, size)) 
        {
            Click();
        }
        ImGui.PopID();
        ImGui.PopStyleColor();

        if (Enabled)
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
        Enabled = !Enabled;
        OnClick?.Invoke(Data, Enabled);
        if (Enabled)
            OnEnable?.Invoke(Data);
        else
            OnDisable?.Invoke(Data);
    }
    public void Disable(bool silent)
    {
        Enabled = false;
        if (!silent)
            OnDisable?.Invoke(Data);
    }
    public void Enable(bool silent)
    {
        Enabled = true;
        if (!silent)
            OnEnable?.Invoke(Data);
    }
    public void SetState(bool enabled) => Enabled = enabled;
}