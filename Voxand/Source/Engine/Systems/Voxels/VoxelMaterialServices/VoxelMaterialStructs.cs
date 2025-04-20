using OpenTK.Mathematics;
using System.Runtime.InteropServices;
using Voxand.Helpers.ExtensionMethods;

namespace Voxand.Engine.Systems.Voxels.VoxelMaterialServices;
public struct VoxelMaterial(Vector3 baseColor, float baseColorVariance, Vector3 emissionColor, float emissionIntensity)
{
    public Vector3 baseColor = baseColor;
    public float baseColorVariance = baseColorVariance;
    public Vector3 emissionColor = emissionColor;
    public float emissionIntensity = emissionIntensity;
}

[StructLayout(LayoutKind.Sequential)]
public struct VoxelMaterialInternal
{
    public Vector3 diffuse;
    public float diffuseVariance;
    public Vector3 emission;
    float padding;

    public VoxelMaterialInternal(Vector3 diffuse, float diffuseVariance, Vector3 emission)
    {
        this.diffuse = diffuse;
        this.diffuseVariance = diffuseVariance;
        this.emission = emission;
    }

    public VoxelMaterialInternal(VoxelMaterial material)
    {
        diffuse = material.baseColor.Pow(2.2f) / MathF.PI;
        diffuseVariance = MathF.Pow(material.baseColorVariance, 2.2f) / MathF.PI;
        emission = material.emissionColor * material.emissionIntensity;
    }
}
