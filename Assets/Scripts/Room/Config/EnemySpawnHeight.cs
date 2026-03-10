using System;
using UnityEngine;

[Serializable]
public sealed class EnemySpawnHeight
{
    [Tooltip("Префаб врага, для которого задаётся высота спавна.")]
    [SerializeField] private GameObject _prefab;

    [Tooltip("Высота спавна в блоках для этого врага.")]
    [SerializeField, Min(0f)] private float _spawnHeight = 1.75f;

    public GameObject Prefab => _prefab;
    public float SpawnHeight => _spawnHeight;
}
