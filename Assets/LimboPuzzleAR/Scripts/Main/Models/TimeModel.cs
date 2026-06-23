using R3;

namespace LimboPuzzleAR.Main.Models
{
    /// <summary>
    /// Mainゲーム中の残り時間を保持する。
    /// </summary>
    public sealed class TimeModel
    {
        private readonly float _initialSeconds;
        private readonly ReactiveProperty<float> _remainingSeconds;
        private readonly ReactiveProperty<bool> _isTimeUp;

        public TimeModel(float initialSeconds)
        {
            _initialSeconds = initialSeconds;
            _remainingSeconds = new ReactiveProperty<float>(initialSeconds);
            _isTimeUp = new ReactiveProperty<bool>(initialSeconds <= 0f);
        }

        public ReadOnlyReactiveProperty<float> RemainingSeconds => _remainingSeconds;

        public ReadOnlyReactiveProperty<bool> IsTimeUp => _isTimeUp;

        public void Tick(float deltaSeconds)
        {
            if (_remainingSeconds.Value <= 0f)
            {
                return;
            }

            var nextSeconds = _remainingSeconds.Value - deltaSeconds;
            _remainingSeconds.Value = nextSeconds <= 0f ? 0f : nextSeconds;
            if (_remainingSeconds.Value <= 0f)
            {
                _isTimeUp.Value = true;
            }
        }

        public void Reset()
        {
            _remainingSeconds.Value = _initialSeconds;
            _isTimeUp.Value = _initialSeconds <= 0f;
        }
    }
}
