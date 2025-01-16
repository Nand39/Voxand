using OpenTK.Mathematics;
using Voxand.App.Map;
using Voxand.Engine.Systems.General.Events;
using Voxand.Engine.Systems.Graphics;
using Voxand.Engine.Systems.ScriptableObjects;
using Voxand.Engine.Systems.Voxels;
using Voxand.Helpers;

namespace Voxand.App.VoxelEditing;
public class VoxelTool : BaseObject
{
    ChunkMap map;
    public EventDispatcher Events { get; private set; } = new("user_voxelTool_events");
    int activeMaterial;
    int activePlacementTechnique;

    List<PlacementTechnique> builders = [];
    public int ActiveMaterial
    {
        get => activeMaterial; 
        set
        {
            activeMaterial = value;
            Events.Invoke("activeMaterial_changed", ActiveMaterial);
        }
    }
    public int ActivePlacementTechnique
    {
        get => activePlacementTechnique;
        set
        {
            activePlacementTechnique = value;
            Events.Invoke("activePlacementTechnique_changed", ActivePlacementTechnique);
        }
    }

    public override void Initialize()
    {
        Events.AddEvent("activeMaterial_changed");
        Events.AddEvent("activePlacementTechnique_changed");

        EngineState.Events.Subscribe("main_voxelMap_changed", (args) => {
            map = (ChunkMap)args;
            UpdateBuilders();
        });
        map = EngineState.VoxelMap;
        UpdateBuilders();
    }

    void UpdateBuilders()
    {
        ISinglePlaceable singlePlaceable = map.RawStructure as ISinglePlaceable ?? throw new Exception($"Map should support {nameof(ISinglePlaceable)}.");
        builders.Clear();
        builders.Add(new SingleBuilder(singlePlaceable));
        builders.Add(new SingleRemover(singlePlaceable));
        builders.Add(new BlockBuilder(singlePlaceable, map.Dimensions));
        builders.Add(new BlockRemover(singlePlaceable, map.Dimensions));
    }

    public void Use(RaycastResult raycastResult)
    {
        builders[activePlacementTechnique].Use(new(raycastResult, activeMaterial));
    }

    public void NextTechnique() => ActivePlacementTechnique = (ActivePlacementTechnique + 1) % builders.Count;
    public void PreviousTechnique() => ActivePlacementTechnique = (ActivePlacementTechnique - 1) % builders.Count;
}

public struct PlacementInput(RaycastResult raycastResult, int material)
{
    public RaycastResult raycastResult = raycastResult;
    public int material = material;
}
public abstract class PlacementTechnique
{
    public abstract void Use(PlacementInput input);
}
public class SingleBuilder : PlacementTechnique
{
    readonly ISinglePlaceable singlePlaceable;
    public SingleBuilder(ISinglePlaceable singlePlaceable) => this.singlePlaceable = singlePlaceable;
    public override void Use(PlacementInput input)
    {
        Vector3i position = input.raycastResult.voxelHitPos + Util.VectorFromNormalIndex(input.raycastResult.normal);
        singlePlaceable.PlaceSingle(position, input.material);
    }
}
public class SingleRemover : PlacementTechnique
{
    readonly ISinglePlaceable singlePlaceable;
    public SingleRemover(ISinglePlaceable singlePlaceable) => this.singlePlaceable = singlePlaceable;
    public override void Use(PlacementInput input)
    {
        singlePlaceable.RemoveSingle(input.raycastResult.voxelHitPos);
    }
}

public abstract class BatchPlacementTechnique : PlacementTechnique
{
    protected PlacementInput[] InputBatch { get; set; }
    int inputsReceived = 0;

    public BatchPlacementTechnique(int batchLength)
    {
        InputBatch = new PlacementInput[batchLength];
    }

    public sealed override void Use(PlacementInput input)
    {
        InputBatch[inputsReceived] = input;
        inputsReceived++;
        if (inputsReceived == InputBatch.Length)
        {
            OnBatchFull();
            inputsReceived = 0;
        }
    }

    protected abstract void OnBatchFull();
}

public class BlockBuilder : BatchPlacementTechnique
{
    readonly ISinglePlaceable singlePlaceable;
    readonly Vector3i mapSize;

    public BlockBuilder(ISinglePlaceable singlePlaceable, Vector3i mapSize) : base(2)
    {
        this.singlePlaceable = singlePlaceable;
        this.mapSize = mapSize;
    }
    protected override void OnBatchFull()
    {
        Vector3i start = Vector3i.Clamp(InputBatch[0].raycastResult.voxelHitPos, Vector3i.Zero, mapSize - Vector3i.One);
        Vector3i finish = Vector3i.Clamp(InputBatch[1].raycastResult.voxelHitPos, Vector3i.Zero, mapSize - Vector3i.One);
        Util.OrderBounds(start, finish, out Vector3i min, out Vector3i max);
        int material = InputBatch[0].material;
        Util.Loop3(min, max, (pos) => singlePlaceable.PlaceSingle(pos, material));
    }
}

public class BlockRemover : BatchPlacementTechnique
{
    readonly ISinglePlaceable singlePlaceable;
    readonly Vector3i mapSize;

    public BlockRemover(ISinglePlaceable singlePlaceable, Vector3i mapSize) : base(2)
    {
        this.singlePlaceable = singlePlaceable;
        this.mapSize = mapSize;
    }
    protected override void OnBatchFull()
    {
        Vector3i start = Vector3i.Clamp(InputBatch[0].raycastResult.voxelHitPos, Vector3i.Zero, mapSize - Vector3i.One);
        Vector3i finish = Vector3i.Clamp(InputBatch[1].raycastResult.voxelHitPos, Vector3i.Zero, mapSize - Vector3i.One);
        Util.OrderBounds(start, finish, out Vector3i min, out Vector3i max);
        int material = InputBatch[0].material;
        Util.Loop3(min, max, singlePlaceable.RemoveSingle);
    }
}