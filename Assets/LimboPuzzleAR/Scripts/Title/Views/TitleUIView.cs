using LimboPuzzleAR.Title.ViewModels;
using R3;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VContainer;

namespace LimboPuzzleAR.Title.Views
{
    /// <summary>
    /// Title画面のuGUI表示とボタン入力をViewModelへ接続する。
    /// </summary>
    public sealed class TitleUIView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI highScoreText;
        [SerializeField] private Button startButton;
        [SerializeField] private Button exitButton;

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

            if (exitButton != null)
            {
                exitButton.onClick.AddListener(_viewModel.ExitGame);
            }
        }

        private void OnDestroy()
        {
            if (startButton != null)
            {
                startButton.onClick.RemoveListener(_viewModel.StartGame);
            }

            if (exitButton != null)
            {
                exitButton.onClick.RemoveListener(_viewModel.ExitGame);
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
