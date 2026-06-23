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
        private readonly Vector3 _stackCenterOffsetMeters;
        private readonly ReactiveProperty<Pose> _placementPose = new();
        private readonly ReactiveProperty<Pose> _stackPose = new();
        private readonly ReactiveProperty<bool> _isPlaced = new(false);
        private readonly ReactiveProperty<float> _clearHeightY = new();

        public PlacementModel(float clearHeightMeters, Vector3 stackCenterOffsetMeters)
        {
            _clearHeightMeters = clearHeightMeters;
            _stackCenterOffsetMeters = stackCenterOffsetMeters;
        }

        public ReadOnlyReactiveProperty<Pose> PlacementPose => _placementPose;

        public ReadOnlyReactiveProperty<Pose> StackPose => _stackPose;

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
            // 積み場はAR設置点からの相対位置として持ち、鬼や背景を増やしても石の基準が土台とズレないようにする。
            var stackPosition = pose.position + pose.rotation * _stackCenterOffsetMeters;
            _stackPose.Value = new Pose(stackPosition, pose.rotation);
            _clearHeightY.Value = stackPosition.y + _clearHeightMeters;
            _isPlaced.Value = true;
            return true;
        }
    }
}
