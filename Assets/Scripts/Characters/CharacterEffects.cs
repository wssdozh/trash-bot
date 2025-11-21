using UnityEngine;


public class CharacterEffects : MonoBehaviour
{
    [SerializeField] private Health _health;
    [SerializeField] private Stamina _stamina;

    public void Heal(float amount)
    {
        if (amount <= 0f)
        {
            return;
        }

        _health.Increase(amount);
    }

    public void RestoreStamina(float amount)
    {
        if (amount <= 0f)
        {
            return;
        }

        _stamina.Increase(amount);
    }
}