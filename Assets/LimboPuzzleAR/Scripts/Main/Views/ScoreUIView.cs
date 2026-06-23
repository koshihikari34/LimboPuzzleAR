using LimboPuzzleAR.Main.ViewModels;
using R3;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace LimboPuzzleAR.Main.Views
{
    /// <summary>
    /// クリアセット数を画面上のテキストへ反映する。
    /// </summary>
    public sealed class ScoreUIView : MonoBehaviour
    {
        [SerializeField] private Text scoreText;

        private StoneViewModel _stoneViewModel;

        [Inject]
        public void Construct(StoneViewModel stoneViewModel)
        {
            _stoneViewModel = stoneViewModel;
        }

        private void Start()
        {
            _stoneViewModel.ClearSetCount
                .Subscribe(ApplyScore)
                .AddTo(this);
        }

        private void ApplyScore(int clearSetCount)
        {
            if (scoreText == null)
            {
                return;
            }

            scoreText.text = $"Score: {clearSetCount}";
        }
    }
}
