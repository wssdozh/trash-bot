using System;
using UnityEngine;

public sealed class RoomInteriorChunkRoots : MonoBehaviour
{
    [SerializeField] private Transform _combinedRoot;
    [SerializeField] private Transform _staticChunksRoot;
    [SerializeField] private Transform _notStaticChunksRoot;

    public Transform CombinedRoot
    {
        get
        {
            return _combinedRoot;
        }
    }

    public Transform StaticChunksRoot
    {
        get
        {
            return _staticChunksRoot;
        }
    }

    public Transform NotStaticChunksRoot
    {
        get
        {
            return _notStaticChunksRoot;
        }
    }

    public void Ensure(Transform interiorBlocksRoot)
    {
        if (interiorBlocksRoot == null)
        {
            throw new MissingReferenceException(nameof(interiorBlocksRoot));
        }

        EnsureCombinedRoot(interiorBlocksRoot);

        EnsureChunkRoot(ref _staticChunksRoot, "chunk (static)");
        EnsureChunkRoot(ref _notStaticChunksRoot, "chunk (not static)");
    }

    public void ClearCombined()
    {
        if (_combinedRoot == null)
        {
            return;
        }

        if (_staticChunksRoot != null)
        {
            DestroyChildren(_staticChunksRoot);
        }

        if (_notStaticChunksRoot != null)
        {
            DestroyChildren(_notStaticChunksRoot);
        }
    }

    private void EnsureCombinedRoot(Transform interiorBlocksRoot)
    {
        if (_combinedRoot != null)
        {
            return;
        }

        GameObject combined = new GameObject("__InteriorCombined");
        combined.transform.SetParent(interiorBlocksRoot, false);

        _combinedRoot = combined.transform;
    }

    private void EnsureChunkRoot(ref Transform chunkRoot, string name)
    {
        if (chunkRoot != null)
        {
            return;
        }

        Transform existing = _combinedRoot.Find(name);

        if (existing != null)
        {
            chunkRoot = existing;

            return;
        }

        GameObject rootObject = new GameObject(name);
        rootObject.transform.SetParent(_combinedRoot, false);

        chunkRoot = rootObject.transform;
    }

    private void DestroyChildren(Transform root)
    {
        int childCount = root.childCount;

        for (int childIndex = childCount - 1; childIndex >= 0; childIndex--)
        {
            Transform childTransform = root.GetChild(childIndex);

            if (Application.isPlaying == false)
            {
                DestroyImmediate(childTransform.gameObject);
            }
            else
            {
                Destroy(childTransform.gameObject);
            }
        }
    }
}
