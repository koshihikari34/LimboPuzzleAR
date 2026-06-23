using LimboPuzzleAR.Common.Services;
using LimboPuzzleAR.Title.ViewModels;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace LimboPuzzleAR.Title.Scopes
{
    /// <summary>
    /// Titleシーン内で使用する依存関係を登録する。
    /// </summary>
    public sealed class TitleLifetimeScope : LifetimeScope
    {
        [SerializeField] private string mainSceneName = "Main";

        protected override void Configure(IContainerBuilder builder)
        {
            builder.Register<IScoreRepository, PlayerPrefsScoreRepository>(Lifetime.Scoped);
            builder.Register<TitleViewModel>(Lifetime.Scoped)
                .WithParameter(mainSceneName);
        }
    }
}
