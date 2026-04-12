using UnityEngine;

[CreateAssetMenu(fileName = "PlayerModifierPool", menuName = "Shop/Player Modifier Pool")]
public sealed class PlayerModifierPool : ScriptableObject
{
    [SerializeField] private PlayerModifierOffer[] _offers;

    public PlayerModifierOffer[] Offers => _offers;
}
