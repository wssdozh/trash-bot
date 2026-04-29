using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class PickupAudio : MonoBehaviour
{
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private BasePickup _pickup;
    [SerializeField] private AudioClip[] _clips;
    [SerializeField, Min(0f)] private float _volumeScale = 1f;

    private void Awake()
    {
        if (_audioSource == null)
        {
            throw new InvalidOperationException(nameof(_audioSource));
        }

        if (_pickup == null)
        {
            throw new InvalidOperationException(nameof(_pickup));
        }

        if (HasClips() == false)
        {
            throw new InvalidOperationException(nameof(_clips));
        }

        _audioSource.playOnAwake = false;
        _audioSource.loop = false;
    }

    private void OnEnable()
    {
        _pickup.PickedUp += HandlePickedUp;
    }

    private void OnDisable()
    {
        _pickup.PickedUp -= HandlePickedUp;
    }

    private void HandlePickedUp()
    {
        AudioClip clip = GetRandomClip();

        if (clip == null)
        {
            return;
        }

        _audioSource.pitch = 1f;
        _audioSource.PlayOneShot(clip, _volumeScale);
    }

    private AudioClip GetRandomClip()
    {
        int index = UnityEngine.Random.Range(0, _clips.Length);

        return _clips[index];
    }

    private bool HasClips()
    {
        return _clips != null && _clips.Length > 0;
    }
}
