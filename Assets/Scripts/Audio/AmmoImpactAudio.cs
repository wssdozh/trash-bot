using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class AmmoImpactAudio : AmmoLifeListener
{
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip[] _impactClips;
    [SerializeField] private AudioClip[] _targetImpactClips;
    [SerializeField, Min(0f)] private float _volumeScale = 1f;

    private bool _isImpactStarted;

    public override bool IsLifeEndComplete
    {
        get
        {
            if (_isImpactStarted == false)
            {
                return true;
            }

            return _audioSource.isPlaying == false;
        }
    }

    protected override void Awake()
    {
        base.Awake();

        if (_audioSource == null)
        {
            throw new InvalidOperationException(nameof(_audioSource));
        }

        if (HasAnyClips() == false)
        {
            throw new InvalidOperationException(nameof(_impactClips));
        }

        _audioSource.playOnAwake = false;
        _audioSource.loop = false;
    }

    protected override void OnDisable()
    {
        base.OnDisable();

        if (_audioSource != null)
        {
            _audioSource.Stop();
        }

        _isImpactStarted = false;
    }

    protected override void OnAmmoEnabled()
    {
        _isImpactStarted = false;
        _audioSource.Stop();
    }

    protected override void OnAmmoTargetImpacted(Collider hitCollider)
    {
        if (TryPlay(_targetImpactClips))
        {
            _isImpactStarted = true;
        }
    }

    protected override void OnAmmoImpacted()
    {
        if (_isImpactStarted)
        {
            return;
        }

        if (TryPlay(_impactClips))
        {
            _isImpactStarted = true;
        }
    }

    private bool TryPlay(AudioClip[] clips)
    {
        AudioClip clip = GetRandomClip(clips);

        if (clip == null)
        {
            return false;
        }

        _audioSource.pitch = 1f;
        _audioSource.PlayOneShot(clip, _volumeScale);

        return true;
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
        return HasClips(_impactClips) || HasClips(_targetImpactClips);
    }

    private bool HasClips(AudioClip[] clips)
    {
        return clips != null && clips.Length > 0;
    }
}
