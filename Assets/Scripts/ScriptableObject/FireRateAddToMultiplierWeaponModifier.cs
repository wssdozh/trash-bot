using UnityEngine;

[CreateAssetMenu(fileName = "FireRateWeaponFilteredModifier", menuName = "Weapons/Modifiers/Fire Rate Weapon Filtered Modifier")]
public sealed class FireRateWeaponFilteredModifier : WeaponTypeFilteredModifier
{
    [SerializeField] private float _addToMultiplier = 0.1f;

    public float AddToMultiplier => _addToMultiplier;

    protected override void ApplyAllowed(ref WeaponModifierContext context)
    {
        context.FireRateMultiplier += _addToMultiplier;
    }
}
