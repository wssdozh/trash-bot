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
    [SerializeField] private float _battleLayerWeight = 1f;

    private AnimatorOverrideController _runtimeOverrideController;
    private Dictionary<WeaponType, AnimatorOverrideController> _overridesByWeaponType;
    private List<KeyValuePair<AnimationClip, AnimationClip>> _baseOverrides;

    private WeaponType _currentWeaponType;

    private Coroutine _layerBlendCoroutine;
    private Coroutine _weaponSwitchCoroutine;

    public bool IsBattleMode { get; private set; }

    private void Awake()
    {
        _runtimeOverrideController = new AnimatorOverrideController(_baseController);
        _animator.runtimeAnimatorController = _runtimeOverrideController;

        CacheBaseOverrides();
        BuildOverridesDictionary();

        _currentWeaponType = _defaultWeaponType;
        ApplyWeaponType(_currentWeaponType);

        _animator.SetLayerWeight(_weaponLayerIndex, 0f);
    }

    public void SetBattleMode(bool isBattleMode)
    {

        if (IsBattleMode == isBattleMode)
        {
            return;
        }

        IsBattleMode = isBattleMode;


        if (_weaponSwitchCoroutine != null)
        {
            return;
        }


        if (_layerBlendCoroutine != null)
        {
            StopCoroutine(_layerBlendCoroutine);
        }

        float targetWeight = GetBattleLayerTargetWeight();
        _layerBlendCoroutine = StartCoroutine(BlendLayerWeightRoutine(_weaponLayerIndex, targetWeight, _switchBlendTime));
    }

    public void SetBattleModeInstant(bool isBattleMode)
    {
        IsBattleMode = isBattleMode;

        if (_layerBlendCoroutine != null)
        {
            StopCoroutine(_layerBlendCoroutine);
            _layerBlendCoroutine = null;
        }

        float targetWeight = GetBattleLayerTargetWeight();
        _animator.SetLayerWeight(_weaponLayerIndex, targetWeight);
    }

    public void SetWeaponType(WeaponType weaponType)
    {

        if (_currentWeaponType == weaponType)
        {
            return;
        }

        _currentWeaponType = weaponType;


        if (_weaponSwitchCoroutine != null)
        {
            StopCoroutine(_weaponSwitchCoroutine);
        }

        _weaponSwitchCoroutine = StartCoroutine(SwitchWeaponRoutine(_currentWeaponType));
    }

    public void SetWeaponTypeInstant(WeaponType weaponType)
    {
        _currentWeaponType = weaponType;

        if (_weaponSwitchCoroutine != null)
        {
            StopCoroutine(_weaponSwitchCoroutine);
            _weaponSwitchCoroutine = null;
        }

        ApplyWeaponType(_currentWeaponType);
    }

    private void CacheBaseOverrides()
    {
        List<KeyValuePair<AnimationClip, AnimationClip>> overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();
        _runtimeOverrideController.GetOverrides(overrides);

        _baseOverrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();

        int count = overrides.Count;

        for (int i = 0; i < count; i++)
        {
            AnimationClip originalClip = overrides[i].Key;
            _baseOverrides.Add(new KeyValuePair<AnimationClip, AnimationClip>(originalClip, originalClip));
        }
    }

    private void BuildOverridesDictionary()
    {
        _overridesByWeaponType = new Dictionary<WeaponType, AnimatorOverrideController>();

        int count = _weaponAnimators.Count;

        for (int i = 0; i < count; i++)
        {
            WeaponAnimatorEntry entry = _weaponAnimators[i];


            if (_overridesByWeaponType.ContainsKey(entry.WeaponType))
            {
                continue;
            }

            _overridesByWeaponType.Add(entry.WeaponType, entry.Controller);
        }
    }

    private IEnumerator SwitchWeaponRoutine(WeaponType weaponType)
    {

        if (_layerBlendCoroutine != null)
        {
            StopCoroutine(_layerBlendCoroutine);
            _layerBlendCoroutine = null;
        }

        yield return BlendLayerWeightRoutine(_weaponLayerIndex, 0f, _switchBlendTime);

        ApplyWeaponType(weaponType);

        float targetWeight = GetBattleLayerTargetWeight();

        yield return BlendLayerWeightRoutine(_weaponLayerIndex, targetWeight, _switchBlendTime);

        _weaponSwitchCoroutine = null;
    }

    private float GetBattleLayerTargetWeight()
    {

        if (IsBattleMode)
        {
            return Mathf.Clamp01(_battleLayerWeight);
        }

        return 0f;
    }

    private IEnumerator BlendLayerWeightRoutine(int layerIndex, float targetWeight, float duration)
    {
        float startWeight = _animator.GetLayerWeight(layerIndex);


        if (duration <= 0f)
        {
            _animator.SetLayerWeight(layerIndex, targetWeight);

            yield break;
        }

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

        if (_overridesByWeaponType.TryGetValue(weaponType, out AnimatorOverrideController sourceOverrideController))
        {
            List<KeyValuePair<AnimationClip, AnimationClip>> overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();
            sourceOverrideController.GetOverrides(overrides);
            _runtimeOverrideController.ApplyOverrides(overrides);

            return;
        }

        _runtimeOverrideController.ApplyOverrides(_baseOverrides);
    }
}
