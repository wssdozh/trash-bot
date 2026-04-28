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
        AudioClip clip = _profile.GetClip(_weaponHolder.CurrentItem);

        if (clip == null)
        {
            return;
        }

        _audioSource.pitch = 1f;
        _audioSource.PlayOneShot(clip, _volumeScale);
    }
}
