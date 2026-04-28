using UnityEngine;

[CreateAssetMenu(fileName = "FootstepAudioProfile", menuName = "Audio/FootstepAudioProfile")]
public sealed class FootstepAudioProfile : ScriptableObject
{
    [SerializeField] private AudioClip[] _walkClips;
    [SerializeField] private AudioClip[] _runClips;

    public bool HasWalkClips()
    {
        return HasClips(_walkClips);
    }

    public AudioClip GetClip(bool isRunning)
    {
        if (isRunning)
        {
            AudioClip runClip = GetRandomClip(_runClips);

            if (runClip != null)
            {
                return runClip;
            }
        }

        return GetRandomClip(_walkClips);
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
