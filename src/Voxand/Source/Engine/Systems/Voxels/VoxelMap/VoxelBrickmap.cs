using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

using GLAV.Types;
using DisposableExt;

using Voxand.Engine;
using Voxand.Engine.Graphics;
using Voxand.Engine.Systems.Structures;
using Voxand.Helpers;
using Voxand.Helpers.ExtensionMethods;

using Buffer = GLAV.Types.Buffer;

namespace Voxand.Engine.Systems.Voxels;
public class VoxelBrickmap : VoxelMap
{
    Vector3i brickmapSize;

    int[,,] C_brickmap;
    UnsafeList<VoxelBrick> C_brickList;

    Buffer G_brickmap;
    G_HalfList<VoxelBrick> G_BrickList;

    DDAUnit DDABrick;
    DDAUnit DDAVoxel;

    const int VOXEL_PACK_ARRAY_OFFSET = 2 * sizeof(uint);
    [StructLayout(LayoutKind.Sequential)] unsafe struct VoxelBrick
    {
        fixed uint bitmask[2];
        fixed uint packs[16];
        public VoxelBrick()
        {
        }

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
            uint pack = (packs[packIndex] & (~(255u << shift))) | (value << shift); ;
            packs[packIndex] = pack;
            return pack;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        int GetVoxelPackIndex(Vector3i localPosition) => (localPosition.Y << 2) + localPosition.Z;
        public uint GetVoxelBit(Vector3i localPosition)
        {
            int bitmaskIndex = 0;
            if (localPosition.Y > 1)
            {
                localPosition.Y -= 2;
                bitmaskIndex++;
            }
            return bitmask[bitmaskIndex] & CreateVoxelMask(localPosition);
        }
        public uint SetVoxelBit(Vector3i localPosition, bool solid)
        {
            int bitmaskIndex = 0;
            if (localPosition.Y > 1)
            {
                localPosition.Y -= 2;
                bitmaskIndex++;
            }

            uint mask = 1u << ((localPosition.Y << 4) + (localPosition.Z << 2) + localPosition.X);
            if (solid)
            {
                bitmask[bitmaskIndex] = bitmask[bitmaskIndex] | mask;
                return bitmask[bitmaskIndex];
            }
            bitmask[bitmaskIndex] = bitmask[bitmaskIndex] & (~mask);
            return bitmask[bitmaskIndex];
        }
        uint CreateVoxelMask(Vector3i localPos) => 1u << ((localPos.Y << 4) + (localPos.Z << 2) + localPos.X);
    }

    public VoxelBrickmap(Vector3i dimensions, IVoxelMapPersistence persistenceModule) : base(dimensions, persistenceModule)
    {
        if (dimensions.X % 4 != 0 || dimensions.Y % 4 != 0 || dimensions.Z % 4 != 0)
        {
            throw new ArgumentException("voxel brickmap dimensions should always be divisible by 4");
        }
        brickmapSize = VoxelPositionToBrickPosition(dimensions);
        C_brickmap = new int[brickmapSize.Y, brickmapSize.Z, brickmapSize.X];
        for (int x = 0, lx = C_brickmap.GetLength(2); x < lx; x++)
            for (int y = 0, ly = C_brickmap.GetLength(0); y < ly; y++)
                for (int z = 0, lz = C_brickmap.GetLength(1); z < lz; z++)
                    C_brickmap[y, z, x] = -1;
        C_brickList = new UnsafeList<VoxelBrick>(1);
        
        G_BrickList = new G_HalfList<VoxelBrick>(
            target: BufferTarget.ShaderStorageBuffer, 
            rangeTarget: BufferRangeTarget.ShaderStorageBuffer, 
            bindingIndex: 0, 
            initialCapacity: 1, 
            itemSize: 16 * sizeof(uint) + 2 * sizeof(uint),
            usageHint: BufferUsageHint.DynamicDraw,
            lable: "*** Brick List"
            );

        G_brickmap = new();
        G_brickmap.Alloc(
            bufferTarget: BufferTarget.ShaderStorageBuffer,
            size: brickmapSize.X * brickmapSize.Y * brickmapSize.Z * sizeof(int), 
            usageHint: BufferUsageHint.DynamicDraw);
        G_brickmap.BindBufferBase(new(BufferRangeTarget.ShaderStorageBuffer, 1));
        unsafe
        {
            fixed (int* initBrickmapPtr = C_brickmap)
                G_brickmap.Store((nint)initBrickmapPtr, 0, C_brickmap.Length * sizeof(int));
        }
        G_brickmap.Lable = "*** Brickmap";

        DDABrick = new();
        DDAVoxel = new();
    }

    public unsafe override void SetVoxelValue(Vector3i position, uint value)
    {
        int brickIndex = GetOrAllocBrick(position);
        Vector3i localPosition = VoxelPositionToLocalPosition(position);
        SetVoxelValueInBrick(localPosition, brickIndex, value);
    }
    public unsafe void SetVoxelValueAndBit(Vector3i position, uint value, bool solid)
    {
        int brickIndex = GetOrAllocBrick(position);
        Vector3i localPosition = VoxelPositionToLocalPosition(position);
        SetVoxelValueInBrick(localPosition, brickIndex, value);
        SetVoxelBitInBrick(localPosition, brickIndex, solid);
    }
    unsafe void SetVoxelValueInBrick(Vector3i localPosition, int brickIndex, uint value)
    {
        uint pack = C_brickList[brickIndex]->SetVoxelValue(localPosition, value);
        GPUSetVoxelPack(localPosition, brickIndex, pack);
    }
    unsafe void SetVoxelBitInBrick(Vector3i localPosition, int brickIndex, bool solid)
    {
        uint bitmask = C_brickList[brickIndex]->SetVoxelBit(localPosition, solid);
        int offset = 0;
        if (localPosition.Y > 1)
            offset += sizeof(uint);
        G_BrickList.Write((nint)(&bitmask), brickIndex, offset, sizeof(uint));
    }
    unsafe void GPUSetVoxelPack(Vector3i localPosition, int brickIndex, uint pack)
    {
        int packIndex = VoxelPositionToVoxelPackIndex(localPosition);
        int offset = packIndex * sizeof(uint) + VOXEL_PACK_ARRAY_OFFSET;
        G_BrickList.Write((nint)(&pack), brickIndex, offset, sizeof(uint));
    }
    public unsafe override bool IsSolid(Vector3i position)
    {
        int brickIndex = GetBrickIndex(position);
        if (brickIndex == -1) return false;
        return IsSolid(VoxelPositionToLocalPosition(position), brickIndex);
    }
    public unsafe bool IsSolid(Vector3i localPosition, int brickIndex) 
    {
        return C_brickList[brickIndex]->GetVoxelBit(localPosition) == 0u;
    }
    public unsafe override (uint, bool) GetVoxelValue(Vector3i position)
    {
        int brickIndex = GetBrickIndex(position);
        if (brickIndex == -1)
            return (0, false);
        return (C_brickList[brickIndex]->GetVoxelValue(VoxelPositionToLocalPosition(position)), true);
    }
    public (uint voxelValue, uint voxelBit, int brickIndex) Examine(Vector3i position, bool fromGPU)
    {
        VoxelBrick brick;
        Vector3i localPosition = VoxelPositionToLocalPosition(position);
        int brickIndex;
        uint voxelValue;
        uint voxelBit;

        if (!fromGPU)
        {
            brickIndex = GetBrickIndex(position);
            if (brickIndex == -1)
                return (0, 0, -1);
            brick = GetBrick(brickIndex);
            
        }
        else
        {
            brickIndex = GetBrickIndexFromGPU(position);
            if (brickIndex == -1)
                return (0, 0, -1);
            brick = GetBrickFromGPU(brickIndex);
        }

        voxelValue = brick.GetVoxelValue(localPosition);
        voxelBit = brick.GetVoxelBit(localPosition);

        return (voxelValue, voxelBit, brickIndex);
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public override DDAOut Raycast(Vector3 origin, Vector3 dir)
    {
        int brickIndex;
        DDABrick.Begin(origin / 4, dir);
        brickIndex = GetBrickIndexDirect(DDABrick.voxelPos);
        if (brickIndex >= 0)
        {

            if (!IsSolid(VoxelPositionToLocalPosition((Vector3i)origin), brickIndex))
            {
                return new()
                {
                    hit = true,
                    hitPos = origin,
                    voxelHitPos = (Vector3i)origin,
                    normal = 0 // Normal is undefined here
                };
            }

            DDAVoxel.Begin(origin, dir);

            float escapeTime = DDABrick.nextIntersectionTime.Min() * 4;
            Vector3 escape = escapeTime * dir + origin;
            int totalSteps = ((Vector3i)escape - (Vector3i)origin).AsAbs().Sum();

            for (int i = 0; i < totalSteps; i++)
            {
                DDAVoxel.Step();
                Vector3i voxPos = DDAVoxel.voxelPos;

                if (!IsSolid(VoxelPositionToLocalPosition(voxPos), brickIndex))
                {
                    return new()
                    {
                        hit = true,
                        hitPos = dir * (DDAVoxel.lastHitDepth + DDABrick.lastHitDepth) + origin,
                        voxelHitPos = voxPos,
                        normal = NormalIndex(dir, DDAVoxel.lastAxis)
                    };
                }
            }
        }

        while (true)
        {
            DDABrick.Step();
            Vector3i pos = DDABrick.voxelPos;
            if (!Util.Inbounds(ref pos, ref brickmapSize))
            {
                return new() { hit = false };
            }

            brickIndex = GetBrickIndexDirect(pos);
            if (brickIndex >= 0)
            {
                Vector3 enterance = dir * (DDABrick.lastHitDepth * 4) + origin;
                Vector3 enteranceVoxel = enterance;
                enteranceVoxel[DDABrick.lastAxis] += dir[DDABrick.lastAxis] > 0 ? 0.5f : -0.5f;

                if (!IsSolid(VoxelPositionToLocalPosition((Vector3i)enteranceVoxel), brickIndex))
                {
                    return new()
                    {
                        hit = true,
                        hitPos = enterance,
                        voxelHitPos = (Vector3i)enteranceVoxel,
                        normal = NormalIndex(dir, DDABrick.lastAxis)
                    };
                }

                DDAVoxel.Begin(enterance, dir, (Vector3i)enteranceVoxel);

                float escapeTime = DDABrick.nextIntersectionTime.Min() * 4;
                Vector3 escape = escapeTime * dir + origin;
                int nextAxis = DDABrick.nextIntersectionTime.IndexMin();
                escape[nextAxis] += dir[nextAxis] > 0 ? -0.5f : 0.5f;
                enteranceVoxel[DDABrick.lastAxis] += dir[DDABrick.lastAxis] > 0 ? 0.5f : -0.5f;
                int totalSteps = ((Vector3i)escape - (Vector3i)enterance).AsAbs().Sum();

                for (int i = 0; i < totalSteps; i++)
                {
                    DDAVoxel.Step();
                    Vector3i voxPos = DDAVoxel.voxelPos;

                    if (!IsSolid(VoxelPositionToLocalPosition(voxPos), brickIndex))
                    {
                        return new()
                        {
                            hit = true,
                            hitPos = dir * (DDAVoxel.lastHitDepth + DDABrick.lastHitDepth) + origin,
                            voxelHitPos = voxPos,
                            normal = NormalIndex(dir, DDAVoxel.lastAxis)
                        };
                    }
                }
            }
        }
    }
    int NormalIndex(Vector3 dir, int lastAxis) => dir[lastAxis] > 0 ? lastAxis << 1 : (lastAxis << 1) + 1;
    public override long GetMemoryUsage()
    {
        return C_brickmap.Length * sizeof(int) + C_brickList.Capacity * (16 * sizeof(uint) + 2 * sizeof(uint));
    }
    public override long GetGraphicsMemoryUsage()
    {
        return G_brickmap.Size + G_BrickList.Capacity * G_BrickList.ItemSize;
    }
    public override void Free()
    {
        G_BrickList.Dispose();
    }

    #region Internal helpers

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    Vector3i VoxelPositionToBrickPosition(Vector3i position) => new(position.X >> 2, position.Y >> 2, position.Z >> 2);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    Vector3i VoxelPositionToLocalPosition(Vector3i position) => new(position.X & 3, position.Y & 3, position.Z & 3);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    int VoxelPositionToVoxelPackIndex(Vector3i localPosition) => (localPosition.Y << 2) + localPosition.Z;
    unsafe int CreateOrModifyBrick(Vector3i brickPosition, ref readonly VoxelBrick brick)
    {
        int brickIndex = GetBrickIndexDirect(brickPosition);
        if (brickIndex == -1)
        {
            brickIndex = G_BrickList.Add(brick);
            C_brickList.Add(brick);
            G_brickmap.Store(ref brickIndex, brickPosition.AsIndexYZX(brickmapSize) * sizeof(int));
            C_brickmap[brickPosition.Y, brickPosition.Z, brickPosition.X] = brickIndex;
        }
        else
        {
            brickIndex = G_BrickList.Add(brick);
            fixed (VoxelBrick* brickPtr = &brick)
            {
                C_brickList[brickIndex] = brickPtr;
                G_BrickList[brickIndex] = brick;
            }
        }
        return brickIndex;
    }
    int GetOrAllocBrick(Vector3i position)
    {
        Vector3i brickPosition = VoxelPositionToBrickPosition(position);
        return GetOrAllocBrickDirect(brickPosition);
    }
    int GetOrAllocBrickDirect(Vector3i brickPosition)
    {
        int brickIndex = GetBrickIndexDirect(brickPosition);
        if (brickIndex == -1)
        {
            VoxelBrick newBrick = new();
            brickIndex = CreateOrModifyBrick(brickPosition, ref newBrick);
        }
        return brickIndex;
    }
    int GetBrickIndexDirect(Vector3i brickPosition) => C_brickmap[brickPosition.Y, brickPosition.Z, brickPosition.X];
    int GetBrickIndex(Vector3i position)
    {
        Vector3i brickPosition = VoxelPositionToBrickPosition(position);
        return C_brickmap[brickPosition.Y, brickPosition.Z, brickPosition.X];
    }
    unsafe VoxelBrick GetBrick(int brickIndex) => *C_brickList[brickIndex];

    #region GPU-Side getter
    public int GetBrickIndexFromGPU(Vector3i position)
    {
        Vector3i brickPosition = VoxelPositionToBrickPosition(position);
        return G_brickmap.Retrieve<int>(brickPosition.X, brickPosition.Y, brickPosition.Z, brickmapSize.X, brickmapSize.Z);
    }
    VoxelBrick GetBrickFromGPU(int brickIndex) => G_BrickList[brickIndex];

    #endregion

    #endregion
}