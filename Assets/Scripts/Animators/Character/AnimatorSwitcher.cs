using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimatorSwitcher : MonoBehaviour
{
    [Header("Зависимости")]
    [SerializeField] private Animator _animator;
    [SerializeField] private RuntimeAnimatorController _baseController;
    [SerializeField] private List<WeaponAnimatorEntry> _weaponAnimators;
    [SerializeField] private WeaponType _defaultWeaponType = WeaponType.None;

    [Header("Blend")]
    [SerializeField] private int _weaponLayerIndex = 1;
    [SerializeField] private float _switchBlendTime = 0.1f;

    private AnimatorOverrideController _runtimeOverrideController;
    private Dictionary<WeaponType, AnimatorOverrideController> _overridesByWeaponType;
    private WeaponType _currentWeaponType;
    private Coroutine _switchCoroutine;

    private void Awake()
    {
        _runtimeOverrideController = new AnimatorOverrideController(_baseController);
        _animator.runtimeAnimatorController = _runtimeOverrideController;

        BuildOverridesDictionary();

        _currentWeaponType = _defaultWeaponType;
        ApplyWeaponType(_currentWeaponType);

        float startWeight = 0f;

        if (_currentWeaponType != WeaponType.None)
        {
            startWeight = 1f;
        }

        _animator.SetLayerWeight(_weaponLayerIndex, startWeight);
    }

    public void SetWeaponType(WeaponType weaponType)
    {
        if (_currentWeaponType == weaponType)
        {
            return;
        }

        _currentWeaponType = weaponType;

        if (_switchCoroutine != null)
        {
            StopCoroutine(_switchCoroutine);
        }

        _switchCoroutine = StartCoroutine(SwitchWeaponRoutine(_currentWeaponType));
    }

    private void BuildOverridesDictionary()
    {
        _overridesByWeaponType = new Dictionary<WeaponType, AnimatorOverrideController>();

        int count = _weaponAnimators.Count;

        for (int i = 0; i < count; i++)
        {
            WeaponAnimatorEntry entry = _weaponAnimators[i];

            if (_overridesByWeaponType.ContainsKey(entry.WeaponType) == true)
            {
                continue;
            }

            _overridesByWeaponType.Add(entry.WeaponType, entry.Controller);
        }
    }

    private IEnumerator SwitchWeaponRoutine(WeaponType weaponType)
    {
        yield return BlendLayerWeight(_weaponLayerIndex, 0f, _switchBlendTime);

        if (weaponType != WeaponType.None)
        {
            ApplyWeaponType(weaponType);
        }

        float targetWeight = 0f;

        if (weaponType != WeaponType.None)
        {
            targetWeight = 1f;
        }

        yield return BlendLayerWeight(_weaponLayerIndex, targetWeight, _switchBlendTime);

        _switchCoroutine = null;
    }

    private IEnumerator BlendLayerWeight(int layerIndex, float targetWeight, float duration)
    {
        float startWeight = _animator.GetLayerWeight(layerIndex);
        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;

            float t = time / duration;
            float weight = Mathf.Lerp(startWeight, targetWeight, t);

            _animator.SetLayerWeight(layerIndex, weight);

            yield return null;
        }

        _animator.SetLayerWeight(layerIndex, targetWeight);
    }

    private void ApplyWeaponType(WeaponType weaponType)
    {
        if (_overridesByWeaponType.TryGetValue(weaponType, out AnimatorOverrideController sourceOverrideController) == false)
        {
            return;
        }

        List<KeyValuePair<AnimationClip, AnimationClip>> overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();
        sourceOverrideController.GetOverrides(overrides);
        _runtimeOverrideController.ApplyOverrides(overrides);
    }
}
