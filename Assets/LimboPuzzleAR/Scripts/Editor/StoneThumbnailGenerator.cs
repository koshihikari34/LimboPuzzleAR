using System.IO;
using UnityEditor;
using UnityEngine;

namespace LimboPuzzleAR.Editor
{
    public static class StoneThumbnailGenerator
    {
        private const string OutputDirectory = "Assets/LimboPuzzleAR/Sprites/Stones";
        private const int PreviewSize = 256;
        private const int ThumbnailLayer = 30;

        private static readonly (string PrefabPath, string OutputName)[] Stones =
        {
            ("Assets/LimboPuzzleAR/Prefabs/Stones/StoneRockType203.prefab", "StoneRockType203.png"),
            ("Assets/LimboPuzzleAR/Prefabs/Stones/StoneRockType401.prefab", "StoneRockType401.png"),
            ("Assets/LimboPuzzleAR/Prefabs/Stones/StoneRockType604.prefab", "StoneRockType604.png"),
            ("Assets/LimboPuzzleAR/Prefabs/Stones/StoneRockType304.prefab", "StoneRockType304.png")
        };

        [MenuItem("LimboPuzzleAR/Generate Stone Thumbnails")]
        public static void Generate()
        {
            Directory.CreateDirectory(OutputDirectory);

            foreach (var stone in Stones)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(stone.PrefabPath);
                if (prefab == null)
                {
                    Debug.LogWarning($"Stone thumbnail skipped. Missing prefab: {stone.PrefabPath}");
                    continue;
                }

                var outputPath = $"{OutputDirectory}/{stone.OutputName}";
                var thumbnail = RenderPrefabThumbnail(prefab);
                File.WriteAllBytes(outputPath, thumbnail.EncodeToPNG());
                Object.DestroyImmediate(thumbnail);
            }

            AssetDatabase.Refresh();
            ConfigureSprites();
            Debug.Log($"Generated stone thumbnails: {OutputDirectory}");
        }

        private static Texture2D RenderPrefabThumbnail(GameObject prefab)
        {
            GameObject instance = null;
            GameObject cameraObject = null;
            GameObject lightObject = null;
            RenderTexture renderTexture = null;
            var previousActive = RenderTexture.active;

            try
            {
                instance = Object.Instantiate(prefab);
                SetHideFlagsRecursively(instance.transform, HideFlags.HideAndDontSave);
                SetLayerRecursively(instance.transform, ThumbnailLayer);
                instance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

                cameraObject = new GameObject("Stone Thumbnail Camera");
                cameraObject.hideFlags = HideFlags.HideAndDontSave;
                var camera = cameraObject.AddComponent<Camera>();
                ConfigurePreviewCamera(camera, CalculateBounds(instance));

                lightObject = new GameObject("Stone Thumbnail Light");
                lightObject.hideFlags = HideFlags.HideAndDontSave;
                var light = lightObject.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1.2f;
                light.transform.rotation = Quaternion.Euler(45f, 35f, 0f);

                renderTexture = RenderTexture.GetTemporary(PreviewSize, PreviewSize, 24, RenderTextureFormat.ARGB32);
                camera.targetTexture = renderTexture;
                camera.Render();

                RenderTexture.active = renderTexture;
                var readableTexture = new Texture2D(PreviewSize, PreviewSize, TextureFormat.RGBA32, false);
                readableTexture.ReadPixels(new Rect(0, 0, PreviewSize, PreviewSize), 0, 0);
                readableTexture.Apply();
                return readableTexture;
            }
            finally
            {
                RenderTexture.active = previousActive;

                if (renderTexture != null)
                {
                    RenderTexture.ReleaseTemporary(renderTexture);
                }

                if (instance != null)
                {
                    Object.DestroyImmediate(instance);
                }

                if (cameraObject != null)
                {
                    Object.DestroyImmediate(cameraObject);
                }

                if (lightObject != null)
                {
                    Object.DestroyImmediate(lightObject);
                }
            }
        }

        private static void ConfigurePreviewCamera(Camera camera, Bounds bounds)
        {
            var rotation = Quaternion.Euler(25f, -35f, 0f);
            var radius = Mathf.Max(bounds.extents.magnitude, 0.1f);
            var distance = radius / Mathf.Sin(camera.fieldOfView * 0.5f * Mathf.Deg2Rad) * 1.2f;

            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.clear;
            camera.cullingMask = 1 << ThumbnailLayer;
            camera.transform.SetPositionAndRotation(bounds.center - rotation * Vector3.forward * distance, rotation);
            camera.nearClipPlane = Mathf.Max(0.01f, distance - radius * 2f);
            camera.farClipPlane = distance + radius * 2f;
        }

        private static Bounds CalculateBounds(GameObject root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                return new Bounds(root.transform.position, Vector3.one);
            }

            var bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            return bounds;
        }

        private static void SetLayerRecursively(Transform root, int layer)
        {
            root.gameObject.layer = layer;
            foreach (Transform child in root)
            {
                SetLayerRecursively(child, layer);
            }
        }

        private static void SetHideFlagsRecursively(Transform root, HideFlags hideFlags)
        {
            root.gameObject.hideFlags = hideFlags;
            foreach (Transform child in root)
            {
                SetHideFlagsRecursively(child, hideFlags);
            }
        }

        private static void ConfigureSprites()
        {
            foreach (var stone in Stones)
            {
                var outputPath = $"{OutputDirectory}/{stone.OutputName}";
                if (AssetImporter.GetAtPath(outputPath) is not TextureImporter importer)
                {
                    continue;
                }

                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
            }
        }

    }
}
