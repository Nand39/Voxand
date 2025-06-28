using OpenTK.Mathematics;

using Voxand.Engine.Systems.Graphics.Tools.ShaderServices;

namespace Voxand.Engine.Systems.Services.Graphics;
public interface IRendererPathTracing
{
    public UniformAccessor<int> Samples { get; }
    public UniformAccessor<Vector3> SunDirection { get; }
}