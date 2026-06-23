using LimboPuzzleAR.Main.Models;
using R3;

namespace LimboPuzzleAR.Main.ViewModels
{
    /// <summary>
    /// ゲーム開始カウントダウンをViewへ公開する。
    /// </summary>
    public sealed class GameStartViewModel
    {
        private readonly GameStartModel _model;

        public GameStartViewModel(GameStartModel model)
        {
            _model = model;
        }

        public ReadOnlyReactiveProperty<bool> IsCountdownVisible => _model.IsCountdownVisible;

        public ReadOnlyReactiveProperty<bool> IsPlaying => _model.IsPlaying;

        public ReadOnlyReactiveProperty<string> CountdownLabel => _model.CountdownLabel;

        public void StartCountdown()
        {
            _model.StartCountdown();
        }

        public void Tick(float deltaSeconds)
        {
            _model.Tick(deltaSeconds);
        }
    }
}
