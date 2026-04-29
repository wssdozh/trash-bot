using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class RangedFireAudio : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private FireExecutor _fireExecutor;
    [SerializeField] private Turret _turret;
    [SerializeField] private EnemyDroneBrain _droneBrain;
    [SerializeField] private AudioClip _clip;

    [Header("Settings")]
    [SerializeField, Min(0f)] private float _volumeScale = 1f;

    private void Awake()
    {
        if (_audioSource == null)
        {
            throw new InvalidOperationException(nameof(_audioSource));
        }

        if (GetSourceCount() != 1)
        {
            throw new InvalidOperationException(nameof(_fireExecutor));
        }

        if (_clip == null)
        {
            throw new InvalidOperationException(nameof(_clip));
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
        if (_fireExecutor != null)
        {
            _fireExecutor.ShotPerformed += OnShotPerformed;
        }

        if (_turret != null)
        {
            _turret.ShotPerformed += OnShotPerformed;
        }

        if (_droneBrain != null)
        {
            _droneBrain.ShotPerformed += OnShotPerformed;
        }
    }

    private void OnDisable()
    {
        if (_fireExecutor != null)
        {
            _fireExecutor.ShotPerformed -= OnShotPerformed;
        }

        if (_turret != null)
        {
            _turret.ShotPerformed -= OnShotPerformed;
        }

        if (_droneBrain != null)
        {
            _droneBrain.ShotPerformed -= OnShotPerformed;
        }
    }

    private void OnShotPerformed()
    {
        _audioSource.pitch = 1f;
        _audioSource.PlayOneShot(_clip, _volumeScale);
    }

    private int GetSourceCount()
    {
        int count = 0;

        if (_fireExecutor != null)
        {
            count += 1;
        }

        if (_turret != null)
        {
            count += 1;
        }

        if (_droneBrain != null)
        {
            count += 1;
        }

        return count;
    }
}
