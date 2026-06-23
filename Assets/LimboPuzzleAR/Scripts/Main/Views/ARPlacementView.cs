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

        private PlacementViewModel _viewModel;

        [Inject]
        public void Construct(PlacementViewModel viewModel)
        {
            _viewModel = viewModel;
        }

        private void Awake()
        {
            placementRoot.SetActive(false);
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

            var localPosition = heightGuide.localPosition;
            localPosition.y = clearHeightMeters;
            heightGuide.localPosition = localPosition;
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
    }
}
