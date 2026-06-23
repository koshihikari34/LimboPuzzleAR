using UnityEngine;

namespace LimboPuzzleAR.Main.Services
{
    /// <summary>
    /// 画面座標を、プレイエリアを通る2.5D操作面上の石保持位置へ変換する。
    /// </summary>
    public sealed class StonePlacementService : IStonePlacementService
    {
        private readonly Camera _camera;

        public StonePlacementService(Camera camera)
        {
            _camera = camera;
        }

        public bool TryGetHoldPosition(
            Vector2 screenPosition,
            Pose placementPose,
            float minimumY,
            out Vector3 position)
        {
            var ray = _camera.ScreenPointToRay(screenPosition);
            var planeNormal = GetHorizontalCameraForward();
            var operationPlane = new Plane(planeNormal, placementPose.position);

            if (!operationPlane.Raycast(ray, out var distance))
            {
                position = default;
                return false;
            }

            position = ray.GetPoint(distance);

            // 石が積まれた石の下へ潜らないよう、保持中の最低高さをゲームルール側から渡す。
            if (position.y < minimumY)
            {
                position.y = minimumY;
            }

            return true;
        }

        private Vector3 GetHorizontalCameraForward()
        {
            var forward = _camera.transform.forward;
            forward.y = 0f;

            if (forward.sqrMagnitude < 0.0001f)
            {
                return Vector3.forward;
            }

            return forward.normalized;
        }
    }
}
