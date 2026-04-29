using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class HealthAudio : MonoBehaviour
{
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private Health _health;
    [SerializeField] private AudioClip[] _damagedClips;
    [SerializeField] private AudioClip[] _endedClips;
    [SerializeField, Min(0f)] private float _damagedVolumeScale = 1f;
    [SerializeField, Min(0f)] private float _endedVolumeScale = 1f;

    private bool _isEnded;

    private void Awake()
    {
        if (_audioSource == null)
        {
            throw new InvalidOperationException(nameof(_audioSource));
        }

        if (_health == null)
        {
            throw new InvalidOperationException(nameof(_health));
        }

        if (HasAnyClips() == false)
        {
            throw new InvalidOperationException(nameof(_damagedClips));
        }

        _audioSource.playOnAwake = false;
        _audioSource.loop = false;
    }

    private void OnEnable()
    {
        _isEnded = _health.Value <= _health.MinValue;
        _health.Damaged += OnDamaged;
        _health.Ended += OnEnded;
    }

    private void OnDisable()
    {
        _health.Damaged -= OnDamaged;
        _health.Ended -= OnEnded;
    }

    private void OnDamaged(float amount)
    {
        if (_isEnded)
        {
            return;
        }

        Play(_damagedClips, _damagedVolumeScale);
    }

    private void OnEnded()
    {
        if (_isEnded)
        {
            return;
        }

        _isEnded = true;
        Play(_endedClips, _endedVolumeScale);
    }

    private void Play(AudioClip[] clips, float volumeScale)
    {
        AudioClip clip = GetRandomClip(clips);

        if (clip == null)
        {
            return;
        }

        _audioSource.pitch = 1f;
        _audioSource.PlayOneShot(clip, volumeScale);
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

    private bool HasAnyClips()
    {
        return HasClips(_damagedClips) || HasClips(_endedClips);
    }

    private bool HasClips(AudioClip[] clips)
    {
        return clips != null && clips.Length > 0;
    }
}
