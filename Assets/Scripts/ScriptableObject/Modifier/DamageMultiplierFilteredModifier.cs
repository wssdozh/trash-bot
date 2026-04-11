using UnityEngine;

[CreateAssetMenu(fileName = "DamageMultiplierFilteredModifier", menuName = "Weapons/Modifiers/Damage Multiplier Filtered")]
public sealed class DamageMultiplierFilteredModifier : WeaponTypeFilteredModifier
{
    [SerializeField] private float _multiplier = 1.2f;

    public float Multiplier => _multiplier;

    protected override void ApplyAllowed(ref WeaponModifierContext context)
    {
        context.DamageMultiplier *= _multiplier;
    }
}
