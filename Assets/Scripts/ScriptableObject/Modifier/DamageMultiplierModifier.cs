using UnityEngine;

[CreateAssetMenu(fileName = "DamageMultiplierModifier", menuName = "Weapons/Modifiers/Damage Multiplier")]
public sealed class DamageMultiplierModifier : WeaponModifier
{
    [SerializeField] private float _multiplier = 1.2f;

    public float Multiplier => _multiplier;

    public override void Apply(ref WeaponModifierContext context)
    {
        context.DamageMultiplier *= _multiplier;
    }
}
