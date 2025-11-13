#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

public enum BakeMode
{
    All,
    OnlyNew
}

public static class TerrainMarkersBaker
{
    public static void Bake(Terrain terrain, MarkerRules rules, bool clearTreesAfterBake, Transform globalParent, BakeMode mode)
    {
        string batchId = System.Guid.NewGuid().ToString();
        TerrainData terrainData = terrain.terrainData;
        Dictionary<GameObject, MarkerRule> ruleMap = BuildRuleMap(rules);
        string terrainId = GetTerrainId(terrain);
        HashSet<string> existing = BuildExistingKeys(terrainId);

        TreeInstance[] treeInstances = terrainData.treeInstances;

        for (int i = 0; i < treeInstances.Length; i++)
        {
            TreeInstance treeInstance = treeInstances[i];

            MarkerRule markerRule;
            bool hasRule = ruleMap.TryGetValue(terrainData.treePrototypes[treeInstance.prototypeIndex].prefab, out markerRule);
            if (hasRule == false)
            {
                continue;
            }

            string key = MakeMarkerKey(treeInstance);
            if (mode == BakeMode.OnlyNew && existing.Contains(key) == true)
            {
                continue;
            }

            Vector3 worldXZ = ComputeWorldXZ(terrain, terrainData, treeInstance);
            Quaternion rotation = ComputeRotation(terrain, terrainData, worldXZ, markerRule.AlignToNormal, markerRule.RandomYaw);

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(markerRule.SpawnPrefab);
            Transform targetParent = ResolveParent(markerRule, globalParent);

            instance.transform.rotation = rotation;
            ApplyScaleFromTree(instance.transform, treeInstance);
            PlaceWithAnchorRaycast(terrain, instance.transform, markerRule.AnchorPath, worldXZ, markerRule.YOffset);
            instance.transform.SetParent(targetParent, true);

            BakedMarkerTag tag = instance.GetComponent<BakedMarkerTag>();
            if (tag == null)
            {
                tag = instance.AddComponent<BakedMarkerTag>();
            }
            tag.BatchId = batchId;
            tag.TerrainId = terrainId;
            tag.Key = key;

            existing.Add(key);
            Undo.RegisterCreatedObjectUndo(instance, "Bake Marker");
            EditorUtility.SetDirty(instance);
        }

        if (clearTreesAfterBake == true)
        {
            terrainData.treeInstances = new TreeInstance[0];
            TerrainCollider terrainCollider = terrain.GetComponent<TerrainCollider>();
            terrainCollider.terrainData = terrainData;
            EditorUtility.SetDirty(terrainData);
        }
    }

    public static void DeleteBakedUnderParent(Terrain terrain, Transform parent)
    {
        if (parent == null)
        {
            return;
        }

        string terrainId = GetTerrainId(terrain);
        BakedMarkerTag[] tags = parent.GetComponentsInChildren<BakedMarkerTag>(true);
        for (int i = 0; i < tags.Length; i++)
        {
            if (tags[i].TerrainId == terrainId)
            {
                Undo.DestroyObjectImmediate(tags[i].gameObject);
            }
        }
    }

    public static void DeleteAllBakedForTerrain(Terrain terrain)
    {
        string terrainId = GetTerrainId(terrain);
        BakedMarkerTag[] tags = Object.FindObjectsByType<BakedMarkerTag>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < tags.Length; i++)
        {
            if (tags[i].TerrainId == terrainId)
            {
                Undo.DestroyObjectImmediate(tags[i].gameObject);
            }
        }
    }

    private static Dictionary<GameObject, MarkerRule> BuildRuleMap(MarkerRules rules)
    {
        Dictionary<GameObject, MarkerRule> map = new Dictionary<GameObject, MarkerRule>();
        for (int i = 0; i < rules.Rules.Count; i++)
        {
            MarkerRule rule = rules.Rules[i];
            if (map.ContainsKey(rule.TreePrototypePrefab) == false)
            {
                map.Add(rule.TreePrototypePrefab, rule);
            }
            else
            {
                map[rule.TreePrototypePrefab] = rule;
            }
        }
        return map;
    }

    private static string MakeMarkerKey(TreeInstance treeInstance)
    {
        int qx = Mathf.RoundToInt(treeInstance.position.x * 100000f);
        int qz = Mathf.RoundToInt(treeInstance.position.z * 100000f);
        string s = treeInstance.prototypeIndex.ToString() + ":" + qx.ToString() + ":" + qz.ToString();
        return s;
    }

    private static HashSet<string> BuildExistingKeys(string terrainId)
    {
        HashSet<string> set = new HashSet<string>();
        BakedMarkerTag[] tags = Object.FindObjectsByType<BakedMarkerTag>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < tags.Length; i++)
        {
            if (tags[i].TerrainId == terrainId)
            {
                if (string.IsNullOrEmpty(tags[i].Key) == false)
                {
                    set.Add(tags[i].Key);
                }
            }
        }
        return set;
    }

    private static Transform ResolveParent(MarkerRule rule, Transform globalParent)
    {
        if (rule.ParentOverride != null)
        {
            return rule.ParentOverride;
        }
        return globalParent;
    }

    private static Vector3 ComputeWorldXZ(Terrain terrain, TerrainData data, TreeInstance treeInstance)
    {
        Vector3 terrainPosition = terrain.transform.position;
        Vector3 size = data.size;
        float worldX = terrainPosition.x + treeInstance.position.x * size.x;
        float worldZ = terrainPosition.z + treeInstance.position.z * size.z;
        Vector3 worldXZ = new Vector3(worldX, 0f, worldZ);
        return worldXZ;
    }

    private static Quaternion ComputeRotation(Terrain terrain, TerrainData data, Vector3 worldXZ, bool alignToNormal, bool randomYaw)
    {
        Quaternion rotation = Quaternion.identity;
        if (alignToNormal == true)
        {
            float u = (worldXZ.x - terrain.transform.position.x) / data.size.x;
            float v = (worldXZ.z - terrain.transform.position.z) / data.size.z;
            Vector3 normal = data.GetInterpolatedNormal(u, v);
            rotation = Quaternion.FromToRotation(Vector3.up, normal);
        }
        if (randomYaw == true)
        {
            rotation = rotation * Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);
        }
        return rotation;
    }

    private static void ApplyScaleFromTree(Transform transform, TreeInstance treeInstance)
    {
        Vector3 baseScale = transform.localScale;
        float sx = baseScale.x * treeInstance.widthScale;
        float sy = baseScale.y * treeInstance.heightScale;
        float sz = baseScale.z * treeInstance.widthScale;
        Vector3 finalScale = new Vector3(sx, sy, sz);
        transform.localScale = finalScale;
    }

    private static void PlaceWithAnchorRaycast(Terrain terrain, Transform root, string anchorPath, Vector3 worldXZ, float yOffset)
    {
        Transform anchor = root;
        if (string.IsNullOrEmpty(anchorPath) == false)
        {
            Transform found = root.Find(anchorPath);
            if (found != null)
            {
                anchor = found;
            }
        }

        Vector3 delta = anchor.position - root.position;

        float rayStartY = terrain.transform.position.y + terrain.terrainData.size.y + 1000f;
        Vector3 origin = new Vector3(worldXZ.x, rayStartY, worldXZ.z);
        Ray ray = new Ray(origin, Vector3.down);

        RaycastHit[] hits = Physics.RaycastAll(ray, 20000f, ~0, QueryTriggerInteraction.Ignore);
        bool hitFound = false;
        Vector3 hitPoint = Vector3.zero;

        float minDist = float.MaxValue;
        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i].transform == null)
            {
                continue;
            }
            if (hits[i].transform.IsChildOf(root) == true)
            {
                continue;
            }
            float d = hits[i].distance;
            if (d < minDist)
            {
                minDist = d;
                hitPoint = hits[i].point;
                hitFound = true;
            }
        }

        if (hitFound == false)
        {
            float groundY = terrain.SampleHeight(new Vector3(worldXZ.x, 0f, worldXZ.z)) + terrain.transform.position.y;
            hitPoint = new Vector3(worldXZ.x, groundY, worldXZ.z);
        }

        Vector3 newRootPos = new Vector3(hitPoint.x - delta.x, hitPoint.y - delta.y + yOffset, hitPoint.z - delta.z);
        root.position = newRootPos;
    }

    private static string GetTerrainId(Terrain terrain)
    {
        GlobalObjectId id = GlobalObjectId.GetGlobalObjectIdSlow(terrain);
        string s = id.ToString();
        return s;
    }
}
#endif
