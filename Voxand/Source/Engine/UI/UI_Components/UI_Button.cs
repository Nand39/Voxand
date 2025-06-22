using ImGuiNET;
using System.Numerics;
using Voxand.Helpers;
using Voxand.UI.Components;

[Flags]
public enum SizeMode
{
    Fixed = 0,
    FillWidth = 1 << 1,
    FillHeight = 1 << 2,
    FillAll = FillWidth | FillHeight
}

public class UI_Button(string label, Vector2 size, int pushID = 0) : UI_Element
{
    public string Label { get; set; } = label;
    public string? Description { get; set; }
    public int PushID { get; set; } = pushID;
    public Vector2 Size { get; set; } = size;
    public SizeMode SizeMode { get; set; } = SizeMode.Fixed;
    public Vector4? Color { get; set; }
    public Vector4? HoverColor { get; set; }
    public Vector4? ActiveColor { get; set; }

    public bool IsHovered { get; protected set; }
    protected float hoverTime = 0;

    public event Action? OnClick;
    public override void Display()
    {
        if (Color is not null)
            ImGui.PushStyleColor(ImGuiCol.Button, Color.Value);
        if (HoverColor is not null)
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, HoverColor.Value);
        if (ActiveColor is not null)
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, ActiveColor.Value);

        if (PushID != 0)
            ImGui.PushID(PushID);

        Vector2 size = ImGui.GetContentRegionAvail();

        size.X = SizeMode.HasFlag(SizeMode.FillWidth) ? size.X : Size.X;
        size.Y = SizeMode.HasFlag(SizeMode.FillHeight) ? size.Y : Size.Y;

        if (ImGui.Button(Label, size))
            Click();

        ImGui.PopStyleColor();
        ImGui.PopID();

        IsHovered = ImGui.IsItemHovered();

        if (IsHovered && Description is not null)
        {
            hoverTime += FrameTime.Delta;
            if (hoverTime > 0.5f)
            {
                ImGui.BeginTooltip();
                ImGui.Text(Description);
                ImGui.EndTooltip();
            }
        }
        else hoverTime = 0;
    }

    public virtual void Click()
    {
        OnClick?.Invoke();
    }
}