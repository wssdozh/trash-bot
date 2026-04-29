using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class WeaponHolderAudio : MonoBehaviour
{
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private WeaponHolder _weaponHolder;
    [SerializeField] private AudioClip[] _equipClips;
    [SerializeField, Min(0f)] private float _volumeScale = 1f;

    private Item _currentItem;

    private void Awake()
    {
        if (_audioSource == null)
        {
            throw new InvalidOperationException(nameof(_audioSource));
        }

        if (_weaponHolder == null)
        {
            throw new InvalidOperationException(nameof(_weaponHolder));
        }

        if (HasClips(_equipClips) == false)
        {
            throw new InvalidOperationException(nameof(_equipClips));
        }

        _audioSource.playOnAwake = false;
        _audioSource.loop = false;
    }

    private void OnEnable()
    {
        _currentItem = _weaponHolder.CurrentItem;
        _weaponHolder.Changed += OnWeaponHolderChanged;
    }

    private void OnDisable()
    {
        _weaponHolder.Changed -= OnWeaponHolderChanged;
    }

    private void OnWeaponHolderChanged()
    {
        Item currentItem = _weaponHolder.CurrentItem;

        if (currentItem != null && currentItem != _currentItem)
        {
            Play();
        }

        _currentItem = currentItem;
    }

    private void Play()
    {
        AudioClip clip = GetRandomClip(_equipClips);

        if (clip == null)
        {
            return;
        }

        _audioSource.pitch = 1f;
        _audioSource.PlayOneShot(clip, _volumeScale);
    }

    private AudioClip GetRandomClip(AudioClip[] clips)
    {
        if (HasClips(clips) == false)
        {
            return null;
        }

        int index = UnityEngine.Random.Range(0, clips.Length);

        return clips[index];
    }

    private bool HasClips(AudioClip[] clips)
    {
        return clips != null && clips.Length > 0;
    }
}
