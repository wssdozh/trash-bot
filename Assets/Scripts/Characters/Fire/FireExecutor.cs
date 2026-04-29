using System;
using UnityEngine;

public abstract class FireExecutor : MonoBehaviour
{
    [Header("Настройки")]
    [SerializeField] private float _fireRatePerSecond = 5f;
    [SerializeField] private LayerMask _targetLayers;
    [SerializeField] private float _maxAimAngleDegrees = 30f;
    [SerializeField] private float _readyAngleDegrees = 8f;

    private FireExecutorPresenter _presenter;
    private bool _isPresenterEnabled;

    private FireModifierState _modifierState;
    private IFireRateProvider _fireRateProvider;
    private IDamageCalculator _damageCalculator;
    private IShotStrategy _shotStrategy;
    private Transform _ignoredRoot;

    private bool _hasStarted;

    private bool _shouldEnablePresenterWhenReady;
    private bool _shouldStartFiringWhenReady;

    private bool _hasBufferedAimPoint;
    private Vector3 _bufferedAimPoint;

    protected abstract Transform Muzzle { get; }

    public event Action ShotPerformed;

    protected abstract IShotStrategy CreateShotStrategy(FireModifierState modifierState);

    private void Awake()
    {
        if (_fireRatePerSecond <= 0f)
        {
            throw new InvalidOperationException(nameof(_fireRatePerSecond));
        }

        if (_maxAimAngleDegrees <= 0f)
        {
            throw new InvalidOperationException(nameof(_maxAimAngleDegrees));
        }

        if (_readyAngleDegrees <= 0f)
        {
            throw new InvalidOperationException(nameof(_readyAngleDegrees));
        }

        _modifierState = new FireModifierState();
        WeaponModifierContext weaponModifierContext = new WeaponModifierContext();
        weaponModifierContext.SetDefaults();
        _modifierState.SetContext(weaponModifierContext);

        _fireRateProvider = new FireRateProvider(_fireRatePerSecond, _modifierState);
        _damageCalculator = new FireDamageCalculator(_modifierState);
    }

    private void Start()
    {
        _hasStarted = true;

        EnsurePresenterCreated();

        if (_shouldEnablePresenterWhenReady)
        {
            _shouldEnablePresenterWhenReady = false;

            EnablePresenterIfNeeded();
        }

        if (_hasBufferedAimPoint)
        {
            _presenter.SetAimPoint(_bufferedAimPoint);
        }

        if (_shouldStartFiringWhenReady)
        {
            _shouldStartFiringWhenReady = false;

            _presenter.StartFiring();
        }
    }

    private void OnEnable()
    {
        _shouldEnablePresenterWhenReady = true;

        if (_presenter == null)
        {
            return;
        }

        EnablePresenterIfNeeded();
    }

    private void OnDisable()
    {
        _shouldEnablePresenterWhenReady = false;
        _shouldStartFiringWhenReady = false;

        if (_presenter == null)
        {
            return;
        }

        if (_isPresenterEnabled == false)
        {
            return;
        }

        _presenter.OnDisable();
        _isPresenterEnabled = false;
    }

    private void Update()
    {
        if (_presenter == null)
        {
            return;
        }

        if (_isPresenterEnabled == false)
        {
            return;
        }

        _presenter.Tick(Time.time);

    }

    public void ApplyModifierContext(WeaponModifierContext context)
    {

        _modifierState.SetContext(context);

    }

    public void SetAimPoint(Vector3 aimPoint)
    {

        _hasBufferedAimPoint = true;
        _bufferedAimPoint = aimPoint;

        if (_presenter == null)
        {
            return;
        }

        _presenter.SetAimPoint(aimPoint);
    }

    public void ClearAimPoint()
    {
        _hasBufferedAimPoint = false;

        if (_presenter == null)
        {
            return;
        }

        _presenter.ClearAimPoint();
    }

    public void StartFiring()
    {
        _shouldStartFiringWhenReady = true;
        _shouldEnablePresenterWhenReady = true;

        if (_presenter == null)
        {
            return;
        }

        if (isActiveAndEnabled == false)
        {
            return;
        }

        _presenter.StartFiring();
    }

    public void StopFiring()
    {
        _shouldStartFiringWhenReady = false;

        if (_presenter == null)
        {
            return;
        }

        _presenter.StopFiring();
    }

    public bool TryStartFiring()
    {
        _shouldStartFiringWhenReady = true;
        _shouldEnablePresenterWhenReady = true;

        EnsurePresenterCreated();

        if (_presenter == null)
        {
            return true;
        }

        if (isActiveAndEnabled == false)
        {
            return true;
        }

        return _presenter.TryStartFiring();
    }

    public bool TryFire()
    {

        if (_presenter == null)
        {
            return false;
        }

        if (isActiveAndEnabled == false)
        {
            return false;
        }

        return _presenter.TryFireOnce(Time.time);

    }

    public bool IsAimReady()
    {

        if (_presenter == null)
        {
            return false;
        }

        if (isActiveAndEnabled == false)
        {
            return false;
        }

        return _presenter.IsAimReady();

    }

    public float GetFireCooldown01()
    {

        if (_presenter == null)
        {
            return 1f;
        }

        return _presenter.GetFireCooldown01(Time.time);

    }

    public void SetTargetLayers(LayerMask targetLayers)
    {
        _targetLayers = targetLayers;

        if (_presenter == null)
        {
            return;
        }

        _presenter.SetTargetLayers(targetLayers);
    }

    public void SetIgnoredRoot(Transform ignoredRoot)
    {
        if (ignoredRoot == null)
        {
            throw new InvalidOperationException(nameof(ignoredRoot));
        }

        _ignoredRoot = ignoredRoot;

        if (_presenter == null)
        {
            return;
        }

        _presenter.SetIgnoredRoot(ignoredRoot);
    }

    private void EnsurePresenterCreated()
    {
        if (_presenter != null)
        {
            return;
        }

        if (_hasStarted == false)
        {
            return;
        }

        _shotStrategy = CreateShotStrategy(_modifierState);

        if (_shotStrategy == null)
        {
            throw new InvalidOperationException(nameof(_shotStrategy));
        }

        Transform ignoredRoot = GetIgnoredRoot();

        _presenter = new FireExecutorPresenter(
            transform,
            ignoredRoot,
            Muzzle,
            _shotStrategy,
            _fireRateProvider,
            _damageCalculator,
            _targetLayers,
            _maxAimAngleDegrees,
            _readyAngleDegrees);

        _presenter.ShotPerformed += InvokeShotPerformed;
    }

    private Transform GetIgnoredRoot()
    {
        if (_ignoredRoot != null)
        {
            return _ignoredRoot;
        }

        Transform rootTransform = transform.root;

        if (rootTransform == null)
        {
            throw new InvalidOperationException(nameof(rootTransform));
        }

        _ignoredRoot = rootTransform;

        return _ignoredRoot;
    }

    private void EnablePresenterIfNeeded()
    {

        if (_presenter == null)
        {
            return;
        }

        if (_isPresenterEnabled)
        {
            return;
        }

        _presenter.OnEnable();
        _isPresenterEnabled = true;
    }

    private void InvokeShotPerformed()
    {
        ShotPerformed?.Invoke();
    }
}
