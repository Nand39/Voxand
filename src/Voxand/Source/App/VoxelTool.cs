using OpenTK.Mathematics;
using Voxand.App.Map;
using Voxand.Engine.Systems.General.Events;
using Voxand.Engine.Systems.ScriptableObjects;
using Voxand.Engine.Systems.Voxels;
using Voxand.Helpers;
using Voxand.Helpers.ExtensionMethods;

namespace Voxand.App.VoxelEditing;
public class VoxelTool : BaseObject
{
    ChunkMap map;
    public EventDispatcher Events { get; private set; } = new("user_voxelTool_events");
    int activeMaterial;
    public override void Initialize()
    {
        Events.AddEvent("activeMaterial_changed");
        EngineState.Events.Subscribe("main_voxelMap_changed", (args) => {
            map = (ChunkMap)args;
        });
        map = EngineState.VoxelMap;
    }
    public int ActiveMaterial
    {
        get => activeMaterial; 
        set
        {
            activeMaterial = value;
            Events.Invoke("activeMaterial_changed", ActiveMaterial);
        }
    }
    public void PlaceSingle(Vector3i position)
    {
        if (position.Inbounds(Vector3i.Zero, map.Dimensions))
            map.Place(position, ActiveMaterial);
    }
    public void PlaceSphere(Vector3i position, float radius)
    {
        float rSqr = radius * radius;
        int b = (int)MathF.Ceiling(radius);
        Vector3i voxPos = default;
        for (voxPos.Y = position.Y - b; voxPos.Y <= position.Y + b; voxPos.Y++)
        {
            for (voxPos.Z = position.Z - b; voxPos.Z <= position.Z + b; voxPos.Z++)
            {
                for (voxPos.X = position.X - b; voxPos.X <= position.X + b; voxPos.X++)
                {
                    if ((voxPos - position).EuclideanLengthSquared < rSqr)
                        PlaceSingle(voxPos);
                }
            }
        } 
    }
    public void RemoveSingle(Vector3i position)
    {
        if (position.Inbounds(Vector3i.Zero, map.Dimensions))
            map.Remove(position);
    }
    public void RemoveSphere(Vector3i position, float radius)
    {
        float rSqr = radius * radius;
        int b = (int)MathF.Ceiling(radius);
        Vector3i voxPos = default;
        for (voxPos.Y = position.Y - b; voxPos.Y <= position.Y + b; voxPos.Y++)
        {
            for (voxPos.Z = position.Z - b; voxPos.Z <= position.Z + b; voxPos.Z++)
            {
                for (voxPos.X = position.X - b; voxPos.X <= position.X + b; voxPos.X++)
                {
                    if ((voxPos - position).EuclideanLengthSquared < rSqr)
                        RemoveSingle(voxPos);
                }
            }
        }
    }
}