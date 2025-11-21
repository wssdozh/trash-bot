using System.Collections.Generic;
using UnityEngine;

public class CharacterAudio : MonoBehaviour
{
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private FootstepSet _footstepSet;

    public void PlayFootstep()
    {
        if (_footstepSet == null)
        {
            return;
        }

        IReadOnlyList<AudioClip> clips = _footstepSet.Clips;

        if (clips == null)
        {
            return;
        }

        if (clips.Count == 0)
        {
            return;
        }

        int index = Random.Range(0, clips.Count);
        AudioClip clip = clips[index];

        if (clip == null)
        {
            return;
        }

        float previousPitch = _audioSource.pitch;
        float randomPitch = Random.Range(_footstepSet.PitchMin, _footstepSet.PitchMax);

        _audioSource.pitch = randomPitch;
        _audioSource.PlayOneShot(clip);
        _audioSource.pitch = previousPitch;
    }

    public void PlayItemUse(Item item)
    {
        if (item == null)
        {
            return;
        }

        ItemAudioProfile audioProfile = item.AudioProfile;

        if (audioProfile == null)
        {
            return;
        }

        AudioClip clip = audioProfile.UseClip;

        if (clip == null)
        {
            return;
        }

        _audioSource.PlayOneShot(clip);
    }
}
