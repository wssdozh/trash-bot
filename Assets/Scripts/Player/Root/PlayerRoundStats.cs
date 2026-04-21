using JunkyardBoss;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerRoundStats : MonoBehaviour
{
    [SerializeField] private CurrencyWallet _currencyWallet;

    private float _durationSeconds;
    private int _defeatedEnemies;
    private int _defeatedBosses;
    private int _collectedCoins;
    private int _previousCoins;

    private void Awake()
    {
        ValidateReference(_currencyWallet, nameof(_currencyWallet));
        _previousCoins = _currencyWallet.Coins;
    }

    private void OnEnable()
    {
        Enemy.AnyDied += OnEnemyDied;
        Turret.AnyDied += OnTurretDied;
        BossExcavator.AnyDied += OnBossDied;
        _currencyWallet.CoinsChanged += OnCoinsChanged;
    }

    private void OnDisable()
    {
        Enemy.AnyDied -= OnEnemyDied;
        Turret.AnyDied -= OnTurretDied;
        BossExcavator.AnyDied -= OnBossDied;
        _currencyWallet.CoinsChanged -= OnCoinsChanged;
    }

    private void Update()
    {
        _durationSeconds += Time.deltaTime;
    }

    public PlayerRoundStatsSnapshot CreateSnapshot()
    {
        return new PlayerRoundStatsSnapshot(
            _durationSeconds,
            _defeatedEnemies,
            _defeatedBosses,
            _collectedCoins);
    }

    private void OnEnemyDied(Enemy enemy)
    {
        if (enemy == null)
        {
            return;
        }

        _defeatedEnemies += 1;
    }

    private void OnTurretDied(Turret turret)
    {
        if (turret == null)
        {
            return;
        }

        _defeatedEnemies += 1;
    }

    private void OnBossDied(BossExcavator boss)
    {
        if (boss == null)
        {
            return;
        }

        _defeatedBosses += 1;
    }

    private void OnCoinsChanged(int coins)
    {
        if (coins > _previousCoins)
        {
            _collectedCoins += coins - _previousCoins;
        }

        _previousCoins = coins;
    }

    private void ValidateReference(Object target, string fieldName)
    {
        if (target == null)
        {
            throw new MissingReferenceException(fieldName);
        }
    }
}
