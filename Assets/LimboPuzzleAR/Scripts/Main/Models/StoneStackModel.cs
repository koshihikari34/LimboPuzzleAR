using R3;

namespace LimboPuzzleAR.Main.Models
{
    /// <summary>
    /// 安定済みの石積み情報を保持する。
    /// </summary>
    public sealed class StoneStackModel
    {
        private readonly ReactiveProperty<float> _highestTopY = new(float.MinValue);

        public ReadOnlyReactiveProperty<float> HighestTopY => _highestTopY;

        public void RegisterStableStoneTop(float topY)
        {
            if (topY <= _highestTopY.Value)
            {
                return;
            }

            _highestTopY.Value = topY;
        }

        public bool HasReachedHeight(float guideY)
        {
            return _highestTopY.Value >= guideY;
        }

        public void Reset()
        {
            _highestTopY.Value = float.MinValue;
        }
    }
}
