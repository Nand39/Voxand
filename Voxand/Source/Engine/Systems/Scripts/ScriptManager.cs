using DisposableExt;
namespace Voxand.Engine.Systems.Scripts;
public class ScriptManager : IDisposableExt
{
    List<Script> scripts = [];
    Dictionary<Script, int> indexMap = [];

    public DisposeHelper DisposeHelper { get; }
    public ScriptManager() => DisposeHelper = new(this);

    public void AddScript(Script script)
    {
        ArgumentNullException.ThrowIfNull(script);
        script.Initialize();
        indexMap[script] = scripts.Count;
        scripts.Add(script);
    }
    public void AddScripts(params Script[] scripts)
    {
        foreach (var script in scripts)
            AddScript(script);
    }
    public void RemoveScript(Script script)
    {
        ArgumentNullException.ThrowIfNull(script);
        if (indexMap.TryGetValue(script, out int index))
        {
            Script lastObject = scripts[scripts.Count - 1];
            scripts[index] = lastObject;
            indexMap[lastObject] = index;

            scripts.RemoveAt(scripts.Count - 1);
            indexMap.Remove(script);
            script.Dispose();
            return;
        }
        throw new KeyNotFoundException($"Cannot remove script: not found.");
    }
    public void RemoveScripts(params Script[] scripts)
    {
        foreach (var script in scripts)
            RemoveScript(script);
    }
    public void Update()
    {
        for (int i = 0; i < scripts.Count; i++)
            scripts[i].Update();
    }
    void IDisposableExt.Free()
    {
        for (int i = scripts.Count - 1; i >= 0; i--)
            RemoveScript(scripts[0]);
    }
}