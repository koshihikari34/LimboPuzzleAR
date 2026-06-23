using LimboPuzzleAR.Main.Models;
using System.Collections.Generic;
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

        private StoneViewModel _viewModel;
        private OniViewModel _oniViewModel;
        private readonly Subject<Rigidbody> _releasedStone = new();
        private readonly List<Rigidbody> _releasedStones = new();
        private Rigidbody _currentStone;

        public Observable<Rigidbody> ReleasedStone => _releasedStone;

        [Inject]
        public void Construct(
            StoneViewModel viewModel,
            OniViewModel oniViewModel)
        {
            _viewModel = viewModel;
            _oniViewModel = oniViewModel;
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
                .Subscribe(_ => ClearReleasedStones())
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
                .Subscribe(_ => ClearAllStonesForAttack())
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

            _currentStone = Instantiate(stonePrefab);
            _currentStone.isKinematic = true;
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

        private void ClearReleasedStones()
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
            // タイムアップ時は、物理落下へ移行させず掴み中の石だけ片付ける。
            var holdingStone = _currentStone;
            _currentStone = null;
            if (holdingStone != null)
            {
                Destroy(holdingStone.gameObject);
            }

            _viewModel.StopInteraction();
        }

        private void ClearAllStonesForAttack()
        {
            // Attack時は掴み中の石も配置済みの石も破壊対象にする。
            if (_currentStone != null)
            {
                Destroy(_currentStone.gameObject);
                _currentStone = null;
            }

            ClearReleasedStones();
        }

        private void ClearAllStonesForRetry()
        {
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
    }
}
