using System.Collections;
using DG.Tweening;
using JunkyardBoss;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class PlayerVictory : MonoBehaviour
{
    [SerializeField] private TimeScaleSettings _timeScaleSettings;
    [SerializeField] private HeldMode _heldMode;
    [SerializeField] private Player _player;
    [SerializeField] private BlurOverlay _blurOverlay;
    [SerializeField] private BaseMenuView _victoryMenuView;
    [SerializeField] private Button _restartButton;
    [SerializeField] private Button _mainMenuButton;
    [SerializeField] private string _mainMenuSceneName = "MainMenuScene";
    [SerializeField] private float _transitionDelaySeconds = 0.6f;

    private TimeScale _timeScale;
    private Coroutine _transitionCoroutine;
    private bool _isPlayerDead;
    private bool _isVictoryShown;

    private void Awake()
    {
        ValidateReference(_timeScaleSettings, nameof(_timeScaleSettings));
        ValidateReference(_heldMode, nameof(_heldMode));
        ValidateReference(_player, nameof(_player));

        if (string.IsNullOrWhiteSpace(_mainMenuSceneName))
        {
            throw new MissingReferenceException(nameof(_mainMenuSceneName));
        }

        _timeScale = new TimeScale(_timeScaleSettings.BaseFixedDeltaTime, _timeScaleSettings.MinPhysicsTimeScale);
    }

    private void OnEnable()
    {
        BossExcavator.AnyDied += OnBossDied;
        _player.Died += OnPlayerDied;

        if (_restartButton != null)
        {
            _restartButton.onClick.AddListener(OnRestartClicked);
        }

        if (_mainMenuButton != null)
        {
            _mainMenuButton.onClick.AddListener(OnMainMenuClicked);
        }
    }

    private void OnDisable()
    {
        BossExcavator.AnyDied -= OnBossDied;
        _player.Died -= OnPlayerDied;

        if (_restartButton != null)
        {
            _restartButton.onClick.RemoveListener(OnRestartClicked);
        }

        if (_mainMenuButton != null)
        {
            _mainMenuButton.onClick.RemoveListener(OnMainMenuClicked);
        }

        if (_transitionCoroutine != null)
        {
            StopCoroutine(_transitionCoroutine);
            _transitionCoroutine = null;
        }

        _timeScale.ResetToDefault();
        _isPlayerDead = false;
        _isVictoryShown = false;
    }

    private void OnBossDied(BossExcavator boss)
    {
        if (boss == null)
        {
            return;
        }

        if (_isVictoryShown)
        {
            return;
        }

        if (_isPlayerDead)
        {
            return;
        }

        if (HasAliveBosses())
        {
            return;
        }

        ShowVictory();
    }

    private void OnPlayerDied()
    {
        _isPlayerDead = true;
    }

    private bool HasAliveBosses()
    {
        System.Collections.Generic.IReadOnlyList<BossExcavator> bosses = BossExcavator.Instances;

        for (int bossIndex = 0; bossIndex < bosses.Count; bossIndex++)
        {
            BossExcavator boss = bosses[bossIndex];

            if (boss == null)
            {
                continue;
            }

            if (boss.IsDead == false)
            {
                return true;
            }
        }

        return false;
    }

    private void ShowVictory()
    {
        ValidateReference(_blurOverlay, nameof(_blurOverlay));
        ValidateReference(_victoryMenuView, nameof(_victoryMenuView));
        ValidateReference(_restartButton, nameof(_restartButton));
        ValidateReference(_mainMenuButton, nameof(_mainMenuButton));

        _isVictoryShown = true;

        _heldMode.SetHeld(true);

        _timeScale.Animate(
            _timeScaleSettings.PausedTimeScale,
            _timeScaleSettings.PauseDurationSeconds,
            _timeScaleSettings.PauseEaseCurve,
            true
        );

        _blurOverlay.Show();
        _victoryMenuView.Show();
    }

    private void OnRestartClicked()
    {
        StartTransition(true);
    }

    private void OnMainMenuClicked()
    {
        StartTransition(false);
    }

    private void StartTransition(bool isRestart)
    {
        if (_isVictoryShown == false)
        {
            return;
        }

        if (_transitionCoroutine != null)
        {
            StopCoroutine(_transitionCoroutine);
        }

        _transitionCoroutine = StartCoroutine(TransitionRoutine(isRestart));
    }

    private IEnumerator TransitionRoutine(bool isRestart)
    {
        _victoryMenuView.Hide();

        yield return new WaitForSecondsRealtime(_transitionDelaySeconds);

        _timeScale.ResetToDefault();
        DOTween.KillAll(true);

        if (isRestart)
        {
            SceneLoadingScreen.ReloadCurrentScene();

            yield break;
        }

        SceneLoadingScreen.LoadScene(_mainMenuSceneName);
    }

    private void ValidateReference(Object target, string fieldName)
    {
        if (target == null)
        {
            throw new MissingReferenceException(fieldName);
        }
    }
}
