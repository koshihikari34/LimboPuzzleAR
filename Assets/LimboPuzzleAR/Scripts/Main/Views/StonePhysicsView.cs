using LimboPuzzleAR.Main.Models;
using LimboPuzzleAR.Main.ViewModels;
using R3;
using UnityEngine;
using VContainer;

namespace LimboPuzzleAR.Main.Views
{
    /// <summary>
    /// Release後の石の物理状態を監視し、安定したらViewModelへ通知する。
    /// </summary>
    public sealed class StonePhysicsView : MonoBehaviour
    {
        [SerializeField] private StoneView stoneView;
        [SerializeField] private Transform placementRoot;
        [SerializeField] private float stableDurationSeconds = 0.5f;
        [SerializeField] private float stableVelocityThreshold = 0.02f;
        [SerializeField] private float stableAngularVelocityThresholdDegrees = 2f;
        [SerializeField] private float fallDistanceFromPlacement = 0.5f;

        private StoneViewModel _viewModel;
        private OniViewModel _oniViewModel;
        private Rigidbody _targetStone;
        private float _stableElapsedSeconds;

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
            stoneView.ReleasedStone
                .Subscribe(StartWatching)
                .AddTo(this);
        }

        private void FixedUpdate()
        {
            if (_oniViewModel.CurrentState.CurrentValue == OniState.Attack)
            {
                _targetStone = null;
                _stableElapsedSeconds = 0f;
                return;
            }

            if (_targetStone == null)
            {
                return;
            }

            if (IsFallenBelowLimit(_targetStone))
            {
                // 場外に落ちた石は残すと負荷になるため破棄し、次の石を許可する。
                stoneView.DestroyReleasedStone(_targetStone);
                _viewModel.MarkStable();
                _targetStone = null;
                _stableElapsedSeconds = 0f;
                return;
            }

            if (!IsStable(_targetStone))
            {
                _stableElapsedSeconds = 0f;
                return;
            }

            _stableElapsedSeconds += Time.fixedDeltaTime;
            if (_stableElapsedSeconds < stableDurationSeconds)
            {
                return;
            }

            _viewModel.RegisterStableStoneTop(GetStoneTopY(_targetStone));
            _viewModel.MarkStable();
            _targetStone = null;
            _stableElapsedSeconds = 0f;
        }

        private void StartWatching(Rigidbody releasedStone)
        {
            _targetStone = releasedStone;
            _stableElapsedSeconds = 0f;
        }

        private bool IsStable(Rigidbody stone)
        {
            var angularVelocityThreshold = stableAngularVelocityThresholdDegrees * Mathf.Deg2Rad;
            return stone.linearVelocity.sqrMagnitude <= stableVelocityThreshold * stableVelocityThreshold
                && stone.angularVelocity.sqrMagnitude <= angularVelocityThreshold * angularVelocityThreshold;
        }

        private bool IsFallenBelowLimit(Rigidbody stone)
        {
            var baseY = placementRoot != null ? placementRoot.position.y : 0f;
            return stone.position.y < baseY - fallDistanceFromPlacement;
        }

        private float GetStoneTopY(Rigidbody stone)
        {
            // クリア条件は「一番高い位置の石がガイド高さに届く」なので、Collider上端を記録する。
            if (stone.TryGetComponent<Collider>(out var stoneCollider))
            {
                return stoneCollider.bounds.max.y;
            }

            return stone.position.y;
        }
    }
}
