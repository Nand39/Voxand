using ImGuiNET;
using System.Numerics;
using Voxand.Engine.Systems.Structures.PrimitiveStructs;
using Voxand.Helpers.ExtensionMethods;

namespace Voxand.Engine.Systems.General.ImGuiIntegration.Systems.DragAndDrop;
public class DragDropTarget
{
    public Vector2 Position { get; set; }
    public Vector2 Size { get; set; }
    public RectF Rect => new RectF(Position.AsTK(), Size.AsTK());
    public event Action<Payload>? OnPayloadDropped;

    public DragDropTarget(Vector2 position, Vector2 size)
    {
        Position = position;
        Size = size;
        ImGuiBackend.RegisterDragDropTarget(this);
    }

    public void HandlePayloadDropped(Payload payload)
    {
        OnPayloadDropped?.Invoke(payload);
    }

    public void CoverLastImGuiItem()
    {
        Position = ImGui.GetItemRectMin();
        Size = ImGui.GetItemRectSize();
    }
}