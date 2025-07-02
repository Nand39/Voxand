using System.Numerics;

using ImGuiNET;
using Voxand.Engine.Systems.Structures.PrimitiveStructs;
using Voxand.Helpers.ExtensionMethods;

namespace Voxand.Engine.Systems.General.ImGuiIntegration;

public static class ImGuiInput
{
    static ImGuiBackend.ImGuiInputCache inputCache = null!;

    public static void Initialize(ImGuiBackend.ImGuiInputCache inputCache) => ImGuiInput.inputCache ??= inputCache;

    public static Vector2 GetMousePosition() => inputCache.State.MousePosition;
    public static Vector2 GetPastMousePosition() => inputCache.Past.MousePosition;
    public static bool IsHovering(RectF rect) => rect.Contains(inputCache.State.MousePosition.AsTK());
    public static bool WasHovering(RectF rect) => rect.Contains(inputCache.Past.MousePosition.AsTK());
    public static bool WasHoveringOnLastClick(RectF rect, ImGuiMouseButton button) => rect.Contains(GetLastClickPosition(button).AsTK());
    public static bool WasHoveringOnLastClick(RectF rect, MouseButtonsMask buttons)
    {
        bool result = false;

        for (int i = 0; i < 3; i++)
            if ((buttons & (MouseButtonsMask)(1 << i)) != 0)
                result |= WasHoveringOnLastClick(rect, (ImGuiMouseButton)i);

        return result;
    }


    public static MouseButtonsMask GetMouseDragMask() => inputCache.State.IsMouseButtonDragging;
    public static bool IsMouseDragging(ImGuiMouseButton button) => ((int)inputCache.State.IsMouseButtonDragging & 1 << (int)button) != 0;
    public static bool WasMouseDragging(ImGuiMouseButton button) => ((int)inputCache.Past.IsMouseButtonDragging & 1 << (int)button) != 0;
    public static bool IsAnyMouseButtonDragging() => inputCache.State.IsMouseButtonDragging != 0;
    public static bool WasAnyMouseButtonDragging() => inputCache.Past.IsMouseButtonDragging != 0;
    public static bool MouseDragStarted(ImGuiMouseButton button) => !WasMouseDragging(button) && IsMouseDragging(button);
    public static bool MouseDragEnded(ImGuiMouseButton button) => WasMouseDragging(button) && !IsMouseDragging(button);
    public static bool MouseDragStarted() => !WasAnyMouseButtonDragging() && IsAnyMouseButtonDragging();
    public static bool MouseDragEnded() => WasAnyMouseButtonDragging() && !IsAnyMouseButtonDragging();
    public static bool IsAnyMouseButtonDragStarted() => (~inputCache.Past.IsMouseButtonDragging & inputCache.State.IsMouseButtonDragging) != 0;
    public static bool IsAnyMouseButtonDragEnded() => (inputCache.Past.IsMouseButtonDragging & inputCache.State.IsMouseButtonDragging) != inputCache.Past.IsMouseButtonDragging;
    public static bool IsAnyMouseButtonDragChanged() => inputCache.Past.IsMouseButtonDragging != inputCache.State.IsMouseButtonDragging;
    public static Vector2 GetLastClickPosition(ImGuiMouseButton button) => inputCache.mouseClickPositions[(int)button];
    public static bool IsInteractingWithUI() => ImGui.IsAnyItemActive() || ImGui.IsAnyItemHovered() || ImGui.IsWindowHovered(ImGuiHoveredFlags.AnyWindow);
}

[Flags]
public enum MouseButtonsMask
{
    Left = 1,
    Right = 1 << 1,
    Middle = 1 << 2
}
