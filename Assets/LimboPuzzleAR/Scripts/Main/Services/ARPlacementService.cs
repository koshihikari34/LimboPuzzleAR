using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace LimboPuzzleAR.Main.Services
{
    /// <summary>
    /// AR FoundationのRaycastを使用して、検出済み平面上の設置位置を返す。
    /// </summary>
    public sealed class ARPlacementService : IARPlacementService
    {
        private readonly ARRaycastManager _raycastManager;
        private readonly List<ARRaycastHit> _hits = new();

        public ARPlacementService(ARRaycastManager raycastManager)
        {
            _raycastManager = raycastManager;
        }

        public bool TryGetPlacementPose(Vector2 screenPosition, out Pose pose)
        {
            // 平面の推定範囲外を除外し、ユーザーが確認できる面の内側だけを設置対象にする。
            if (!_raycastManager.Raycast(
                    screenPosition,
                    _hits,
                    TrackableType.PlaneWithinPolygon))
            {
                pose = default;
                return false;
            }

            pose = _hits[0].pose;
            return true;
        }
    }
}
