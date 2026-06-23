namespace LimboPuzzleAR.Main.Models
{
    /// <summary>
    /// 鬼ステートの遷移ルールを管理する。
    /// </summary>
    public sealed class OniStateMachine
    {
        private readonly OniModel _oniModel;

        public OniStateMachine(OniModel oniModel)
        {
            _oniModel = oniModel;
        }

        public void SetState(OniState state)
        {
            _oniModel.SetState(state);
        }

        public void AdvanceCycle()
        {
            var nextState = _oniModel.CurrentState.CurrentValue switch
            {
                OniState.Safe => OniState.Warning,
                OniState.Warning => OniState.Watching,
                OniState.Watching => OniState.Safe,
                OniState.Attack => OniState.Attack,
                _ => OniState.Safe
            };

            _oniModel.SetState(nextState);
        }

        public void RecoverFromAttack()
        {
            if (_oniModel.CurrentState.CurrentValue != OniState.Attack)
            {
                return;
            }

            _oniModel.SetState(OniState.Safe);
        }
    }
}
