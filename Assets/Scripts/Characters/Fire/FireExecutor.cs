using System;
using UnityEngine;

public abstract class FireExecutor : MonoBehaviour
{
    [Header("Настройки")]
    [SerializeField] private float _fireRatePerSecond = 5f;
    [SerializeField] private LayerMask _targetLayers;
    [SerializeField] private float _maxAimAngleDegrees = 30f;

    private FireExecutorPresenter _presenter;
    private bool _isPresenterEnabled;

    private FireModifierState _modifierState;
    private IFireRateProvider _fireRateProvider;
    private IDamageCalculator _damageCalculator;
    private IShotStrategy _shotStrategy;

    protected abstract Transform Muzzle { get; }

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

        _modifierState = new FireModifierState();
        _modifierState.SetContext(WeaponModifierContext.CreateDefault());

        _fireRateProvider = new FireRateProvider(_fireRatePerSecond, _modifierState);
        _damageCalculator = new FireDamageCalculator(_modifierState);

        _shotStrategy = CreateShotStrategy(_modifierState);

        if (_shotStrategy == null)
        {
            throw new InvalidOperationException(nameof(_shotStrategy));
        }

        _presenter = new FireExecutorPresenter(
            transform,
            Muzzle,
            _shotStrategy,
            _fireRateProvider,
            _damageCalculator,
            _targetLayers,
            _maxAimAngleDegrees);

    }

    private void OnEnable()
    {

        if (_presenter == null)
        {
            throw new InvalidOperationException(nameof(_presenter));
        }

        if (_isPresenterEnabled == true)
        {
            return;
        }

        _presenter.OnEnable();
        _isPresenterEnabled = true;

    }

    private void OnDisable()
    {

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

        if (_modifierState == null)
        {
            throw new InvalidOperationException(nameof(_modifierState));
        }

        _modifierState.SetContext(context);

    }

    public float GetFireCooldown01()
    {

        if (_presenter == null)
        {
            return 1f;
        }

        return _presenter.GetFireCooldown01(Time.time);

    }

    public void SetAimPoint(Vector3 aimPoint)
    {

        if (_presenter == null)
        {
            throw new InvalidOperationException(nameof(_presenter));
        }

        _presenter.SetAimPoint(aimPoint);

    }

    public void ClearAimPoint()
    {

        if (_presenter == null)
        {
            return;
        }

        _presenter.ClearAimPoint();

    }

    public bool TryStartFiring()
    {

        if (_presenter == null)
        {
            throw new InvalidOperationException(nameof(_presenter));
        }

        return _presenter.TryStartFiring();

    }

    public void StartFiring()
    {

        if (_presenter == null)
        {
            throw new InvalidOperationException(nameof(_presenter));
        }

        _presenter.StartFiring();

    }

    public void StopFiring()
    {

        if (_presenter == null)
        {
            return;
        }

        _presenter.StopFiring();

    }

    public bool TryFire()
    {

        if (_presenter == null)
        {
            throw new InvalidOperationException(nameof(_presenter));
        }

        return _presenter.TryFireOnce(Time.time);

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
}
