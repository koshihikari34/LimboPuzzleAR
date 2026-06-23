using LimboPuzzleAR.Title.ViewModels;
using R3;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace LimboPuzzleAR.Title.Views
{
    /// <summary>
    /// Title画面のuGUI表示とSTARTボタン入力をViewModelへ接続する。
    /// </summary>
    public sealed class TitleUIView : MonoBehaviour
    {
        [SerializeField] private Text highScoreText;
        [SerializeField] private Button startButton;

        private TitleViewModel _viewModel;

        [Inject]
        public void Construct(TitleViewModel viewModel)
        {
            _viewModel = viewModel;
        }

        private void Start()
        {
            _viewModel.HighScore
                .Subscribe(ApplyHighScore)
                .AddTo(this);

            if (startButton != null)
            {
                startButton.onClick.AddListener(_viewModel.StartGame);
            }
        }

        private void OnDestroy()
        {
            if (startButton != null)
            {
                startButton.onClick.RemoveListener(_viewModel.StartGame);
            }
        }

        private void ApplyHighScore(int highScore)
        {
            if (highScoreText == null)
            {
                return;
            }

            highScoreText.text = $"High Score: {highScore}";
        }
    }
}
