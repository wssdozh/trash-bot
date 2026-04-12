using UnityEngine;

[CreateAssetMenu(fileName = "PlayerMultiplierModifier", menuName = "Player/Modifiers/Multiplier")]
public sealed class PlayerMultiplierModifier : PlayerModifier
{
    [SerializeField] private PlayerModifierStat _stat;
    [SerializeField] private float _multiplier = 1.1f;

    public PlayerModifierStat Stat => _stat;

    public float Multiplier => _multiplier;

    public override void Apply(ref PlayerModifierContext context)
    {
        switch (_stat)
        {
            case PlayerModifierStat.MaxHealth:
                context.MaxHealth *= _multiplier;
                break;

            case PlayerModifierStat.HealthRegenPerSecond:
                context.HealthRegenPerSecond *= _multiplier;
                break;

            case PlayerModifierStat.HealthRegenDelay:
                context.HealthRegenDelay *= _multiplier;
                break;

            case PlayerModifierStat.MaxStamina:
                context.MaxStamina *= _multiplier;
                break;

            case PlayerModifierStat.StaminaRegenPerSecond:
                context.StaminaRegenPerSecond *= _multiplier;
                break;

            case PlayerModifierStat.StaminaRegenDelay:
                context.StaminaRegenDelay *= _multiplier;
                break;

            case PlayerModifierStat.MoveSpeed:
                context.MoveSpeed *= _multiplier;
                break;

            case PlayerModifierStat.SprintSpeed:
                context.SprintSpeed *= _multiplier;
                break;

            case PlayerModifierStat.JumpStaminaCost:
                context.JumpStaminaCost *= _multiplier;
                break;

            case PlayerModifierStat.SprintStaminaCost:
                context.SprintStaminaCostPerSecond *= _multiplier;
                break;

            case PlayerModifierStat.AttackStaminaCost:
                context.AttackStaminaCost *= _multiplier;
                break;
        }
    }
}
