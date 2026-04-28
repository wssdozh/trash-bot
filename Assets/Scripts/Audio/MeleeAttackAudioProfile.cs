using UnityEngine;

[CreateAssetMenu(fileName = "MeleeAttackAudioProfile", menuName = "Audio/MeleeAttackAudioProfile")]
public sealed class MeleeAttackAudioProfile : ScriptableObject
{
    [SerializeField] private AudioClip[] _fistClips;
    [SerializeField] private AudioClip[] _batonClips;

    public bool HasClips()
    {
        if (HasClips(_fistClips))
        {
            return true;
        }

        return HasClips(_batonClips);
    }

    public AudioClip GetClip(Item item)
    {
        if (item != null)
        {
            if (item.WeaponType == WeaponType.Melee)
            {
                AudioClip batonClip = GetRandomClip(_batonClips);

                if (batonClip != null)
                {
                    return batonClip;
                }
            }
        }

        AudioClip fistClip = GetRandomClip(_fistClips);

        if (fistClip != null)
        {
            return fistClip;
        }

        return GetRandomClip(_batonClips);
    }

    private bool HasClips(AudioClip[] clips)
    {
        if (clips == null)
        {
            return false;
        }

        for (int i = 0; i < clips.Length; i++)
        {
            if (clips[i] != null)
            {
                return true;
            }
        }

        return false;
    }

    private AudioClip GetRandomClip(AudioClip[] clips)
    {
        if (clips == null)
        {
            return null;
        }

        if (clips.Length <= 0)
        {
            return null;
        }

        int startIndex = UnityEngine.Random.Range(0, clips.Length);

        for (int i = 0; i < clips.Length; i++)
        {
            int clipIndex = (startIndex + i) % clips.Length;
            AudioClip clip = clips[clipIndex];

            if (clip != null)
            {
                return clip;
            }
        }

        return null;
    }
}
