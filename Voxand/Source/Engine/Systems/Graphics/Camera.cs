using OpenTK.Mathematics;

using Voxand.Helpers.ExtensionMethods;
using Voxand.Engine.Systems.Services.General;
using Voxand.Engine.Systems.Common;

namespace Voxand.Engine.Systems.Graphics;
public class Camera : ICamera
{
    public Pose Pose { get; set; } = new();

    /// <inheritdoc/>
    public float FOV { get; set; }

    /// <inheritdoc/>
    public Matrix4 CreateCameraMatrix(float aspectRatio)
    {
        return
            Matrix4.CreateRotationY(Pose.rotation.Y) *
            Matrix4.CreateRotationX(Pose.rotation.X) *
            Matrix4.CreateRotationZ(Pose.rotation.Z) * 
            CreateProjectionMatrix(aspectRatio);
    }
    Matrix4 CreateProjectionMatrix(float aspectRatio)
    {
        return Matrix4.CreatePerspectiveFieldOfView(FOV, aspectRatio, 1, 2);
    }

    /// <inheritdoc/>
    public Vector3 PixelToRay(Vector2 uv, float aspectRatio)
    {
        Vector2 targetNDC = uv * 2f;
        targetNDC.X--; targetNDC.Y = -(targetNDC.Y - 1);

        Vector4 viewPlaneCoord = new(targetNDC.X, targetNDC.Y, 1, 1);
        Vector4 transformedViewPlaneCoord = viewPlaneCoord * Matrix4.Invert(CreateCameraMatrix(aspectRatio));
        return (transformedViewPlaneCoord.Xyz / transformedViewPlaneCoord.W).Normalized();
    }

    /// <inheritdoc/>
    public Vector2 RayToPixel(Vector3 rayDirection, float aspectRatio)
    {
        rayDirection = rayDirection.Normalized();
        Vector4 ndc = new Vector4(rayDirection, 0) * CreateCameraMatrix(aspectRatio);
        return (ndc.Xy / ndc.W * 0.5f).Add(0.5f);
    }
}