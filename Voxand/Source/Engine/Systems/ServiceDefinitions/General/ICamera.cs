using OpenTK.Mathematics;
using Voxand.Engine.Systems.Common;
using Voxand.Helpers.ExtensionMethods;

namespace Voxand.Engine.Systems.Services.General;
public interface ICamera
{
    Pose Pose { get; set; }

    /// <summary>
    /// Angle of the field of view in radians used to create camera projection matrix.
    /// </summary>
    public float FOV { get; set; }

    /// <summary>
    /// Creates a perspective projection matrix of the camera based on its rotation and FOV. 
    /// Translation is NOT included.
    /// </summary>
    /// <param name="aspectRatio">Aspect ratio of the target display.</param>
    Matrix4 CreateCameraMatrix(float aspectRatio);

    /// <summary>
    /// Generates ray direction from the camera through a point on the screen specified by <paramref name="uv"/>.
    /// </summary>
    /// <param name="uv">Point on the screen that defines ray direction based on the field of view of the camera.</param>
    /// <param name="aspectRatio">Aspect ratio of the screen.</param>
    public Vector3 PixelToRay(Vector2 uv, float aspectRatio)
    {
        Vector2 targetNDC = uv * 2f;
        targetNDC.X--; targetNDC.Y = -(targetNDC.Y - 1);

        Vector4 viewPlaneCoord = new(targetNDC.X, targetNDC.Y, 1, 1);
        Vector4 transformedViewPlaneCoord = viewPlaneCoord * Matrix4.Invert(CreateCameraMatrix(aspectRatio));
        return (transformedViewPlaneCoord.Xyz / transformedViewPlaneCoord.W).Normalized();
    }

    /// <summary>
    /// For a given <paramref name="rayDirection"/> calculates the UV coordinates in the camera view that "look" in that direction. 
    /// <paramref name="rayDirection"/> is implicitly normalized.
    /// </summary>
    /// <returns>UV coordinates that fall in range [0; 1] if in view.</returns>
    public Vector2 RayToPixel(Vector3 rayDirection, float aspectRatio)
    {
        rayDirection = rayDirection.Normalized();
        Vector4 ndc = new Vector4(rayDirection, 0) * CreateCameraMatrix(aspectRatio);
        return (ndc.Xy / ndc.W * 0.5f).Add(0.5f);
    }
}