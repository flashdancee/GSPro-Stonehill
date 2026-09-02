using UnityEditor;
using UnityEngine;

/// <summary>
/// A one-time new editor asset that makes Unity 2018 invalidate the transferred
/// ScriptAssemblies cache. It is harmless after the first successful compile.
/// </summary>
[InitializeOnLoad]
public static class StonehillRecompileTrigger
{
    static StonehillRecompileTrigger()
    {
        Debug.Log("STONEHILL_FRONT_NINE_EDITOR_ASSEMBLY_LOADED");
    }
}
