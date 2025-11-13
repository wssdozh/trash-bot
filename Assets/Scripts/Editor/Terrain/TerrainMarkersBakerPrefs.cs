#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class TerrainMarkersBakerPrefs
{
    private const string TerrainKey = "TMB_Terrain";
    private const string RulesKey = "TMB_Rules";
    private const string ParentKey = "TMB_Parent";
    private const string ClearKey = "TMB_Clear";
    private const string OnlyNewKey = "TMB_OnlyNew";

    public static void Save(Terrain terrain, MarkerRules rules, Transform parent, bool clear, bool onlyNew)
    {
        if (terrain != null)
        {
            GlobalObjectId terrainId = GlobalObjectId.GetGlobalObjectIdSlow(terrain);
            EditorPrefs.SetString(TerrainKey, terrainId.ToString());
        }
        else
        {
            EditorPrefs.DeleteKey(TerrainKey);
        }

        if (rules != null)
        {
            string path = AssetDatabase.GetAssetPath(rules);
            EditorPrefs.SetString(RulesKey, path);
        }
        else
        {
            EditorPrefs.DeleteKey(RulesKey);
        }

        if (parent != null)
        {
            GlobalObjectId parentId = GlobalObjectId.GetGlobalObjectIdSlow(parent);
            EditorPrefs.SetString(ParentKey, parentId.ToString());
        }
        else
        {
            EditorPrefs.DeleteKey(ParentKey);
        }

        EditorPrefs.SetInt(ClearKey, clear == true ? 1 : 0);
        EditorPrefs.SetInt(OnlyNewKey, onlyNew == true ? 1 : 0);
    }

    public static void Load(out Terrain terrain, out MarkerRules rules, out Transform parent, out bool clear, out bool onlyNew)
    {
        terrain = null;
        rules = null;
        parent = null;
        clear = EditorPrefs.GetInt(ClearKey, 1) == 1;
        onlyNew = EditorPrefs.GetInt(OnlyNewKey, 1) == 1;

        string terrainStr = EditorPrefs.GetString(TerrainKey, string.Empty);
        if (string.IsNullOrEmpty(terrainStr) == false)
        {
            GlobalObjectId terrainId;
            bool parsed = GlobalObjectId.TryParse(terrainStr, out terrainId);
            if (parsed == true)
            {
                UnityEngine.Object terrainObj = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(terrainId);
                terrain = terrainObj as Terrain;
            }
        }

        string rulesPath = EditorPrefs.GetString(RulesKey, string.Empty);
        if (string.IsNullOrEmpty(rulesPath) == false)
        {
            rules = AssetDatabase.LoadAssetAtPath<MarkerRules>(rulesPath);
        }

        string parentStr = EditorPrefs.GetString(ParentKey, string.Empty);
        if (string.IsNullOrEmpty(parentStr) == false)
        {
            GlobalObjectId parentId;
            bool parsed = GlobalObjectId.TryParse(parentStr, out parentId);
            if (parsed == true)
            {
                UnityEngine.Object parentObj = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(parentId);
                parent = parentObj as Transform;
            }
        }
    }
}
#endif
