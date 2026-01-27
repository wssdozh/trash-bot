using UnityEngine;

[DisallowMultipleComponent]
public sealed class WeaponHolderCursorFireCooldownBinder : MonoBehaviour
{
    [SerializeField] private WeaponHolder _weaponHolder;
    [SerializeField] private CursorRadialIndicator _cooldownView;

    private FireExecutor _fireExecutor;

    private void OnEnable()
    {
        _weaponHolder.Changed += OnWeaponHolderChanged;

        OnWeaponHolderChanged();
    }

    private void OnDisable()
    {
        _weaponHolder.Changed -= OnWeaponHolderChanged;

        _fireExecutor = null;

        _cooldownView.SetReady();
    }

    private void Update()
    {
        if (_fireExecutor == null)
            return;

        float cooldown01 = _fireExecutor.GetFireCooldown01();

        _cooldownView.SetCooldown01(cooldown01);
    }

    private void OnWeaponHolderChanged()
    {
        _fireExecutor = _weaponHolder.FireExecutor;

        if (_fireExecutor == null)
        {
            _cooldownView.SetReady();

            return;
        }
    }
}
