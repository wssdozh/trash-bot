using UnityEngine;

[CreateAssetMenu(fileName = "CriticalHitModifier", menuName = "Weapons/Modifiers/Critical Hit")]
public sealed class CriticalHitModifier : WeaponTypeFilteredModifier
{
    [SerializeField] private float _chanceAdd01 = 0.1f;
    [SerializeField] private float _damageMultiplier = 2f;

    protected override void ApplyAllowed(ref WeaponModifierContext context)
    {
        context.CriticalChance01 += _chanceAdd01;

        if (context.CriticalDamageMultiplier < _damageMultiplier)
        {
            context.CriticalDamageMultiplier = _damageMultiplier;
        }
    }
}
