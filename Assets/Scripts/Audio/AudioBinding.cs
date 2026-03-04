using UnityEngine;
using UnityEngine.UI;


public class AudioBinder : MonoBehaviour
{
    [SerializeField] private AudioControler _panelAudio;

    [SerializeField] private Slider _masterSlider;
    [SerializeField] private Slider _buttonsSlider;
    [SerializeField] private Slider _musicSlider;
    [SerializeField] private Toggle _masterToggle;

    private void OnEnable()
    {
        if (_masterSlider != null)
            _masterSlider.onValueChanged.AddListener(OnMasterChanged);

        if (_buttonsSlider != null)
            _buttonsSlider.onValueChanged.AddListener(OnButtonsChanged);

        if (_musicSlider != null)
            _musicSlider.onValueChanged.AddListener(OnMusicChanged);

        if (_masterToggle != null)
            _masterToggle.onValueChanged.AddListener(OnMasterToggled);
    }

    private void OnDisable()
    {
        if (_masterSlider != null)
            _masterSlider.onValueChanged.RemoveListener(OnMasterChanged);

        if (_buttonsSlider != null)
            _buttonsSlider.onValueChanged.RemoveListener(OnButtonsChanged);

        if (_musicSlider != null)
            _musicSlider.onValueChanged.RemoveListener(OnMusicChanged);

        if (_masterToggle != null)
            _masterToggle.onValueChanged.RemoveListener(OnMasterToggled);
    }

    private void Start()
    {
        if (_masterSlider  != null)
            OnMasterChanged (_masterSlider.value);

        if (_buttonsSlider != null)
            OnButtonsChanged(_buttonsSlider.value);

        if (_musicSlider != null)
            OnMusicChanged(_musicSlider.value);


        if (_masterToggle  != null)
            OnMasterToggled(_masterToggle.isOn);
    }

    private void OnMasterToggled(bool enabled)
    {
        _masterSlider.enabled = enabled;
        _buttonsSlider.enabled = enabled;
        _musicSlider.enabled = enabled;

        _panelAudio.ToggleMaster(enabled);
    }

    private void OnMasterChanged(float value) => _panelAudio.ChangeMaster(value);

    private void OnButtonsChanged(float value) => _panelAudio.ChangeButtons(value);

    private void OnMusicChanged(float value) => _panelAudio.ChangeMusic(value);

}