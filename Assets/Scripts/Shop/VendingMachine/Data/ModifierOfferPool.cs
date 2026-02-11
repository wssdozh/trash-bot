using UnityEngine;

[CreateAssetMenu(fileName = "ModifierOfferPool", menuName = "Shop/Modifier Offer Pool")]
public sealed class ModifierOfferPool : ScriptableObject
{
    [SerializeField] private ModifierOffer[] _offers;

    public ModifierOffer[] Offers => _offers;
}
