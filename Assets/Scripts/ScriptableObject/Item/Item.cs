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
    [SerializeField] private PickupSpawnerRef _pickupSpawnerRef;
    [SerializeField] private List<ItemEffect> _effects;
    [SerializeField] private ItemAudioProfile _audioProfile;

    public string Id
    {
        get { return _id; }
    }

    public string DisplayName
    {
        get { return _displayName; }
    }

    public Sprite Icon
    {
        get { return _icon; }
    }

    public bool IsStackable
    {
        get { return _isStackable; }
    }

    public int MaxStack
    {
        get { return _maxStack; }
    }

    public PickupSpawnerRef PickupSpawnerRef
    {
        get { return _pickupSpawnerRef; }
    }

    public IReadOnlyList<ItemEffect> Effects
    {
        get { return _effects; }
    }

    public ItemAudioProfile AudioProfile
    {
        get { return _audioProfile; }
    }
}
