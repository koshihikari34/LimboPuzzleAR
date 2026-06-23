using LimboPuzzleAR.Main.Models;
using LimboPuzzleAR.Main.Services;
using R3;
using UnityEngine;

namespace LimboPuzzleAR.Main.ViewModels
{
    /// <summary>
    /// 設置入力をServiceへ渡し、Viewが購読する設置状態を公開する。
    /// </summary>
    public sealed class PlacementViewModel
    {
        private readonly PlacementModel _model;
        private readonly IARPlacementService _placementService;

        public PlacementViewModel(
            PlacementModel model,
            IARPlacementService placementService)
        {
            _model = model;
            _placementService = placementService;
        }

        public ReadOnlyReactiveProperty<Pose> PlacementPose => _model.PlacementPose;

        public ReadOnlyReactiveProperty<bool> IsPlaced => _model.IsPlaced;

        public ReadOnlyReactiveProperty<float> ClearHeightY => _model.ClearHeightY;

        public bool TryPlace(Vector2 screenPosition)
        {
            if (!_placementService.TryGetPlacementPose(screenPosition, out var pose))
            {
                return false;
            }

            return _model.TryPlace(pose);
        }
    }
}
