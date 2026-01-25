using System;
using UnityEngine;

public class WeaponHolder : MonoBehaviour
{
    [SerializeField] private Transform _weaponSocket;
    [SerializeField] private LayerMask _targetLayers;

    private Spawner<BasePickup> _pickupSpawner;
    private BasePickup _pickup;

    private bool _hasPendingPickupSpawner;
    private string _pendingPickupSpawnerKey;

    public FireExecutor FireExecutor { get; private set; }
    public bool IsHoldAllowed { get; private set; }
    public bool IsSwitchLocked { get; private set; }

    public event Action Changed;

    public void SetHoldAllowed(bool isHoldAllowed)
    {

        if (IsHoldAllowed == isHoldAllowed)
        {
            return;
        }

        IsHoldAllowed = isHoldAllowed;

        if (IsHoldAllowed == false)
        {
            ClearInternal(false);
            return;
        }

        if (_hasPendingPickupSpawner == true && IsSwitchLocked == false)
        {
            EquipInternal(_pendingPickupSpawnerKey);
        }

    }

    public void SetSwitchLocked(bool isSwitchLocked)
    {

        if (IsSwitchLocked == isSwitchLocked)
        {
            return;
        }

        IsSwitchLocked = isSwitchLocked;

        if (IsSwitchLocked == false && IsHoldAllowed == true && _hasPendingPickupSpawner == true)
        {
            Spawner<BasePickup> pendingPickupSpawner = SpawnerServiceLocator.Get<BasePickup>(_pendingPickupSpawnerKey);

            if (_pickupSpawner != pendingPickupSpawner || _pickup == null)
            {
                EquipInternal(_pendingPickupSpawnerKey);
            }
        }

    }

    public void Equip(BasePickup pickupPrefab)
    {

        if (pickupPrefab == null)
        {
            throw new ArgumentNullException(nameof(pickupPrefab));
        }

        string pickupSpawnerKey = pickupPrefab.name;

        _pendingPickupSpawnerKey = pickupSpawnerKey;
        _hasPendingPickupSpawner = true;

        if (IsHoldAllowed == false)
        {
            ClearInternal(false);
            return;
        }

        if (IsSwitchLocked == true)
        {
            return;
        }

        EquipInternal(pickupSpawnerKey);

    }

    public void Clear()
    {
        ClearInternal(true);
    }

    private void EquipInternal(string pickupSpawnerKey)
    {

        Spawner<BasePickup> pickupSpawner = SpawnerServiceLocator.Get<BasePickup>(pickupSpawnerKey);

        if (_pickupSpawner == pickupSpawner && _pickup != null)
        {
            return;
        }

        ClearInternal(false);

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

        FireExecutor = _pickup.GetComponentInChildren<FireExecutor>();

        if (FireExecutor != null)
        {
            FireExecutor.SetTargetLayers(_targetLayers);
        }

        Changed?.Invoke();

    }

    private void ClearInternal(bool clearPending)
    {

        if (FireExecutor != null)
        {
            FireExecutor.StopFiring();
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

            _pickup.transform.SetParent(null, true);
        }

        if (_pickup != null && _pickupSpawner != null)
        {
            _pickupSpawner.Despawn(_pickup);
        }

        _pickupSpawner = null;
        _pickup = null;
        FireExecutor = null;

        if (clearPending)
        {
            _hasPendingPickupSpawner = false;
            _pendingPickupSpawnerKey = null;
        }

        Changed?.Invoke();

    }
}
