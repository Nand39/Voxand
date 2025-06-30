using ImGuiNET;
using System.Numerics;
using Voxand.Helpers;

namespace Voxand.UI.Components;
public class UI_Grid : UI_Element
{
    public string Label { get; private set; }
    public ImGuiTableFlags Flags { get; set; }
    public Action<int> GridBuilder { get; set; }
    public Action? OnBegin { get; set; }
    public Action? OnEnd { get; set; }
    public int ItemCount { get; set; }
    public float ColumnWidth { get; set; }
    public Vector2 GridSize { get; set; }
    public int NumColumns { get; set; }

    public UI_Grid(string label, ImGuiTableFlags flags, int itemCount, float columnWidth, Vector2 gridSize, int numColumns, Action<int> gridBuilder, Action? onBegin = null, Action? onEnd = null)
    {
        Label = label;
        Flags = flags;
        ItemCount = itemCount;
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

        if (ImGui.BeginTable(
            str_id: Label,
            columns: numColumns,
            flags: Flags |= ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.NoHostExtendX))
        {
            for (int i = 0; i < numColumns; i++)
                ImGui.TableSetupColumn(
                    label: i.ToString(),
                    flags: ImGuiTableColumnFlags.WidthFixed,
                    init_width_or_weight: ColumnWidth);

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