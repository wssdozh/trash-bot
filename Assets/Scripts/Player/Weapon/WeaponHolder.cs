using System;
using UnityEngine;

public class WeaponHolder : MonoBehaviour
{
    [SerializeField] private Transform _weaponSocket;
    [SerializeField] private string _targetTag = "Enemy";

    private PickupSpawner _pickupSpawner;
    private BasePickup _pickup;
    private FireExecutor _fireExecutor;

    public FireExecutor FireExecutor
    {
        get { return _fireExecutor; }
    }

    public event Action Changed;

    public void Equip(PickupSpawnerRef pickupSpawnerRef)
    {
        PickupSpawner pickupSpawner = pickupSpawnerRef.Value;

        if (_pickupSpawner == pickupSpawner && _pickup != null)
        {
            return;
        }

        Clear();

        _pickupSpawner = pickupSpawner;

        _pickup = _pickupSpawner.Spawn(_weaponSocket.position);
        _pickup.transform.SetParent(_weaponSocket, true);

        WeaponGrip weaponGrip = _pickup.GetComponentInChildren<WeaponGrip>();

        Vector3 localPositionOffset = Vector3.zero;
        Vector3 localRotationOffsetEuler = Vector3.zero;

        if (weaponGrip != null)
        {
            localPositionOffset = weaponGrip.LocalPositionOffset;
            localRotationOffsetEuler = weaponGrip.LocalRotationOffsetEuler;
        }

        _pickup.transform.localPosition = localPositionOffset;
        _pickup.transform.localRotation = Quaternion.Euler(localRotationOffsetEuler);

        PickupReturner pickupReturner = _pickup.GetComponent<PickupReturner>();
        if (pickupReturner != null)
        {
            pickupReturner.SetCanReturn(false);
        }

        HeldMode heldMode = _pickup.GetComponent<HeldMode>();
        if (heldMode != null)
        {
            heldMode.SetHeld(true);
        }

        _fireExecutor = _pickup.GetComponentInChildren<FireExecutor>();

        if (_fireExecutor != null)
        {
            _fireExecutor.SetTargetTag(_targetTag);
        }

        Changed?.Invoke();
    }

    public void Clear()
    {
        if (_fireExecutor != null)
        {
            _fireExecutor.StopFiring();
        }

        if (_pickup != null)
        {
            HeldMode heldMode = _pickup.GetComponent<HeldMode>();
            if (heldMode != null)
            {
                heldMode.SetHeld(false);
            }

            PickupReturner pickupReturner = _pickup.GetComponent<PickupReturner>();
            if (pickupReturner != null)
            {
                pickupReturner.SetCanReturn(true);
            }
        }

        if (_pickup != null && _pickupSpawner != null)
        {
            _pickupSpawner.Despawn(_pickup);
        }

        _pickupSpawner = null;
        _pickup = null;
        _fireExecutor = null;

        Changed?.Invoke();
    }
}
