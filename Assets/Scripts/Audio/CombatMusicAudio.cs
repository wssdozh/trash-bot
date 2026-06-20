using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class CombatMusicAudio : MonoBehaviour
{
    private const float MinVolumeDelta = 0.0001f;

    [SerializeField] private AudioSource _defaultSource;
    [SerializeField] private AudioSource _bossSource;
    [SerializeField, Min(0f)] private float _defaultTargetVolume = 0.06f;
    [SerializeField, Min(0f)] private float _bossTargetVolume = 0.06f;
    [SerializeField, Min(0f)] private float _fadeSpeed = 0.6f;
    [SerializeField, Min(0f)] private float _bossReleaseDelay = 1.2f;

    private MusicState _targetState;
    private MusicState _releaseState;
    private float _releaseTimer;
    private Coroutine _fadeCoroutine;

    private enum MusicState
    {
        Default = 0,
        Boss = 1
    }

    private void Awake()
    {
        ValidateSource(_defaultSource, nameof(_defaultSource));
        ValidateSource(_bossSource, nameof(_bossSource));
        ValidateNonNegative(_defaultTargetVolume, nameof(_defaultTargetVolume));
        ValidateNonNegative(_bossTargetVolume, nameof(_bossTargetVolume));
        ValidateNonNegative(_fadeSpeed, nameof(_fadeSpeed));
        ValidateNonNegative(_bossReleaseDelay, nameof(_bossReleaseDelay));

        SetupSource(_defaultSource);
        SetupSource(_bossSource);
    }

    private void OnEnable()
    {
        RoomCombatLock.StateChanged += OnRoomCombatLockStateChanged;
        RefreshMusicState();
    }

    private void OnDisable()
    {
        RoomCombatLock.StateChanged -= OnRoomCombatLockStateChanged;
        _targetState = MusicState.Default;
        _releaseState = MusicState.Default;
        _releaseTimer = 0f;
        StopFadeRoutine();
        StopSource(_defaultSource);
        StopSource(_bossSource);
    }

    private void RefreshMusicState()
    {
        MusicState targetState = GetTargetState();

        if (targetState == _targetState)
        {
            EnsureFadeRoutine();

            return;
        }

        if (targetState == MusicState.Default && _targetState != MusicState.Default)
        {
            _releaseState = _targetState;
            _releaseTimer = _bossReleaseDelay;
        }
        else
        {
            _releaseState = MusicState.Default;
            _releaseTimer = 0f;
        }

        _targetState = targetState;
        EnsureFadeRoutine();
    }

    private IEnumerator FadeRoutine()
    {
        bool shouldContinue = true;

        while (shouldContinue)
        {
            if (_targetState == MusicState.Default && _releaseTimer > 0f)
                _releaseTimer = Mathf.Max(0f, _releaseTimer - Time.deltaTime);

            MusicState audibleState = GetAudibleState();
            float defaultTargetVolume = audibleState == MusicState.Default ? _defaultTargetVolume : 0f;
            float bossTargetVolume = audibleState == MusicState.Boss ? _bossTargetVolume : 0f;

            FadeSource(_defaultSource, defaultTargetVolume);
            FadeSource(_bossSource, bossTargetVolume);
            shouldContinue = ShouldContinueFade(defaultTargetVolume, bossTargetVolume);

            if (shouldContinue)
            {
                yield return null;
            }
        }

        _fadeCoroutine = null;
    }

    private MusicState GetTargetState()
    {
        IReadOnlyList<RoomCombatLock> roomCombatLocks = RoomCombatLock.Instances;

        for (int i = 0; i < roomCombatLocks.Count; i++)
        {
            RoomCombatLock roomCombatLock = roomCombatLocks[i];

            if (roomCombatLock == null)
                continue;

            if (roomCombatLock.IsLocked == false)
                continue;

            if (roomCombatLock.HasAliveBoss)
                return MusicState.Boss;
        }

        return MusicState.Default;
    }

    private MusicState GetAudibleState()
    {
        if (_targetState == MusicState.Default && _releaseTimer > 0f)
            return _releaseState;

        return _targetState;
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
        source.playOnAwake = false;
        source.loop = true;
        source.volume = 0f;

        if (source.isPlaying)
            source.Stop();
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

    private bool ShouldContinueFade(float defaultTargetVolume, float bossTargetVolume)
    {
        if (_releaseTimer > 0f)
        {
            return true;
        }

        if (Mathf.Abs(_defaultSource.volume - defaultTargetVolume) > MinVolumeDelta)
        {
            return true;
        }

        if (Mathf.Abs(_bossSource.volume - bossTargetVolume) > MinVolumeDelta)
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

    private void OnRoomCombatLockStateChanged()
    {
        RefreshMusicState();
    }
}
