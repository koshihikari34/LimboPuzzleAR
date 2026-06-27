using System.Collections;
using LimboPuzzleAR.Common.Services;
using LimboPuzzleAR.Main.ViewModels;
using R3;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using VContainer;

namespace LimboPuzzleAR.Main.Views
{
    /// <summary>
    /// タイムアップ時の最終スコアをリザルトダイアログへ反映する。
    /// </summary>
    public sealed class ResultUIView : MonoBehaviour
    {
        [SerializeField] private GameObject resultRoot;
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private TextMeshProUGUI highScoreText;
        [SerializeField] private TextMeshProUGUI newRecordText;
        [SerializeField] private Button retryButton;
        [SerializeField] private Button titleButton;
        [SerializeField] private string titleSceneName = "Title";
        [SerializeField, Min(0.05f)] private float titleTransitionCloseSeconds = 0.35f;

        private StoneViewModel _stoneViewModel;
        private TimeViewModel _timeViewModel;
        private OniViewModel _oniViewModel;
        private GameStartViewModel _gameStartViewModel;
        private IScoreRepository _scoreRepository;
        private Canvas _titleTransitionCanvas;
        private CanvasGroup _titleTransitionGroup;
        private RectTransform _titleTransitionTopCurtain;
        private RectTransform _titleTransitionBottomCurtain;
        private Coroutine _titleTransitionCoroutine;

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

            ApplyText(scoreText, $"SCORE {currentScore}");
            ApplyText(highScoreText, $"HIGH SCORE {highScore}");
            ApplyText(newRecordText, isNewRecord ? "NEW RECORD!" : string.Empty);
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

        private static void ApplyText(TextMeshProUGUI text, string value)
        {
            if (text == null)
            {
                return;
            }

            text.text = value;
        }

        private void LoadTitleScene()
        {
            if (_titleTransitionCoroutine != null)
            {
                return;
            }

            _titleTransitionCoroutine = StartCoroutine(PlayTitleTransition());
        }

        private void RetryGame()
        {
            SetResultVisible(false);
            _stoneViewModel.ResetForRetry();
            _timeViewModel.Reset();
            _oniViewModel.ResetToIdle();
            _gameStartViewModel.ResetAndStartCountdown();
        }

        private IEnumerator PlayTitleTransition()
        {
            if (_titleTransitionCanvas == null)
            {
                SetupTitleTransitionOverlay();
            }

            if (titleButton != null)
            {
                titleButton.interactable = false;
            }

            if (retryButton != null)
            {
                retryButton.interactable = false;
            }

            BringTitleTransitionToFront();
            var elapsedSeconds = 0f;
            while (elapsedSeconds < titleTransitionCloseSeconds)
            {
                elapsedSeconds += Time.unscaledDeltaTime;
                var phase = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsedSeconds / titleTransitionCloseSeconds));
                SetTitleTransitionCurtain(phase);
                yield return null;
            }

            SetTitleTransitionCurtain(1f);
            SceneManager.LoadScene(titleSceneName);
        }

        private void SetupTitleTransitionOverlay()
        {
            if (_titleTransitionCanvas != null)
            {
                return;
            }

            var transitionCanvasObject = new GameObject(
                "TitleTransitionCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            transitionCanvasObject.layer = gameObject.layer;

            _titleTransitionCanvas = transitionCanvasObject.GetComponent<Canvas>();
            _titleTransitionCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _titleTransitionCanvas.overrideSorting = true;
            _titleTransitionCanvas.sortingOrder = short.MaxValue;

            var transitionCanvasScaler = transitionCanvasObject.GetComponent<CanvasScaler>();
            transitionCanvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            transitionCanvasScaler.referenceResolution = new Vector2(1080f, 1920f);
            transitionCanvasScaler.matchWidthOrHeight = 1f;

            _titleTransitionGroup = transitionCanvasObject.AddComponent<CanvasGroup>();
            _titleTransitionGroup.alpha = 0f;
            _titleTransitionGroup.interactable = false;
            _titleTransitionGroup.blocksRaycasts = false;

            _titleTransitionTopCurtain = CreateTitleTransitionCurtain(
                transitionCanvasObject.transform,
                "TitleTransitionTopCurtain",
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(0.5f, 1f));
            _titleTransitionBottomCurtain = CreateTitleTransitionCurtain(
                transitionCanvasObject.transform,
                "TitleTransitionBottomCurtain",
                Vector2.zero,
                new Vector2(1f, 0f),
                new Vector2(0.5f, 0f));

            SetTitleTransitionCurtain(0f);
        }

        private void BringTitleTransitionToFront()
        {
            if (_titleTransitionCanvas == null)
            {
                return;
            }

            _titleTransitionCanvas.sortingOrder = short.MaxValue;
        }

        private static RectTransform CreateTitleTransitionCurtain(
            Transform parent,
            string objectName,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot)
        {
            var curtainObject = new GameObject(
                objectName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            curtainObject.layer = parent.gameObject.layer;
            curtainObject.transform.SetParent(parent, false);

            var curtainRect = curtainObject.GetComponent<RectTransform>();
            curtainRect.anchorMin = anchorMin;
            curtainRect.anchorMax = anchorMax;
            curtainRect.pivot = pivot;
            curtainRect.anchoredPosition = Vector2.zero;
            curtainRect.sizeDelta = Vector2.zero;

            var curtainImage = curtainObject.GetComponent<Image>();
            curtainImage.color = Color.black;
            curtainImage.raycastTarget = false;

            return curtainRect;
        }

        private void SetTitleTransitionCurtain(float phase)
        {
            if (_titleTransitionGroup != null)
            {
                _titleTransitionGroup.alpha = phase > 0f ? 1f : 0f;
                _titleTransitionGroup.blocksRaycasts = phase > 0f;
            }

            var height = Mathf.Lerp(0f, 960f, phase);
            if (_titleTransitionTopCurtain != null)
            {
                _titleTransitionTopCurtain.sizeDelta = new Vector2(0f, height);
            }

            if (_titleTransitionBottomCurtain != null)
            {
                _titleTransitionBottomCurtain.sizeDelta = new Vector2(0f, height);
            }
        }
    }
}
