using OpenTK.Graphics.OpenGL4;

using GLAV.Types;

namespace Voxand.Engine.Graphics.Tools.ShaderServices;
public struct UniformInfo(int location, int type)
{
    public int location = location;
    public int type = type;
}
public class ShaderInfo
{
    protected Dictionary<string, UniformInfo> uniforms = [];
    protected Dictionary<string, int> SSBOs = [];
    public Shader Shader { get; }
    public ShaderInfo(Shader shader)
    {
        Shader = shader;
        FetchUniforms();
        FetchSSBOs();
    }
    void FetchUniforms()
    {
        int uniformCount = Shader.GetInterfaceProperty(ProgramInterface.Uniform, ProgramInterfaceParameter.ActiveResources);

        ProgramProperty[] props = [ProgramProperty.NameLength, ProgramProperty.Type, ProgramProperty.Location];

        for (int i = 0; i < uniformCount; i++)
        {
            int[] info = Shader.GetResourceInfo(ProgramInterface.Uniform, i, ref props);
            string name = Shader.GetResourceName(ProgramInterface.Uniform, i);
            uniforms[name] = new(info[2], info[1]);
        }
    }
    void FetchSSBOs()
    {
        int SSBOCount = Shader.GetInterfaceProperty(ProgramInterface.ShaderStorageBlock, ProgramInterfaceParameter.ActiveResources);

        ProgramProperty[] props = [ProgramProperty.NameLength, ProgramProperty.BlockIndex];

        for (int i = 0; i < SSBOCount; i++)
        {
            int[] info = Shader.GetResourceInfo(ProgramInterface.ShaderStorageBlock, i, ref props);
            string name = Shader.GetResourceName(ProgramInterface.ShaderStorageBlock, i);
            SSBOs[name] = info[1];
        }
    }
    public UniformInfo? GetUniformInfo(string uniformName)
    {
        return uniforms.TryGetValue(uniformName, out UniformInfo uniformInfo) ? uniformInfo : null;
    }
    public int? GetShaderStorageBufferIndex(string shaderStorageBufferName)
    {
        return SSBOs.TryGetValue(shaderStorageBufferName, out int index) ? index : null;
    }
    public bool HasUniform(string uniformName) => uniforms.ContainsKey(uniformName); 
}