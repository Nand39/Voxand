using OpenTK.Mathematics;
using Voxand.Helpers.ExtensionMethods;

namespace Voxand.Engine.Systems.Graphics;
public class Camera 
{
    public Vector3 position;
    public Vector3 rotation;

    /// <summary>
    /// Angle of the field of view in radians used to create camera projection matrix.
    /// </summary>
    public float FOV { get; set; }

    /// <summary>
    /// Creates a perspective projection matrix of the camera based on its rotation and FOV. 
    /// Translation is NOT included.
    /// </summary>
    /// <param name="aspectRatio">Aspect ratio of the target display.</param>
    public Matrix4 CreateCameraMatrix(float aspectRatio)
    {
        return
            Matrix4.CreateRotationY(rotation.Y) *
            Matrix4.CreateRotationX(rotation.X) *
            Matrix4.CreateRotationZ(rotation.Z) * 
            CreateProjectionMatrix(aspectRatio);
    }
    Matrix4 CreateProjectionMatrix(float aspectRatio)
    {
        return Matrix4.CreatePerspectiveFieldOfView(FOV, aspectRatio, 1, 2);
    }

    /// <summary>
    /// Generates ray direction from the camera through a point on the screen specified by <paramref name="uv"/>.
    /// </summary>
    /// <param name="uv">Point on the screen that defines ray direction based on the field of view of the camera.</param>
    /// <param name="aspectRatio">Aspect ratio of the screen.</param>
    /// <returns></returns>
    public Vector3 PixelToRay(Vector2 uv, float aspectRatio)
    {
        Vector2 targetNDC = uv * 2f;
        targetNDC.X--; targetNDC.Y = -(targetNDC.Y - 1);

        Vector4 viewPlaneCoord = new(targetNDC.X, targetNDC.Y, 1, 1);
        Vector4 transformedViewPlaneCoord = viewPlaneCoord * Matrix4.Invert(CreateCameraMatrix(aspectRatio));
        return (transformedViewPlaneCoord.Xyz / transformedViewPlaneCoord.W).Normalized();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="rayDirection"></param>
    /// <param name="aspectRatio"></param>
    /// <returns></returns>
    public Vector2 RayToPixel(Vector3 rayDirection, float aspectRatio)
    {
        rayDirection = rayDirection.Normalized();
        Vector4 ndc = new Vector4(rayDirection, 0) * CreateCameraMatrix(aspectRatio);
        return (ndc.Xy / ndc.W * 0.5f).Add(0.5f);
    }
}