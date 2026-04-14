using System.Collections;
using UnityEngine;

public class Health : Stat
{
    private readonly DeveloperCheatSave _developerCheatSave = new DeveloperCheatSave();

    [SerializeField] private bool _autoRegen = true;
    [SerializeField] private float _regenPerSecond = 2f;
    [SerializeField] private float _regenDelay = 3f;
    [SerializeField, Min(0.02f)] private float _regenTick = 0.1f;

    private Coroutine _regenCoroutine;
    private WaitForSecondsRealtime _regenWait;
    private float _sinceLastDamage;
    private Player _player;

    public bool AutoRegen => _autoRegen;

    public float RegenPerSecond => _regenPerSecond;

    public float RegenDelay => _regenDelay;

    private void Awake()
    {
        _player = GetComponentInParent<Player>();
        _regenWait = new WaitForSecondsRealtime(GetRegenTick());
        RefreshRegenState();
    }

    private void OnEnable()
    {
        RefreshRegenState();
        StartRegenLoop();
    }

    private void OnDisable()
    {
        StopRegenLoop();
    }

    public override void Decrease(float amount)
    {
        if (CanIgnoreDamage())
        {
            return;
        }

        base.Decrease(amount);
        _sinceLastDamage = 0f;
        RefreshRegenState();
    }

    public void SetAutoRegen(bool isRegen)
    {
        _autoRegen = isRegen;
        RefreshRegenState();
    }

    public void SetRegenPerSecond(float regenPerSecond)
    {
        _regenPerSecond = Mathf.Max(0f, regenPerSecond);
        RestartRegenLoop();
    }

    public void SetRegenDelay(float regenDelay)
    {
        _regenDelay = Mathf.Max(0f, regenDelay);
        RestartRegenLoop();
    }

    public void ApplyModifier(float maxValue, bool autoRegen, float regenPerSecond, float regenDelay)
    {
        SetMaxValue(maxValue);

        _autoRegen = autoRegen;
        _regenPerSecond = Mathf.Max(0f, regenPerSecond);
        _regenDelay = Mathf.Max(0f, regenDelay);

        RefreshRegenState();
        RestartRegenLoop();
    }

    private void RefreshRegenState()
    {
        if (_autoRegen == false)
        {
            enabled = false;

            return;
        }

        enabled = _value < _maxValue;
    }

    private IEnumerator RegenLoop()
    {
        while (enabled)
        {
            float regenTick = GetRegenTick();
            _sinceLastDamage += regenTick;

            if (_sinceLastDamage >= _regenDelay)
            {
                Increase(_regenPerSecond * regenTick);

                if (_value >= _maxValue)
                {
                    enabled = false;

                    yield break;
                }
            }

            yield return _regenWait;
        }
    }

    private void StartRegenLoop()
    {
        if (_regenCoroutine != null)
        {
            StopCoroutine(_regenCoroutine);
        }

        if (enabled == false)
        {
            return;
        }

        _regenWait = new WaitForSecondsRealtime(GetRegenTick());
        _regenCoroutine = StartCoroutine(RegenLoop());
    }

    private void StopRegenLoop()
    {
        if (_regenCoroutine == null)
        {
            return;
        }

        StopCoroutine(_regenCoroutine);
        _regenCoroutine = null;
    }

    private float GetRegenTick()
    {
        return Mathf.Max(0.02f, _regenTick);
    }

    private void RestartRegenLoop()
    {
        StopRegenLoop();

        if (enabled == false)
        {
            return;
        }

        StartRegenLoop();
    }

    private bool CanIgnoreDamage()
    {
        if (_player == null)
        {
            return false;
        }

        return _developerCheatSave.LoadInfiniteHealth();
    }
}
