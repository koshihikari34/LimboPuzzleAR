using System.Collections;
using LimboPuzzleAR.Main.ViewModels;
using R3;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using VContainer;

namespace LimboPuzzleAR.Main.Views
{
    /// <summary>
    /// 設置済みPoseをシーン上のプレイエリア表示へ反映する。
    /// </summary>
    public sealed class ARPlacementView : MonoBehaviour
    {
        [SerializeField] private GameObject placementRoot;
        [SerializeField] private Transform heightGuide;
        [SerializeField] private ARPlaneManager planeManager;
        [SerializeField, Min(0f)] private float guideBounceOffsetMeters = 0.025f;
        [SerializeField, Min(0f)] private float guideBounceDurationSeconds = 0.22f;
        [SerializeField] private bool useGeneratedGuideFrame = true;
        [SerializeField, Min(0.01f)] private float guideFrameRadiusXMeters = 0.13f;
        [SerializeField, Min(0.01f)] private float guideFrameRadiusZMeters = 0.09f;
        [SerializeField, Min(8)] private int guideFrameSegments = 48;
        [SerializeField, Min(0.001f)] private float guideFrameLineWidthMeters = 0.006f;
        [SerializeField] private Color guideFrameColor = new(1f, 0.92f, 0.08f, 0.75f);

        private PlacementViewModel _viewModel;
        private Coroutine _guideBounceCoroutine;
        private Transform _generatedGuideFrame;
        private Material _guideFrameMaterial;
        private float _currentGuideHeightMeters;
        private bool _hasAppliedGuideHeight;

        [Inject]
        public void Construct(PlacementViewModel viewModel)
        {
            _viewModel = viewModel;
        }

        private void Awake()
        {
            placementRoot.SetActive(false);
            SetupGeneratedGuideFrame();
        }

        private void Start()
        {
            // Poseの初期値では表示せず、Modelが設置済みになった時だけ反映する。
            _viewModel.IsPlaced
                .Where(isPlaced => isPlaced)
                .Subscribe(_ => ApplyPlacement(_viewModel.PlacementPose.CurrentValue))
                .AddTo(this);

            _viewModel.ClearHeightMeters
                .Subscribe(ApplyGuideHeight)
                .AddTo(this);
        }

        private void OnDestroy()
        {
            if (_guideFrameMaterial == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(_guideFrameMaterial);
            }
            else
            {
                DestroyImmediate(_guideFrameMaterial);
            }
        }

        private void ApplyPlacement(Pose pose)
        {
            placementRoot.transform.SetPositionAndRotation(pose.position, pose.rotation);
            placementRoot.SetActive(true);
            HideDetectedPlanes();
        }

        private void ApplyGuideHeight(float clearHeightMeters)
        {
            if (heightGuide == null)
            {
                return;
            }

            ApplyGuideLocalY(clearHeightMeters);
            _currentGuideHeightMeters = clearHeightMeters;

            if (_hasAppliedGuideHeight && placementRoot.activeInHierarchy)
            {
                PlayGuideBounce();
            }

            _hasAppliedGuideHeight = true;
        }

        private void HideDetectedPlanes()
        {
            if (planeManager == null)
            {
                return;
            }

            // 仕様: プレイエリア確定後はAR平面表示を消し、石積みに集中できる状態へ移行する。
            foreach (var plane in planeManager.trackables)
            {
                plane.gameObject.SetActive(false);
            }

            planeManager.enabled = false;
        }

        private void PlayGuideBounce()
        {
            if (heightGuide == null || guideBounceOffsetMeters <= 0f || guideBounceDurationSeconds <= 0f)
            {
                return;
            }

            if (_guideBounceCoroutine != null)
            {
                StopCoroutine(_guideBounceCoroutine);
            }

            _guideBounceCoroutine = StartCoroutine(AnimateGuideBounce());
        }

        private IEnumerator AnimateGuideBounce()
        {
            var halfDuration = guideBounceDurationSeconds * 0.5f;
            yield return MoveGuideHeight(_currentGuideHeightMeters, _currentGuideHeightMeters + guideBounceOffsetMeters, halfDuration);
            yield return MoveGuideHeight(_currentGuideHeightMeters + guideBounceOffsetMeters, _currentGuideHeightMeters, halfDuration);
            ApplyGuideLocalY(_currentGuideHeightMeters);
            _guideBounceCoroutine = null;
        }

        private IEnumerator MoveGuideHeight(float fromY, float toY, float durationSeconds)
        {
            if (durationSeconds <= 0f)
            {
                ApplyGuideLocalY(toY);
                yield break;
            }

            var elapsedSeconds = 0f;
            while (elapsedSeconds < durationSeconds)
            {
                elapsedSeconds += Time.deltaTime;
                var progress = Mathf.Clamp01(elapsedSeconds / durationSeconds);
                ApplyGuideLocalY(Mathf.Lerp(fromY, toY, Mathf.SmoothStep(0f, 1f, progress)));
                yield return null;
            }

            ApplyGuideLocalY(toY);
        }

        private void ApplyGuideLocalY(float localY)
        {
            if (heightGuide == null)
            {
                return;
            }

            var localPosition = heightGuide.localPosition;
            localPosition.y = localY;
            heightGuide.localPosition = localPosition;

            if (_generatedGuideFrame == null)
            {
                return;
            }

            var frameLocalPosition = _generatedGuideFrame.localPosition;
            frameLocalPosition.y = localY;
            _generatedGuideFrame.localPosition = frameLocalPosition;
        }

        private void SetupGeneratedGuideFrame()
        {
            if (!useGeneratedGuideFrame || heightGuide == null || heightGuide.parent == null)
            {
                return;
            }

            if (heightGuide.TryGetComponent<Renderer>(out var guideRenderer))
            {
                guideRenderer.enabled = false;
            }

            var frameObject = new GameObject("HeightGuideFrame");
            _generatedGuideFrame = frameObject.transform;
            _generatedGuideFrame.SetParent(heightGuide.parent, false);
            _generatedGuideFrame.localPosition = heightGuide.localPosition;
            _generatedGuideFrame.localRotation = Quaternion.identity;
            _generatedGuideFrame.localScale = Vector3.one;

            var lineRenderer = frameObject.AddComponent<LineRenderer>();
            lineRenderer.useWorldSpace = false;
            lineRenderer.loop = true;
            lineRenderer.positionCount = guideFrameSegments;
            lineRenderer.widthMultiplier = guideFrameLineWidthMeters;
            lineRenderer.numCornerVertices = 4;
            lineRenderer.numCapVertices = 4;
            lineRenderer.startColor = guideFrameColor;
            lineRenderer.endColor = guideFrameColor;
            lineRenderer.sharedMaterial = GetGuideFrameMaterial();

            for (var index = 0; index < guideFrameSegments; index++)
            {
                var angle = Mathf.PI * 2f * index / guideFrameSegments;
                var point = new Vector3(
                    Mathf.Cos(angle) * guideFrameRadiusXMeters,
                    0f,
                    Mathf.Sin(angle) * guideFrameRadiusZMeters);
                lineRenderer.SetPosition(index, point);
            }
        }

        private Material GetGuideFrameMaterial()
        {
            if (_guideFrameMaterial != null)
            {
                return _guideFrameMaterial;
            }

            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                ?? Shader.Find("Sprites/Default");
            if (shader == null)
            {
                return null;
            }

            _guideFrameMaterial = new Material(shader)
            {
                name = "HeightGuideFrameMaterialRuntime",
                color = Color.white
            };
            return _guideFrameMaterial;
        }
    }
}
