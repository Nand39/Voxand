using OpenTK.Graphics.OpenGL4;

using GLAV.Types;

namespace Voxand.Engine.Systems.Graphics.Tools.ShaderServices;
public struct UniformInfo(int location, int type)
{
    public int location = location;
    public int type = type;
}
public class ShaderInfo
{
    protected Dictionary<string, UniformInfo> uniforms = [];
    protected Dictionary<int, int> uniformLocationsToTypes = [];
    protected Dictionary<string, int> SSBOs = [];
    public Shader Shader { get; }
    public ShaderInfo(Shader shader)
    {
        Shader = shader;
        FetchUniforms();
        FetchSSBOs();
    }

    #region Initialization
    void FetchUniforms()
    {
        int uniformCount = Shader.GetInterfaceProperty(ProgramInterface.Uniform, ProgramInterfaceParameter.ActiveResources);

        ProgramProperty[] props = [ProgramProperty.NameLength, ProgramProperty.Type, ProgramProperty.Location];

        for (int i = 0; i < uniformCount; i++)
        {
            int[] info = Shader.GetResourceInfo(ProgramInterface.Uniform, i, props);
            string name = Shader.GetResourceName(ProgramInterface.Uniform, i);
            uniforms[name] = new(info[2], info[1]);
            uniformLocationsToTypes[info[2]] = info[1];
        }
    }
    void FetchSSBOs()
    {
        int SSBOCount = Shader.GetInterfaceProperty(ProgramInterface.ShaderStorageBlock, ProgramInterfaceParameter.ActiveResources);
        for (int i = 0; i < SSBOCount; i++)
            SSBOs[Shader.GetResourceName(ProgramInterface.ShaderStorageBlock, i)] = i;
    }
    #endregion

    #region Uniforms
    public bool HasUniform(string uniformName) => uniforms.ContainsKey(uniformName); 
    public bool HasUniform(int uniformLocation) => uniformLocationsToTypes.ContainsKey(uniformLocation);

    public bool TryGetUniformInfo(string uniformName, out UniformInfo uniformInfo)
    {
        return uniforms.TryGetValue(uniformName, out uniformInfo);
    }
    public bool TryGetUniformType(int uniformLocation, out int type)
    {
        return uniformLocationsToTypes.TryGetValue(uniformLocation, out type);
    }

    public UniformInfo[] GetUniformInfos() => uniforms.Values.ToArray();
    #endregion

    #region Shader Storage Buffers
    public bool HasShaderStorageBuffer(string shaderStorageBufferName) => SSBOs.ContainsKey(shaderStorageBufferName);

    public bool TryGetShaderStorageBufferIndex(string shaderStorageBufferName, out int index)
    {
        return SSBOs.TryGetValue(shaderStorageBufferName, out index);
    }
    #endregion
}