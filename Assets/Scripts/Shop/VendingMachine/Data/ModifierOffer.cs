using UnityEngine;

[CreateAssetMenu(fileName = "ModifierOffer", menuName = "Shop/Modifier Offer")]
public sealed class ModifierOffer : ScriptableObject
{
    [SerializeField] private string _title;
    [SerializeField] private Sprite _icon;
    [SerializeField] private int _price;
    [SerializeField] private WeaponModifier[] _modifiers;

    public string Title => _title;

    public Sprite Icon => _icon;

    public int Price => _price;

    public WeaponModifier[] Modifiers => _modifiers;

    private void OnValidate()
    {
        if (_price < 0)
        {
            _price = 0;
        }
    }
}
