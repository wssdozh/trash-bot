using UnityEngine;

[CreateAssetMenu(fileName = "FadableSettings", menuName = "Gameplay/Fadable Settings")]
public class FadableSettings : ScriptableObject
{
    public float FadeDuration = 0.4f;
    public float OccludedAlpha = 0.4f;
}
