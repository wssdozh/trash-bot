using UnityEngine;

[CreateAssetMenu(fileName = "ItemAudioProfile", menuName = "Audio/ItemAudioProfile")]
public class ItemAudioProfile : ScriptableObject
{
    [SerializeField] private AudioClip _useClip;

    public AudioClip UseClip
    {
        get { return _useClip; }
    }
}
 