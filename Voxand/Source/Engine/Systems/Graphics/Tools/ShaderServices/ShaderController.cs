using DisposableExt;
using GLAV;
using GLAV.Types;
using OpenTK.Mathematics;
using Voxand.Content;

namespace Voxand.Engine.Systems.Graphics.Tools.ShaderServices;
public class ShaderController : IDisposableExt
{
    public Shader Shader { get; protected set; }
    public ShaderInfo ShaderInfo { get; protected set; }
    public DisposeState DisposeState { get; }

    List<FileSystemWatcher> watchers;
    (string codePath, bool embedded)[] codePaths;

    public ShaderController(ContentManager content, params (string codePath, bool embedded)[] codePaths)
    {
        Shader = content.LoadShader(codePaths);
        ShaderInfo = new ShaderInfo(Shader);
        DisposeState = new(this);
        this.codePaths = codePaths;


        watchers = [];
        for (int i = 0; i < codePaths.Length; i++)
        {
            if (codePaths[i].embedded)
                continue;

            string path = Path.GetFullPath(content.CompleteFilePath(codePaths[i].codePath));
            FileSystemWatcher watcher = new(Path.GetDirectoryName(path)!, Path.GetFileName(path))
            {
                NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.FileName,
                EnableRaisingEvents = true
            };

            watcher.Changed += (sender, args) =>
            {
                GLRegistry.Instance.ScheduleAction(() =>
                {
                    Console.WriteLine($"Reloading");
                    Reload(content, this.codePaths);
                });
            };
            watcher.Renamed += (sender, args) =>
            {
                GLRegistry.Instance.ScheduleAction(() =>
                {
                    Console.WriteLine($"Reloading");
                    Reload(content, this.codePaths);
                });
            };

            watchers.Add(watcher);
        }
    }

    public void Reload(ContentManager content, params (string codePath, bool embedded)[] codePaths)
    {
        Shader newShader;
        try
        {
            newShader = content.LoadShader(codePaths);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to reload {Shader}. Log:\n{ex.Message}");
            return;
        }
        
        ShaderInfo newShaderInfo = new(newShader);

        UniformInfo[] oldUniformInfos = ShaderInfo.GetUniformInfos();

        for (int i = 0; i < oldUniformInfos.Length; i++)
        {
            UniformInfo oldUniformInfo = oldUniformInfos[i];
            if (newShaderInfo.TryGetUniformType(oldUniformInfo.location, out int newType))
            {
                if (newType == oldUniformInfo.type)
                {
                    object oldValue = Shader.GetUniformByGLType(oldUniformInfo.location, oldUniformInfo.type);
                    newShader.SetUniformByGLType(oldUniformInfo.location, oldUniformInfo.type, oldValue);
                }
            }
        }

        Shader.Dispose();
        Shader = newShader;
        ShaderInfo = newShaderInfo;
    }

    public void SetUniform(string uniformName, object value)
    {
        if (!ShaderInfo.TryGetUniformInfo(uniformName, out UniformInfo info)) 
        {
            Console.WriteLine($"Failed to set uniform \"{uniformName}\" of {Shader}; Not found.");
            return; 
        }

        Shader.SetUniformByGLType(info.location, info.type, value);
    }

    public object? GetUniform(string uniformName)
    {
        if (!ShaderInfo.TryGetUniformInfo(uniformName, out UniformInfo info))
        {
            Console.WriteLine($"Failed to get uniform \"{uniformName}\" of {Shader}; Not found.");
            return null;
        }

        return Shader.GetUniformByGLType(info.location, info.type);
    }

    public UniformAccessor<T> GetUniformAccessor<T>(string uniformName) where T : struct
    {
        if (ShaderInfo.TryGetUniformInfo(uniformName, out UniformInfo info))
            return new UniformAccessor<T>(info.location, Shader);

        Console.WriteLine($"Uniform \"{uniformName}\" of {Shader} not found.");
        return new UniformAccessor<T>(-1, Shader);
    }

    public void SetShaderStorageBinding(string bufferName, int binding)
    {
        if (ShaderInfo.TryGetShaderStorageBufferIndex(bufferName, out int index))
            Shader.SetShaderStorageBufferBinding(index, binding);

        Console.WriteLine($"Failed to set binding of buffer \"{bufferName}\" of {Shader}; Not found.");
    }

    public int GetShaderStorageBinding(string bufferName)
    {
        if (ShaderInfo.TryGetShaderStorageBufferIndex(bufferName, out int index))
            return Shader.GetShaderStorageBufferBinding(index);
        
        Console.WriteLine($"Failed to set binding of buffer \"{bufferName}\" of {Shader}; Not found.");
        return -1;
    }

    void IDisposableExt.Free()
    {
        Shader.Dispose();
        foreach (FileSystemWatcher watcher in watchers)
            watcher.Dispose();
        GC.SuppressFinalize(this);
    }

    ~ShaderController() => this.Dispose();
}