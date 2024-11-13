using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using Voxand.Helpers;

namespace Voxand.Engine;
public static class Camera 
{
    public static Vector2i screenSize;
    public static Vector3 position;
    public static Vector3 rotation;
    static float fov;
    public static float FOV 
    {
        get { return fov; }
        set 
        { 
            fov = value;
            RefreshProjection();
        }
    }
    public static Matrix4 projectionMat;
    public static Matrix4 CreateCameraMatrix()
    {
        return
               Matrix4.CreateRotationY(rotation.Y) *
               Matrix4.CreateRotationX(rotation.X) *
               Matrix4.CreateRotationZ(rotation.Z) *
               projectionMat;
    }
    public static void RefreshProjection()
    {
        projectionMat = Matrix4.CreatePerspectiveFieldOfView(fov * (float)Math.PI / 180, (float)screenSize.X / screenSize.Y, 1, 2);
    }

    public static Vector3 PixelToRay(Vector2i pixel)
    {
        Vector2 targetNDC = (Vector2)pixel / Util.ClientSize * 2f;
        targetNDC.X--; targetNDC.Y = -(targetNDC.Y - 1);

        Matrix4 inverseCameraMatrix = Matrix4.Invert(CreateCameraMatrix());

        Vector4 screenSpaceNear = new(targetNDC.X, targetNDC.Y, 0, 1);
        Vector4 screenSpaceFar = new(targetNDC.X, targetNDC.Y, 1, 1);

        Vector4 near = screenSpaceNear * inverseCameraMatrix;
        near /= near.W;
        Vector4 far = screenSpaceFar * inverseCameraMatrix;
        far /= far.W;

        return new Vector3(far.Xyz / far.W - near.Xyz / near.W).Normalized();
    }
}