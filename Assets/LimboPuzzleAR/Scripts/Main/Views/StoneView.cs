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
        [SerializeField, Min(0f)] private float attackClearDelaySeconds = 1.2f;

        private StoneViewModel _viewModel;
        private OniViewModel _oniViewModel;
        private readonly Subject<Rigidbody> _releasedStone = new();
        private readonly Subject<Sprite> _nextStonePreviewSpriteChanged = new();
        private readonly List<Rigidbody> _releasedStones = new();
        private Rigidbody _currentStone;
        private Rigidbody _nextStonePrefab;
        private Sprite _nextStonePreviewSprite;
        private Coroutine _attackCoroutine;

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
            ClearReleasedStoneObjects();
            _viewModel.PrepareNextClearSet();
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
            StopAttackCoroutine();
            // タイムアップ時は、物理落下へ移行させず掴み中の石だけ片付ける。
            var holdingStone = _currentStone;
            _currentStone = null;
            if (holdingStone != null)
            {
                Destroy(holdingStone.gameObject);
            }

            _viewModel.StopInteraction();
        }

        private void BlowAwayStonesForAttack()
        {
            if (_attackCoroutine != null)
            {
                return;
            }

            _attackCoroutine = StartCoroutine(BlowAwayAndClearStonesForAttack());
        }

        private void ClearAllStonesForRetry()
        {
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

        private IEnumerator BlowAwayAndClearStonesForAttack()
        {
            MakeHoldingStoneAttackTarget();

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

        private void StopAttackCoroutine()
        {
            if (_attackCoroutine == null)
            {
                return;
            }

            StopCoroutine(_attackCoroutine);
            _attackCoroutine = null;
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
