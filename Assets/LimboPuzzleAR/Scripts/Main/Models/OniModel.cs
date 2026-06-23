using R3;

namespace LimboPuzzleAR.Main.Models
{
    /// <summary>
    /// 鬼の現在状態を保持する。
    /// </summary>
    public sealed class OniModel
    {
        private readonly ReactiveProperty<OniState> _currentState = new(OniState.Safe);

        public ReadOnlyReactiveProperty<OniState> CurrentState => _currentState;

        public void SetState(OniState state)
        {
            if (_currentState.Value == state)
            {
                return;
            }

            _currentState.Value = state;
        }
    }
}
