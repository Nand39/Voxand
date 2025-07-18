using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Text;

using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

using DisposableExt;
using GLAV.Types.Extended;
using GLAV.Helpers.Public.Extensions.Unsafe;

using Voxand.Engine.Systems.Structures;
using Voxand.Helpers.ExtensionMethods;
using System.Diagnostics;

namespace Voxand.Engine.Systems.Voxels;
public class VoxelBrickmap : VoxelStructure, ISinglePlaceable, IVoxelMapPersistence
{
    Vector3i brickmapSize;

    VoxelBrickHandle[,,] C_BrickGrid;
    UnmanagedList<VoxelBrickValues> C_BrickValues;
    UnmanagedList<VoxelBrickOccupancy> C_BrickOccupancy;

    TypedArray<VoxelBrickHandle> G_BrickGrid;
    HalfList<VoxelBrickValues> G_BrickValues;
    HalfList<VoxelBrickOccupancy> G_BrickOccupancy;

    const int IndexPoolChunkCapacity = 64;

    ChunkedStack<int> SpareIndexPool = new(IndexPoolChunkCapacity);

    DDAUnit DDABrick;
    DDAUnit DDAVoxel;

    readonly Func<int, int> brickListGrowthFunc = (capacity) => capacity < 24000 ? (int)MathF.Floor(-((capacity * 0.005f - 540) * capacity * 0.005f)) + 2 : (int)(capacity * 1.5f);

    public int BrickValuesBinding { set => G_BrickValues.array.BindAsShaderStorage(BufferRangeTarget.ShaderStorageBuffer, value); }
    public int BrickOccupancyBinding { set => G_BrickOccupancy.array.BindAsShaderStorage(BufferRangeTarget.ShaderStorageBuffer, value); }

    public event Action? OnMapImported;
    public event Action? OnMapExported;

    const float MaxDistanceField = 10;
    const float MinDistanceField = 0.01f;
    const float MaxDistanceFieldSquared = MaxDistanceField * MaxDistanceField;
    const float DistanceFieldBias = -0.8f;

    const int InitialBrickListCapacity = 16384;

    public VoxelBrickmap(Vector3i dimensions) : base(dimensions)
    {
        if (dimensions.X % 4 != 0 || dimensions.Y % 4 != 0 || dimensions.Z % 4 != 0)
            throw new ArgumentException("voxel brickmap dimensions should always be divisible by 4");

        brickmapSize = dimensions.BitshiftRight(2);
        C_BrickGrid = new VoxelBrickHandle[brickmapSize.Y, brickmapSize.Z, brickmapSize.X];

        for (int y = 0, ly = C_BrickGrid.GetLength(0); y < ly; y++)
            for (int z = 0, lz = C_BrickGrid.GetLength(1); z < lz; z++)
                for (int x = 0, lx = C_BrickGrid.GetLength(2); x < lx; x++)
                {
                    C_BrickGrid[y, z, x] = new(MaxDistanceField + DistanceFieldBias);
                }

        C_BrickValues = new UnmanagedList<VoxelBrickValues>(InitialBrickListCapacity, brickListGrowthFunc);
        C_BrickOccupancy = new UnmanagedList<VoxelBrickOccupancy>(InitialBrickListCapacity, brickListGrowthFunc);

        G_BrickValues = new HalfList<VoxelBrickValues>(
            target: BufferTarget.ShaderStorageBuffer, 
            initialCapacity: InitialBrickListCapacity, 
            usageHint: BufferUsageHint.DynamicDraw,
            growthFunction: brickListGrowthFunc
            );
        G_BrickValues.BindAsShaderStorage(BufferRangeTarget.ShaderStorageBuffer, 0);
        G_BrickValues.Label = "*** Brick values list";

        G_BrickOccupancy = new HalfList<VoxelBrickOccupancy>(
            target: BufferTarget.ShaderStorageBuffer,
            initialCapacity: InitialBrickListCapacity,
            usageHint: BufferUsageHint.DynamicDraw,
            growthFunction: brickListGrowthFunc
            );
        G_BrickOccupancy.BindAsShaderStorage(BufferRangeTarget.ShaderStorageBuffer, 1);
        G_BrickOccupancy.Label = "*** Brick occupancy list";

        unsafe
        {
            fixed (VoxelBrickHandle* brickGridPtr = C_BrickGrid)
            {
                Span<VoxelBrickHandle> brickGridSpan = new(brickGridPtr, C_BrickGrid.Length);
                G_BrickGrid = new(brickGridSpan, BufferTarget.ShaderStorageBuffer, BufferUsageHint.DynamicDraw);
            }
        }

        G_BrickGrid.BindAsShaderStorage(BufferRangeTarget.ShaderStorageBuffer, 2);
        G_BrickGrid.Label = "*** Brickmap";

        DDABrick = new();
        DDAVoxel = new();
    }

    public unsafe override void SetVoxelValue(Vector3i position, uint value)
    {
        VoxelBrickHandle brickHandle = GetOrAllocBrick(position);
        Vector3i localPosition = VoxelPositionToLocalPosition(position);
        SetVoxelValueInBrick(localPosition, brickHandle, value);
    }
    public unsafe void SetVoxelValueAndBit(Vector3i position, uint value, bool solid)
    {
        Vector3i brickPosition = VoxelPositionToBrickPosition(position);

        if (!solid && GetBrickHandle(brickPosition).IsEmpty)
            return;

        VoxelBrickHandle brickHandle = GetOrAllocBrickDirect(brickPosition);
        Vector3i localPosition = VoxelPositionToLocalPosition(position);

        if (!solid && C_BrickOccupancy[brickHandle.BrickIndex]->GetVoxelBit(localPosition) == C_BrickOccupancy[brickHandle.BrickIndex]->Bitmask)
        {
            RemoveBrick(brickPosition);
            return;
        }

        SetVoxelValueInBrick(localPosition, brickHandle, value);
        SetVoxelBitInBrick(localPosition, brickHandle, solid);
    }
    unsafe void SetVoxelValueInBrick(Vector3i localPosition, VoxelBrickHandle brickHandle, uint value)
    {
        uint pack = C_BrickValues[brickHandle.BrickIndex]->SetVoxelValue(localPosition, value);
        GPUSetVoxelPack(localPosition, brickHandle, pack);
    }
    unsafe void SetVoxelBitInBrick(Vector3i localPosition, VoxelBrickHandle brickHandle, bool solid)
    {
        C_BrickOccupancy[brickHandle.BrickIndex]->SetVoxelBit(localPosition, solid);
        ulong newBitmask = C_BrickOccupancy[brickHandle.BrickIndex]->Bitmask;
        G_BrickOccupancy.Write((nint)(&newBitmask), brickHandle.BrickIndex, 0, sizeof(ulong));
    }
    unsafe void GPUSetVoxelPack(Vector3i localPosition, VoxelBrickHandle brickHandle, uint pack)
    {
        int packIndex = VoxelPositionToVoxelPackIndex(localPosition);
        G_BrickValues.Write((nint)(&pack), brickHandle.BrickIndex, packIndex * sizeof(uint), sizeof(uint));
    }
    public unsafe override bool IsSolid(Vector3i position)
    {
        VoxelBrickHandle brickHandle = GetBrickHandle(VoxelPositionToBrickPosition(position));
        return brickHandle.IsEmpty ? false : IsSolid(VoxelPositionToLocalPosition(position), brickHandle.BrickIndex);
    }
    public unsafe bool IsSolid(Vector3i localPosition, int brickIndex) 
    {
        return C_BrickOccupancy[brickIndex]->GetVoxelBit(localPosition) == 0u;
    }
    public unsafe override (uint, bool) GetVoxelValue(Vector3i position)
    {
        VoxelBrickHandle brickHandle = GetBrickHandle(VoxelPositionToBrickPosition(position));
        if (brickHandle.IsEmpty)
            return (0, false);
        return (C_BrickValues[brickHandle.BrickIndex]->GetVoxelValue(VoxelPositionToLocalPosition(position)), true);
    }
    public void PlaceSingle(Vector3i position, int value) => SetVoxelValueAndBit(position, (uint)value, true);
    public void RemoveSingle(Vector3i position) => SetVoxelValueAndBit(position, 0, false);

    public unsafe (uint voxelValue, ulong voxelBit, VoxelBrickHandle brickHandle) Examine(Vector3i position, bool fromGPU)
    {
        VoxelBrickValues brickValues;
        VoxelBrickOccupancy brickOccupancy;
        Vector3i localPosition = VoxelPositionToLocalPosition(position);
        VoxelBrickHandle brickHandle;
        uint voxelValue;
        ulong voxelBit;

        if (!fromGPU)
        {
            brickHandle = GetBrickHandle(VoxelPositionToBrickPosition(position));
            if (brickHandle.IsEmpty)
                return (0, 0, brickHandle);
            brickValues = *C_BrickValues[brickHandle.BrickIndex];
            brickOccupancy = *C_BrickOccupancy[brickHandle.BrickIndex];
        }
        else
        {
            Vector3i brickPosition = VoxelPositionToBrickPosition(position);
            brickHandle = G_BrickGrid[brickPosition.AsIndexYZX(brickmapSize)];
            if (brickHandle.IsEmpty)
                return (0, 0, brickHandle);
            brickValues = G_BrickValues[brickHandle.BrickIndex];
            brickOccupancy = G_BrickOccupancy[brickHandle.BrickIndex];
        }

        voxelValue = brickValues.GetVoxelValue(localPosition);
        voxelBit = brickOccupancy.GetVoxelBit(localPosition);

        return (voxelValue, voxelBit, brickHandle);
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public override unsafe RaycastResult Raycast(Vector3 origin, Vector3 dir)
    {
        Vector3i originVoxel = (Vector3i)origin;
        Vector3i brickPosition = originVoxel.BitshiftRight(2);
        DDAUnit dda = new();
        ulong bitmask;
        Vector3 entrance;
        BrickTraversalResult traversalResult;
        Vector3 brickSpaceOrigin = origin / 4;
        Vector3i brickOffset = VoxelPositionToBrickPosition(originVoxel).BitshiftLeft(2);

        dda.Begin(brickSpaceOrigin, dir, brickPosition);

        // Traversing origin brick
        VoxelBrickHandle brickHandle = GetBrickHandle(dda.CurrentVoxelPos);

        if (!brickHandle.IsEmpty)
        {
            bitmask = C_BrickOccupancy[brickHandle.BrickIndex]->Bitmask;
            
            entrance = origin - brickOffset;

            traversalResult = TraverseBrickHardcoded(bitmask, dir, VoxelPositionToLocalPosition(originVoxel), entrance, dda.TimeToCross);

            if (traversalResult.escaped == false)
            {
                if (traversalResult.lastAxis == -1)
                {
                    return new RaycastResult()
                    {
                        hit = true,
                        hitPos = origin,
                        voxelHitPos = traversalResult.lastVoxelPosition + brickOffset,
                        normal = -1,
                        depth = 0
                    };
                }

                int normal = NormalIndex(dir, traversalResult.lastAxis);

                float localDepth = MathF.Abs((dir[traversalResult.lastAxis] > 0 ? traversalResult.lastVoxelPosition[traversalResult.lastAxis] : traversalResult.lastVoxelPosition[traversalResult.lastAxis] + 1) - entrance[traversalResult.lastAxis]) * dda.TimeToCross[traversalResult.lastAxis];
                return new RaycastResult()
                {
                    hit = true,
                    hitPos = dir * localDepth + origin,
                    normal = normal,
                    voxelHitPos = traversalResult.lastVoxelPosition + brickOffset,
                    depth = localDepth
                };
            }
        }
        
        // Traversing bricks along the ray direction
        while (true)
        {
            dda.Step();
            if (!dda.CurrentVoxelPos.Inbounds(Vector3i.Zero, brickmapSize))
            {
                return new RaycastResult()
                {
                    hit = false,
                    depth = dda.LastHitDepth,
                    normal = -1,
                };
            }
            brickOffset = dda.CurrentVoxelPos.BitshiftLeft(2);

            brickHandle = GetBrickHandle(dda.CurrentVoxelPos);
            if (!brickHandle.IsEmpty)
            {
                bitmask = C_BrickOccupancy[brickHandle.BrickIndex]->Bitmask;
                entrance = dda.LastHitDepth * dir + brickSpaceOrigin;

                entrance -= (Vector3i)entrance;
                entrance[dda.LastAxis] = dir[dda.LastAxis] > 0 ? 0 : 1;
                entrance *= 4;
                Vector3i entranceVoxel = (Vector3i)entrance;
                entranceVoxel[dda.LastAxis] = dir[dda.LastAxis] > 0 ? 0 : 3;

                traversalResult = TraverseBrickHardcoded(bitmask, dir, entranceVoxel, entrance, dda.TimeToCross);

                if (traversalResult.escaped == false)
                {
                    traversalResult.lastAxis = traversalResult.lastAxis == -1 ? dda.LastAxis : traversalResult.lastAxis;

                    int normal = NormalIndex(dir, traversalResult.lastAxis);

                    float localDepth = MathF.Abs((dir[traversalResult.lastAxis] > 0 ? traversalResult.lastVoxelPosition[traversalResult.lastAxis] : traversalResult.lastVoxelPosition[traversalResult.lastAxis] + 1) - entrance[traversalResult.lastAxis]) * dda.TimeToCross[traversalResult.lastAxis];

                    Vector3 hitPos = dir * localDepth + entrance + brickOffset;
                    return new RaycastResult()
                    {
                        hit = true,
                        hitPos = hitPos,
                        normal = normal,
                        voxelHitPos = traversalResult.lastVoxelPosition + brickOffset,
                        depth = (hitPos - origin).Length
                    };
                }
            }
        }
    }

    struct BrickTraversalResult
    {
        public bool escaped;
        public int lastAxis;
        public Vector3i lastVoxelPosition;
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    BrickTraversalResult TraverseBrick(ulong bitmask, Vector3 dir, Vector3i voxelPosition, Vector3 startingPosition, Vector3 timeToCross)
    {
        Vector3 nextIntersectionTime = new(
        dir.X < 0 ? startingPosition.X - voxelPosition.X : voxelPosition.X + 1 - startingPosition.X,
        dir.Y < 0 ? startingPosition.Y - voxelPosition.Y : voxelPosition.Y + 1 - startingPosition.Y,
        dir.Z < 0 ? startingPosition.Z - voxelPosition.Z : voxelPosition.Z + 1 - startingPosition.Z);
        nextIntersectionTime *= timeToCross;


        ulong voxelBit = VoxelBrickOccupancy.CreateVoxelMask(voxelPosition);

        if ((bitmask & voxelBit) > 0)
        {
            return new BrickTraversalResult()
            {
                escaped = false,
                lastAxis = -1,
                lastVoxelPosition = voxelPosition,
            };
        }

        int axisToStep;

        while (true)
        {
            axisToStep = nextIntersectionTime.X <= nextIntersectionTime.Y ?
            (nextIntersectionTime.X <= nextIntersectionTime.Z ? 0 : 2) :
            (nextIntersectionTime.Y <= nextIntersectionTime.Z ? 1 : 2);

            if (axisToStep == 0)
            {
                if (dir.X > 0)
                {
                    if (voxelPosition.X == 3)
                        break;

                    voxelBit <<= 1;
                    voxelPosition.X++;
                }
                else
                {
                    if (voxelPosition.X == 0)
                        break;

                    voxelBit >>= 1;
                    voxelPosition.X--;
                }
                nextIntersectionTime.X += timeToCross.X;
            }
            else if (axisToStep == 2)
            {
                if (dir.Z > 0)
                {
                    if (voxelPosition.Z == 3)
                        break;

                    voxelBit <<= 4;
                    voxelPosition.Z++;
                }
                else
                {
                    if (voxelPosition.Z == 0)
                        break;

                    voxelBit >>= 4;
                    voxelPosition.Z--;
                }
                nextIntersectionTime.Z += timeToCross.Z;
            }
            else
            {
                if (dir.Y > 0)
                {
                    if (voxelPosition.Y == 3)
                        break;

                    voxelBit <<= 16;
                    voxelPosition.Y++;
                }
                else
                {
                    if (voxelPosition.Y == 0)
                        break;

                    voxelBit >>= 16;
                    voxelPosition.Y--;
                }
                nextIntersectionTime.Y += timeToCross.Y;
            }

            if ((bitmask & voxelBit) > 0)
            {
                return new BrickTraversalResult()
                {
                    escaped = false,
                    lastAxis = axisToStep,
                    lastVoxelPosition = voxelPosition,
                };
            }
        }

        return new BrickTraversalResult()
        {
            escaped = true,
            lastAxis = axisToStep,
            lastVoxelPosition = voxelPosition,
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    BrickTraversalResult TraverseBrickHardcoded(ulong bitmask, Vector3 dir, Vector3i voxelPosition, Vector3 startingPosition, Vector3 timeToCross)
    {
        Vector3 nextIntersectionTime = new(
        dir.X < 0 ? startingPosition.X - voxelPosition.X : voxelPosition.X + 1 - startingPosition.X,
        dir.Y < 0 ? startingPosition.Y - voxelPosition.Y : voxelPosition.Y + 1 - startingPosition.Y,
        dir.Z < 0 ? startingPosition.Z - voxelPosition.Z : voxelPosition.Z + 1 - startingPosition.Z);
        nextIntersectionTime *= timeToCross;


        ulong voxelBit = VoxelBrickOccupancy.CreateVoxelMask(voxelPosition);

        if ((bitmask & voxelBit) > 0)
        {
            return new BrickTraversalResult()
            {
                escaped = false,
                lastAxis = -1,
                lastVoxelPosition = voxelPosition,
            };
        }

        int axisToStep;

        int route = dir.Y < 0 ? 0 : 4;
        route += dir.Z < 0 ? 0 : 2;
        route += dir.X < 0 ? 0 : 1;

        switch (route)
        {
            default: throw new InvalidOperationException("Invalid brick traversal route.");

            // X-; Y-; Z-;
            case 0:
                {
                    while (true)
                    {
                        axisToStep = nextIntersectionTime.X <= nextIntersectionTime.Y ?
                        (nextIntersectionTime.X <= nextIntersectionTime.Z ? 0 : 2) :
                        (nextIntersectionTime.Y <= nextIntersectionTime.Z ? 1 : 2);

                        if (axisToStep == 0)
                        {
                            if (voxelPosition.X == 0)
                                break;

                            voxelBit >>= 1;
                            voxelPosition.X--;
                            nextIntersectionTime.X += timeToCross.X;
                        }
                        else if (axisToStep == 2)
                        {
                            if (voxelPosition.Z == 0)
                                break;

                            voxelBit >>= 4;
                            voxelPosition.Z--;
                            nextIntersectionTime.Z += timeToCross.Z;
                        }
                        else
                        {
                            if (voxelPosition.Y == 0)
                                break;

                            voxelBit >>= 16;
                            voxelPosition.Y--;
                            nextIntersectionTime.Y += timeToCross.Y;
                        }

                        if ((bitmask & voxelBit) > 0)
                        {
                            return new BrickTraversalResult()
                            {
                                escaped = false,
                                lastAxis = axisToStep,
                                lastVoxelPosition = voxelPosition,
                            };
                        }
                    }
                    return new BrickTraversalResult()
                    {
                        escaped = true,
                        lastAxis = axisToStep,
                        lastVoxelPosition = voxelPosition,
                    };
                }

            // X+; Y+; Z+;
            case 7:
                {
                    while (true)
                    {
                        axisToStep = nextIntersectionTime.X <= nextIntersectionTime.Y ?
                        (nextIntersectionTime.X <= nextIntersectionTime.Z ? 0 : 2) :
                        (nextIntersectionTime.Y <= nextIntersectionTime.Z ? 1 : 2);

                        if (axisToStep == 0)
                        {
                            if (voxelPosition.X == 3)
                                break;

                            voxelBit <<= 1;
                            voxelPosition.X++;
                            nextIntersectionTime.X += timeToCross.X;
                        }
                        else if (axisToStep == 2)
                        {
                            if (voxelPosition.Z == 3)
                                break;

                            voxelBit <<= 4;
                            voxelPosition.Z++;
                            nextIntersectionTime.Z += timeToCross.Z;
                        }
                        else
                        {
                            if (voxelPosition.Y == 3)
                                break;

                            voxelBit <<= 16;
                            voxelPosition.Y++;
                            nextIntersectionTime.Y += timeToCross.Y;
                        }

                        if ((bitmask & voxelBit) > 0)
                        {
                            return new BrickTraversalResult()
                            {
                                escaped = false,
                                lastAxis = axisToStep,
                                lastVoxelPosition = voxelPosition,
                            };
                        }
                    }
                    return new BrickTraversalResult()
                    {
                        escaped = true,
                        lastAxis = axisToStep,
                        lastVoxelPosition = voxelPosition,
                    };
                }

            // X-; Y+; Z+;
            case 6:
                {
                    while (true)
                    {
                        axisToStep = nextIntersectionTime.X <= nextIntersectionTime.Y ?
                        (nextIntersectionTime.X <= nextIntersectionTime.Z ? 0 : 2) :
                        (nextIntersectionTime.Y <= nextIntersectionTime.Z ? 1 : 2);

                        if (axisToStep == 0)
                        {
                            if (voxelPosition.X == 0)
                                break;

                            voxelBit >>= 1;
                            voxelPosition.X--;
                            nextIntersectionTime.X += timeToCross.X;
                        }
                        else if (axisToStep == 2)
                        {
                            if (voxelPosition.Z == 3)
                                break;

                            voxelBit <<= 4;
                            voxelPosition.Z++;
                            nextIntersectionTime.Z += timeToCross.Z;
                        }
                        else
                        {
                            if (voxelPosition.Y == 3)
                                break;

                            voxelBit <<= 16;
                            voxelPosition.Y++;
                            nextIntersectionTime.Y += timeToCross.Y;
                        }

                        if ((bitmask & voxelBit) > 0)
                        {
                            return new BrickTraversalResult()
                            {
                                escaped = false,
                                lastAxis = axisToStep,
                                lastVoxelPosition = voxelPosition,
                            };
                        }
                    }
                    return new BrickTraversalResult()
                    {
                        escaped = true,
                        lastAxis = axisToStep,
                        lastVoxelPosition = voxelPosition,
                    };
                }

            // X+; Y+; Z-;
            case 5:
                {
                    while (true)
                    {
                        axisToStep = nextIntersectionTime.X <= nextIntersectionTime.Y ?
                        (nextIntersectionTime.X <= nextIntersectionTime.Z ? 0 : 2) :
                        (nextIntersectionTime.Y <= nextIntersectionTime.Z ? 1 : 2);

                        if (axisToStep == 0)
                        {
                            if (voxelPosition.X == 3)
                                break;

                            voxelBit <<= 1;
                            voxelPosition.X++;
                            nextIntersectionTime.X += timeToCross.X;
                        }
                        else if (axisToStep == 2)
                        {
                            if (voxelPosition.Z == 0)
                                break;

                            voxelBit >>= 4;
                            voxelPosition.Z--;
                            nextIntersectionTime.Z += timeToCross.Z;
                        }
                        else
                        {
                            if (voxelPosition.Y == 3)
                                break;

                            voxelBit <<= 16;
                            voxelPosition.Y++;
                            nextIntersectionTime.Y += timeToCross.Y;
                        }

                        if ((bitmask & voxelBit) > 0)
                        {
                            return new BrickTraversalResult()
                            {
                                escaped = false,
                                lastAxis = axisToStep,
                                lastVoxelPosition = voxelPosition,
                            };
                        }
                    }
                    return new BrickTraversalResult()
                    {
                        escaped = true,
                        lastAxis = axisToStep,
                        lastVoxelPosition = voxelPosition,
                    };
                }

            // X-; Y+; Z-;
            case 4:
                {
                    while (true)
                    {
                        axisToStep = nextIntersectionTime.X <= nextIntersectionTime.Y ?
                        (nextIntersectionTime.X <= nextIntersectionTime.Z ? 0 : 2) :
                        (nextIntersectionTime.Y <= nextIntersectionTime.Z ? 1 : 2);

                        if (axisToStep == 0)
                        {
                            if (voxelPosition.X == 0)
                                break;

                            voxelBit >>= 1;
                            voxelPosition.X--;
                            nextIntersectionTime.X += timeToCross.X;
                        }
                        else if (axisToStep == 2)
                        {
                            if (voxelPosition.Z == 0)
                                break;

                            voxelBit >>= 4;
                            voxelPosition.Z--;
                            nextIntersectionTime.Z += timeToCross.Z;
                        }
                        else
                        {
                            if (voxelPosition.Y == 3)
                                break;

                            voxelBit <<= 16;
                            voxelPosition.Y++;
                            nextIntersectionTime.Y += timeToCross.Y;
                        }

                        if ((bitmask & voxelBit) > 0)
                        {
                            return new BrickTraversalResult()
                            {
                                escaped = false,
                                lastAxis = axisToStep,
                                lastVoxelPosition = voxelPosition,
                            };
                        }
                    }
                    return new BrickTraversalResult()
                    {
                        escaped = true,
                        lastAxis = axisToStep,
                        lastVoxelPosition = voxelPosition,
                    };
                }

            // X+; Y-; Z+;
            case 3:
                {
                    while (true)
                    {
                        axisToStep = nextIntersectionTime.X <= nextIntersectionTime.Y ?
                        (nextIntersectionTime.X <= nextIntersectionTime.Z ? 0 : 2) :
                        (nextIntersectionTime.Y <= nextIntersectionTime.Z ? 1 : 2);

                        if (axisToStep == 0)
                        {
                            if (voxelPosition.X == 3)
                                break;

                            voxelBit <<= 1;
                            voxelPosition.X++;
                            nextIntersectionTime.X += timeToCross.X;
                        }
                        else if (axisToStep == 2)
                        {
                            if (voxelPosition.Z == 3)
                                break;

                            voxelBit <<= 4;
                            voxelPosition.Z++;
                            nextIntersectionTime.Z += timeToCross.Z;
                        }
                        else
                        {
                            if (voxelPosition.Y == 0)
                                break;

                            voxelBit >>= 16;
                            voxelPosition.Y--;
                            nextIntersectionTime.Y += timeToCross.Y;
                        }

                        if ((bitmask & voxelBit) > 0)
                        {
                            return new BrickTraversalResult()
                            {
                                escaped = false,
                                lastAxis = axisToStep,
                                lastVoxelPosition = voxelPosition,
                            };
                        }
                    }
                    return new BrickTraversalResult()
                    {
                        escaped = true,
                        lastAxis = axisToStep,
                        lastVoxelPosition = voxelPosition,
                    };
                }

            // X-; Y-; Z+;
            case 2:
                {
                    while (true)
                    {
                        axisToStep = nextIntersectionTime.X <= nextIntersectionTime.Y ?
                        (nextIntersectionTime.X <= nextIntersectionTime.Z ? 0 : 2) :
                        (nextIntersectionTime.Y <= nextIntersectionTime.Z ? 1 : 2);

                        if (axisToStep == 0)
                        {
                            if (voxelPosition.X == 0)
                                break;

                            voxelBit >>= 1;
                            voxelPosition.X--;
                            nextIntersectionTime.X += timeToCross.X;
                        }
                        else if (axisToStep == 2)
                        {
                            if (voxelPosition.Z == 3)
                                break;

                            voxelBit <<= 4;
                            voxelPosition.Z++;
                            nextIntersectionTime.Z += timeToCross.Z;
                        }
                        else
                        {
                            if (voxelPosition.Y == 0)
                                break;

                            voxelBit >>= 16;
                            voxelPosition.Y--;
                            nextIntersectionTime.Y += timeToCross.Y;
                        }

                        if ((bitmask & voxelBit) > 0)
                        {
                            return new BrickTraversalResult()
                            {
                                escaped = false,
                                lastAxis = axisToStep,
                                lastVoxelPosition = voxelPosition,
                            };
                        }
                    }
                    return new BrickTraversalResult()
                    {
                        escaped = true,
                        lastAxis = axisToStep,
                        lastVoxelPosition = voxelPosition,
                    };
                }

            // X+; Y-; Z-;
            case 1:
                {
                    while (true)
                    {
                        axisToStep = nextIntersectionTime.X <= nextIntersectionTime.Y ?
                        (nextIntersectionTime.X <= nextIntersectionTime.Z ? 0 : 2) :
                        (nextIntersectionTime.Y <= nextIntersectionTime.Z ? 1 : 2);

                        if (axisToStep == 0)
                        {
                            if (voxelPosition.X == 3)
                                break;

                            voxelBit <<= 1;
                            voxelPosition.X++;
                            nextIntersectionTime.X += timeToCross.X;
                        }
                        else if (axisToStep == 2)
                        {
                            if (voxelPosition.Z == 0)
                                break;

                            voxelBit >>= 4;
                            voxelPosition.Z--;
                            nextIntersectionTime.Z += timeToCross.Z;
                        }
                        else
                        {
                            if (voxelPosition.Y == 0)
                                break;

                            voxelBit >>= 16;
                            voxelPosition.Y--;
                            nextIntersectionTime.Y += timeToCross.Y;
                        }

                        if ((bitmask & voxelBit) > 0)
                        {
                            return new BrickTraversalResult()
                            {
                                escaped = false,
                                lastAxis = axisToStep,
                                lastVoxelPosition = voxelPosition,
                            };
                        }
                    }
                    return new BrickTraversalResult()
                    {
                        escaped = true,
                        lastAxis = axisToStep,
                        lastVoxelPosition = voxelPosition,
                    };
                }
        }
    }

    int NormalIndex(Vector3 dir, int lastAxis) => dir[lastAxis] > 0 ? lastAxis << 1 : (lastAxis << 1) + 1;
    public override unsafe long GetMemoryUsage()
    {
        return C_BrickGrid.Length * sizeof(VoxelBrickHandle) + C_BrickValues.Capacity * sizeof(VoxelBrickValues) + C_BrickOccupancy.Capacity * sizeof(VoxelBrickOccupancy);
    }
    public override unsafe long GetGraphicsMemoryUsage()
    {
        return G_BrickGrid.Length * sizeof(VoxelBrickHandle) + C_BrickValues.Capacity * sizeof(VoxelBrickValues) + C_BrickOccupancy.Capacity * sizeof(VoxelBrickOccupancy);
    }
    protected override void Free()
    {
        G_BrickValues.Dispose();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    Vector3i VoxelPositionToBrickPosition(Vector3i position) => position.BitshiftRight(2);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    Vector3i VoxelPositionToLocalPosition(Vector3i position) => position.BitwiseAnd(3);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    int VoxelPositionToVoxelPackIndex(Vector3i localPosition) => (localPosition.Y << 2) + localPosition.Z;
    public unsafe VoxelBrickHandle SetBrick(Vector3i brickPosition, in VoxelBrickValues brickValues, VoxelBrickOccupancy brickOccupancy)
    {
        VoxelBrickHandle brickHandle = GetBrickHandle(brickPosition);
        if (brickHandle.IsEmpty)
        {
            AllocBrick(brickPosition, brickValues, brickOccupancy);
        }
        else
        {
            fixed (VoxelBrickValues* brickPtr = &brickValues)
            {
                C_BrickValues[brickHandle.BrickIndex] = brickPtr;
                G_BrickValues[brickHandle.BrickIndex] = brickValues;
                C_BrickOccupancy[brickHandle.BrickIndex] = &brickOccupancy;
                G_BrickOccupancy[brickHandle.BrickIndex] = brickOccupancy;
            }
        }
        return brickHandle;
    }

    public VoxelBrickHandle AllocBrick(Vector3i brickPosition, in VoxelBrickValues values, VoxelBrickOccupancy occupancy)
    {
        int brickIndex;

        if (!SpareIndexPool.Empty)
        {
            brickIndex = SpareIndexPool.Pop();

            G_BrickValues[brickIndex] = values;
            G_BrickOccupancy[brickIndex] = occupancy;
            
            unsafe
            {
                *C_BrickValues[brickIndex] = values;
                *C_BrickOccupancy[brickIndex] = occupancy;
            }
        }
        else
        {
            brickIndex = G_BrickValues.Add(values);
            G_BrickOccupancy.Add(occupancy);

            C_BrickValues.Add(values);
            C_BrickOccupancy.Add(occupancy);
        }
        

        VoxelBrickHandle brickHandle = new(brickIndex);

        SetBrickHandleSync(brickPosition, brickHandle);

        UpdateDistanceFieldOnBrickAllocated(brickPosition);

        return brickHandle;
    }
    VoxelBrickHandle GetOrAllocBrick(Vector3i position)
    {
        Vector3i brickPosition = VoxelPositionToBrickPosition(position);
        return GetOrAllocBrickDirect(brickPosition);
    }
    VoxelBrickHandle GetOrAllocBrickDirect(Vector3i brickPosition)
    {
        VoxelBrickHandle brickHandle = GetBrickHandle(brickPosition);
        if (brickHandle.IsEmpty)
        {
            VoxelBrickValues newBrickValues = new();
            VoxelBrickOccupancy newBrickOccupancy = new();
            brickHandle = AllocBrick(brickPosition, newBrickValues, newBrickOccupancy);
        }
        return brickHandle;
    }

    void RemoveBrick(Vector3i brickPosition)
    {
        RemoveBrickContents(brickPosition);
        SetBrickHandleLocal(brickPosition, new(MinDistanceField));
        UpdateDistanceFieldOnBrickRemoved(brickPosition);
    }
    
    void RemoveBrickContents(Vector3i brickPosition)
    {
        VoxelBrickHandle brickHandle = GetBrickHandle(brickPosition);
        
        if (brickHandle.IsEmpty)
            return;
        
        SpareIndexPool.Push(brickHandle.BrickIndex);
    }

    VoxelBrickHandle GetBrickHandle(Vector3i brickPosition)
    {
        VoxelBrickHandle handle = C_BrickGrid[brickPosition.Y, brickPosition.Z, brickPosition.X];
        if (!handle.IsEmpty && handle.BrickIndex > C_BrickValues.Count)
#if DEBUG
            Debugger.Break();
#else
            throw new InvalidOperationException("out of range handle");
#endif
        return handle;
    }
    void SetBrickHandleSync(Vector3i brickPosition, VoxelBrickHandle brickHandle)
    {
        SetBrickHandleLocal(brickPosition, brickHandle);
        G_BrickGrid[brickPosition.AsIndexYZX(brickmapSize)] = brickHandle;
    }

    void SetBrickHandleLocal(Vector3i brickPosition, VoxelBrickHandle brickHandle)
    {
        C_BrickGrid[brickPosition.Y, brickPosition.Z, brickPosition.X] = brickHandle;
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    void UpdateDistanceFieldOnBrickAllocated(Vector3i brickPosition)
    {
        Vector3i min = Vector3i.Clamp(brickPosition - new Vector3i((int)MaxDistanceField), Vector3i.Zero, brickmapSize - Vector3i.One);
        Vector3i max = Vector3i.Clamp(brickPosition + new Vector3i((int)MaxDistanceField), Vector3i.Zero, brickmapSize - Vector3i.One);
        Vector3i pos;
        VoxelBrickHandle brickHandle;
        for (pos.Y = min.Y; pos.Y <= max.Y; pos.Y++)
            for (pos.Z = min.Z; pos.Z <= max.Z; pos.Z++)
                for (pos.X = min.X; pos.X <= max.X; pos.X++)
                {
                    brickHandle = GetBrickHandle(pos);

                    if (!brickHandle.IsEmpty)
                        continue;
                    
                    float distance = MinDistanceBetweenBricksSquared(pos, brickPosition);

                    if (distance > MaxDistanceFieldSquared)
                        continue;

                    distance = Math.Max(MathF.Sqrt(distance) + DistanceFieldBias, MinDistanceField);

                    if (distance < brickHandle.DistanceFieldValue)
                        SetBrickHandleSync(pos, new VoxelBrickHandle(distance));
                }
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    void UpdateDistanceFieldOnBrickRemoved(Vector3i brickPosition)
    {
        Vector3i min = Vector3i.Clamp(brickPosition - new Vector3i((int)MaxDistanceField), Vector3i.Zero, brickmapSize - Vector3i.One);
        Vector3i max = Vector3i.Clamp(brickPosition + new Vector3i((int)MaxDistanceField), Vector3i.Zero, brickmapSize - Vector3i.One);
        Vector3i pos;
        VoxelBrickHandle brickHandle;
        for (pos.Y = min.Y; pos.Y <= max.Y; pos.Y++)
            for (pos.Z = min.Z; pos.Z <= max.Z; pos.Z++)
                for (pos.X = min.X; pos.X <= max.X; pos.X++)
                {
                    brickHandle = GetBrickHandle(pos);

                    if (!brickHandle.IsEmpty)
                        continue;

                    float distance = MinDistanceBetweenBricksSquared(pos, brickPosition);

                    if (distance > MaxDistanceFieldSquared)
                        continue;

                    distance = Math.Max(MathF.Sqrt(distance) + DistanceFieldBias, MinDistanceField);

                    if (distance == brickHandle.DistanceFieldValue)
                        SetBrickHandleSync(pos, new VoxelBrickHandle(DistanceToNearestOccupiedBrick(pos)));
                }
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    float DistanceToNearestOccupiedBrick(Vector3i brickPosition)
    {
        Vector3i min = Vector3i.Clamp(brickPosition - new Vector3i((int)MaxDistanceField), Vector3i.Zero, brickmapSize - Vector3i.One);
        Vector3i max = Vector3i.Clamp(brickPosition + new Vector3i((int)MaxDistanceField), Vector3i.Zero, brickmapSize - Vector3i.One);
        Vector3i pos;
        VoxelBrickHandle brickHandle;
        float minDistance = MaxDistanceField + DistanceFieldBias;

        for (pos.Y = min.Y; pos.Y <= max.Y; pos.Y++)
            for (pos.Z = min.Z; pos.Z <= max.Z; pos.Z++)
                for (pos.X = min.X; pos.X <= max.X; pos.X++)
                {
                    brickHandle = GetBrickHandle(pos);

                    if (brickHandle.IsEmpty)
                        continue;

                    float distance = MinDistanceBetweenBricksSquared(pos, brickPosition);
                    
                    if (distance > MaxDistanceFieldSquared)
                        continue;

                    distance = Math.Max(MathF.Sqrt(distance) + DistanceFieldBias, MinDistanceField);
                    minDistance = Math.Min(distance, minDistance);
                }

        return minDistance;
    }

    static float MinDistanceBetweenBricksSquared(Vector3i pos0, Vector3i pos1)
    {
        Vector3i delta = pos0 - pos1;
        delta -= delta.Sign();
        return delta.EuclideanLengthSquared;
    }

    public unsafe void Export(Stream stream)
    {
        using BinaryWriter writer = new(stream, Encoding.UTF8, true);
        writer.Write(dimensions);
        writer.Write(C_BrickValues.Count);

        ReadOnlySpan<byte> data;
        fixed (VoxelBrickHandle* indexGridPtr = C_BrickGrid)
        {
            data = new(indexGridPtr, C_BrickGrid.Length * sizeof(VoxelBrickHandle));
            writer.Write(data);
        }

        VoxelBrickValues* brickListPtr = C_BrickValues[0];
        data = new(brickListPtr, C_BrickValues.Count * sizeof(VoxelBrickValues));
        writer.Write(data);

        VoxelBrickOccupancy* occupancyListPtr = C_BrickOccupancy[0];
        data = new(occupancyListPtr, C_BrickOccupancy.Count * sizeof(VoxelBrickOccupancy));
        writer.Write(data);

        int spareIndicesCount = SpareIndexPool.Count;
        writer.Write(spareIndicesCount);

        Console.WriteLine($"saved {C_BrickValues.Count} brickNum; {spareIndicesCount} indices.");

        IEnumerator<int[]> indexChunksEnumerator = SpareIndexPool.ReadChunks().GetEnumerator();

        int fullChunks = spareIndicesCount / SpareIndexPool.ChunkCapacity;
        for (int i = 0; i < fullChunks; i++)
        {
            indexChunksEnumerator.MoveNext();
            fixed (int* chunkPtr = indexChunksEnumerator.Current)
            {
                writer.Write(new ReadOnlySpan<byte>(chunkPtr, sizeof(int) * indexChunksEnumerator.Current.Length));
            }
        }
        indexChunksEnumerator.MoveNext();
        fixed (int* chunkPtr = indexChunksEnumerator.Current)
        {
            writer.Write(new ReadOnlySpan<byte>(chunkPtr, sizeof(int) * (spareIndicesCount - fullChunks * SpareIndexPool.ChunkCapacity)));
        }

        OnMapExported?.Invoke();
    }

    public unsafe void Import(Stream stream)
    {
        using BinaryReader reader = new(stream);

        dimensions = reader.Read<Vector3i>();
        int numBricks = reader.ReadInt32();

        brickmapSize = dimensions.BitshiftRight(2);
        C_BrickGrid = new VoxelBrickHandle[brickmapSize.Y, brickmapSize.Z, brickmapSize.X];
        C_BrickValues = new UnmanagedList<VoxelBrickValues>(numBricks, brickListGrowthFunc);
        C_BrickOccupancy = new UnmanagedList<VoxelBrickOccupancy>(numBricks, brickListGrowthFunc);

        fixed (VoxelBrickHandle* indexGridPtr = C_BrickGrid) 
        {
            Span<byte> indexGridSpan = new(indexGridPtr, C_BrickGrid.Length * sizeof(VoxelBrickHandle));
            reader.Read(indexGridSpan);
        }

        Span<byte> buffer = new byte[numBricks * sizeof(VoxelBrickValues)];

        reader.Read(buffer);
        C_BrickValues.WriteOrAdd(MemoryMarshal.Cast<byte, VoxelBrickValues>(buffer), 0);

        G_BrickValues.Dispose();
        G_BrickValues = new HalfList<VoxelBrickValues>(
            target: BufferTarget.ShaderStorageBuffer,
            initialCapacity: numBricks,
            usageHint: BufferUsageHint.DynamicDraw,
            growthFunction: brickListGrowthFunc
            );
        G_BrickValues.BindAsShaderStorage(BufferRangeTarget.ShaderStorageBuffer, 0);
        G_BrickValues.Label = "*** Brick values list";
        G_BrickValues.WriteOrAdd(MemoryMarshal.Cast<byte, VoxelBrickValues>(buffer), 0);



        buffer = buffer.Slice(0, numBricks * sizeof(VoxelBrickOccupancy));
        reader.Read(buffer);
        C_BrickOccupancy.WriteOrAdd(MemoryMarshal.Cast<byte, VoxelBrickOccupancy>(buffer), 0);

        G_BrickOccupancy.Dispose();
        G_BrickOccupancy = new HalfList<VoxelBrickOccupancy>(
            target: BufferTarget.ShaderStorageBuffer,
            initialCapacity: numBricks,
            usageHint: BufferUsageHint.DynamicDraw,
            growthFunction: brickListGrowthFunc
            );
        G_BrickOccupancy.BindAsShaderStorage(BufferRangeTarget.ShaderStorageBuffer, 1);
        G_BrickOccupancy.Label = "*** Brick occupancy list";
        G_BrickOccupancy.WriteOrAdd(MemoryMarshal.Cast<byte, VoxelBrickOccupancy>(buffer), 0);



        SpareIndexPool = new(IndexPoolChunkCapacity);
        int indexCount = reader.ReadInt32();
        Console.WriteLine($"restored {numBricks} brickNum; {indexCount} indices.");

        Span<int> indexPoolbuffer = indexCount < 256 ? stackalloc int[indexCount] : GC.AllocateUninitializedArray<int>(indexCount);
        reader.Read(MemoryMarshal.Cast<int, byte>(indexPoolbuffer));

        for (int i = 0; i < indexCount; i++)
        {
            SpareIndexPool.Push(indexPoolbuffer[i]);
        }

        for (int i = 0; i < indexCount && i < 3; i++)
            Console.WriteLine(indexPoolbuffer[i]);

        G_BrickGrid.Dispose();
        fixed (VoxelBrickHandle* indexGridPtr = C_BrickGrid)
        {
            Span<VoxelBrickHandle> indexGridSpan = new(indexGridPtr, C_BrickGrid.Length);
            G_BrickGrid = new(indexGridSpan, BufferTarget.ShaderStorageBuffer, BufferUsageHint.DynamicDraw);
        }

        G_BrickGrid.BindAsShaderStorage(BufferRangeTarget.ShaderStorageBuffer, 2);
        G_BrickGrid.Label = "*** Brickmap";

        OnMapImported?.Invoke();
    }
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct VoxelBrickOccupancy
{
    ulong bitmask;

    public ulong Bitmask => bitmask;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ulong GetVoxelBit(Vector3i localPosition) => bitmask & CreateVoxelMask(localPosition);
    public unsafe void SetVoxelBit(Vector3i localPosition, bool solid)
    {
        ulong mask = CreateVoxelMask(localPosition);
        bitmask = solid ? bitmask | mask : bitmask & (~mask);
    }
    public static ulong CreateVoxelMask(Vector3i localPos) => 1UL << ((localPos.Y << 4) + (localPos.Z << 2) + localPos.X);
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct VoxelBrickValues
{
    fixed uint packs[16];

    public uint GetVoxelValue(Vector3i localPosition)
    {
        uint pack = packs[GetVoxelPackIndex(localPosition)];
        int shift = localPosition.X << 3;
        return (pack & (255u << shift)) >> shift;
    }
    public uint SetVoxelValue(Vector3i localPosition, uint value)
    {
        int packIndex = GetVoxelPackIndex(localPosition);
        int shift = localPosition.X << 3;
        uint pack = (packs[packIndex] & (~(255u << shift))) | (value << shift);
        packs[packIndex] = pack;
        return pack;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    int GetVoxelPackIndex(Vector3i localPosition) => (localPosition.Y << 2) + localPosition.Z;
}

public struct VoxelBrickHandle
{
    readonly int value;

    public bool IsEmpty => value < 0;
    public int BrickIndex => !IsEmpty ? value : throw new InvalidOperationException($"Failed to get {nameof(BrickIndex)} of an empty brick. It's data is not allocated; it stores {nameof(DistanceFieldValue)} instead.");
    public unsafe float DistanceFieldValue
    {
        get
        {
            if (!IsEmpty)
                throw new InvalidOperationException($"Failed to get {nameof(DistanceFieldValue)} of an already allocated brick. It stores {nameof(BrickIndex)} instead.");
            
            int val = value;
            return -*(float*)&val;
        }
    }
    public VoxelBrickHandle(int brickIndex)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(brickIndex);

        value = brickIndex;
    }
    public unsafe VoxelBrickHandle(float distanceToNearest)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(distanceToNearest);

        if (distanceToNearest > 10)
        {
            throw new ArgumentOutOfRangeException(nameof(distanceToNearest), "Distance to nearest brick cannot be greater than 10. This is a safety check to prevent overflow in the distance field.");
        }

        distanceToNearest = -distanceToNearest;
        value = *(int*)&distanceToNearest;
    }
}