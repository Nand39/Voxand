using System.Numerics;

using ImGuiNET;
using Voxand.Engine.Systems.Structures.PrimitiveStructs;
using Voxand.Helpers.ExtensionMethods;

namespace Voxand.Engine.Systems.General.ImGuiIntegration.Systems.DragAndDrop;

public class DragDropSource
{
    public Vector2 Position { get; set; }
    public Vector2 Size { get; set; }
    public RectF Rect => new RectF(Position.AsTK(), Size.AsTK());
    public Payload Payload { get; set; }
    public ImGuiDragDropFlags Flags { get; set; }
    public bool Enabled { get; set; } = true;
    public Action<Payload> DragDropTooltipBuilder { get; set; }

    public MouseButtonsMask TrackedMouseButtons { get; set; }
    public DragDropSource(Payload payload, ImGuiDragDropFlags flags, Action<Payload> dragDropTooltipBuilder, MouseButtonsMask mouseButtonsToTrack = MouseButtonsMask.Left)
    {
        Payload = payload;
        Flags = flags;
        DragDropTooltipBuilder = dragDropTooltipBuilder;
        TrackedMouseButtons = mouseButtonsToTrack;
        ImGuiBackend.RegisterDragDropSource(this);
    }

    public void CoverLastImGuiItem()
    {
        Position = ImGui.GetItemRectMin();
        Size = ImGui.GetItemRectSize();
    }
}