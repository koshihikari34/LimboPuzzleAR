using R3;
using UnityEngine;

namespace LimboPuzzleAR.Main.Models
{
    /// <summary>
    /// プレイエリアの設置状態を保持する。
    /// </summary>
    public sealed class PlacementModel
    {
        private readonly float _clearHeightMeters;
        private readonly ReactiveProperty<Pose> _placementPose = new();
        private readonly ReactiveProperty<bool> _isPlaced = new(false);
        private readonly ReactiveProperty<float> _clearHeightY = new();

        public PlacementModel(float clearHeightMeters)
        {
            _clearHeightMeters = clearHeightMeters;
        }

        public ReadOnlyReactiveProperty<Pose> PlacementPose => _placementPose;

        public ReadOnlyReactiveProperty<bool> IsPlaced => _isPlaced;

        public ReadOnlyReactiveProperty<float> ClearHeightY => _clearHeightY;

        public bool TryPlace(Pose pose)
        {
            // 設置後の誤操作でプレイエリアが移動しないよう、最初の設置だけを受け付ける。
            if (_isPlaced.Value)
            {
                return false;
            }

            _placementPose.Value = pose;
            _clearHeightY.Value = pose.position.y + _clearHeightMeters;
            _isPlaced.Value = true;
            return true;
        }
    }
}
