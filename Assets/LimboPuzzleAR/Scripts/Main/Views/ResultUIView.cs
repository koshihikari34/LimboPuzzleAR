using LimboPuzzleAR.Common.Services;
using LimboPuzzleAR.Main.ViewModels;
using R3;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using VContainer;

namespace LimboPuzzleAR.Main.Views
{
    /// <summary>
    /// タイムアップ時の最終スコアをリザルトダイアログへ反映する。
    /// </summary>
    public sealed class ResultUIView : MonoBehaviour
    {
        [SerializeField] private GameObject resultRoot;
        [SerializeField] private Text scoreText;
        [SerializeField] private Text highScoreText;
        [SerializeField] private Text newRecordText;
        [SerializeField] private Button retryButton;
        [SerializeField] private Button titleButton;
        [SerializeField] private string titleSceneName = "Title";

        private StoneViewModel _stoneViewModel;
        private TimeViewModel _timeViewModel;
        private OniViewModel _oniViewModel;
        private GameStartViewModel _gameStartViewModel;
        private IScoreRepository _scoreRepository;

        [Inject]
        public void Construct(
            StoneViewModel stoneViewModel,
            TimeViewModel timeViewModel,
            OniViewModel oniViewModel,
            GameStartViewModel gameStartViewModel,
            IScoreRepository scoreRepository)
        {
            _stoneViewModel = stoneViewModel;
            _timeViewModel = timeViewModel;
            _oniViewModel = oniViewModel;
            _gameStartViewModel = gameStartViewModel;
            _scoreRepository = scoreRepository;
        }

        private void Start()
        {
            SetResultVisible(false);

            _timeViewModel.IsTimeUp
                .Where(isTimeUp => isTimeUp)
                .Subscribe(_ => ApplyResult())
                .AddTo(this);

            if (titleButton != null)
            {
                titleButton.onClick.AddListener(LoadTitleScene);
            }

            if (retryButton != null)
            {
                retryButton.onClick.AddListener(RetryGame);
            }
        }

        private void OnDestroy()
        {
            if (titleButton != null)
            {
                titleButton.onClick.RemoveListener(LoadTitleScene);
            }

            if (retryButton != null)
            {
                retryButton.onClick.RemoveListener(RetryGame);
            }
        }

        private void ApplyResult()
        {
            var currentScore = _stoneViewModel.ClearSetCount.CurrentValue;
            var savedHighScore = _scoreRepository.LoadHighScore();
            var highScore = savedHighScore;
            var isNewRecord = currentScore > savedHighScore;
            if (isNewRecord)
            {
                // ハイスコアはリザルト確定時だけ保存し、通常プレイ中の表示更新とは分ける。
                _scoreRepository.SaveHighScore(currentScore);
                highScore = currentScore;
            }

            ApplyText(scoreText, $"Score: {currentScore}");
            ApplyText(highScoreText, $"High Score: {highScore}");
            ApplyText(newRecordText, isNewRecord ? "New Record!" : string.Empty);
            SetResultVisible(true);
        }

        private void SetResultVisible(bool isVisible)
        {
            if (resultRoot == null)
            {
                return;
            }

            resultRoot.SetActive(isVisible);
        }

        private static void ApplyText(Text text, string value)
        {
            if (text == null)
            {
                return;
            }

            text.text = value;
        }

        private void LoadTitleScene()
        {
            SceneManager.LoadScene(titleSceneName);
        }

        private void RetryGame()
        {
            SetResultVisible(false);
            _stoneViewModel.ResetForRetry();
            _timeViewModel.Reset();
            _oniViewModel.ResetToIdle();
            _gameStartViewModel.ResetAndStartCountdown();
        }
    }
}
