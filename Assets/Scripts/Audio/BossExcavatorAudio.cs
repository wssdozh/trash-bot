using System;
using UnityEngine;

namespace JunkyardBoss
{
    [DisallowMultipleComponent]
    public sealed class BossExcavatorAudio : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private BossExcavator _boss;
        [SerializeField] private AudioSource _oneShotSource;
        [SerializeField] private AudioSource _loopSource;

        [Header("Loop")]
        [SerializeField] private AudioClip _engineLoopClip;
        [SerializeField, Min(0f)] private float _engineLoopVolume = 0.18f;

        [Header("Attacks")]
        [SerializeField] private AudioClip _bucketPrepareClip;
        [SerializeField] private AudioClip _bucketImpactClip;
        [SerializeField] private AudioClip _sweepClip;
        [SerializeField] private AudioClip _throwPrepareClip;
        [SerializeField] private AudioClip _throwReleaseClip;
        [SerializeField] private AudioClip _chargePrepareClip;
        [SerializeField] private AudioClip _chargeDashClip;
        [SerializeField, Min(0f)] private float _attackVolumeScale = 0.55f;
        [SerializeField, Min(0f)] private float _impactVolumeScale = 0.65f;

        private void Awake()
        {
            if (_boss == null)
                throw new InvalidOperationException(nameof(_boss));

            if (_oneShotSource == null)
                throw new InvalidOperationException(nameof(_oneShotSource));

            if (_loopSource == null)
                throw new InvalidOperationException(nameof(_loopSource));

            _oneShotSource.playOnAwake = false;
            _oneShotSource.loop = false;
            _loopSource.playOnAwake = false;
            _loopSource.loop = true;
        }

        private void OnEnable()
        {
            _boss.AttackStarted += OnAttackStarted;
            _boss.BucketImpacted += OnBucketImpacted;
            _boss.ScrapThrown += OnScrapThrown;
            _boss.ChargeDashed += OnChargeDashed;
            _boss.Died += OnDied;

            PlayEngineLoop();
        }

        private void OnDisable()
        {
            _boss.AttackStarted -= OnAttackStarted;
            _boss.BucketImpacted -= OnBucketImpacted;
            _boss.ScrapThrown -= OnScrapThrown;
            _boss.ChargeDashed -= OnChargeDashed;
            _boss.Died -= OnDied;

            StopEngineLoop();
        }

        private void OnAttackStarted(BossExcavatorAttack attack)
        {
            if (attack == BossExcavatorAttack.BucketStrike)
            {
                PlayOneShot(_bucketPrepareClip, _attackVolumeScale);

                return;
            }

            if (attack == BossExcavatorAttack.Sweep)
            {
                PlayOneShot(_sweepClip, _attackVolumeScale);

                return;
            }

            if (attack == BossExcavatorAttack.ThrowScrap)
            {
                PlayOneShot(_throwPrepareClip, _attackVolumeScale);

                return;
            }

            if (attack == BossExcavatorAttack.Charge)
                PlayOneShot(_chargePrepareClip, _attackVolumeScale);
        }

        private void OnBucketImpacted()
        {
            PlayOneShot(_bucketImpactClip, _impactVolumeScale);
        }

        private void OnScrapThrown()
        {
            PlayOneShot(_throwReleaseClip, _impactVolumeScale);
        }

        private void OnChargeDashed()
        {
            PlayOneShot(_chargeDashClip, _impactVolumeScale);
        }

        private void OnDied()
        {
            StopEngineLoop();
        }

        private void PlayEngineLoop()
        {
            if (_engineLoopClip == null)
                return;

            if (_boss.IsDead)
                return;

            _loopSource.clip = _engineLoopClip;
            _loopSource.volume = _engineLoopVolume;
            _loopSource.pitch = 1f;
            _loopSource.Play();
        }

        private void StopEngineLoop()
        {
            if (_loopSource.isPlaying == false)
                return;

            _loopSource.Stop();
        }

        private void PlayOneShot(AudioClip clip, float volumeScale)
        {
            if (clip == null)
                return;

            _oneShotSource.pitch = 1f;
            _oneShotSource.PlayOneShot(clip, volumeScale);
        }
    }
}
