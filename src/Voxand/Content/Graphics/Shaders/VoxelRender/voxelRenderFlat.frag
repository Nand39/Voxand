#version 430 core

in vec2 fragPosition;
in vec2 trueUV;

out vec3 outColor;

uniform mat4 inverseCameraMatrix;
uniform vec3 cameraPosition;
uniform ivec3 mapSize = ivec3(96, 48, 96);

ivec3 compoundMapSize = ivec3(mapSize.x >> 2, mapSize.y, mapSize.z);
ivec3 compoundVoxelBitmaskSize = ivec3(mapSize.x >> 2, mapSize.y >> 2, mapSize.z >> 2);


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

layout(std430, binding = 2) buffer bitmaskSSBO
{
    uint voxelBitmask[];
};

layout(std430, binding = 1) buffer palette
{
    material materialPalette[];
};

void tracePath(vec3 origin, vec3 dir, out vec3 color);
int trace(vec3 origin, vec3 dir, out material hitMat, out vec3 hitPos, out int normal, out float hitTime);

void main() 
{
    vec4 screenSpaceNear = vec4(fragPosition, 0, 1);
    vec4 screenSpaceFar = vec4(fragPosition, 1, 1);
    vec4 near = screenSpaceNear * inverseCameraMatrix;
    vec4 far = screenSpaceFar * inverseCameraMatrix;

    vec3 rayDirection = normalize(far.xyz / far.w - near.xyz / near.w);

    tracePath(cameraPosition, rayDirection, outColor);
}

int trace(vec3 origin, vec3 dir, out material hitMat, out vec3 hitPos, out int normal, out float hitTime)
{
    ivec3 voxelPosition = ivec3(origin);
    if (voxelPosition.x >= mapSize.x || voxelPosition.x < 0 || 
        voxelPosition.y >= mapSize.y || voxelPosition.y < 0 || 
        voxelPosition.z >= mapSize.z || voxelPosition.z < 0)
    {
        return 2;
    }

    //initialization

    vec3 nextIntersectionTime;
    vec3 timeToCross = abs(1.0 / dir);
    ivec3 gridStep;
    uint voxelValue;
    int side = 0;
    float timeToClosestVoxel = float(voxelMap[voxelPosition.y * compoundMapSize.x * compoundMapSize.z + 
                                     voxelPosition.z * compoundMapSize.x + 
                                     (voxelPosition.x >> 2)] >> ((voxelPosition.x & 3) * 8) & 255);
    uint voxelBit = 0;
    int steps;

    if (dir.x < 0)
    {
        gridStep.x = -1;
        nextIntersectionTime.x = timeToCross.x * (origin.x - voxelPosition.x);
    }
    else if (dir.x > 0)
    {
        gridStep.x = 1;
        nextIntersectionTime.x = timeToCross.x * (voxelPosition.x + 1 - origin.x);
    }
    else
    {
        gridStep.x = 0;
    }

    if (dir.y < 0)
    {
        gridStep.y = -1;
        nextIntersectionTime.y = timeToCross.y * (origin.y - voxelPosition.y);
    }
    else if (dir.y > 0)
    {
        gridStep.y = 1;
        nextIntersectionTime.y = timeToCross.y * (voxelPosition.y + 1 - origin.y);
    }
    else
    {
        gridStep.y = 0;
    }

    if (dir.z < 0)
    {
        gridStep.z = -1;
        nextIntersectionTime.z = timeToCross.z * (origin.z - voxelPosition.z);
    }
    else if (dir.z > 0)
    {
        gridStep.z = 1;
        nextIntersectionTime.z = timeToCross.z * (voxelPosition.z + 1 - origin.z);
    }
    else
    {
        gridStep.z = 0;
    }

    //traversal
    do
	{
        steps = abs(int(dir.x * timeToClosestVoxel)) + abs(int(dir.y * timeToClosestVoxel)) + abs(int(dir.z * timeToClosestVoxel)) - 1;
        steps = steps < 1 ? 1 : steps;

        for (int i = 0; i < steps; i++)
        {
            if (nextIntersectionTime.x < nextIntersectionTime.y)
		    {
			    if (nextIntersectionTime.x < nextIntersectionTime.z)
			    {
				    voxelPosition.x += gridStep.x;
				    if (voxelPosition.x >= mapSize.x || voxelPosition.x < 0)
					    return 1;
                    hitTime = nextIntersectionTime.x;
                    side = 0;
				    nextIntersectionTime.x += timeToCross.x;
			    }
			    else
			    {
				    voxelPosition.z += gridStep.z;
				    if (voxelPosition.z >= mapSize.z || voxelPosition.z < 0)
					    return 1;
                    hitTime = nextIntersectionTime.z;
                    side = 1;
				    nextIntersectionTime.z += timeToCross.z;
			    }
		    }
		    else
		    {
			    if (nextIntersectionTime.y < nextIntersectionTime.z)
			    {
				    voxelPosition.y += gridStep.y;
				    if (voxelPosition.y >= mapSize.y || voxelPosition.y < 0)
					    return 1;
                    hitTime = nextIntersectionTime.y;
                    side = 2;
				    nextIntersectionTime.y += timeToCross.y;
			    }
			    else
			    {
				    voxelPosition.z += gridStep.z;
				    if (voxelPosition.z >= mapSize.z || voxelPosition.z < 0)
					    return 1;
                    hitTime = nextIntersectionTime.z;
                    side = 1;
				    nextIntersectionTime.z += timeToCross.z;
			    }
		    }
        }
		
        ivec3 maskRegion = ivec3(voxelPosition.x >> 2, voxelPosition.y >> 1, voxelPosition.z >> 2);
        ivec3 maskIndex = ivec3(voxelPosition.x & 3, voxelPosition.y & 1, voxelPosition.z & 3);
		
        voxelValue = voxelMap[voxelPosition.y * compoundMapSize.x * compoundMapSize.z + 
                                voxelPosition.z * compoundMapSize.x + 
                                maskRegion.x];
        voxelValue = (voxelValue >> (maskIndex.x * 8)) & 255;

        uint mask = voxelBitmask[maskRegion.y * compoundVoxelBitmaskSize.x * compoundVoxelBitmaskSize.z + 
                                    maskRegion.z * compoundVoxelBitmaskSize.x +
                                    maskRegion.x];
        voxelBit = mask & (1 << ((maskIndex.y << 4) + (maskIndex.z << 2) + maskIndex.x));

        if (voxelBit == 0)
        {
            timeToClosestVoxel = voxelValue;
        }
        else 
        {
            break;
        }
        
	} while (true);
    
    hitPos = origin + dir * hitTime;
    hitMat = materialPalette[voxelValue];
    if (side == 2)
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
    else if (side == 0)
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
    else if (side == 1)
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

void tracePath(vec3 origin, vec3 dir, out vec3 color)
{
    vec3 firstHitPos;
    material firstHitMat;
    material secondHitMat;
    int secondHitResult;
    int firstHitNormal;
    float depth;

    int firstHitResult = trace(origin, dir, firstHitMat, firstHitPos, firstHitNormal, depth);

    if (firstHitResult == 1)
    {
        color = vec3(0.23);
        return;
    }
    if (firstHitResult != 0)
    {
        color = vec3(0.08);
        return;
    }
    color = clamp(firstHitMat.color * 0.8 + firstHitNormal * 0.02 + depth * 0.002, vec3(0), vec3(1));
    return;
}