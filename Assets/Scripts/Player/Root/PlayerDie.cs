using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

class PlayerDie : MonoBehaviour
{
    [SerializeField] private CharacterDied _characterDied;
    [SerializeField] private TimeScaleSettings _timeScaleSettings;
    [SerializeField] private HeldMode _heldMode;
    [SerializeField] private Player _player;
    [SerializeField] private Health _health;
    [SerializeField] private BlurOverlay _blurOverlay;
    [SerializeField] private ExitMenuView _exitMenuView;
    [SerializeField] private Button _buttonRevival;

    private TimeScale _timeScale;
    private Coroutine _coroutine;

    private void Awake()
    {
        _timeScale = new TimeScale(_timeScaleSettings.BaseFixedDeltaTime, _timeScaleSettings.MinPhysicsTimeScale);
    }

    private void OnEnable()
    {
        _player.Died += Die;
        _buttonRevival.onClick.AddListener(ReloadScene);
    }

    private void OnDisable()
    {
        _player.Died -= Die;
        _buttonRevival.onClick.RemoveListener(ReloadScene);
        _timeScale.ResetToDefault();
    }

    private void Die()
    {
        _characterDied.EnableRegdoll();
        _heldMode.SetHeld(true);
        _health.SetAutoRegen(false);

        _timeScale.Animate(
            _timeScaleSettings.PausedTimeScale,
            _timeScaleSettings.PauseDurationSeconds,
            _timeScaleSettings.PauseEaseCurve,
            true
        );

        _blurOverlay.Show();
        _exitMenuView.Show();
    }

    private void ReloadScene()
    {
        if (_coroutine != null)
        {
            StopCoroutine(_coroutine);
        }

        _coroutine = StartCoroutine(ReloadSceneRoutine());
    }

    private IEnumerator ReloadSceneRoutine()
    {
        _exitMenuView.Hide();

        yield return new WaitForSecondsRealtime(0.6f);

        _timeScale.ResetToDefault();
        DOTween.KillAll(true);
        SceneLoadingScreen.ReloadCurrentScene();
    }
}
