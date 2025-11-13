using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "TerrainMarkers/MarkerRules")]
public class MarkerRules : ScriptableObject
{
    public List<MarkerRule> Rules = new List<MarkerRule>();
}

[System.Serializable]
public class MarkerRule
{
    public GameObject TreePrototypePrefab;
    public GameObject SpawnPrefab;
    public Transform ParentOverride;
    public string AnchorPath;
    public float YOffset = 0f;
    public bool AlignToNormal = true;
    public bool RandomYaw = true;
}
