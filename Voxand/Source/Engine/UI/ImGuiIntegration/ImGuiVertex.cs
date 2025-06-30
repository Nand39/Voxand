using GLAV.Data;
using OpenTK.Mathematics;
using System.Runtime.InteropServices;

namespace Voxand.UI.ImGuiIntegration.Backend;

[StructLayout(LayoutKind.Sequential)]
public unsafe struct ImGuiVertex
{
    [VertexAttrib(0, false)] public Vector2 position;
    [VertexAttrib(1, false)] public Vector2 texCoord;
    [VertexAttrib(2, true)] public Vec4i8 color;
}