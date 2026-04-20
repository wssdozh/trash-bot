using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public sealed class SceneLoadingScreen : MonoBehaviour
{
    private const string PrefabResourcePath = "Prefabs/UI/SceneLoadingScreen";

    private static SceneLoadingScreen s_instance;

    [SerializeField] private BlurOverlay _blurOverlay;
    [SerializeField] private DungeonSchematicView _schematicView;
    [SerializeField] private Canvas _canvas;
    [SerializeField] private CanvasGroup _panelCanvasGroup;
    [SerializeField] private RectTransform _panelTransform;
    [SerializeField] private TMP_Text _subtitleText;
    [SerializeField] private float _enterDelaySeconds = 0.08f;
    [SerializeField] private float _panelFadeDurationSeconds = 0.12f;
    [SerializeField] private float _panelShowDurationSeconds = 0.28f;
    [SerializeField] private float _panelHideDurationSeconds = 0.18f;
    [SerializeField] private float _panelHiddenOffsetY = 180.0f;
    [SerializeField] private float _panelHiddenScale = 0.88f;
    [SerializeField] private float _panelShownScale = 1.0f;
    [SerializeField] private float _subtitleDotIntervalSeconds = 0.18f;
    [SerializeField] private float _postLoadDelaySeconds = 0.08f;
    [SerializeField] private float _blurHideWaitSeconds = 0.30f;

    private readonly List<Vector2Int> _sceneRoomCells = new List<Vector2Int>(256);
    private readonly List<Vector2Int> _sceneCorridorCells = new List<Vector2Int>(256);
    private Sequence _sequence;
    private Coroutine _loadCoroutine;
    private Coroutine _subtitleCoroutine;
    private Tween _panelPulseTween;
    private Vector2 _shownAnchoredPosition;
    private string _defaultSubtitleText;
    private string _subtitleBaseText;
    private bool _isLoading;
    private bool _sceneLayoutApplied;

    private void Awake()
    {
        if (s_instance != null && s_instance != this)
        {
            Destroy(gameObject);
            return;
        }

        s_instance = this;

        ValidateReference(_blurOverlay, nameof(_blurOverlay));
        ResolveSchematicView();
        ValidateReference(_schematicView, nameof(_schematicView));
        ValidateReference(_canvas, nameof(_canvas));
        ValidateReference(_panelCanvasGroup, nameof(_panelCanvasGroup));
        ValidateReference(_panelTransform, nameof(_panelTransform));
        ValidateReference(_subtitleText, nameof(_subtitleText));

        _shownAnchoredPosition = _panelTransform.anchoredPosition;
        _defaultSubtitleText = _subtitleText.text;
        _subtitleBaseText = _subtitleText.text;

        DontDestroyOnLoad(gameObject);
        ApplyHiddenState();
    }

    private void OnDisable()
    {
        KillSequence();
        StopLoadingVisuals();
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

        yield return WaitForSceneReadyRoutine();

        yield return PlayHideRoutine();

        _isLoading = false;
        _loadCoroutine = null;
        Destroy(gameObject);
    }

    private IEnumerator PlayShowRoutine()
    {
        _canvas.enabled = true;
        _blurOverlay.ShowImmediate();

        KillSequence();
        StopLoadingVisuals();

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
            .OnComplete(() =>
            {
                ApplyShownState();
                StartLoadingVisuals();
            });

        yield return _sequence.WaitForCompletion();
    }

    private IEnumerator PlayHideRoutine()
    {
        KillSequence();
        StopLoadingVisuals();

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
        StopLoadingVisuals();
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

    private void StartLoadingVisuals()
    {
        _sceneLayoutApplied = false;
        _schematicView.PlayLoadingAnimation();

        if (_subtitleCoroutine != null)
        {
            StopCoroutine(_subtitleCoroutine);
        }

        _subtitleCoroutine = StartCoroutine(AnimateSubtitleRoutine());

        KillPanelPulseTween();
        _panelTransform.localScale = new Vector3(_panelShownScale, _panelShownScale, 1.0f);
    }

    private void StopLoadingVisuals()
    {
        _sceneLayoutApplied = false;

        if (_schematicView != null)
        {
            _schematicView.StopAnimation();
        }

        if (_subtitleCoroutine != null)
        {
            StopCoroutine(_subtitleCoroutine);
            _subtitleCoroutine = null;
        }

        if (_subtitleText != null)
        {
            _subtitleBaseText = _defaultSubtitleText;
            _subtitleText.text = _defaultSubtitleText;
        }

        KillPanelPulseTween();
    }

    private void KillPanelPulseTween()
    {
        if (_panelPulseTween != null && _panelPulseTween.IsActive())
        {
            _panelPulseTween.Kill(false);
        }

        _panelPulseTween = null;
    }

    private IEnumerator WaitForSceneReadyRoutine()
    {
        yield return null;

        TryApplySceneLayout();

        if (HasPendingSceneWork() == false)
        {
            yield break;
        }

        SetSubtitleBaseText("Генерируем уровень");

        while (HasPendingSceneWork())
        {
            TryApplySceneLayout();
            yield return null;
        }

        TryApplySceneLayout();
        SetSubtitleBaseText(_defaultSubtitleText);
    }

    private bool HasPendingSceneWork()
    {
        Scene activeScene = SceneManager.GetActiveScene();

        if (activeScene.IsValid() == false)
        {
            return false;
        }

        GameObject[] rootObjects = activeScene.GetRootGameObjects();

        for (int rootIndex = 0; rootIndex < rootObjects.Length; rootIndex++)
        {
            GameObject rootObject = rootObjects[rootIndex];
            LevelGenerator[] levelGenerators = rootObject.GetComponentsInChildren<LevelGenerator>(true);

            for (int generatorIndex = 0; generatorIndex < levelGenerators.Length; generatorIndex++)
            {
                LevelGenerator levelGenerator = levelGenerators[generatorIndex];

                if (levelGenerator == null)
                {
                    continue;
                }

                if (levelGenerator.IsGenerating)
                {
                    return true;
                }
            }
        }

        for (int rootIndex = 0; rootIndex < rootObjects.Length; rootIndex++)
        {
            GameObject rootObject = rootObjects[rootIndex];
            LevelRuntimeNavMesh[] navMeshes = rootObject.GetComponentsInChildren<LevelRuntimeNavMesh>(true);

            for (int navMeshIndex = 0; navMeshIndex < navMeshes.Length; navMeshIndex++)
            {
                LevelRuntimeNavMesh navMesh = navMeshes[navMeshIndex];

                if (navMesh == null)
                {
                    continue;
                }

                if (navMesh.IsBusy)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private void SetSubtitleBaseText(string subtitleText)
    {
        _subtitleBaseText = subtitleText;

        if (_subtitleText == null)
        {
            return;
        }

        _subtitleText.text = subtitleText;
    }

    private void ResolveSchematicView()
    {
        if (_schematicView != null)
        {
            return;
        }

        _schematicView = _blurOverlay.GetComponent<DungeonSchematicView>();

        if (_schematicView == null)
        {
            _schematicView = _blurOverlay.gameObject.AddComponent<DungeonSchematicView>();
        }
    }

    private bool TryApplySceneLayout()
    {
        if (_sceneLayoutApplied)
        {
            return true;
        }

        LevelGenerator levelGenerator = FindActiveLevelGenerator();

        if (levelGenerator == null)
        {
            return false;
        }

        Vector2Int startCell;
        Vector2Int exitCell;

        if (levelGenerator.TryGetSchematicLayout(
            _schematicView.GridWidth,
            _schematicView.GridHeight,
            _sceneRoomCells,
            _sceneCorridorCells,
            out startCell,
            out exitCell
        ) == false)
        {
            return false;
        }

        _schematicView.ShowLayout(_sceneRoomCells, _sceneCorridorCells, startCell, exitCell);
        _sceneLayoutApplied = true;
        return true;
    }

    private LevelGenerator FindActiveLevelGenerator()
    {
        Scene activeScene = SceneManager.GetActiveScene();

        if (activeScene.IsValid() == false)
        {
            return null;
        }

        GameObject[] rootObjects = activeScene.GetRootGameObjects();

        for (int rootIndex = 0; rootIndex < rootObjects.Length; rootIndex++)
        {
            GameObject rootObject = rootObjects[rootIndex];
            LevelGenerator[] levelGenerators = rootObject.GetComponentsInChildren<LevelGenerator>(true);

            for (int generatorIndex = 0; generatorIndex < levelGenerators.Length; generatorIndex++)
            {
                LevelGenerator levelGenerator = levelGenerators[generatorIndex];

                if (levelGenerator == null)
                {
                    continue;
                }

                return levelGenerator;
            }
        }

        return null;
    }

    private IEnumerator AnimateSubtitleRoutine()
    {
        float timer = 0.0f;
        int dotCount = 0;

        while (true)
        {
            timer += Time.unscaledDeltaTime;

            if (timer >= _subtitleDotIntervalSeconds)
            {
                timer = 0.0f;
                dotCount += 1;

                if (dotCount > 3)
                {
                    dotCount = 0;
                }

                _subtitleText.text = _subtitleBaseText + new string('.', dotCount);
            }

            yield return null;
        }
    }

    private void ValidateReference(Object target, string fieldName)
    {
        if (target == null)
        {
            throw new MissingReferenceException(fieldName);
        }
    }
}
