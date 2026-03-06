using System.Collections;
using UnityEngine;

public class Turret : MonoBehaviour
{
    private const string LogPrefix = "[TurretLife]";

    [Header("Зависимости")]
    [SerializeField] private TargetVision _targetVision;
    [SerializeField] private TargetRotator _targetRotator;
    [SerializeField] private IdleRotator _idleRotator;
    [SerializeField] private FireExecutor _fireExecutor;

    [Header("Настройки")]
    [SerializeField] private float _fireDelaySeconds = 0.25f;

    private Health _health;
    private Coroutine _fireDelayCoroutine;

    private void Awake()
    {
        _health = GetComponent<Health>();

        if (_health == null)
        {
            throw new MissingReferenceException(nameof(_health));
        }
    }

    private void OnEnable()
    {
        _health.Ended += OnHealthEnded;
        _targetVision.TargetDetected += OnTargetFound;
        _targetVision.TargetCleared += OnTargetLost;
    }

    private void OnDisable()
    {
        LogLifeEvent(nameof(OnDisable));

        _health.Ended -= OnHealthEnded;
        _targetVision.TargetDetected -= OnTargetFound;
        _targetVision.TargetCleared -= OnTargetLost;

        StopFireDelay();
    }

    private void OnDestroy()
    {
        LogLifeEvent(nameof(OnDestroy));
    }

    private void Start()
    {
        SetIdleState();
    }

    private void Update()
    {
        if (_targetRotator.enabled == false)
        {
            _fireExecutor.ClearAimPoint();

            return;
        }

        if (_targetVision.IsTargetVisible == false)
        {
            _fireExecutor.ClearAimPoint();

            return;
        }

        Transform currentTarget = _targetVision.CurrentTarget;

        if (currentTarget == null)
        {
            _fireExecutor.ClearAimPoint();

            return;
        }

        _fireExecutor.SetAimPoint(currentTarget.position);
    }

    private void OnTargetFound()
    {
        SetTrackingState();
    }

    private void OnHealthEnded()
    {
        LogLifeEvent(nameof(OnHealthEnded));
    }

    private void OnTargetLost()
    {
        SetIdleState();
    }

    private void SetTrackingState()
    {
        _idleRotator.enabled = false;
        _targetRotator.enabled = true;

        _fireExecutor.StopFiring();

        StopFireDelay();

        _fireDelayCoroutine = StartCoroutine(FireWithDelayCoroutine());
    }

    private void SetIdleState()
    {
        StopFireDelay();

        _fireExecutor.StopFiring();
        _fireExecutor.ClearAimPoint();

        _targetRotator.enabled = false;
        _idleRotator.ResetBaseRotation();
        _idleRotator.enabled = true;
    }

    private void StopFireDelay()
    {
        if (_fireDelayCoroutine == null)
        {
            return;
        }

        StopCoroutine(_fireDelayCoroutine);
        _fireDelayCoroutine = null;
    }

    private IEnumerator FireWithDelayCoroutine()
    {
        if (_fireDelaySeconds > 0f)
        {
            yield return new WaitForSeconds(_fireDelaySeconds);
        }

        _fireDelayCoroutine = null;
        _fireExecutor.StartFiring();
    }

    private void LogLifeEvent(string eventName)
    {
        string healthText = "null";

        if (_health != null)
        {
            healthText = _health.Value + "/" + _health.MaxValue;
        }

        string message =
            LogPrefix +
            " " +
            name +
            " event:" +
            eventName +
            " frame:" +
            Time.frameCount +
            " scene:" +
            gameObject.scene.name +
            " activeSelf:" +
            gameObject.activeSelf +
            " activeInHierarchy:" +
            gameObject.activeInHierarchy +
            " enabled:" +
            enabled +
            " health:" +
            healthText +
            " position:" +
            transform.position +
            "\n" +
            StackTraceUtility.ExtractStackTrace();

        Debug.LogWarning(message, this);
    }
}
