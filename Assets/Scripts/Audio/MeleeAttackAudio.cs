using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class MeleeAttackAudio : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private Attacker _attacker;
    [SerializeField] private WeaponHolder _weaponHolder;
    [SerializeField] private MeleeAttackAudioProfile _profile;

    [Header("Settings")]
    [SerializeField, Min(0f)] private float _volumeScale = 1f;
    [SerializeField, Min(0f)] private float _fistPitchMin = 0.94f;
    [SerializeField, Min(0f)] private float _fistPitchMax = 1.06f;
    [SerializeField, Min(0f)] private float _batonPitchMin = 0.92f;
    [SerializeField, Min(0f)] private float _batonPitchMax = 1.08f;

    private void Awake()
    {
        if (_audioSource == null)
        {
            throw new InvalidOperationException(nameof(_audioSource));
        }

        if (_attacker == null)
        {
            throw new InvalidOperationException(nameof(_attacker));
        }

        if (_weaponHolder == null)
        {
            throw new InvalidOperationException(nameof(_weaponHolder));
        }

        if (_profile == null)
        {
            throw new InvalidOperationException(nameof(_profile));
        }

        if (_profile.HasClips() == false)
        {
            throw new InvalidOperationException(nameof(_profile));
        }

        if (_volumeScale < 0f)
        {
            throw new InvalidOperationException(nameof(_volumeScale));
        }

        if (_fistPitchMin > _fistPitchMax)
            throw new InvalidOperationException(nameof(_fistPitchMin));

        if (_batonPitchMin > _batonPitchMax)
            throw new InvalidOperationException(nameof(_batonPitchMin));

        if (_audioSource.enabled == false)
        {
            throw new InvalidOperationException(nameof(_audioSource));
        }

        _audioSource.playOnAwake = false;
        _audioSource.loop = false;
    }

    private void OnEnable()
    {
        _attacker.HitPerformed += OnHitPerformed;
    }

    private void OnDisable()
    {
        _attacker.HitPerformed -= OnHitPerformed;
    }

    private void OnHitPerformed()
    {
        Item item = _weaponHolder.CurrentItem;
        AudioClip clip = _profile.GetClip(item);

        if (clip == null)
        {
            return;
        }

        _audioSource.pitch = GetPitch(item);
        _audioSource.PlayOneShot(clip, _volumeScale);
    }

    private float GetPitch(Item item)
    {
        if (item != null && item.WeaponType == WeaponType.Melee)
            return UnityEngine.Random.Range(_batonPitchMin, _batonPitchMax);

        return UnityEngine.Random.Range(_fistPitchMin, _fistPitchMax);
    }
}
