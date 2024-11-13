using System.Runtime.CompilerServices;

using OpenTK.Graphics.OpenGL4;

using GLAV.Helpers.ExtensionMethods;

namespace GLAV.Systems;
public static class GLRegistry
{
    static int[] textureUnits = new int[GL.GetInteger(GetPName.MaxCombinedTextureImageUnits)];
    static int activeTextureUnit;

    static int[] bufferTargets;
    static Dictionary<BufferTarget, int> bufferTargetMapping;

    static int activeShaderProgram;

    static GLRegistry()
    {
        BufferTarget[] bufferTargets = (BufferTarget[])Enum.GetValues(typeof(BufferTarget));
        GLRegistry.bufferTargets = new int[bufferTargets.Length];
        bufferTargetMapping = [];
        for (int i = 0; i < bufferTargets.Length; i++)
        {
            GLRegistry.bufferTargets[i] = -1;
            bufferTargetMapping[bufferTargets[i]] = i;
        }
    }

    #region TEXTURES

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SelectTextureUnit(TextureUnit unit)
    {
        int u = (int)unit - (int)TextureUnit.Texture0;
        if (activeTextureUnit == u) return;
        activeTextureUnit = u;
        GL.ActiveTexture(unit);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void BindTexture(TextureTarget target, int handle)
    {
        if (textureUnits[activeTextureUnit] == handle) return;
        textureUnits[activeTextureUnit] = handle;
        GL.BindTexture(target, handle);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void BindTexture(TextureUnit unit, TextureTarget target, int handle)
    {
        SelectTextureUnit(unit);
        BindTexture(target, handle);
    }

    #endregion

    #region BUFFERS
    public static void BindBuffer(BufferTarget target, int handle)
    {
        int targetIndex = bufferTargetMapping[target];
        if (bufferTargets[targetIndex] == handle) 
            return;
        bufferTargets.ReplaceFirst(handle, -1);
        bufferTargets[targetIndex] = handle;
        GL.BindBuffer(target, handle);
    }

    #endregion

    public static void UseProgram(int handle)
    {
        if (activeShaderProgram == handle)
            return;
        activeShaderProgram = handle;
        GL.UseProgram(handle);
    }
}