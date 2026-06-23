using LimboPuzzleAR.Main.Models;
using LimboPuzzleAR.Main.Services;
using R3;
using UnityEngine;

namespace LimboPuzzleAR.Main.ViewModels
{
    /// <summary>
    /// 石操作入力をModelと座標変換Serviceへ橋渡しし、View用の保持位置を公開する。
    /// </summary>
    public sealed class StoneViewModel
    {
        private const float BaseHoldHeightOffset = 0.05f;
        private const float StableStoneHoldMargin = 0.05f;

        private readonly StoneModel _stoneModel;
        private readonly StoneStackModel _stoneStackModel;
        private readonly ScoreModel _scoreModel;
        private readonly OniModel _oniModel;
        private readonly TimeModel _timeModel;
        private readonly PlacementModel _placementModel;
        private readonly IStonePlacementService _stonePlacementService;
        private readonly ReactiveProperty<Vector3> _holdPosition = new();
        private readonly ReactiveProperty<bool> _hasReachedClearHeight = new(false);

        public StoneViewModel(
            StoneModel stoneModel,
            StoneStackModel stoneStackModel,
            ScoreModel scoreModel,
            OniModel oniModel,
            TimeModel timeModel,
            PlacementModel placementModel,
            IStonePlacementService stonePlacementService)
        {
            _stoneModel = stoneModel;
            _stoneStackModel = stoneStackModel;
            _scoreModel = scoreModel;
            _oniModel = oniModel;
            _timeModel = timeModel;
            _placementModel = placementModel;
            _stonePlacementService = stonePlacementService;
        }

        public ReadOnlyReactiveProperty<bool> CanSpawnStone => _stoneModel.CanSpawnStone;

        public ReadOnlyReactiveProperty<bool> IsHolding => _stoneModel.IsHolding;

        public ReadOnlyReactiveProperty<bool> IsWaitingForStability => _stoneModel.IsWaitingForStability;

        public ReadOnlyReactiveProperty<bool> IsTimeUp => _timeModel.IsTimeUp;

        public ReadOnlyReactiveProperty<float> HighestStableStoneTopY => _stoneStackModel.HighestTopY;

        public ReadOnlyReactiveProperty<bool> HasReachedClearHeight => _hasReachedClearHeight;

        public ReadOnlyReactiveProperty<int> ClearSetCount => _scoreModel.ClearSetCount;

        public ReadOnlyReactiveProperty<Vector3> HoldPosition => _holdPosition;

        public bool TryBeginHold(Vector2 screenPosition)
        {
            // 仕様: タイムアップ後は新しい操作を受け付けない。
            if (_timeModel.IsTimeUp.CurrentValue)
            {
                return false;
            }

            if (_oniModel.CurrentState.CurrentValue == OniState.Attack)
            {
                return false;
            }

            if (!_placementModel.IsPlaced.CurrentValue)
            {
                return false;
            }

            if (!_stonePlacementService.TryGetHoldPosition(
                    screenPosition,
                    _placementModel.PlacementPose.CurrentValue,
                    GetMinimumHoldY(),
                    out var position))
            {
                return false;
            }

            if (!_stoneModel.BeginHold())
            {
                return false;
            }

            _holdPosition.Value = position;
            return true;
        }

        public bool TryMoveHold(Vector2 screenPosition)
        {
            if (_timeModel.IsTimeUp.CurrentValue)
            {
                return false;
            }

            if (_oniModel.CurrentState.CurrentValue == OniState.Attack)
            {
                return false;
            }

            if (!_stoneModel.IsHolding.CurrentValue)
            {
                return false;
            }

            if (!_stonePlacementService.TryGetHoldPosition(
                    screenPosition,
                    _placementModel.PlacementPose.CurrentValue,
                    GetMinimumHoldY(),
                    out var position))
            {
                return false;
            }

            _holdPosition.Value = position;
            return true;
        }

        public bool TryRelease()
        {
            if (_timeModel.IsTimeUp.CurrentValue)
            {
                return false;
            }

            if (!_stoneModel.ReleaseToStabilityWait())
            {
                return false;
            }

            if (_oniModel.CurrentState.CurrentValue == OniState.Watching)
            {
                // 仕様: Watching中は、Releaseで物理落下へ移行した瞬間にアウト。
                _oniModel.SetState(OniState.Attack);
            }

            return true;
        }

        public bool MarkStable()
        {
            return _stoneModel.MarkStable();
        }

        public void PrepareNextClearSet()
        {
            // クリアやAttack後は、次の石積みを新しい高さ判定として扱い、石操作も再開可能にする。
            _stoneModel.ResetInteraction();
            _stoneStackModel.Reset();
            _hasReachedClearHeight.Value = false;
        }

        public void StopInteraction()
        {
            _stoneModel.StopInteraction();
        }

        public void RegisterStableStoneTop(float topY)
        {
            // クリア判定は、安定した石の一番高い点がガイド高さへ届いたかで見る。
            _stoneStackModel.RegisterStableStoneTop(topY);
            if (_hasReachedClearHeight.Value)
            {
                return;
            }

            var hasReachedClearHeight = _stoneStackModel.HasReachedHeight(_placementModel.ClearHeightY.CurrentValue);
            _hasReachedClearHeight.Value = hasReachedClearHeight;
            if (hasReachedClearHeight)
            {
                _scoreModel.AddClearSet();
            }
        }

        private float GetMinimumHoldY()
        {
            var baseMinimumY = _placementModel.PlacementPose.CurrentValue.position.y + BaseHoldHeightOffset;
            var stackMinimumY = _stoneStackModel.HighestTopY.CurrentValue + StableStoneHoldMargin;
            return Mathf.Max(baseMinimumY, stackMinimumY);
        }
    }
}
