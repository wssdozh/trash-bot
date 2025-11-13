#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public class TerrainMarkersBakerWindow : EditorWindow
{
    private Terrain _Terrain;
    private MarkerRules _Rules;
    private Transform _Parent;
    private bool _ClearTreesAfterBake = true;
    private bool _OnlyNew = true;

    [MenuItem("Tools/Terrain Markers Baker")]
    public static void ShowWindow()
    {
        TerrainMarkersBakerWindow window = GetWindow<TerrainMarkersBakerWindow>("Markers Baker");
        window.Show();
    }

    private void OnEnable()
    {
        TerrainMarkersBakerPrefs.Load(out _Terrain, out _Rules, out _Parent, out _ClearTreesAfterBake, out _OnlyNew);
    }

    private void OnGUI()
    {
        EditorGUI.BeginChangeCheck();

        _Terrain = (Terrain)EditorGUILayout.ObjectField("Terrain", _Terrain, typeof(Terrain), true);
        _Rules = (MarkerRules)EditorGUILayout.ObjectField("Rules", _Rules, typeof(MarkerRules), false);
        _Parent = (Transform)EditorGUILayout.ObjectField("Parent", _Parent, typeof(Transform), true);
        _ClearTreesAfterBake = EditorGUILayout.Toggle("Clear Trees After Bake", _ClearTreesAfterBake);
        _OnlyNew = EditorGUILayout.Toggle("Only New", _OnlyNew);

        if (EditorGUI.EndChangeCheck() == true)
        {
            TerrainMarkersBakerPrefs.Save(_Terrain, _Rules, _Parent, _ClearTreesAfterBake, _OnlyNew);
        }

        if (GUILayout.Button("Bake") == true)
        {
            TerrainMarkersBakerPrefs.Save(_Terrain, _Rules, _Parent, _ClearTreesAfterBake, _OnlyNew);
            BakeMode mode = _OnlyNew == true ? BakeMode.OnlyNew : BakeMode.All;
            TerrainMarkersBaker.Bake(_Terrain, _Rules, _ClearTreesAfterBake, _Parent, mode);
        }

        if (GUILayout.Button("Delete Baked Under Parent") == true)
        {
            TerrainMarkersBaker.DeleteBakedUnderParent(_Terrain, _Parent);
        }

        if (GUILayout.Button("Delete All Baked For Terrain") == true)
        {
            TerrainMarkersBaker.DeleteAllBakedForTerrain(_Terrain);
        }
    }
}
#endif
