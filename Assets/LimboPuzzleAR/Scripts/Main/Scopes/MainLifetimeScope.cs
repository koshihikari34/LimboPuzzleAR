using LimboPuzzleAR.Main.Models;
using LimboPuzzleAR.Main.Services;
using LimboPuzzleAR.Main.ViewModels;
using LimboPuzzleAR.Common.Services;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using VContainer;
using VContainer.Unity;

namespace LimboPuzzleAR.Main.Scopes
{
    /// <summary>
    /// Mainシーン内で使用する依存関係を登録する。
    /// ZenjectにおけるScene単位のInstallerに相当する。
    /// </summary>
    public sealed class MainLifetimeScope : LifetimeScope
    {
        [SerializeField] private Camera mainCamera;
        [SerializeField] private ARRaycastManager raycastManager;
        [SerializeField, Tooltip("PlacementRootからクリアガイドまでの高さ。Scene上のHeightGuideと同じ値にする。")]
        private float clearHeightMeters = 0.3f;
        [SerializeField, Tooltip("AR設置点から積み場中心までの相対位置。左に鬼、右に積み場の構図調整に使う。")]
        private Vector3 stackCenterOffsetMeters = Vector3.zero;
        [SerializeField, Min(1f)] private float gameDurationSeconds = 60f;

#if UNITY_EDITOR
        [SerializeField] private bool useEditorPlacement = true;
#endif

        protected override void Configure(IContainerBuilder builder)
        {
            // Scene上のUnityコンポーネントは生成せず、Inspector参照をそのまま登録する。
            builder.RegisterInstance(mainCamera);
            builder.RegisterInstance(raycastManager);

#if UNITY_EDITOR
            if (useEditorPlacement)
            {
                builder.Register<IARPlacementService>(
                    resolver => new EditorPlacementService(resolver.Resolve<Camera>()),
                    Lifetime.Scoped);
            }
            else
#endif
            {
                builder.Register<IARPlacementService, ARPlacementService>(Lifetime.Scoped);
            }

            builder.Register(_ => new PlacementModel(clearHeightMeters, stackCenterOffsetMeters), Lifetime.Scoped);
            builder.Register<PlacementViewModel>(Lifetime.Scoped);
            builder.Register<StoneModel>(Lifetime.Scoped);
            builder.Register<StoneStackModel>(Lifetime.Scoped);
            builder.Register<ScoreModel>(Lifetime.Scoped);
            builder.Register(_ => new TimeModel(gameDurationSeconds), Lifetime.Scoped);
            builder.Register<OniModel>(Lifetime.Scoped);
            builder.Register<OniStateMachine>(Lifetime.Scoped);
            builder.Register<IScoreRepository, PlayerPrefsScoreRepository>(Lifetime.Scoped);
            builder.Register<IStonePlacementService, StonePlacementService>(Lifetime.Scoped);
            builder.Register<StoneViewModel>(Lifetime.Scoped);
            builder.Register<OniViewModel>(Lifetime.Scoped);
            builder.Register<TimeViewModel>(Lifetime.Scoped);
        }
    }
}
