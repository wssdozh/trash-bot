using System.Collections.Generic;
using UnityEngine;

public class AnimatorSwitcher : MonoBehaviour
{
    [SerializeField] private Animator _animator;
    [SerializeField] private List<RuntimeAnimatorController> _weaponControllers;
    [SerializeField] private WeaponType _defaultWeaponType = WeaponType.None;

    private WeaponType _currentWeaponType;

    private void Awake()
    {
        _currentWeaponType = _defaultWeaponType;
        ApplyWeaponType(_currentWeaponType);
    }

    public void SetWeaponType(WeaponType weaponType)
    {
        _currentWeaponType = weaponType;
        ApplyWeaponType(_currentWeaponType);
    }

    private void ApplyWeaponType(WeaponType weaponType)
    {
        int index = (int)weaponType;

        if (index < 0)
        {
            return;
        }

        if (index >= _weaponControllers.Count)
        {
            return;
        }

        _animator.runtimeAnimatorController = _weaponControllers[index];
    }
}
