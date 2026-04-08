using UnityEngine;

public class PickupTrigger : MonoBehaviour
{
    [SerializeField] private Collider _triggerCollider;
    [SerializeField] private Inventory _inventory;
    [SerializeField] private Transform _pickupTarget;

    private void Awake()
    {
        _triggerCollider.isTrigger = true;

        if (_inventory == null)
        {
            _inventory = GetComponent<Inventory>();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        TryPickup(other);
    }

    private void TryPickup(Collider other)
    {
        BasePickup itemPickup = other.GetComponent<BasePickup>();

        if (itemPickup == null)
        {
            return;
        }

        GameObject collector = GetCollector();

        itemPickup.TryCollect(collector, _inventory);
    }

    private GameObject GetCollector()
    {
        if (_pickupTarget != null)

            return _pickupTarget.gameObject;

        Player player = GetComponentInParent<Player>();

        if (player != null)
        {
            return player.gameObject;
        }

        if (_inventory != null)
        {
            return _inventory.transform.root.gameObject;
        }

        return transform.root.gameObject;
    }
}
