using R3;

namespace LimboPuzzleAR.Main.Models
{
    /// <summary>
    /// プレイ開始前のカウントダウン状態を保持する。
    /// </summary>
    public sealed class GameStartModel
    {
        private readonly float _countdownSeconds;
        private readonly float _startLabelSeconds;
        private readonly ReactiveProperty<bool> _isCountdownVisible = new(false);
        private readonly ReactiveProperty<bool> _isPlaying = new(false);
        private readonly ReactiveProperty<string> _countdownLabel = new(string.Empty);
        private float _remainingCountdownSeconds;
        private bool _hasStartedCountdown;

        public GameStartModel(float countdownSeconds, float startLabelSeconds)
        {
            _countdownSeconds = countdownSeconds;
            _startLabelSeconds = startLabelSeconds;
        }

        public ReadOnlyReactiveProperty<bool> IsCountdownVisible => _isCountdownVisible;

        public ReadOnlyReactiveProperty<bool> IsPlaying => _isPlaying;

        public ReadOnlyReactiveProperty<string> CountdownLabel => _countdownLabel;

        public void StartCountdown()
        {
            if (_hasStartedCountdown || _isPlaying.Value)
            {
                return;
            }

            _hasStartedCountdown = true;
            _remainingCountdownSeconds = _countdownSeconds + _startLabelSeconds;
            _isCountdownVisible.Value = true;
            ApplyCountdownLabel();
        }

        public void Tick(float deltaSeconds)
        {
            if (!_hasStartedCountdown || _isPlaying.Value)
            {
                return;
            }

            _remainingCountdownSeconds -= deltaSeconds;
            if (_remainingCountdownSeconds <= 0f)
            {
                _remainingCountdownSeconds = 0f;
                _countdownLabel.Value = string.Empty;
                _isCountdownVisible.Value = false;
                _isPlaying.Value = true;
                return;
            }

            ApplyCountdownLabel();
        }

        public void ResetAndStartCountdown()
        {
            _isPlaying.Value = false;
            _isCountdownVisible.Value = false;
            _countdownLabel.Value = string.Empty;
            _hasStartedCountdown = false;
            StartCountdown();
        }

        private void ApplyCountdownLabel()
        {
            if (_remainingCountdownSeconds <= _startLabelSeconds)
            {
                _countdownLabel.Value = "START";
                return;
            }

            // 仕様: 設置後に3,2,1を表示し、START表示後にゲームを開始する。
            var number = (int)System.Math.Ceiling(_remainingCountdownSeconds - _startLabelSeconds);
            _countdownLabel.Value = number.ToString();
        }
    }
}
