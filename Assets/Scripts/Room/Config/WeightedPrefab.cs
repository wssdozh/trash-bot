using System;
using UnityEngine;

[Serializable]
public sealed class WeightedPrefab
{
    [SerializeField] private GameObject _prefab;
    [SerializeField] private int _weight = 1;
    [SerializeField, Min(0f)] private float _spawnHeight = 1.75f;

    [Tooltip("If enabled, this prefab must appear at least once when possible.")]
    [SerializeField] private bool _guaranteed = false;

    public GameObject Prefab => _prefab;
    public int Weight => _weight;
    public float SpawnHeight => _spawnHeight;
    public bool Guaranteed => _guaranteed;

    public void SetSpawnHeight(float spawnHeight)
    {
        _spawnHeight = Mathf.Max(0f, spawnHeight);
    }
}
