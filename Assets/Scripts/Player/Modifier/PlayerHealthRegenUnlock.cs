using UnityEngine;

[CreateAssetMenu(fileName = "PlayerHealthRegenUnlock", menuName = "Player/Modifiers/Health Regen Unlock")]
public sealed class PlayerHealthRegenUnlock : PlayerModifier
{
    public override void Apply(ref PlayerModifierContext context)
    {
        context.HealthAutoRegen = true;
    }
}
