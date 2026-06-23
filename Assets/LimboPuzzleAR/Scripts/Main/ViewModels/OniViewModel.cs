using LimboPuzzleAR.Main.Models;
using R3;

namespace LimboPuzzleAR.Main.ViewModels
{
    /// <summary>
    /// 鬼の状態をViewへ公開する。
    /// </summary>
    public sealed class OniViewModel
    {
        private readonly OniModel _oniModel;
        private readonly OniStateMachine _stateMachine;

        public OniViewModel(
            OniModel oniModel,
            OniStateMachine stateMachine)
        {
            _oniModel = oniModel;
            _stateMachine = stateMachine;
        }

        public ReadOnlyReactiveProperty<OniState> CurrentState => _oniModel.CurrentState;

        public void SetState(OniState state)
        {
            _stateMachine.SetState(state);
        }

        public void AdvanceCycle()
        {
            _stateMachine.AdvanceCycle();
        }

        public void ReturnToSafe()
        {
            _stateMachine.RecoverFromAttack();
        }
    }
}
