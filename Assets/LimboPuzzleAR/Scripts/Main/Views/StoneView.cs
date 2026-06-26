using System.Collections;
using System.Collections.Generic;
using LimboPuzzleAR.Main.Models;
using LimboPuzzleAR.Main.ViewModels;
using R3;
using UnityEngine;
using VContainer;

namespace LimboPuzzleAR.Main.Views
{
    /// <summary>
    /// 石の生成、追従、物理状態の切り替えを担当する。
    /// </summary>
    public sealed class StoneView : MonoBehaviour
    {
        [SerializeField] private Rigidbody stonePrefab;
        [SerializeField] private Rigidbody[] stonePrefabs;
        [SerializeField] private Sprite[] stonePreviewSprites;
        [SerializeField, Min(0f)] private float attackImpulse = 1.8f;
        [SerializeField, Min(0f)] private float attackUpwardImpulse = 0.8f;
        [SerializeField, Min(0f)] private float attackTorqueImpulse = 0.6f;
        [SerializeField, Min(0f)] private float attackImpulseDelaySeconds = 0.25f;
        [SerializeField, Min(0f)] private float attackClearDelaySeconds = 1.2f;
        [SerializeField, Min(0f)] private float clearGlowDurationSeconds = 0.45f;
        [SerializeField] private Color clearGlowColor = new(1f, 0.84f, 0.25f, 1f);
        [SerializeField, Min(0f)] private float clearGlowIntensity = 1.6f;
        [SerializeField] private ParticleSystem clearParticlePrefab;
        [SerializeField, Min(1)] private int clearParticleCount = 18;
        [SerializeField, Min(0.05f)] private float clearParticleDurationSeconds = 0.55f;
        [SerializeField, Min(0f)] private float clearParticleSpeed = 0.18f;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        private StoneViewModel _viewModel;
        private OniViewModel _oniViewModel;
        private readonly Subject<Rigidbody> _releasedStone = new();
        private readonly Subject<Sprite> _nextStonePreviewSpriteChanged = new();
        private readonly List<Rigidbody> _releasedStones = new();
        private MaterialPropertyBlock _clearMaterialPropertyBlock;
        private Material _defaultClearParticleMaterial;
        private Rigidbody _currentStone;
        private Rigidbody _nextStonePrefab;
        private Sprite _nextStonePreviewSprite;
        private Coroutine _attackCoroutine;
        private Coroutine _clearCoroutine;

        public Observable<Rigidbody> ReleasedStone => _releasedStone;

        public Observable<Sprite> NextStonePreviewSpriteChanged => _nextStonePreviewSpriteChanged;

        public Sprite CurrentNextStonePreviewSprite => _nextStonePreviewSprite;

        [Inject]
        public void Construct(
            StoneViewModel viewModel,
            OniViewModel oniViewModel)
        {
            _viewModel = viewModel;
            _oniViewModel = oniViewModel;
        }

        private void Awake()
        {
            SelectNextStone();
        }

        private void Start()
        {
            _viewModel.IsHolding
                .Subscribe(OnHoldingChanged)
                .AddTo(this);

            _viewModel.HoldPosition
                .Subscribe(UpdateHoldingStonePosition)
                .AddTo(this);

            _viewModel.HasReachedClearHeight
                .Where(hasReachedClearHeight => hasReachedClearHeight)
                .Subscribe(_ => ClearReleasedStonesForClear())
                .AddTo(this);

            _viewModel.IsTimeUp
                .Where(isTimeUp => isTimeUp)
                .Subscribe(_ => ClearHoldingStone())
                .AddTo(this);

            _viewModel.RetryRequested
                .Subscribe(_ => ClearAllStonesForRetry())
                .AddTo(this);

            _oniViewModel.CurrentState
                .Where(state => state == OniState.Attack)
                .Subscribe(_ => BlowAwayStonesForAttack())
                .AddTo(this);
        }

        private void OnDestroy()
        {
            if (_defaultClearParticleMaterial == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(_defaultClearParticleMaterial);
            }
            else
            {
                DestroyImmediate(_defaultClearParticleMaterial);
            }
        }

        private void OnHoldingChanged(bool isHolding)
        {
            if (isHolding)
            {
                EnsureHoldingStone();
                return;
            }

            ReleaseCurrentStone();
        }

        private void EnsureHoldingStone()
        {
            if (_currentStone != null)
            {
                return;
            }

            var selectedPrefab = SelectStonePrefab();
            if (selectedPrefab == null)
            {
                return;
            }

            _currentStone = Instantiate(selectedPrefab);
            _currentStone.isKinematic = true;
        }

        private Rigidbody SelectStonePrefab()
        {
            if (stonePrefabs != null && stonePrefabs.Length > 0)
            {
                // 仕様: NextStoneボタンに表示していた石を生成し、次候補を先に抽選する。
                if (_nextStonePrefab == null)
                {
                    SelectNextStone();
                }

                var selectedPrefab = _nextStonePrefab;
                SelectNextStone();
                return selectedPrefab;
            }

            return stonePrefab;
        }

        private void SelectNextStone()
        {
            if (stonePrefabs == null || stonePrefabs.Length == 0)
            {
                _nextStonePrefab = stonePrefab;
                _nextStonePreviewSprite = GetPreviewSprite(0);
                _nextStonePreviewSpriteChanged.OnNext(_nextStonePreviewSprite);
                return;
            }

            var stoneIndex = Random.Range(0, stonePrefabs.Length);
            _nextStonePrefab = stonePrefabs[stoneIndex];
            _nextStonePreviewSprite = GetPreviewSprite(stoneIndex);
            _nextStonePreviewSpriteChanged.OnNext(_nextStonePreviewSprite);
        }

        private Sprite GetPreviewSprite(int stoneIndex)
        {
            if (stonePreviewSprites == null
                || stoneIndex < 0
                || stoneIndex >= stonePreviewSprites.Length)
            {
                return null;
            }

            return stonePreviewSprites[stoneIndex];
        }

        private void UpdateHoldingStonePosition(Vector3 position)
        {
            if (_currentStone == null || !_viewModel.IsHolding.CurrentValue)
            {
                return;
            }

            _currentStone.position = position;
        }

        private void ReleaseCurrentStone()
        {
            if (_currentStone == null)
            {
                return;
            }

            var releasedStone = _currentStone;
            releasedStone.isKinematic = false;
            _currentStone = null;
            _releasedStones.Add(releasedStone);
            _releasedStone.OnNext(releasedStone);
        }

        private void ClearReleasedStonesForClear()
        {
            StopAttackCoroutine();
            if (_clearCoroutine != null)
            {
                return;
            }

            // クリア演出中に次の石を出せないよう、次セット準備まで一時的に入力を止める。
            _viewModel.StopInteraction();
            _clearCoroutine = StartCoroutine(PlayClearSequence());
        }

        public void DestroyReleasedStone(Rigidbody releasedStone)
        {
            if (releasedStone == null)
            {
                return;
            }

            _releasedStones.Remove(releasedStone);
            Destroy(releasedStone.gameObject);
        }

        private void ClearHoldingStone()
        {
            var stoppedClear = StopClearCoroutine();
            StopAttackCoroutine();
            // タイムアップ時は、物理落下へ移行させず掴み中の石だけ片付ける。
            var holdingStone = _currentStone;
            _currentStone = null;
            if (holdingStone != null)
            {
                Destroy(holdingStone.gameObject);
            }

            if (stoppedClear)
            {
                ClearReleasedStoneObjects();
            }

            _viewModel.StopInteraction();
        }

        private void BlowAwayStonesForAttack()
        {
            StopClearCoroutine();
            if (_attackCoroutine != null)
            {
                return;
            }

            _attackCoroutine = StartCoroutine(BlowAwayAndClearStonesForAttack());
        }

        private void ClearAllStonesForRetry()
        {
            StopClearCoroutine();
            StopAttackCoroutine();
            // リトライ時は演出を挟まず、残っている石をすべて片付けて同じ設置場所から再開する。
            if (_currentStone != null)
            {
                Destroy(_currentStone.gameObject);
                _currentStone = null;
            }

            foreach (var releasedStone in _releasedStones)
            {
                if (releasedStone == null)
                {
                    continue;
                }

                Destroy(releasedStone.gameObject);
            }

            _releasedStones.Clear();
        }

        private IEnumerator PlayClearSequence()
        {
            var clearStones = new List<Rigidbody>(_releasedStones);
            LockStonesForClear(clearStones);
            ApplyClearGlow(clearStones);
            PlayClearParticles(clearStones);

            if (clearGlowDurationSeconds > 0f)
            {
                yield return new WaitForSeconds(clearGlowDurationSeconds);
            }

            ClearReleasedStoneObjects();
            _viewModel.PrepareNextClearSet();
            _clearCoroutine = null;
        }

        private IEnumerator BlowAwayAndClearStonesForAttack()
        {
            MakeHoldingStoneAttackTarget();

            if (attackImpulseDelaySeconds > 0f)
            {
                yield return new WaitForSeconds(attackImpulseDelaySeconds);
            }

            // 仕様: アウト時は即消去せず、鬼の攻撃で石が崩れたように見せてから片付ける。
            foreach (var releasedStone in _releasedStones)
            {
                ApplyAttackImpulse(releasedStone);
            }

            if (attackClearDelaySeconds > 0f)
            {
                yield return new WaitForSeconds(attackClearDelaySeconds);
            }

            ClearReleasedStoneObjects();
            _viewModel.ResetAfterAttack();
            _attackCoroutine = null;
        }

        private void MakeHoldingStoneAttackTarget()
        {
            if (_currentStone == null)
            {
                return;
            }

            // 通常はReleaseでAttackへ入るが、時間攻撃や入力キャンセル時の保険として掴み中も物理化する。
            var holdingStone = _currentStone;
            _currentStone = null;
            holdingStone.isKinematic = false;
            _releasedStones.Add(holdingStone);
        }

        private void ApplyAttackImpulse(Rigidbody stone)
        {
            if (stone == null)
            {
                return;
            }

            stone.isKinematic = false;
            var horizontalDirection = stone.position - transform.position;
            horizontalDirection.y = 0f;
            if (horizontalDirection.sqrMagnitude <= Mathf.Epsilon)
            {
                horizontalDirection = transform.forward;
            }

            var force = horizontalDirection.normalized * attackImpulse + Vector3.up * attackUpwardImpulse;
            stone.AddForce(force, ForceMode.Impulse);
            stone.AddTorque(Random.onUnitSphere * attackTorqueImpulse, ForceMode.Impulse);
        }

        private void LockStonesForClear(List<Rigidbody> stones)
        {
            foreach (var stone in stones)
            {
                if (stone == null)
                {
                    continue;
                }

                stone.linearVelocity = Vector3.zero;
                stone.angularVelocity = Vector3.zero;
                stone.isKinematic = true;
            }
        }

        private void ApplyClearGlow(List<Rigidbody> stones)
        {
            _clearMaterialPropertyBlock ??= new MaterialPropertyBlock();

            var baseColor = clearGlowColor;
            var emissionColor = clearGlowColor * clearGlowIntensity;
            emissionColor.a = clearGlowColor.a;

            foreach (var stone in stones)
            {
                if (stone == null)
                {
                    continue;
                }

                foreach (var stoneRenderer in stone.GetComponentsInChildren<Renderer>())
                {
                    if (stoneRenderer == null)
                    {
                        continue;
                    }

                    stoneRenderer.GetPropertyBlock(_clearMaterialPropertyBlock);
                    _clearMaterialPropertyBlock.SetColor(BaseColorId, baseColor);
                    _clearMaterialPropertyBlock.SetColor(ColorId, baseColor);
                    _clearMaterialPropertyBlock.SetColor(EmissionColorId, emissionColor);
                    stoneRenderer.SetPropertyBlock(_clearMaterialPropertyBlock);
                }
            }
        }

        private void PlayClearParticles(List<Rigidbody> stones)
        {
            var clearPosition = GetStoneGroupCenter(stones);
            var clearParticle = clearParticlePrefab != null
                ? Instantiate(clearParticlePrefab, clearPosition, Quaternion.identity)
                : CreateDefaultClearParticle(clearPosition);
            clearParticle.Play();

            var main = clearParticle.main;
            var destroyDelay = main.duration + main.startLifetime.constantMax;
            Destroy(clearParticle.gameObject, destroyDelay);
        }

        private ParticleSystem CreateDefaultClearParticle(Vector3 position)
        {
            var particleObject = new GameObject("ClearSetParticle");
            particleObject.transform.position = position;

            var particle = particleObject.AddComponent<ParticleSystem>();
            particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = particle.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = clearParticleDurationSeconds;
            main.startLifetime = clearParticleDurationSeconds * 0.75f;
            main.startSpeed = clearParticleSpeed;
            main.startSize = new ParticleSystem.MinMaxCurve(0.008f, 0.018f);
            main.startColor = new ParticleSystem.MinMaxGradient(clearGlowColor, Color.white);
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = particle.emission;
            emission.enabled = true;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[]
            {
                new ParticleSystem.Burst(0f, (short)Mathf.Min(clearParticleCount, short.MaxValue))
            });

            var shape = particle.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.035f;

            var colorOverLifetime = particle.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var alphaGradient = new Gradient();
            alphaGradient.SetKeys(
                new[]
                {
                    new GradientColorKey(clearGlowColor, 0f),
                    new GradientColorKey(Color.white, 0.35f),
                    new GradientColorKey(clearGlowColor, 1f)
                },
                new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0.85f, 0.35f),
                    new GradientAlphaKey(0f, 1f)
                });
            colorOverLifetime.color = alphaGradient;

            var particleRenderer = particle.GetComponent<ParticleSystemRenderer>();
            particleRenderer.renderMode = ParticleSystemRenderMode.Billboard;
            particleRenderer.sortingFudge = 1f;
            particleRenderer.sharedMaterial = GetDefaultClearParticleMaterial();

            return particle;
        }

        private Material GetDefaultClearParticleMaterial()
        {
            if (_defaultClearParticleMaterial != null)
            {
                return _defaultClearParticleMaterial;
            }

            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                ?? Shader.Find("Particles/Standard Unlit")
                ?? Shader.Find("Sprites/Default");
            if (shader == null)
            {
                return null;
            }

            _defaultClearParticleMaterial = new Material(shader)
            {
                name = "ClearSetParticleMaterialRuntime",
                color = Color.white
            };
            _defaultClearParticleMaterial.SetColor(BaseColorId, Color.white);
            _defaultClearParticleMaterial.SetColor(ColorId, Color.white);
            _defaultClearParticleMaterial.SetColor(EmissionColorId, Color.white);
            return _defaultClearParticleMaterial;
        }

        private Vector3 GetStoneGroupCenter(List<Rigidbody> stones)
        {
            var center = Vector3.zero;
            var count = 0;
            foreach (var stone in stones)
            {
                if (stone == null)
                {
                    continue;
                }

                center += stone.position;
                count++;
            }

            return count > 0 ? center / count : transform.position;
        }

        private void StopAttackCoroutine()
        {
            if (_attackCoroutine == null)
            {
                return;
            }

            StopCoroutine(_attackCoroutine);
            _attackCoroutine = null;
        }

        private bool StopClearCoroutine()
        {
            if (_clearCoroutine == null)
            {
                return false;
            }

            StopCoroutine(_clearCoroutine);
            _clearCoroutine = null;
            return true;
        }

        private void ClearReleasedStoneObjects()
        {
            foreach (var releasedStone in _releasedStones)
            {
                if (releasedStone == null)
                {
                    continue;
                }

                Destroy(releasedStone.gameObject);
            }

            _releasedStones.Clear();
        }
    }
}
