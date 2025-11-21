using UnityEngine;
using UnityEngine.Audio;


public class AudioControler : MonoBehaviour
{
    private const float MinDb = -80f;

    [SerializeField] private AudioMixer _mixer;

    [SerializeField] private AudioMixerGroup _masterParameter;
    [SerializeField] private AudioMixerGroup _buttonsParameter;
    [SerializeField] private AudioMixerGroup _musicParameter;

    private float _saveVolume;
    private bool _isOnSound = true;

    public void ToggleMaster(bool enabled)
    {
        _isOnSound = enabled;
        _mixer.SetFloat(_masterParameter.name, enabled ? LinearToDb(_saveVolume) : MinDb);
    }

    public void ChangeMaster(float linear)
    {
        if (_isOnSound)
        {
            _mixer.SetFloat(_masterParameter.name, LinearToDb(linear));
            _saveVolume = linear;
        }
    }

    public void ChangeButtons(float linear)
    {
        _mixer.SetFloat(_buttonsParameter.name, LinearToDb(linear));
    }

    public void ChangeMusic(float linear)
    {
        _mixer.SetFloat(_musicParameter.name, LinearToDb(linear));
    }
    
    private static float LinearToDb(float value)
    {
        float threshold = 0.0001f;
        float multipler = 20f;

        if (value <= threshold)
            return MinDb;

        return Mathf.Log10(value) * multipler;
    }
}