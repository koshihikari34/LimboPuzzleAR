using R3;

namespace LimboPuzzleAR.Main.Models
{
    /// <summary>
    /// 石操作の進行状態を保持する。
    /// </summary>
    public sealed class StoneModel
    {
        private readonly ReactiveProperty<bool> _canSpawnStone = new(true);
        private readonly ReactiveProperty<bool> _isHolding = new(false);
        private readonly ReactiveProperty<bool> _isWaitingForStability = new(false);

        public ReadOnlyReactiveProperty<bool> CanSpawnStone => _canSpawnStone;

        public ReadOnlyReactiveProperty<bool> IsHolding => _isHolding;

        public ReadOnlyReactiveProperty<bool> IsWaitingForStability => _isWaitingForStability;

        public bool BeginHold()
        {
            // 同時に掴める石は1個だけ。安定待ち中も次の石は出せない。
            if (!_canSpawnStone.Value)
            {
                return false;
            }

            _canSpawnStone.Value = false;
            _isHolding.Value = true;
            _isWaitingForStability.Value = false;
            return true;
        }

        public bool ReleaseToStabilityWait()
        {
            if (!_isHolding.Value)
            {
                return false;
            }

            _isHolding.Value = false;
            _isWaitingForStability.Value = true;
            return true;
        }

        public bool MarkStable()
        {
            if (!_isWaitingForStability.Value)
            {
                return false;
            }

            _isWaitingForStability.Value = false;
            _canSpawnStone.Value = true;
            return true;
        }

        public void ResetInteraction()
        {
            _isHolding.Value = false;
            _isWaitingForStability.Value = false;
            _canSpawnStone.Value = true;
        }

        public void StopInteraction()
        {
            _isHolding.Value = false;
            _isWaitingForStability.Value = false;
            _canSpawnStone.Value = false;
        }
    }
}
