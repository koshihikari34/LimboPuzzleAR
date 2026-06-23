using UnityEngine;

namespace LimboPuzzleAR.Main.Services
{
    /// <summary>
    /// 画面座標を石保持用のWorld座標へ変換する境界。
    /// </summary>
    public interface IStonePlacementService
    {
        bool TryGetHoldPosition(
            Vector2 screenPosition,
            Pose placementPose,
            float minimumY,
            out Vector3 position);
    }
}
