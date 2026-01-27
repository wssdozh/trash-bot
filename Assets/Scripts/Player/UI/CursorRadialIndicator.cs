using UnityEngine;
using UnityEngine.UI;

public sealed class CursorRadialIndicator : MonoBehaviour
{
    [SerializeField] private Image _cooldownImage;

    public void SetCooldown01(float cooldown01)
    {
        _cooldownImage.fillAmount = Mathf.Clamp01(cooldown01);
    }

    public void SetReady()
    {
        _cooldownImage.fillAmount = 1f;
    }
}
