using System.IO;
using UnityEditor;
using UnityEngine;

namespace LimboPuzzleAR.Editor
{
    public static class TutorialPlaneCaptureGenerator
    {
        private const string OutputPath = "Assets/LimboPuzzleAR/Sprites/Tutorial_Plane.png";
        private const int CaptureSize = 512;

        [MenuItem("LimboPuzzleAR/Capture Tutorial Plane")]
        public static void Capture()
        {
            var camera = Camera.main;
            if (camera == null)
            {
                Debug.LogWarning("Tutorial plane capture skipped. Missing Main Camera in the active scene.");
                return;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(OutputPath));
            var texture = RenderSceneCamera(camera);
            File.WriteAllBytes(OutputPath, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);

            AssetDatabase.Refresh();
            ConfigureSprite(OutputPath);
            Debug.Log($"Captured tutorial plane: {OutputPath}");
        }

        private static Texture2D RenderSceneCamera(Camera camera)
        {
            RenderTexture renderTexture = null;
            var previousActive = RenderTexture.active;
            var previousTargetTexture = camera.targetTexture;
            var previousClearFlags = camera.clearFlags;
            var previousBackgroundColor = camera.backgroundColor;
            var previousAllowHDR = camera.allowHDR;
            var previousAllowMSAA = camera.allowMSAA;

            try
            {
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Color.clear;
                camera.allowHDR = false;
                camera.allowMSAA = false;
                renderTexture = RenderTexture.GetTemporary(CaptureSize, CaptureSize, 24, RenderTextureFormat.ARGB32);
                camera.targetTexture = renderTexture;
                camera.Render();

                RenderTexture.active = renderTexture;
                var readableTexture = new Texture2D(CaptureSize, CaptureSize, TextureFormat.RGBA32, false);
                readableTexture.ReadPixels(new Rect(0, 0, CaptureSize, CaptureSize), 0, 0);
                readableTexture.Apply();
                return readableTexture;
            }
            finally
            {
                camera.targetTexture = previousTargetTexture;
                camera.clearFlags = previousClearFlags;
                camera.backgroundColor = previousBackgroundColor;
                camera.allowHDR = previousAllowHDR;
                camera.allowMSAA = previousAllowMSAA;
                RenderTexture.active = previousActive;

                if (renderTexture != null)
                {
                    RenderTexture.ReleaseTemporary(renderTexture);
                }
            }
        }

        private static void ConfigureSprite(string outputPath)
        {
            if (AssetImporter.GetAtPath(outputPath) is not TextureImporter importer)
            {
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }
    }
}
