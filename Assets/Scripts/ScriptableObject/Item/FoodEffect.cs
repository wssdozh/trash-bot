using UnityEngine;

[CreateAssetMenu(fileName = "FoodEffect", menuName = "Effects")]
public class FoodEffect : ItemEffect
{
    [SerializeField] private float _healAmount;
    [SerializeField] private float _staminaAmount;

    public override void Apply(CharacterEffects target)
    {

        target.Heal(_healAmount);
        target.RestoreStamina(_staminaAmount);
    }
}