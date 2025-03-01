using DisposableExt;
using GLAV;
using GLAV.Types;

using Voxand.Content;

namespace Voxand.Engine.Systems.Graphics.Tools.ShaderServices;
public class ShaderController : IDisposableExt
{
    public Shader Shader { get; protected set; }
    public ShaderInfo ShaderInfo { get; protected set; }
    public DisposeHelper DisposeHelper { get; }

    FileSystemWatcher[] watchers;
    string[] sourcePaths;

    public ShaderController(ContentManager content, params string[] sourcePaths)
    {
        Shader = content.LoadShader(sourcePaths);
        ShaderInfo = new ShaderInfo(Shader);
        DisposeHelper = new(this);
        this.sourcePaths = sourcePaths;

        watchers = new FileSystemWatcher[sourcePaths.Length];
        for (int i = 0; i < watchers.Length; i++)
        {
            string path = Path.GetFullPath(content.CompleteFilePath(sourcePaths[i]));
            FileSystemWatcher watcher = new(Path.GetDirectoryName(path)!, Path.GetFileName(path));

            watcher.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.FileName;
            watcher.EnableRaisingEvents = true;

            watcher.Changed += (sender, args) =>
            {
                Console.WriteLine($"File CHANGED!");
                GLRegistry.Instance.ScheduleAction(() =>
                {
                    Console.WriteLine($"Reloading");
                    Reload(content, this.sourcePaths);
                });
            };
            watcher.Renamed += (sender, args) =>
            {
                Console.WriteLine($"File RENAMED!");
                GLRegistry.Instance.ScheduleAction(() =>
                {
                    Console.WriteLine($"Reloading");
                    Reload(content, this.sourcePaths);
                });
            };

            watchers[i] = watcher;
        }
    }

    public void Reload(ContentManager content, params string[] sourcePaths)
    {
        Shader newShader;
        try
        {
            newShader = content.LoadShader(sourcePaths);
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
                    object oldValue = Shader.GetUniform(oldUniformInfo.location, oldUniformInfo.type);
                    newShader.SetUniform(oldUniformInfo.location, oldUniformInfo.type, oldValue);
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

        Shader.SetUniform(info.location, info.type, value);
    }

    public object? GetUniform(string uniformName)
    {
        if (!ShaderInfo.TryGetUniformInfo(uniformName, out UniformInfo info))
        {
            Console.WriteLine($"Failed to get uniform \"{uniformName}\" of {Shader}; Not found.");
            return null;
        }

        return Shader.GetUniform(info.location, info.type);
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