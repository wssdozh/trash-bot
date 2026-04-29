using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class FootstepAudio : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private PlayerAnimator _animator;
    [SerializeField] private FootstepAudioProfile _profile;

    [Header("Settings")]
    [SerializeField, Min(0f)] private float _walkVolumeScale = 0.24f;
    [SerializeField, Min(0f)] private float _runVolumeScale = 0.36f;

    private void Awake()
    {
        if (_audioSource == null)
        {
            throw new InvalidOperationException(nameof(_audioSource));
        }

        if (_animator == null)
        {
            throw new InvalidOperationException(nameof(_animator));
        }

        if (_profile == null)
        {
            throw new InvalidOperationException(nameof(_profile));
        }

        if (_profile.HasWalkClips() == false)
        {
            throw new InvalidOperationException(nameof(_profile));
        }

        if (_walkVolumeScale < 0f)
        {
            throw new InvalidOperationException(nameof(_walkVolumeScale));
        }

        if (_runVolumeScale < 0f)
        {
            throw new InvalidOperationException(nameof(_runVolumeScale));
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
        _animator.Stepped += OnStepped;
    }

    private void OnDisable()
    {
        _animator.Stepped -= OnStepped;
    }

    private void OnStepped(bool isRunning)
    {
        AudioClip clip = _profile.GetClip(isRunning);

        if (clip == null)
        {
            return;
        }

        _audioSource.pitch = 1f;
        _audioSource.PlayOneShot(clip, GetVolumeScale(isRunning));
    }

    private float GetVolumeScale(bool isRunning)
    {
        if (isRunning)
        {
            return _runVolumeScale;
        }

        return _walkVolumeScale;
    }
}
