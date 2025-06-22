using ImGuiNET;
using Voxand.UI.Components;

public class UI_TableColumnInfo(string header, ImGuiTableColumnFlags flags, float width, float stretchWeight, Action<int> columnBuilder)
{
    public string Header { get; set; } = header;
    public ImGuiTableColumnFlags Flags { get; set; } = flags;
    public float Width { get; set; } = width;
    public float StretchWeight { get; set; } = stretchWeight;
    public Action<int> ColumnBuilder { get; set; } = columnBuilder;
}

public class UI_Table : UI_Element
{
    public string Label { get; private set; }
    public int NumColumns => columnInfos.Count;
    public int NumRows { get; private set; }
    public bool DisplayHeadersRow { get; set; }
    public ImGuiTableFlags Flags { get; set; }
    List<UI_TableColumnInfo> columnInfos;
    public UI_Table(string label, ImGuiTableFlags flags, List<UI_TableColumnInfo> columnInfos, int numRows, bool displayHeadersRow)
    {
        Label = label;
        Flags = flags;
        this.columnInfos = columnInfos;
        NumRows = numRows;
        DisplayHeadersRow = displayHeadersRow;
    }
    public override void Display()
    {
        if (ImGui.BeginTable(Label, NumColumns, Flags))
        {
            for (int i = 0; i < NumColumns; i++)
                ImGui.TableSetupColumn(columnInfos[i].Header, columnInfos[i].Flags, columnInfos[i].Flags.HasFlag(ImGuiTableColumnFlags.WidthFixed) ? columnInfos[i].Width : columnInfos[i].StretchWeight);

            if (DisplayHeadersRow)
                ImGui.TableHeadersRow();

            for (int row = 0; row < NumRows; row++)
            {
                ImGui.TableNextRow();
                for (int col = 0; col < NumColumns; col++)
                {
                    ImGui.TableNextColumn();
                    columnInfos[col].ColumnBuilder(row);
                }
            }
            ImGui.EndTable();
        }
    }
}