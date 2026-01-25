using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Item", menuName = "Inventory/Item")]
public class Item : ScriptableObject
{
    [SerializeField] private string _id;
    [SerializeField] private string _displayName;
    [SerializeField] private Sprite _icon;
    [SerializeField] private bool _isStackable;
    [SerializeField] private int _maxStack = 1;
    [SerializeField] private WeaponType _weaponType = WeaponType.None;
    [SerializeField] private List<ItemEffect> _effects;
    [SerializeField] private ItemAudioProfile _audioProfile;
    [SerializeField] private BasePickup _prefab;

    public string Id => _id;
    public string DisplayName => _displayName;
    public Sprite Icon => _icon;
    public bool IsStackable => _isStackable;
    public int MaxStack => _maxStack;
    public WeaponType WeaponType => _weaponType;
    public IReadOnlyList<ItemEffect> Effects => _effects;
    public ItemAudioProfile AudioProfile => _audioProfile;
    public BasePickup Prefab => _prefab;
}