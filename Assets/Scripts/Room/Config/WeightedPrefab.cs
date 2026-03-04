using System;
using UnityEngine;

[Serializable]
public sealed class WeightedPrefab
{
    [SerializeField] private GameObject _prefab;
    [SerializeField] private int _weight = 1;

    [Tooltip("Если включено — этот префаб должен появиться хотя бы 1 раз (если найдётся место).")]
    [SerializeField] private bool _guaranteed = false;

    public GameObject Prefab => _prefab;
    public int Weight => _weight;
    public bool Guaranteed => _guaranteed;
}
