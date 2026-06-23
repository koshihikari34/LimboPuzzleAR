using LimboPuzzleAR.Main.ViewModels;
using R3;
using UnityEngine;
using VContainer;

namespace LimboPuzzleAR.Main.Views
{
    /// <summary>
    /// 設置済みPoseをシーン上のプレイエリア表示へ反映する。
    /// </summary>
    public sealed class ARPlacementView : MonoBehaviour
    {
        [SerializeField] private GameObject placementRoot;

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
        }

        private void ApplyPlacement(Pose pose)
        {
            placementRoot.transform.SetPositionAndRotation(pose.position, pose.rotation);
            placementRoot.SetActive(true);
        }
    }
}
