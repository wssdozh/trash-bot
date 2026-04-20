using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public sealed class SceneLoadingScreen : MonoBehaviour
{
    private const string PrefabResourcePath = "Prefabs/UI/SceneLoadingScreen";

    private static SceneLoadingScreen s_instance;

    [SerializeField] private BlurOverlay _blurOverlay;
    [SerializeField] private Canvas _canvas;
    [SerializeField] private CanvasGroup _panelCanvasGroup;
    [SerializeField] private RectTransform _panelTransform;
    [SerializeField] private float _enterDelaySeconds = 0.08f;
    [SerializeField] private float _panelFadeDurationSeconds = 0.12f;
    [SerializeField] private float _panelShowDurationSeconds = 0.28f;
    [SerializeField] private float _panelHideDurationSeconds = 0.18f;
    [SerializeField] private float _panelHiddenOffsetY = 180.0f;
    [SerializeField] private float _panelHiddenScale = 0.88f;
    [SerializeField] private float _panelShownScale = 1.0f;
    [SerializeField] private float _postLoadDelaySeconds = 0.08f;
    [SerializeField] private float _blurHideWaitSeconds = 0.30f;

    private Sequence _sequence;
    private Coroutine _loadCoroutine;
    private Vector2 _shownAnchoredPosition;
    private bool _isLoading;

    private void Awake()
    {
        if (s_instance != null && s_instance != this)
        {
            Destroy(gameObject);
            return;
        }

        s_instance = this;

        ValidateReference(_blurOverlay, nameof(_blurOverlay));
        ValidateReference(_canvas, nameof(_canvas));
        ValidateReference(_panelCanvasGroup, nameof(_panelCanvasGroup));
        ValidateReference(_panelTransform, nameof(_panelTransform));

        _shownAnchoredPosition = _panelTransform.anchoredPosition;

        DontDestroyOnLoad(gameObject);
        ApplyHiddenState();
    }

    private void OnDisable()
    {
        KillSequence();
    }

    private void OnDestroy()
    {
        if (s_instance == this)
        {
            s_instance = null;
        }
    }

    public static void LoadScene(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
            throw new MissingReferenceException(nameof(sceneName));

        SceneLoadingScreen instance = EnsureInstance();
        instance.StartLoading(sceneName);
    }

    public static void ReloadCurrentScene()
    {
        Scene currentScene = SceneManager.GetActiveScene();

        if (currentScene.IsValid() == false)
            throw new MissingReferenceException(nameof(currentScene));

        LoadScene(currentScene.name);
    }

    private static SceneLoadingScreen EnsureInstance()
    {
        if (s_instance != null)
            return s_instance;

        GameObject prefab = Resources.Load<GameObject>(PrefabResourcePath);

        if (prefab == null)
            throw new MissingReferenceException(nameof(prefab));

        GameObject instanceObject = Instantiate(prefab);
        SceneLoadingScreen instance = instanceObject.GetComponent<SceneLoadingScreen>();

        if (instance == null)
            throw new MissingComponentException(nameof(SceneLoadingScreen));

        return instance;
    }

    private void StartLoading(string sceneName)
    {
        if (_isLoading)
            return;

        if (_loadCoroutine != null)
        {
            StopCoroutine(_loadCoroutine);
        }

        _loadCoroutine = StartCoroutine(LoadSceneRoutine(sceneName));
    }

    private IEnumerator LoadSceneRoutine(string sceneName)
    {
        _isLoading = true;

        yield return PlayShowRoutine();

        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(sceneName);

        if (loadOperation == null)
            throw new MissingReferenceException(nameof(sceneName));

        while (loadOperation.isDone == false)
        {
            yield return null;
        }

        if (_postLoadDelaySeconds > 0.0f)
        {
            yield return new WaitForSecondsRealtime(_postLoadDelaySeconds);
        }

        yield return PlayHideRoutine();

        _isLoading = false;
        _loadCoroutine = null;
        Destroy(gameObject);
    }

    private IEnumerator PlayShowRoutine()
    {
        _canvas.enabled = true;
        _blurOverlay.Show();

        KillSequence();

        _panelCanvasGroup.alpha = 0.0f;
        _panelCanvasGroup.blocksRaycasts = false;
        _panelCanvasGroup.interactable = false;
        _panelTransform.anchoredPosition = GetHiddenAnchoredPosition();
        _panelTransform.localScale = new Vector3(_panelHiddenScale, _panelHiddenScale, 1.0f);

        _sequence = DOTween.Sequence()
            .SetUpdate(true)
            .AppendInterval(_enterDelaySeconds)
            .Append(_panelCanvasGroup.DOFade(1.0f, _panelFadeDurationSeconds).SetEase(Ease.OutCubic))
            .Join(_panelTransform.DOAnchorPos(_shownAnchoredPosition, _panelShowDurationSeconds).SetEase(Ease.OutCubic))
            .Join(_panelTransform.DOScale(_panelShownScale, _panelShowDurationSeconds).SetEase(Ease.OutBack))
            .OnComplete(ApplyShownState);

        yield return _sequence.WaitForCompletion();
    }

    private IEnumerator PlayHideRoutine()
    {
        KillSequence();

        _panelCanvasGroup.blocksRaycasts = false;
        _panelCanvasGroup.interactable = false;

        _sequence = DOTween.Sequence()
            .SetUpdate(true)
            .Append(_panelCanvasGroup.DOFade(0.0f, _panelFadeDurationSeconds).SetEase(Ease.InCubic))
            .Join(_panelTransform.DOAnchorPos(GetHiddenAnchoredPosition(), _panelHideDurationSeconds).SetEase(Ease.InCubic))
            .Join(_panelTransform.DOScale(_panelHiddenScale, _panelHideDurationSeconds).SetEase(Ease.InCubic))
            .OnComplete(ApplyHiddenPanelState);

        yield return _sequence.WaitForCompletion();

        _blurOverlay.Hide();

        if (_blurHideWaitSeconds > 0.0f)
        {
            yield return new WaitForSecondsRealtime(_blurHideWaitSeconds);
        }

        ApplyHiddenState();
    }

    private Vector2 GetHiddenAnchoredPosition()
    {
        return _shownAnchoredPosition + new Vector2(0.0f, _panelHiddenOffsetY);
    }

    private void ApplyShownState()
    {
        _panelCanvasGroup.alpha = 1.0f;
        _panelTransform.anchoredPosition = _shownAnchoredPosition;
        _panelTransform.localScale = new Vector3(_panelShownScale, _panelShownScale, 1.0f);
    }

    private void ApplyHiddenPanelState()
    {
        _panelCanvasGroup.alpha = 0.0f;
        _panelCanvasGroup.blocksRaycasts = false;
        _panelCanvasGroup.interactable = false;
        _panelTransform.anchoredPosition = GetHiddenAnchoredPosition();
        _panelTransform.localScale = new Vector3(_panelHiddenScale, _panelHiddenScale, 1.0f);
    }

    private void ApplyHiddenState()
    {
        _blurOverlay.HideImmediate();
        ApplyHiddenPanelState();
        _canvas.enabled = false;
    }

    private void KillSequence()
    {
        if (_sequence != null && _sequence.IsActive())
        {
            _sequence.Kill(false);
        }

        _sequence = null;
    }

    private void ValidateReference(Object target, string fieldName)
    {
        if (target == null)
            throw new MissingReferenceException(fieldName);
    }
}
