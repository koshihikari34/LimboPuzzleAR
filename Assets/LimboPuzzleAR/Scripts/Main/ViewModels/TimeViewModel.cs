using LimboPuzzleAR.Main.Models;
using R3;

namespace LimboPuzzleAR.Main.ViewModels
{
    /// <summary>
    /// 残り時間をViewへ公開する。
    /// </summary>
    public sealed class TimeViewModel
    {
        private readonly TimeModel _timeModel;

        public TimeViewModel(TimeModel timeModel)
        {
            _timeModel = timeModel;
        }

        public ReadOnlyReactiveProperty<float> RemainingSeconds => _timeModel.RemainingSeconds;

        public ReadOnlyReactiveProperty<bool> IsTimeUp => _timeModel.IsTimeUp;

        public void Tick(float deltaSeconds)
        {
            _timeModel.Tick(deltaSeconds);
        }
    }
}
