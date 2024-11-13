#version 430 core

struct material
{
    vec3 color;
    // 4 bytes padding
    vec3 emission;
    // 4 bytes padding
};

layout(std430, binding = 0) buffer mapSSBO
{
    uint voxelMap[];
};

layout(std430, binding = 1) buffer bitmaskSSBO
{
    uint voxelBitmask[];
};

layout(std430, binding = 2) buffer palette
{
    material materialPalette[];
};

ivec3 compoundMapSizeLocal;
ivec3 compoundVoxelBitmaskSizeLocal;
ivec3 mapSizeLocal;

float maxVec3(vec3 vect);
float minVec3(vec3 vect);

const float depthBias = 0;


#!voxand_export
int trace(vec3 origin, vec3 dir, out vec3 hitPos, out material hitMat, out int normal, out float depth)
{
    // Initialization

    ivec3 voxelPosition = ivec3(origin);
    vec3 nextIntersectionTime;
    vec3 timeToCross = abs(1.0 / dir);
    ivec3 gridStep;
    uint voxelValue;
    int side = 0;
    float timeToClosestVoxel = float(
        voxelMap[voxelPosition.y * compoundMapSizeLocal.x * compoundMapSizeLocal.z + voxelPosition.z * compoundMapSizeLocal.x + (voxelPosition.x >> 2)] 
        >> ((voxelPosition.x & 3) * 8) & 255);
    uint voxelBit = 0;
    int steps;

    int InfoInteger = 0;

    vec3 timeToEscape;

    if (dir.x < 0)
    {
        gridStep.x = -1;
        nextIntersectionTime.x = timeToCross.x * (origin.x - voxelPosition.x);
        timeToEscape.x = nextIntersectionTime.x + voxelPosition.x * timeToCross.x;
    }
    else if (dir.x > 0)
    {
        gridStep.x = 1;
        nextIntersectionTime.x = timeToCross.x * (voxelPosition.x + 1 - origin.x);
        timeToEscape.x = nextIntersectionTime.x + (mapSizeLocal.x - voxelPosition.x - 1) * timeToCross.x;
    }
    else
    {
        gridStep.x = 0;
        timeToEscape.x = 3.402823466e+38;
    }

    if (dir.y < 0)
    {
        gridStep.y = -1;
        nextIntersectionTime.y = timeToCross.y * (origin.y - voxelPosition.y);
        timeToEscape.y = nextIntersectionTime.y + voxelPosition.y * timeToCross.y;
    }
    else if (dir.y > 0)
    {
        gridStep.y = 1;
        nextIntersectionTime.y = timeToCross.y * (voxelPosition.y + 1 - origin.y);
        timeToEscape.y = nextIntersectionTime.y + (mapSizeLocal.y - voxelPosition.y - 1) * timeToCross.y;
    }
    else
    {
        gridStep.y = 0;
        timeToEscape.y = 3.402823466e+38;
    }

    if (dir.z < 0)
    {
        gridStep.z = -1;
        nextIntersectionTime.z = timeToCross.z * (origin.z - voxelPosition.z);
        timeToEscape.z = nextIntersectionTime.z + voxelPosition.z * timeToCross.z;
    }
    else if (dir.z > 0)
    {
        gridStep.z = 1;
        nextIntersectionTime.z = timeToCross.z * (voxelPosition.z + 1 - origin.z);
        timeToEscape.z = nextIntersectionTime.z + (mapSizeLocal.z - voxelPosition.z - 1) * timeToCross.z;
    }
    else
    {
        gridStep.z = 0;
        timeToEscape.z = 3.402823466e+38;
    }

    float maxDepth = minVec3(timeToEscape) + depthBias;

    int maxIterations = 20;

    // Traversal

    bool escaped = true;
    vec3 stepMask;
    do
	{
        steps = abs(int(dir.x * timeToClosestVoxel)) + abs(int(dir.y * timeToClosestVoxel)) + abs(int(dir.z * timeToClosestVoxel)) - 1;
        steps = steps < 1 ? 1 : steps;
        steps = 1;

        for (int i = 0; i < steps; i++)
        {
            stepMask = vec3(lessThanEqual(nextIntersectionTime.xyz, min(nextIntersectionTime.yzx, nextIntersectionTime.zxy)));

            vec3 a = (stepMask * nextIntersectionTime);
            depth = maxVec3(a);

            nextIntersectionTime += stepMask * timeToCross;
            voxelPosition += ivec3(stepMask) * gridStep;
        }

        if (depth > maxDepth) break;
		
        ivec3 maskPosition = ivec3(voxelPosition.x >> 2, voxelPosition.y >> 1, voxelPosition.z >> 2);
        ivec3 maskInternalIndex = ivec3(voxelPosition.x & 3, voxelPosition.y & 1, voxelPosition.z & 3);
		
        voxelValue = voxelMap[voxelPosition.y * compoundMapSizeLocal.x * compoundMapSizeLocal.z + 
                              voxelPosition.z * compoundMapSizeLocal.x + 
                              maskPosition.x];
        voxelValue = (voxelValue >> (maskInternalIndex.x * 8)) & 255;

        uint mask = voxelBitmask[maskPosition.y * compoundVoxelBitmaskSizeLocal.x * compoundVoxelBitmaskSizeLocal.z + 
                                 maskPosition.z * compoundVoxelBitmaskSizeLocal.x +
                                 maskPosition.x];
        voxelBit = mask & (1 << ((maskInternalIndex.y << 4) + (maskInternalIndex.z << 2) + maskInternalIndex.x));

        if (voxelBit == 0)
        {
            timeToClosestVoxel = voxelValue;
        }
        else 
        {
            escaped = false;
            break;
        }
        
	} while (true);

    if (escaped) return 1;

    depth += depthBias;
    hitPos = origin + dir * depth;
    hitMat = materialPalette[voxelValue];
    if (stepMask.y > 0)
    {
        if (gridStep.y == -1)
        {
            side = 0;
        }
        else 
        {
            side = 1;
        }
    }
    else if (stepMask.x > 0)
    {
        if (gridStep.x == 1)
        {
            side = 2;
        }
        else 
        {
            side = 3;
        }
    }
    else if (stepMask.z > 0)
    {
        if (gridStep.z == 1)
        {
            side = 4;
        }
        else 
        {
            side = 5;
        }
    }
    normal = side;
    return 0;
}