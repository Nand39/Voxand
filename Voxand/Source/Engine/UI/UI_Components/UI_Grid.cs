using ImGuiNET;
using System.Numerics;
using Voxand.Helpers;

namespace Voxand.UI.Components;
public class UI_Grid : UI_Element
{
    public enum GridSizeMode
    {
        FixedColumnWidth,
        FixedGridWidth,
    }
    public string Label { get; private set; }
    public ImGuiTableFlags Flags { get; set; }
    public Action<int> GridBuilder { get; set; }
    public Action? OnBegin { get; set; }
    public Action? OnEnd { get; set; }
    public int ItemCount { get; set; }
    public float ColumnWidth { get; set; }
    public Vector2 GridSize { get; set; }
    public int NumColumns { get; set; }
    public GridSizeMode SizeMode { get; set; }

    public UI_Grid(string label, ImGuiTableFlags flags, int itemCount, GridSizeMode sizeMode, float columnWidth, Vector2 gridSize, int numColumns, Action<int> gridBuilder, Action? onBegin = null, Action? onEnd = null)
    {
        Label = label;
        Flags = flags;
        ItemCount = itemCount;
        SizeMode = sizeMode;
        ColumnWidth = columnWidth;
        GridSize = gridSize;
        NumColumns = numColumns;

        ArgumentNullException.ThrowIfNull(gridBuilder);
        GridBuilder = gridBuilder;
        OnBegin = onBegin;
        OnEnd = onEnd;
    }
    public override void Display()
    {
        int numColumns = Util.MaxColumns(ImGui.GetContentRegionAvail().X, ColumnWidth, Flags);

        bool tableVisible;
        
        if (SizeMode == GridSizeMode.FixedColumnWidth)
        {
            tableVisible = ImGui.BeginTable(
            str_id: Label, numColumns,
            flags: Flags |= ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.NoHostExtendX);
        }
        else if (SizeMode == GridSizeMode.FixedGridWidth)
        {
            tableVisible = ImGui.BeginTable(
            str_id: Label, NumColumns,
            flags: Flags |= ImGuiTableFlags.SizingStretchSame,
            outer_size: GridSize);
        }
        else throw new ArgumentOutOfRangeException($"Undefined {nameof(SizeMode)} value: '{SizeMode}'.");

        if (tableVisible)
        {
            for (int i = 0; i < numColumns; i++)
                ImGui.TableSetupColumn(
                    label: i.ToString(),
                    flags: SizeMode == GridSizeMode.FixedColumnWidth ? ImGuiTableColumnFlags.WidthFixed : ImGuiTableColumnFlags.WidthStretch,
                    init_width_or_weight: SizeMode == GridSizeMode.FixedColumnWidth ? ColumnWidth : 1f);

            OnBegin?.Invoke();

            for (int i = 0; i < ItemCount;)
            {
                ImGui.TableNextRow();
                for (; i < ItemCount; i++)
                {
                    ImGui.TableNextColumn();
                    GridBuilder(i);
                }
            }

            OnEnd?.Invoke();
            ImGui.EndTable();
        }
    }
}