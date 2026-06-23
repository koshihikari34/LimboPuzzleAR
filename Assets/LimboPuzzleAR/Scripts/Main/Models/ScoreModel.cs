using R3;

namespace LimboPuzzleAR.Main.Models
{
    /// <summary>
    /// Mainゲーム中のクリアセット数を保持する。
    /// </summary>
    public sealed class ScoreModel
    {
        private readonly ReactiveProperty<int> _clearSetCount = new(0);

        public ReadOnlyReactiveProperty<int> ClearSetCount => _clearSetCount;

        public void AddClearSet()
        {
            _clearSetCount.Value++;
        }

        public void Reset()
        {
            _clearSetCount.Value = 0;
        }
    }
}
