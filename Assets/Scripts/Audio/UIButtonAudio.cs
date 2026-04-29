using System;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class UIButtonAudio : MonoBehaviour
{
    [SerializeField] private Button _button;
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip _clickClip;
    [SerializeField, Min(0f)] private float _volumeScale = 2.1f;

    private void Awake()
    {
        if (_button == null)
            throw new InvalidOperationException(nameof(_button));

        if (_audioSource == null)
            throw new InvalidOperationException(nameof(_audioSource));

        if (_clickClip == null)
            throw new InvalidOperationException(nameof(_clickClip));

        if (_volumeScale < 0f)
            throw new InvalidOperationException(nameof(_volumeScale));

        _audioSource.playOnAwake = false;
        _audioSource.loop = false;
    }

    private void OnEnable()
    {
        _button.onClick.AddListener(HandleClicked);
    }

    private void OnDisable()
    {
        _button.onClick.RemoveListener(HandleClicked);
    }

    private void HandleClicked()
    {
        if (_button.interactable == false)
            return;

        _audioSource.pitch = 1f;
        _audioSource.PlayOneShot(_clickClip, _volumeScale);
    }
}
