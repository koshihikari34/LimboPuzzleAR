using UnityEditor;
using UnityEngine;

namespace LimboPuzzleAR.Editor
{
    public static class StoneColliderFitter
    {
        private const float ColliderPadding = 1.03f;

        private static readonly string[] StonePrefabPaths =
        {
            "Assets/LimboPuzzleAR/Prefabs/Stones/StoneRockType203.prefab",
            "Assets/LimboPuzzleAR/Prefabs/Stones/StoneRockType401.prefab",
            "Assets/LimboPuzzleAR/Prefabs/Stones/StoneRockType604.prefab",
            "Assets/LimboPuzzleAR/Prefabs/Stones/StoneRockType304.prefab"
        };

        [MenuItem("LimboPuzzleAR/Fit Stone Box Colliders")]
        public static void FitStoneBoxColliders()
        {
            foreach (var prefabPath in StonePrefabPaths)
            {
                var root = PrefabUtility.LoadPrefabContents(prefabPath);
                try
                {
                    FitBoxCollider(root, prefabPath);
                    PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }

            AssetDatabase.Refresh();
            Debug.Log("Fit stone BoxColliders to mesh bounds.");
        }

        private static void FitBoxCollider(GameObject root, string prefabPath)
        {
            var boxCollider = root.GetComponent<BoxCollider>();
            if (boxCollider == null)
            {
                Debug.LogWarning($"Stone collider skipped. Missing BoxCollider: {prefabPath}");
                return;
            }

            if (!TryCalculateMeshBounds(root, boxCollider.transform, out var bounds))
            {
                Debug.LogWarning($"Stone collider skipped. Missing mesh bounds: {prefabPath}");
                return;
            }

            boxCollider.center = bounds.center;
            boxCollider.size = bounds.size * ColliderPadding;
        }

        private static bool TryCalculateMeshBounds(
            GameObject root,
            Transform colliderTransform,
            out Bounds bounds)
        {
            var meshFilters = root.GetComponentsInChildren<MeshFilter>();
            bounds = default;
            var hasBounds = false;

            foreach (var meshFilter in meshFilters)
            {
                var mesh = meshFilter.sharedMesh;
                if (mesh == null)
                {
                    continue;
                }

                var meshBounds = mesh.bounds;
                for (var x = -1; x <= 1; x += 2)
                {
                    for (var y = -1; y <= 1; y += 2)
                    {
                        for (var z = -1; z <= 1; z += 2)
                        {
                            var meshCorner = meshBounds.center + Vector3.Scale(
                                meshBounds.extents,
                                new Vector3(x, y, z));
                            var worldCorner = meshFilter.transform.TransformPoint(meshCorner);
                            var colliderLocalCorner = colliderTransform.InverseTransformPoint(worldCorner);

                            if (!hasBounds)
                            {
                                bounds = new Bounds(colliderLocalCorner, Vector3.zero);
                                hasBounds = true;
                                continue;
                            }

                            bounds.Encapsulate(colliderLocalCorner);
                        }
                    }
                }
            }

            return hasBounds;
        }
    }
}
