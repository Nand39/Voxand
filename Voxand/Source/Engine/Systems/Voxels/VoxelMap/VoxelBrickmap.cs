using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

using DisposableExt;
using GLAV.Types.Extended;
using Buffer = GLAV.Types.Buffer;

using Voxand.Engine.Systems.Structures;
using Voxand.Helpers.ExtensionMethods;
using GLAV.Helpers.Public.Extensions.Unsafe;

namespace Voxand.Engine.Systems.Voxels;
public class VoxelBrickmap : VoxelMap, ISinglePlaceable
{
    Vector3i brickmapSize;

    VoxelBrickHandle[,,] C_brickmap;
    UnmanagedList<VoxelBrickValues> C_BrickValues;
    UnmanagedList<VoxelBrickOccupancy> C_BrickOccupancy;

    Buffer G_brickmap;
    HalfList<VoxelBrickValues> G_BrickValues;
    HalfList<VoxelBrickOccupancy> G_BrickOccupancy; 

    DDAUnit DDABrick;
    DDAUnit DDAVoxel;

    public int BrickValuesBinding { set => G_BrickValues.array.BindAsShaderStorage(BufferRangeTarget.ShaderStorageBuffer, value); }
    public int BrickOccupancyBinding { set => G_BrickOccupancy.array.BindAsShaderStorage(BufferRangeTarget.ShaderStorageBuffer, value); }

    const int MAX_DISTANCE_FIELD = 5;

    public VoxelBrickmap(Vector3i dimensions, IVoxelMapPersistence persistenceModule) : base(dimensions, persistenceModule)
    {
        if (dimensions.X % 4 != 0 || dimensions.Y % 4 != 0 || dimensions.Z % 4 != 0)
            throw new ArgumentException("voxel brickmap dimensions should always be divisible by 4");

        brickmapSize = dimensions.BitshiftRight(2);
        C_brickmap = new VoxelBrickHandle[brickmapSize.Y, brickmapSize.Z, brickmapSize.X];

        for (int x = 0, lx = C_brickmap.GetLength(2); x < lx; x++)
            for (int y = 0, ly = C_brickmap.GetLength(0); y < ly; y++)
                for (int z = 0, lz = C_brickmap.GetLength(1); z < lz; z++)
                    C_brickmap[y, z, x] = new(-1);

        C_BrickValues = new UnmanagedList<VoxelBrickValues>(1);
        C_BrickOccupancy = new UnmanagedList<VoxelBrickOccupancy>(1);

        Func<int, int> growthFunc = (capacity) => capacity < 24000 ? (int)MathF.Floor(-((capacity * 0.005f - 540) * capacity * 0.005f)) + 2 : (int)(capacity * 1.5f);

        G_BrickValues = new HalfList<VoxelBrickValues>(
            target: BufferTarget.ShaderStorageBuffer, 
            initialCapacity: 1, 
            usageHint: BufferUsageHint.DynamicDraw,
            growthFunction: growthFunc
            );
        G_BrickValues.BindAsShaderStorage(BufferRangeTarget.ShaderStorageBuffer, 0);
        G_BrickValues.Label = "*** Brick values list";

        G_BrickOccupancy = new HalfList<VoxelBrickOccupancy>(
            target: BufferTarget.ShaderStorageBuffer,
            initialCapacity: 1,
            usageHint: BufferUsageHint.DynamicDraw,
            growthFunction: growthFunc
            );
        G_BrickOccupancy.BindAsShaderStorage(BufferRangeTarget.ShaderStorageBuffer, 1);
        G_BrickOccupancy.Label = "*** Brick occupancy list";

        G_brickmap = new();
        G_brickmap.Alloc(
            bufferTarget: BufferTarget.ShaderStorageBuffer,
            size: brickmapSize.X * brickmapSize.Y * brickmapSize.Z * sizeof(int),
            usageHint: BufferUsageHint.DynamicDraw);
        G_brickmap.BindAsShaderStorage(BufferRangeTarget.ShaderStorageBuffer, new(2));
        unsafe
        {
            fixed (VoxelBrickHandle* initBrickmapPtr = C_brickmap)
                G_brickmap.Store(0, (nint)initBrickmapPtr, C_brickmap.Length * sizeof(int));
        }
        G_brickmap.Lable = "*** Brickmap";

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
        VoxelBrickHandle brickHandle = GetOrAllocBrick(position);
        Vector3i localPosition = VoxelPositionToLocalPosition(position);
        SetVoxelValueInBrick(localPosition, brickHandle, value);
        SetVoxelBitInBrick(localPosition, brickHandle, solid);
    }
    unsafe void SetVoxelValueInBrick(Vector3i localPosition, VoxelBrickHandle brickHandle, uint value)
    {
        uint pack = C_BrickValues[brickHandle.Value]->SetVoxelValue(localPosition, value);
        GPUSetVoxelPack(localPosition, brickHandle, pack);
    }
    unsafe void SetVoxelBitInBrick(Vector3i localPosition, VoxelBrickHandle brickHandle, bool solid)
    {
        C_BrickOccupancy[brickHandle.Value]->SetVoxelBit(localPosition, solid);
        ulong newBitmask = C_BrickOccupancy[brickHandle.Value]->Bitmask;
        G_BrickOccupancy.Write((nint)(&newBitmask), brickHandle.Value, 0, sizeof(ulong));
    }
    unsafe void GPUSetVoxelPack(Vector3i localPosition, VoxelBrickHandle brickHandle, uint pack)
    {
        int packIndex = VoxelPositionToVoxelPackIndex(localPosition);
        G_BrickValues.Write((nint)(&pack), brickHandle.Value, packIndex * sizeof(uint), sizeof(uint));
    }
    public unsafe override bool IsSolid(Vector3i position)
    {
        VoxelBrickHandle brickHandle = GetBrickHandle(position);
        return brickHandle.IsEmpty ? false : IsSolid(VoxelPositionToLocalPosition(position), brickHandle.Value);
    }
    public unsafe bool IsSolid(Vector3i localPosition, int brickIndex) 
    {
        return C_BrickOccupancy[brickIndex]->GetVoxelBit(localPosition) == 0u;
    }
    public unsafe override (uint, bool) GetVoxelValue(Vector3i position)
    {
        VoxelBrickHandle brickHandle = GetBrickHandle(position);
        if (brickHandle.IsEmpty)
            return (0, false);
        return (C_BrickValues[brickHandle.Value]->GetVoxelValue(VoxelPositionToLocalPosition(position)), true);
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
            brickHandle = GetBrickHandle(position);
            if (brickHandle.IsEmpty)
                return (0, 0, new(-1));
            brickValues = *C_BrickValues[brickHandle.Value];
            brickOccupancy = *C_BrickOccupancy[brickHandle.Value];


        }
        else
        {
            Vector3i brickPosition = VoxelPositionToBrickPosition(position);
            brickHandle = new VoxelBrickHandle(G_brickmap.Retrieve<int>(brickPosition.Y, brickPosition.Z, brickPosition.X, brickmapSize.X, brickmapSize.Z));
            if (brickHandle.IsEmpty)
                return (0, 0, new(-1));
            brickValues = G_BrickValues[brickHandle.Value];
            brickOccupancy = G_BrickOccupancy[brickHandle.Value];
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
        VoxelBrickHandle brickHandle = GetBrickHandleDirect(dda.CurrentVoxelPos);

        if (!brickHandle.IsEmpty)
        {
            bitmask = C_BrickOccupancy[brickHandle.Value]->Bitmask;
            
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
                        normal = 0,
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

            brickHandle = GetBrickHandleDirect(dda.CurrentVoxelPos);
            if (!brickHandle.IsEmpty)
            {
                bitmask = C_BrickOccupancy[brickHandle.Value]->Bitmask;
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
    public override long GetMemoryUsage()
    {
        return C_brickmap.Length * sizeof(int) + C_BrickValues.Capacity * (16 * sizeof(uint) + 2 * sizeof(uint));
    }
    public override long GetGraphicsMemoryUsage()
    {
        return G_brickmap.Size + G_BrickValues.Capacity * G_BrickValues.itemSize;
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
        VoxelBrickHandle brickHandle = GetBrickHandleDirect(brickPosition);
        if (brickHandle.IsEmpty)
        {
            AllocBrick(brickPosition, brickValues, brickOccupancy);
        }
        else
        {
            fixed (VoxelBrickValues* brickPtr = &brickValues)
            {
                C_BrickValues[brickHandle.Value] = brickPtr;
                G_BrickValues[brickHandle.Value] = brickValues;
                C_BrickOccupancy[brickHandle.Value] = &brickOccupancy;
                G_BrickOccupancy[brickHandle.Value] = brickOccupancy;
            }
        }
        return brickHandle;
    }

    public VoxelBrickHandle AllocBrick(Vector3i brickPosition, in VoxelBrickValues values, VoxelBrickOccupancy occupancy)
    {
        int brickIndex = G_BrickValues.Add(values);
        VoxelBrickHandle brickHandle = new(brickIndex);
        C_BrickValues.Add(values);
        G_BrickOccupancy.Add(occupancy);
        C_BrickOccupancy.Add(occupancy);

        G_brickmap.Store(ref brickIndex, brickPosition.AsIndexYZX(brickmapSize) * sizeof(int));
        C_brickmap[brickPosition.Y, brickPosition.Z, brickPosition.X] = brickHandle;
        return brickHandle;
    }
    VoxelBrickHandle GetOrAllocBrick(Vector3i position)
    {
        Vector3i brickPosition = VoxelPositionToBrickPosition(position);
        return GetOrAllocBrickDirect(brickPosition);
    }
    VoxelBrickHandle GetOrAllocBrickDirect(Vector3i brickPosition)
    {
        VoxelBrickHandle brickHandle = GetBrickHandleDirect(brickPosition);
        if (brickHandle.IsEmpty)
        {
            VoxelBrickValues newBrickValues = new();
            VoxelBrickOccupancy newBrickOccupancy = new();
            brickHandle = AllocBrick(brickPosition, newBrickValues, newBrickOccupancy);
        }
        return brickHandle;
    }
    VoxelBrickHandle GetBrickHandleDirect(Vector3i brickPosition) => C_brickmap[brickPosition.Y, brickPosition.Z, brickPosition.X];
    VoxelBrickHandle GetBrickHandle(Vector3i position)
    {
        Vector3i brickPosition = VoxelPositionToBrickPosition(position);
        return C_brickmap[brickPosition.Y, brickPosition.Z, brickPosition.X];
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
    int value;

    public bool IsEmpty => value < 0;
    public int Value => value;

    public VoxelBrickHandle(int value)
    {
        this.value = value;
    }
}