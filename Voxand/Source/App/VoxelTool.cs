using OpenTK.Mathematics;
using System;
using Voxand.App.Map;
using Voxand.Engine.Systems.General.Events;
using Voxand.Engine.Systems.Graphics;
using Voxand.Engine.Systems.Graphics.Tools.UI;
using Voxand.Engine.Systems.ScriptableObjects;
using Voxand.Engine.Systems.Voxels;
using Voxand.Helpers;

namespace Voxand.App.VoxelEditing;
public class VoxelTool : BaseObject
{
    ChunkMap map;
    public EventDispatcher Events { get; private set; } = new("user_voxelTool_events");
    public event Action? OnActivePlacementTechniqueChanged;
    int activePlacementTechniqueIndex;

    List<PlacementTechnique> builders = [];

    public event Action? OnPlacementTechniquesLoaded;

    VoxelToolContext context = new();

    public int TechniqueCount => builders.Count;

    public PlacementTechnique this[int index]
    {
        get => builders[index];
        set => builders[index] = value;
    }

    public int ActivePlacementTechniqueIndex
    {
        get => activePlacementTechniqueIndex;
        set
        {
            if (value < 0 || value >= TechniqueCount)
                throw new ArgumentOutOfRangeException(nameof(value), $"{nameof (ActivePlacementTechniqueIndex)} should be between zero and {nameof(TechniqueCount)} - 1.");

            activePlacementTechniqueIndex = value;
            Events.Invoke("activePlacementTechnique_changed", ActivePlacementTechnique);
            OnActivePlacementTechniqueChanged?.Invoke();
        }
    }

    public int ActiveMaterialIndex
    {
        get => context.Material; 
        set
        {
            context.Material = value;
            Events.Invoke("activeMaterial_changed", ActiveMaterialIndex);
        }
    }

    public PlacementTechnique ActivePlacementTechnique => this[ActivePlacementTechniqueIndex];

    public override void Initialize()
    {
        Events.AddEvent("activeMaterial_changed");
        Events.AddEvent("activePlacementTechnique_changed");

        EngineState.Events.Subscribe("main_voxelMap_changed", (args) => {
            map = (ChunkMap)args;
            LoadPlacementTechniques();
        });
        map = EngineState.VoxelMap;
        LoadPlacementTechniques();
        ActivePlacementTechniqueIndex = 0;
    }

    void LoadPlacementTechniques()
    {
        ISinglePlaceable singlePlaceable = map.RawStructure as ISinglePlaceable ?? throw new Exception($"Map should support {nameof(ISinglePlaceable)}.");
        builders.Clear();
        builders.Add(new SingleBuilder(singlePlaceable));
        builders.Add(new SingleRemover(singlePlaceable));
        builders.Add(new BlockBuilder(singlePlaceable, map.Dimensions));
        builders.Add(new BlockRemover(singlePlaceable, map.Dimensions));
        builders.Add(new SphereBuilder(singlePlaceable, map.Dimensions));
        builders.Add(new SphereRemover(singlePlaceable, map.Dimensions));
        OnPlacementTechniquesLoaded?.Invoke();
    }

    public void Use(RaycastResult raycastResult)
    {
        ActivePlacementTechnique.Use(new(raycastResult, context));
    }

    public void Apply()
    {
        if (ActivePlacementTechnique is ComplexPlacementTechnique complexTechnique)
            complexTechnique.Apply(context);
    }
    public void Cancel()
    {
        if (ActivePlacementTechnique is ComplexPlacementTechnique complexTechnique)
            complexTechnique.Cancel();
    }

    public void NextTechnique() => ActivePlacementTechniqueIndex = Util.Mod(ActivePlacementTechniqueIndex + 1, builders.Count);
    public void PreviousTechnique() => ActivePlacementTechniqueIndex = Util.Mod(ActivePlacementTechniqueIndex - 1, builders.Count);
}

public struct PlacementInput(RaycastResult raycastResult, VoxelToolContext context)
{
    public RaycastResult RaycastResult { get; set; } = raycastResult;
    public VoxelToolContext Context { get; set; } = context;
}
public abstract class PlacementTechnique
{
    protected abstract string DefaultName { get; }

    string? name;
    public string Name
    {
        get => name ?? DefaultName;
        set => name = value;
    }
    public abstract void Use(PlacementInput input);
}

public class SingleBuilder : PlacementTechnique
{
    readonly ISinglePlaceable singlePlaceable;
    public SingleBuilder(ISinglePlaceable singlePlaceable) => this.singlePlaceable = singlePlaceable;

    protected override string DefaultName => "Single voxel builder";

    public override void Use(PlacementInput input)
    {
        Vector3i normalOffset = input.RaycastResult.normal < 0 ? Vector3i.Zero : Util.VectorFromNormalIndex(input.RaycastResult.normal);
        Vector3i position = input.RaycastResult.voxelHitPos + normalOffset;
        singlePlaceable.PlaceSingle(position, input.Context.Material);
    }
}
public class SingleRemover : PlacementTechnique
{
    readonly ISinglePlaceable singlePlaceable;
    public SingleRemover(ISinglePlaceable singlePlaceable) => this.singlePlaceable = singlePlaceable;

    protected override string DefaultName => "Single voxel remover";

    public override void Use(PlacementInput input)
    {
        singlePlaceable.RemoveSingle(input.RaycastResult.voxelHitPos);
    }
}

public struct VoxelToolContext
{
    public int Material { get; set; }
}

public abstract class ComplexPlacementTechnique : PlacementTechnique
{
    public abstract void Cancel();
    public abstract void Apply(VoxelToolContext context);
}

public abstract class BatchPlacementTechnique : ComplexPlacementTechnique
{
    int nextInputIndex = 0;
    int batchLength;
    protected bool IsBatchFull { get; private set; }

    public BatchPlacementTechnique(int batchLength)
    {
        this.batchLength = batchLength;
    }

    public sealed override void Use(PlacementInput input)
    {
        if (IsBatchFull)
            return;

        OnInputReceived(input, nextInputIndex);
        nextInputIndex++;
        if (nextInputIndex == batchLength)
        {
            IsBatchFull = true;
            OnBatchFull();
        }
    }

    protected abstract void OnInputReceived(PlacementInput input, int index);
    protected virtual void OnBatchFull() { }
    protected void ResetCounter()
    {
        nextInputIndex = 0;
        IsBatchFull = false;
    }
}

public class BlockBuilder : BatchPlacementTechnique
{

    readonly ISinglePlaceable singlePlaceable;
    readonly Vector3i mapSize;

    [InspectorProperty("Points", "Two voxel positions that define a region to fill", 0)]
    public Vector3i[] Positions { get; set; }

    public BlockBuilder(ISinglePlaceable singlePlaceable, Vector3i mapSize) : base(2)
    {
        Positions = new Vector3i[2];
        this.singlePlaceable = singlePlaceable;
        this.mapSize = mapSize;
    }

    protected override string DefaultName => "Block builder";

    protected override void OnInputReceived(PlacementInput input, int index)
    {
        Positions[index] = input.RaycastResult.voxelHitPos;
    }

    public override void Cancel()
    {
        Positions[0] = Vector3i.Zero;
        Positions[1] = Vector3i.Zero;
        ResetCounter();
    }

    public override void Apply(VoxelToolContext context)
    {
        Vector3i start = Vector3i.Clamp(Positions[0], Vector3i.Zero, mapSize - Vector3i.One);
        Vector3i finish = Vector3i.Clamp(Positions[1], Vector3i.Zero, mapSize - Vector3i.One);
        Util.OrderBounds(start, finish, out Vector3i min, out Vector3i max);
        Util.LoopYZX(min, max, (pos) => singlePlaceable.PlaceSingle(pos, context.Material));

        Positions[0] = Vector3i.Zero;
        Positions[1] = Vector3i.Zero;
        ResetCounter();
    }
}

public class BlockRemover : BatchPlacementTechnique
{

    readonly ISinglePlaceable singlePlaceable;
    readonly Vector3i mapSize;

    [InspectorProperty("Points", "Two voxel positions that define a region to clear", 0)]
    public Vector3i[] Positions { get; set; }

    public BlockRemover(ISinglePlaceable singlePlaceable, Vector3i mapSize) : base(2)
    {
        Positions = new Vector3i[2];
        this.singlePlaceable = singlePlaceable;
        this.mapSize = mapSize;
    }

    protected override string DefaultName => "Block remover";

    protected override void OnInputReceived(PlacementInput input, int index)
    {
        Positions[index] = input.RaycastResult.voxelHitPos;
    }

    public override void Cancel()
    {
        Positions[0] = Vector3i.Zero;
        Positions[1] = Vector3i.Zero;
        ResetCounter();
    }

    public override void Apply(VoxelToolContext context)
    {
        Vector3i start = Vector3i.Clamp(Positions[0], Vector3i.Zero, mapSize - Vector3i.One);
        Vector3i finish = Vector3i.Clamp(Positions[1], Vector3i.Zero, mapSize - Vector3i.One);
        Util.OrderBounds(start, finish, out Vector3i min, out Vector3i max);
        Util.LoopYZX(min, max, singlePlaceable.RemoveSingle);

        Positions[0] = Vector3i.Zero;
        Positions[1] = Vector3i.Zero;
        ResetCounter();
    }
}

public class SphereBuilder : PlacementTechnique
{
    readonly ISinglePlaceable singlePlaceable;
    readonly Vector3i mapSize;

    [InspectorProperty("Radius", "A radius of the sphere to fill; in voxels", 0)]
    public float Radius { get; set; } = 5;
    protected override string DefaultName => "Sphere builder";

    public SphereBuilder(ISinglePlaceable singlePlaceable, Vector3i mapSize)
    {
        this.singlePlaceable = singlePlaceable;
        this.mapSize = mapSize;
    }

    public override void Use(PlacementInput input)
    {
        int radiusBoxSize = (int)MathF.Ceiling(Radius);
        float radiusSq = Radius * Radius;
        Vector3i start = Vector3i.Clamp(input.RaycastResult.voxelHitPos - new Vector3i(radiusBoxSize), Vector3i.Zero, mapSize - Vector3i.One);
        Vector3i finish = Vector3i.Clamp(input.RaycastResult.voxelHitPos + new Vector3i(radiusBoxSize), Vector3i.Zero, mapSize - Vector3i.One);

        Util.LoopYZX(start, finish, (pos) => 
        {
            if ((input.RaycastResult.voxelHitPos - pos).EuclideanLengthSquared < radiusSq)
                singlePlaceable.PlaceSingle(pos, input.Context.Material);
        });
    }
}

public class SphereRemover : PlacementTechnique
{
    readonly ISinglePlaceable singlePlaceable;
    readonly Vector3i mapSize;

    [InspectorProperty("Radius", "A radius of the sphere to clear; in voxels", 0)]
    public float Radius { get; set; } = 5;
    protected override string DefaultName => "Sphere remover";

    public SphereRemover(ISinglePlaceable singlePlaceable, Vector3i mapSize)
    {
        this.singlePlaceable = singlePlaceable;
        this.mapSize = mapSize;
    }

    public override void Use(PlacementInput input)
    {
        int radiusBoxSize = (int)MathF.Ceiling(Radius);
        float radiusSq = Radius * Radius;
        Vector3i start = Vector3i.Clamp(input.RaycastResult.voxelHitPos - new Vector3i(radiusBoxSize), Vector3i.Zero, mapSize - Vector3i.One);
        Vector3i finish = Vector3i.Clamp(input.RaycastResult.voxelHitPos + new Vector3i(radiusBoxSize), Vector3i.Zero, mapSize - Vector3i.One);

        Util.LoopYZX(start, finish, (pos) =>
        {
            if ((input.RaycastResult.voxelHitPos - pos).EuclideanLengthSquared < radiusSq)
                singlePlaceable.RemoveSingle(pos);
        });
    }
}