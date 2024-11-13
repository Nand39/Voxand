#version 450 core

in vec2 fragPosition;
in vec2 trueUV;

out vec3 outAlbedo;
out vec3 outLuminance;
out float outDepth;
out int outNormal;

uniform mat4 inverseCameraMatrix;
uniform vec3 cameraPosition;
uniform float randSalt;
uniform vec3 ambientLighting = vec3(1);
uniform ivec3 mapSize = ivec3(32, 32, 32);

const int samples = 3;
const float invSamples = 1.0 / samples;
const float halfPi = 1.5707963268;
ivec3 compoundMapSize = ivec3(mapSize.x >> 2, mapSize.y, mapSize.z);
ivec3 compoundVoxelBitmaskSize = ivec3(mapSize.x >> 2, mapSize.y >> 1, mapSize.z >> 2);
float jitterArc = 6.283185 / samples;


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

void tracePath(vec3 origin, vec3 dir, out vec3 albedo, out vec3 luminance, out float depth, out int normal);
int trace(vec3 origin, vec3 dir, out material hitMat, out vec3 hitPos, out int normal, out float hitTime);
int traceLast(vec3 origin, vec3 dir, out material hitMat);
vec3 Scatter(vec3 normal, int sampleIndex);
vec3 RandomVector(vec2 key, int sampleIndex);
float Random (vec2 point);
vec3 rotateVerticalByNormal(vec3 vector, int normal);

float minVec3(vec3 vect)
{
    return min(min(vect.x, vect.y), vect.z);
}

float maxVec3(vec3 vect)
{
    return max(max(vect.x, vect.y), vect.z);
}

void main() 
{
    vec4 screenSpaceNear = vec4(fragPosition, 0, 1);
    vec4 screenSpaceFar = vec4(fragPosition, 1, 1);
    vec4 near = screenSpaceNear * inverseCameraMatrix;
    vec4 far = screenSpaceFar * inverseCameraMatrix;

    vec3 rayDirection = normalize(far.xyz / far.w - near.xyz / near.w);

    tracePath(cameraPosition, rayDirection, outAlbedo, outLuminance, outDepth, outNormal);
}

int traceLast(vec3 origin, vec3 dir, out material hitMat)
{
    
    //initialization

    ivec3 voxelPosition = ivec3(origin);
    vec3 nextIntersectionTime;
    vec3 timeToCross = abs(1.0 / dir);
    ivec3 gridStep;
    uint voxelValue;
    float timeToClosestVoxel = float(
        voxelMap[voxelPosition.y * compoundMapSize.x * compoundMapSize.z + voxelPosition.z * compoundMapSize.x + (voxelPosition.x >> 2)] 
        >> ((voxelPosition.x & 3) * 8) & 255);
    int steps;
    uint voxelBit = 0;

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
        timeToEscape.x = nextIntersectionTime.x + (mapSize.x - voxelPosition.x) * timeToCross.x;
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
        timeToEscape.y = nextIntersectionTime.y + (mapSize.y - voxelPosition.y) * timeToCross.y;
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
        timeToEscape.z = nextIntersectionTime.z + (mapSize.z - voxelPosition.z) * timeToCross.z;
    }
    else
    {
        gridStep.z = 0;
        timeToEscape.z = 3.402823466e+38;
    }

    int maxIterations;
    if (timeToEscape.x < timeToEscape.y) 
    {
        if (timeToEscape.x < timeToEscape.z) 
        {
            maxIterations = abs(int(dir.x * timeToEscape.x)) + abs(int(dir.y * timeToEscape.x)) + abs(int(dir.z * timeToEscape.x)) - 1;
            maxIterations = maxIterations < 1 ? 1 : maxIterations;
        }
        else 
        {
            maxIterations = abs(int(dir.x * timeToEscape.z)) + abs(int(dir.y * timeToEscape.z)) + abs(int(dir.z * timeToEscape.z)) - 1;
            maxIterations = maxIterations < 1 ? 1 : maxIterations;
        }
    }
    else 
    {
        if (timeToEscape.y < timeToEscape.z) 
        {
            maxIterations = abs(int(dir.x * timeToEscape.y)) + abs(int(dir.y * timeToEscape.y)) + abs(int(dir.z * timeToEscape.y)) - 1;
            maxIterations = maxIterations < 1 ? 1 : maxIterations;
        }
        else 
        {
            maxIterations = abs(int(dir.x * timeToEscape.z)) + abs(int(dir.y * timeToEscape.z)) + abs(int(dir.z * timeToEscape.z)) - 1;
            maxIterations = maxIterations < 1 ? 1 : maxIterations;
        }
    }

    //traversal

    bool escaped = true;
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
				    nextIntersectionTime.x += timeToCross.x;
			    }
			    else
			    {
				    voxelPosition.z += gridStep.z;
				    nextIntersectionTime.z += timeToCross.z;
			    }
		    }
		    else
		    {
			    if (nextIntersectionTime.y < nextIntersectionTime.z)
			    {
				    voxelPosition.y += gridStep.y;
				    nextIntersectionTime.y += timeToCross.y;
			    }
			    else
			    {
				    voxelPosition.z += gridStep.z;
				    nextIntersectionTime.z += timeToCross.z;
			    }
		    }
        }
        maxIterations -= steps;
		
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
        escaped = false;
            break; 
        }

	} while (maxIterations > 0);
    
    if (escaped) return 1;

    hitMat = materialPalette[voxelValue];
    return 0;
}

int trace(vec3 origin, vec3 dir, out material hitMat, out vec3 hitPos, out int normal, out float hitTime)
{
    // Initialization

    ivec3 voxelPosition = ivec3(origin);
    vec3 nextIntersectionTime;
    vec3 timeToCross = abs(1.0 / dir);
    ivec3 gridStep;
    uint voxelValue;
    int side = 0;
    float timeToClosestVoxel = float(
        voxelMap[voxelPosition.y * compoundMapSize.x * compoundMapSize.z + voxelPosition.z * compoundMapSize.x + (voxelPosition.x >> 2)] 
        >> ((voxelPosition.x & 3) * 8) & 255);
    uint voxelBit = 0;
    int steps;

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
        timeToEscape.x = nextIntersectionTime.x + (mapSize.x - voxelPosition.x) * timeToCross.x;
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
        timeToEscape.y = nextIntersectionTime.y + (mapSize.y - voxelPosition.y) * timeToCross.y;
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
        timeToEscape.z = nextIntersectionTime.z + (mapSize.z - voxelPosition.z) * timeToCross.z;
    }
    else
    {
        gridStep.z = 0;
        timeToEscape.z = 3.402823466e+38;
    }

    int maxIterations;
    if (timeToEscape.x < timeToEscape.y) 
    {
        if (timeToEscape.x < timeToEscape.z) 
        {
            maxIterations = abs(int(dir.x * timeToEscape.x)) + abs(int(dir.y * timeToEscape.x)) + abs(int(dir.z * timeToEscape.x)) - 1;
            maxIterations = maxIterations < 1 ? 1 : maxIterations;
        }
        else 
        {
            maxIterations = abs(int(dir.x * timeToEscape.z)) + abs(int(dir.y * timeToEscape.z)) + abs(int(dir.z * timeToEscape.z)) - 1;
            maxIterations = maxIterations < 1 ? 1 : maxIterations;
        }
    }
    else 
    {
        if (timeToEscape.y < timeToEscape.z) 
        {
            maxIterations = abs(int(dir.x * timeToEscape.y)) + abs(int(dir.y * timeToEscape.y)) + abs(int(dir.z * timeToEscape.y)) - 1;
            maxIterations = maxIterations < 1 ? 1 : maxIterations;
        }
        else 
        {
            maxIterations = abs(int(dir.x * timeToEscape.z)) + abs(int(dir.y * timeToEscape.z)) + abs(int(dir.z * timeToEscape.z)) - 1;
            maxIterations = maxIterations < 1 ? 1 : maxIterations;
        }
    }

    // Traversal

    bool escaped = true;
    vec3 stepMask;
    do
	{
        steps = abs(int(dir.x * timeToClosestVoxel)) + abs(int(dir.y * timeToClosestVoxel)) + abs(int(dir.z * timeToClosestVoxel)) - 1;
        steps = steps < 1 ? 1 : steps;

        for (int i = 0; i < steps; i++)
        {
            stepMask = vec3(lessThanEqual(nextIntersectionTime.xyz, min(nextIntersectionTime.yzx, nextIntersectionTime.zxy)));

            vec3 a = (stepMask * nextIntersectionTime);
            hitTime = maxVec3(a);

            nextIntersectionTime += stepMask * timeToCross;
            voxelPosition += ivec3(stepMask) * gridStep;
        }
        maxIterations -= steps;
		
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
            escaped = false;
            break;
        }
        
	} while (maxIterations > 0);
    
    if (escaped) return 1;

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

vec3 Scatter(int normal, int sampleIndex)
{
    vec2 randomSampler = fragPosition.xy * sampleIndex * 68;
    float key1 = Random(randomSampler); 
    float key2 = Random((randomSampler * key1 * 22.314));
    return rotateVerticalByNormal(RandomVector(vec2(key1, key2), sampleIndex), normal);
}

vec3 rotateVerticalByNormal(vec3 vector, int normal)
{
    switch (normal)
    {
        case 0: return vector;
        case 1: return vec3(vector.x, -vector.y, vector.z);
        case 2: return vec3(-vector.y, vector.x, vector.z);
        case 3: return vec3(vector.y, -vector.x, vector.z);
        case 4: return vec3(vector.x, vector.z, -vector.y);
        case 5: return vec3(vector.x, -vector.z, vector.y);
    }
    return vector;
}

void tracePath(vec3 origin, vec3 dir, out vec3 albedo, out vec3 luminance, out float depth, out int normal)
{
    vec3 firstHitPos;
    material firstHitMat;

    material secondHitMat;
    int secondHitResult;

    int firstHitResult = trace(origin, dir, firstHitMat, firstHitPos, normal, depth);

    if (firstHitResult == 1)
    {
        albedo = vec3(1);
        luminance = ambientLighting;
        return;
    }
    if (firstHitResult != 0)
    {
        albedo = vec3(1, 0, 0);
        luminance = vec3(1);
        return;
    }

    vec3 indirectContribution = vec3(0);

    int i = 1;
    while(i <= samples)
    {
        vec3 scatteredDir = Scatter(normal, i);
        //scatteredDir = normalize(scatteredDir);
        secondHitResult = traceLast(firstHitPos, scatteredDir, secondHitMat);
        if (secondHitResult == 0)
        {
            indirectContribution += secondHitMat.emission;
            i++;
            continue;
        }
        if (secondHitResult == 1)
        {
            indirectContribution += ambientLighting;
            i++;
            continue;
        }
        i++;
    }

    albedo = firstHitMat.color;
    luminance = firstHitMat.emission + indirectContribution * invSamples;
    return;
}

vec3 RandomVector(vec2 key, int sampleIndex)
{
    float theta = jitterArc * key.x + jitterArc * sampleIndex;
    float phi = (-1 * sqrt(1 - key.y * key.y) + 1) * 1.370796 + 0.2;
    float cosPhi = cos(phi);
    return vec3(sin(theta) * cosPhi, sin(phi), cosPhi * cos(theta));
}

float Random (vec2 point) 
{
    point *= randSalt;
    return fract(sin(dot(point.xy, vec2(12.9898,78.233))) * 43758.5453123);
}