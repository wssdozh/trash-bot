using System.Collections.Generic;
using UnityEngine;

public static class RoomInteriorSourceDisabler
{
    public static void DisableRenderers(MeshFilter[] meshFilters, Transform combinedRoot, HashSet<int> combinedSourceObjectIds)
    {
        HashSet<int> processedObjects = new HashSet<int>();

        for (int index = 0; index < meshFilters.Length; index++)
        {
            MeshFilter meshFilter = meshFilters[index];

            if (meshFilter == null)
            {
                continue;
            }

            if (combinedRoot != null && meshFilter.transform.IsChildOf(combinedRoot) == true)
            {
                continue;
            }

            int id = meshFilter.gameObject.GetInstanceID();

            if (combinedSourceObjectIds.Contains(id) == false)
            {
                continue;
            }

            if (processedObjects.Add(id) == false)
            {
                continue;
            }

            MeshRenderer meshRenderer = meshFilter.GetComponent<MeshRenderer>();

            if (meshRenderer != null)
            {
                meshRenderer.enabled = false;
            }
        }
    }

    public static void DisableColliders(MeshFilter[] meshFilters, Transform combinedRoot, HashSet<int> combinedSourceObjectIds)
    {
        HashSet<int> processedObjects = new HashSet<int>();

        for (int index = 0; index < meshFilters.Length; index++)
        {
            MeshFilter meshFilter = meshFilters[index];

            if (meshFilter == null)
            {
                continue;
            }

            if (combinedRoot != null && meshFilter.transform.IsChildOf(combinedRoot) == true)
            {
                continue;
            }

            int id = meshFilter.gameObject.GetInstanceID();

            if (combinedSourceObjectIds.Contains(id) == false)
            {
                continue;
            }

            if (processedObjects.Add(id) == false)
            {
                continue;
            }

            Collider[] colliders = meshFilter.GetComponents<Collider>();

            for (int colliderIndex = 0; colliderIndex < colliders.Length; colliderIndex++)
            {
                Collider collider = colliders[colliderIndex];

                if (collider == null)
                {
                    continue;
                }

                collider.enabled = false;
            }
        }
    }
}
