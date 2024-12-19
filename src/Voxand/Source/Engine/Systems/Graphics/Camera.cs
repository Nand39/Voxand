using OpenTK.Mathematics;

namespace Voxand.Engine.Systems.Graphics;
public class Camera 
{
    public Vector3 position;
    public Vector3 rotation;

    /// <summary>
    /// Angle of the field of view in degrees used to create camera transformation matrix.
    /// </summary>
    public float FOV { get; set; }
    public Matrix4 CreateCameraMatrix(float aspectRatio)
    {
        return
            Matrix4.CreateRotationY(rotation.Y) *
            Matrix4.CreateRotationX(rotation.X) *
            Matrix4.CreateRotationZ(rotation.Z) * 
            CreateProjectionMatrix(aspectRatio);
    }
    public Matrix4 CreateProjectionMatrix(float aspectRatio)
    {
        return Matrix4.CreatePerspectiveFieldOfView(FOV * Helpers.Util.DEG2RAD, aspectRatio, 1, 2);
    }
    public Vector3 PixelToRay(Vector2 uv, float aspectRatio)
    {
        Vector2 targetNDC = uv * 2f;
        targetNDC.X--; targetNDC.Y = -(targetNDC.Y - 1);

        Matrix4 inverseCameraMatrix = Matrix4.Invert(CreateCameraMatrix(aspectRatio));

        Vector4 screenSpaceNear = new(targetNDC.X, targetNDC.Y, 0, 1);
        Vector4 screenSpaceFar = new(targetNDC.X, targetNDC.Y, 1, 1);

        Vector4 near = screenSpaceNear * inverseCameraMatrix;
        near /= near.W;
        Vector4 far = screenSpaceFar * inverseCameraMatrix;
        far /= far.W;

        return new Vector3(far.Xyz / far.W - near.Xyz / near.W).Normalized();
    }
}