namespace Voxand.UI.Systems.DragAndDrop;
public class Payload
{
    public object Data { get; set; }
    public string Type { get; set; }
    public Payload(object data, string type)
    {
        Data = data;
        Type = type;
    }
}