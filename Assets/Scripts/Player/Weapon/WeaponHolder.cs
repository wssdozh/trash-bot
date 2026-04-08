using System;
using UnityEngine;

public class WeaponHolder : MonoBehaviour
{
    private const CollisionDetectionMode HeldDetection = CollisionDetectionMode.ContinuousSpeculative;
    private const RigidbodyInterpolation HeldInterpolation = RigidbodyInterpolation.None;

    [SerializeField] private Transform _weaponSocket;
    [SerializeField] private Health _ownerHealth;
    [SerializeField] private LayerMask _targetLayers;

    private Spawner<BasePickup> _pickupSpawner;
    private BasePickup _pickup;

    private bool _hasPendingPickupSpawner;
    private string _pendingPickupSpawnerKey;

    public FireExecutor FireExecutor { get; private set; }
    public bool IsHoldAllowed { get; private set; }
    public bool IsSwitchLocked { get; private set; }

    public event Action Changed;

    private void Update()
    {
        if (_hasPendingPickupSpawner == false)
        {
            return;
        }

        if (IsHoldAllowed == false)
        {
            return;
        }

        if (IsSwitchLocked)
        {
            return;
        }

        if (_pickup != null)
        {
            return;
        }

        EquipInternal(_pendingPickupSpawnerKey);
    }

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

        if (_hasPendingPickupSpawner && IsSwitchLocked == false)
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

        if (IsSwitchLocked == false && IsHoldAllowed && _hasPendingPickupSpawner)
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

        if (IsSwitchLocked)
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

        Spawner<BasePickup> pickupSpawner = SpawnerServiceLocator.Find<BasePickup>(pickupSpawnerKey);

        if (pickupSpawner == null)
        {
            return;
        }

        if (_pickupSpawner == pickupSpawner && _pickup != null)
        {
            return;
        }

        ClearInternal(false);

        _pickupSpawner = pickupSpawner;
        _pickup = _pickupSpawner.Spawn(_weaponSocket.position);

        HeldMode heldMode = _pickup.GetComponent<HeldMode>();

        if (heldMode != null)
        {
            heldMode.SetHeld(true);
        }

        WeaponGrip weaponGrip = _pickup.GetComponentInChildren<WeaponGrip>();

        Vector3 localPositionOffset = Vector3.zero;
        Vector3 localRotationOffsetEuler = Vector3.zero;

        if (weaponGrip != null)
        {
            localPositionOffset = weaponGrip.LocalPositionOffset;
            localRotationOffsetEuler = weaponGrip.LocalRotationOffsetEuler;
        }

        _pickup.transform.SetParent(_weaponSocket, false);
        _pickup.transform.localPosition = localPositionOffset;
        _pickup.transform.localRotation = Quaternion.Euler(localRotationOffsetEuler);

        Rigidbody rigidbody = _pickup.GetComponent<Rigidbody>();

        if (rigidbody != null)
        {
            rigidbody.interpolation = HeldInterpolation;
            rigidbody.collisionDetectionMode = HeldDetection;
            rigidbody.position = _pickup.transform.position;
            rigidbody.rotation = _pickup.transform.rotation;
        }

        PickupReturner pickupReturner = _pickup.GetComponent<PickupReturner>();

        if (pickupReturner != null)
        {
            pickupReturner.SetCanReturn(false);
        }

        FireExecutor = _pickup.GetComponentInChildren<FireExecutor>();

        if (FireExecutor != null)
        {
            if (_ownerHealth == null)
            {
                throw new InvalidOperationException(nameof(_ownerHealth));
            }

            FireExecutor.SetIgnoredRoot(_ownerHealth.transform);
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
            _pickup.transform.SetParent(null, true);

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
        FireExecutor = null;

        if (clearPending)
        {
            _hasPendingPickupSpawner = false;
            _pendingPickupSpawnerKey = null;
        }

        Changed?.Invoke();

    }
}
