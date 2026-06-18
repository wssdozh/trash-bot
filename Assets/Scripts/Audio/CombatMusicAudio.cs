using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class CombatMusicAudio : MonoBehaviour
{
    private const float MinVolumeDelta = 0.0001f;

    [SerializeField] private AudioSource _explorationSource;
    [SerializeField] private AudioSource _combatSource;
    [SerializeField, Min(0f)] private float _explorationTargetVolume = 0.06f;
    [SerializeField, Min(0f)] private float _combatTargetVolume = 0.06f;
    [SerializeField, Min(0f)] private float _fadeSpeed = 0.6f;
    [SerializeField, Min(0f)] private float _combatReleaseDelay = 1.2f;

    private bool _isCombatActive;
    private float _combatReleaseTimer;
    private Coroutine _fadeCoroutine;

    private void Awake()
    {
        ValidateSource(_explorationSource, nameof(_explorationSource));
        ValidateSource(_combatSource, nameof(_combatSource));
        ValidateNonNegative(_explorationTargetVolume, nameof(_explorationTargetVolume));
        ValidateNonNegative(_combatTargetVolume, nameof(_combatTargetVolume));
        ValidateNonNegative(_fadeSpeed, nameof(_fadeSpeed));
        ValidateNonNegative(_combatReleaseDelay, nameof(_combatReleaseDelay));

        SetupSource(_explorationSource);
        SetupSource(_combatSource);
    }

    private void OnEnable()
    {
        RoomCombatLock.StateChanged += OnRoomCombatLockStateChanged;
        RefreshCombatState();
    }

    private void OnDisable()
    {
        RoomCombatLock.StateChanged -= OnRoomCombatLockStateChanged;
        _isCombatActive = false;
        _combatReleaseTimer = 0f;
        StopFadeRoutine();
        StopSource(_explorationSource);
        StopSource(_combatSource);
    }

    private void OnRoomCombatLockStateChanged()
    {
        RefreshCombatState();
    }

    private void RefreshCombatState()
    {
        bool hasActiveCombat = HasActiveCombat();

        if (hasActiveCombat)
        {
            _isCombatActive = true;
            _combatReleaseTimer = 0f;
            EnsureFadeRoutine();

            return;
        }

        if (_isCombatActive)
        {
            _isCombatActive = false;
            _combatReleaseTimer = _combatReleaseDelay;
            EnsureFadeRoutine();

            return;
        }

        EnsureFadeRoutine();
    }

    private IEnumerator FadeRoutine()
    {
        bool shouldContinue = true;

        while (shouldContinue)
        {
            if (_isCombatActive == false && _combatReleaseTimer > 0f)
            {
                _combatReleaseTimer = Mathf.Max(0f, _combatReleaseTimer - Time.deltaTime);
            }

            bool shouldPlayCombat = _isCombatActive || _combatReleaseTimer > 0f;
            float explorationTargetVolume = shouldPlayCombat ? 0f : _explorationTargetVolume;
            float combatTargetVolume = shouldPlayCombat ? _combatTargetVolume : 0f;

            FadeSource(_explorationSource, explorationTargetVolume);
            FadeSource(_combatSource, combatTargetVolume);
            shouldContinue = ShouldContinueFade(explorationTargetVolume, combatTargetVolume);

            if (shouldContinue)
            {
                yield return null;
            }
        }

        _fadeCoroutine = null;
    }

    private bool HasActiveCombat()
    {
        IReadOnlyList<RoomCombatLock> roomCombatLocks = RoomCombatLock.Instances;

        for (int i = 0; i < roomCombatLocks.Count; i++)
        {
            RoomCombatLock roomCombatLock = roomCombatLocks[i];

            if (roomCombatLock != null && roomCombatLock.IsLocked)
            {
                return true;
            }
        }

        return false;
    }

    private void ValidateSource(AudioSource source, string fieldName)
    {
        if (source == null)
        {
            throw new InvalidOperationException(fieldName);
        }
    }

    private void ValidateNonNegative(float value, string fieldName)
    {
        if (value < 0f)
        {
            throw new InvalidOperationException(fieldName);
        }
    }

    private void SetupSource(AudioSource source)
    {
        source.playOnAwake = true;
        source.loop = true;
        source.volume = 0f;
    }

    private void FadeSource(AudioSource source, float targetVolume)
    {
        if (targetVolume > 0f)
        {
            EnsurePlaybackStarted(source);
        }

        source.volume = Mathf.MoveTowards(source.volume, targetVolume, _fadeSpeed * Time.deltaTime);

        if (Mathf.Abs(source.volume - targetVolume) > MinVolumeDelta)
        {
            return;
        }

        source.volume = targetVolume;

        if (targetVolume <= 0f)
        {
            StopSource(source);
        }
    }

    private bool ShouldContinueFade(float explorationTargetVolume, float combatTargetVolume)
    {
        if (_combatReleaseTimer > 0f)
        {
            return true;
        }

        if (Mathf.Abs(_explorationSource.volume - explorationTargetVolume) > MinVolumeDelta)
        {
            return true;
        }

        if (Mathf.Abs(_combatSource.volume - combatTargetVolume) > MinVolumeDelta)
        {
            return true;
        }

        return false;
    }

    private void EnsurePlaybackStarted(AudioSource source)
    {
        if (source.isPlaying)
        {
            return;
        }

        source.Play();
    }

    private void StopSource(AudioSource source)
    {
        if (source == null)
        {
            return;
        }

        source.volume = 0f;

        if (source.isPlaying == false)
        {
            return;
        }

        source.Stop();
    }

    private void EnsureFadeRoutine()
    {
        if (_fadeCoroutine != null)
        {
            return;
        }

        _fadeCoroutine = StartCoroutine(FadeRoutine());
    }

    private void StopFadeRoutine()
    {
        if (_fadeCoroutine == null)
        {
            return;
        }

        StopCoroutine(_fadeCoroutine);
        _fadeCoroutine = null;
    }
}
