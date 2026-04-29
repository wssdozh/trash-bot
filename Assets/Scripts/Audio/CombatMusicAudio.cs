using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class CombatMusicAudio : MonoBehaviour
{
    [SerializeField] private AudioSource _audioSource;
    [SerializeField, Min(0f)] private float _targetVolume = 0.14f;
    [SerializeField, Min(0f)] private float _fadeSpeed = 0.6f;

    private void Awake()
    {
        if (_audioSource == null)
            throw new InvalidOperationException(nameof(_audioSource));

        if (_targetVolume < 0f)
            throw new InvalidOperationException(nameof(_targetVolume));

        if (_fadeSpeed < 0f)
            throw new InvalidOperationException(nameof(_fadeSpeed));

        _audioSource.playOnAwake = true;
        _audioSource.loop = true;
        _audioSource.volume = 0f;

        if (_audioSource.isPlaying == false)
            _audioSource.Play();
    }

    private void Update()
    {
        float targetVolume = HasActiveCombat() ? _targetVolume : 0f;
        _audioSource.volume = Mathf.MoveTowards(_audioSource.volume, targetVolume, _fadeSpeed * Time.deltaTime);
    }

    private bool HasActiveCombat()
    {
        IReadOnlyList<RoomCombatLock> roomCombatLocks = RoomCombatLock.Instances;

        for (int i = 0; i < roomCombatLocks.Count; i++)
        {
            RoomCombatLock roomCombatLock = roomCombatLocks[i];

            if (roomCombatLock != null && roomCombatLock.IsLocked)
                return true;
        }

        return false;
    }
}
