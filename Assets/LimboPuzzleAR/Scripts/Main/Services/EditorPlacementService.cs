using UnityEngine;

namespace LimboPuzzleAR.Main.Services
{
    /// <summary>
    /// AR平面を利用できないEditor上で、仮想水平面への設置位置を返す。
    /// </summary>
    public sealed class EditorPlacementService : IARPlacementService
    {
        private readonly Camera _camera;
        private readonly Plane _placementPlane;

        public EditorPlacementService(Camera camera, float placementHeight = 0f)
        {
            _camera = camera;
            _placementPlane = new Plane(Vector3.up, new Vector3(0f, placementHeight, 0f));
        }

        public bool TryGetPlacementPose(Vector2 screenPosition, out Pose pose)
        {
            var ray = _camera.ScreenPointToRay(screenPosition);

            if (!_placementPlane.Raycast(ray, out var distance))
            {
                pose = default;
                return false;
            }

            pose = new Pose(ray.GetPoint(distance), Quaternion.identity);
            return true;
        }
    }
}
