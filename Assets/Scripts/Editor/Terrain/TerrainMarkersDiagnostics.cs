#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEditor.Compilation;
using System.IO;

public static class TerrainMarkersDiagnostics
{
    [MenuItem("Tools/Terrain Markers/Diagnose BakedMarkerTag")]
    public static void DiagnoseBakedMarkerTag()
    {
        string[] guids = AssetDatabase.FindAssets("t:MonoScript BakedMarkerTag");
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]); 
            Debug.Log("Script: " + path);
        }

        Assembly[] assemblies = CompilationPipeline.GetAssemblies();
        for (int a = 0; a < assemblies.Length; a++)
        {
            Assembly assembly = assemblies[a];
            string[] files = assembly.sourceFiles;
            for (int f = 0; f < files.Length; f++)
            {
                string fileName = Path.GetFileNameWithoutExtension(files[f]);
                if (fileName == "BakedMarkerTag")
                {
                    Debug.Log("Assembly: " + assembly.name + " Flags: " + assembly.flags.ToString());
                }
            }
        }
    }
}
#endif
