using OpenTK.Mathematics;
using Voxand.Engine.Systems.General.Events;
using Voxand.Engine.Systems.ScriptableObjects;
using Voxand.Engine.Systems.Voxels;

namespace Voxand.App.VoxelEditing;
public class VoxelTool : BaseObject
{
    VoxelMap map;
    public EventDispatcher Events { get; private set; } = new("user_voxelTool_events");
    int activeMaterial;
    public override void Initialize()
    {
        Events.AddEvent("activeMaterial_changed");
        EngineState.Events.Subscribe("main_voxelMap_changed", (args) => {
            map = (VoxelMap)args;
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
        ((VoxelBrickmap)map).SetVoxelValueAndBit(position, (uint)ActiveMaterial, true);
    }
    public void RemoveSingle(Vector3i position)
    {
        ((VoxelBrickmap)map).SetVoxelValueAndBit(position, 0, false);
    }
}