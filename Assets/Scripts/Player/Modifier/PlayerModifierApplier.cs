using System.Collections.Generic;
using UnityEngine;

public sealed class PlayerModifierApplier : MonoBehaviour
{
    [SerializeField] private Health _health;
    [SerializeField] private Stamina _stamina;
    [SerializeField] private CharacterMover _characterMover;
    [SerializeField] private PlayerMovement _playerMovement;
    [SerializeField] private PlayerCombat _playerCombat;
    [SerializeField] private PlayerModifierStack _playerModifierStack;

    private float _baseMaxHealth;
    private bool _baseHealthAutoRegen;
    private float _baseHealthRegenPerSecond;
    private float _baseHealthRegenDelay;

    private float _baseMaxStamina;
    private bool _baseStaminaAutoRegen;
    private float _baseStaminaRegenPerSecond;
    private float _baseStaminaRegenDelay;

    private float _baseMoveSpeed;
    private float _baseSprintSpeed;

    private float _baseJumpStaminaCost;
    private float _baseSprintStaminaCostPerSecond;
    private float _baseAttackStaminaCost;

    private void Awake()
    {
        CacheBaseValues();
    }

    private void OnEnable()
    {
        _playerModifierStack.Changed += Apply;
    }

    private void Start()
    {
        Apply();
    }

    private void OnDisable()
    {
        _playerModifierStack.Changed -= Apply;
    }

    private void CacheBaseValues()
    {
        _baseMaxHealth = _health.MaxValue;
        _baseHealthAutoRegen = _health.AutoRegen;
        _baseHealthRegenPerSecond = _health.RegenPerSecond;
        _baseHealthRegenDelay = _health.RegenDelay;

        _baseMaxStamina = _stamina.MaxValue;
        _baseStaminaAutoRegen = _stamina.AutoRegen;
        _baseStaminaRegenPerSecond = _stamina.RegenPerSecond;
        _baseStaminaRegenDelay = _stamina.RegenDelay;

        _baseMoveSpeed = _characterMover.Speed;
        _baseSprintSpeed = _characterMover.SprintSpeed;

        _baseJumpStaminaCost = _playerMovement.JumpStaminaCost;
        _baseSprintStaminaCostPerSecond = _playerMovement.SprintStaminaCostPerSecond;
        _baseAttackStaminaCost = _playerCombat.AttackStaminaCost;
    }

    private void Apply()
    {
        PlayerModifierContext context = new PlayerModifierContext();
        context.MaxHealth = _baseMaxHealth;
        context.HealthAutoRegen = _baseHealthAutoRegen;
        context.HealthRegenPerSecond = _baseHealthRegenPerSecond;
        context.HealthRegenDelay = _baseHealthRegenDelay;

        context.MaxStamina = _baseMaxStamina;
        context.StaminaAutoRegen = _baseStaminaAutoRegen;
        context.StaminaRegenPerSecond = _baseStaminaRegenPerSecond;
        context.StaminaRegenDelay = _baseStaminaRegenDelay;

        context.MoveSpeed = _baseMoveSpeed;
        context.SprintSpeed = _baseSprintSpeed;

        context.JumpStaminaCost = _baseJumpStaminaCost;
        context.SprintStaminaCostPerSecond = _baseSprintStaminaCostPerSecond;
        context.AttackStaminaCost = _baseAttackStaminaCost;

        IReadOnlyList<PlayerModifier> modifiers = _playerModifierStack.Modifiers;

        for (int i = 0; i < modifiers.Count; i++)
        {
            PlayerModifier modifier = modifiers[i];

            if (modifier == null)
            {
                continue;
            }

            modifier.Apply(ref context);
        }

        _health.ApplyModifier(
            context.MaxHealth,
            context.HealthAutoRegen,
            context.HealthRegenPerSecond,
            context.HealthRegenDelay);

        _stamina.ApplyModifier(
            context.MaxStamina,
            context.StaminaAutoRegen,
            context.StaminaRegenPerSecond,
            context.StaminaRegenDelay);

        _characterMover.ApplySpeed(context.MoveSpeed, context.SprintSpeed);
        _playerMovement.ApplyModifier(context.JumpStaminaCost, context.SprintStaminaCostPerSecond);
        _playerCombat.ApplyModifier(context.AttackStaminaCost);
    }
}
