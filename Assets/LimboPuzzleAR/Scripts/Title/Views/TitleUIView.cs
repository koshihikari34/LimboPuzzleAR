using System.Collections;
using LimboPuzzleAR.Title.ViewModels;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace LimboPuzzleAR.Title.Views
{
    /// <summary>
    /// Title画面のuGUI表示とボタン入力をViewModelへ接続する。
    /// </summary>
    public sealed class TitleUIView : MonoBehaviour
    {
        private static readonly int GlitchAmountId = Shader.PropertyToID("_GlitchAmount");
        private static readonly int GlitchJitterId = Shader.PropertyToID("_GlitchJitter");

        [SerializeField] private TextMeshProUGUI highScoreText;
        [SerializeField] private TextMeshProUGUI tapScreenText;
        [SerializeField] private Image titleLogoImage;
        [SerializeField] private Material titleLogoGlitchMaterial;
        [SerializeField] private Button startButton;
        [SerializeField] private Button exitButton;
        [SerializeField] private Button tapScreenButton;
        [SerializeField, Min(0.1f)] private float tapBlinkSeconds = 0.85f;
        [SerializeField, Min(0.1f)] private float menuFadeSeconds = 0.25f;
        [SerializeField, Min(0f)] private float firstLogoGlitchDelaySeconds = 1.2f;
        [SerializeField, Min(0.1f)] private float logoGlitchIntervalMinSeconds = 3f;
        [SerializeField, Min(0.1f)] private float logoGlitchIntervalMaxSeconds = 6f;
        [SerializeField, Min(0.05f)] private float logoGlitchDurationSeconds = 0.5f;
        [SerializeField, Range(0f, 1f)] private float logoGlitchStrength = 1f;
        [SerializeField, Min(0.05f)] private float startTransitionSeconds = 0.35f;
        [SerializeField, Range(0f, 1f)] private float startTransitionGlitchStrength = 1f;

        private TitleViewModel _viewModel;
        private CanvasGroup _highScoreGroup;
        private CanvasGroup _startButtonGroup;
        private CanvasGroup _exitButtonGroup;
        private Material _titleLogoGlitchInstance;
        private int _latestHighScore;
        private bool _isMenuVisible;
        private Coroutine _tapBlinkCoroutine;
        private Coroutine _logoGlitchCoroutine;
        private Coroutine _startTransitionCoroutine;

        [Inject]
        public void Construct(TitleViewModel viewModel)
        {
            _viewModel = viewModel;
        }

        private void Start()
        {
            SetupTitleIntro();
            SetupTitleLogoGlitch();

            _viewModel.HighScore
                .Subscribe(ApplyHighScore)
                .AddTo(this);

            if (startButton != null)
            {
                startButton.onClick.AddListener(StartGameWithTransition);
            }

            if (exitButton != null)
            {
                exitButton.onClick.AddListener(_viewModel.ExitGame);
            }

            if (tapScreenButton != null)
            {
                tapScreenButton.onClick.AddListener(RevealMenu);
            }
        }

        private void OnDestroy()
        {
            if (startButton != null)
            {
                startButton.onClick.RemoveListener(StartGameWithTransition);
            }

            if (exitButton != null)
            {
                exitButton.onClick.RemoveListener(_viewModel.ExitGame);
            }

            if (tapScreenButton != null)
            {
                tapScreenButton.onClick.RemoveListener(RevealMenu);
            }

            if (_logoGlitchCoroutine != null)
            {
                StopCoroutine(_logoGlitchCoroutine);
                _logoGlitchCoroutine = null;
            }

            if (titleLogoImage != null && _titleLogoGlitchInstance != null)
            {
                titleLogoImage.material = null;
            }

            if (_titleLogoGlitchInstance != null)
            {
                Destroy(_titleLogoGlitchInstance);
                _titleLogoGlitchInstance = null;
            }
        }

        private void ApplyHighScore(int highScore)
        {
            _latestHighScore = highScore;
            if (!_isMenuVisible)
            {
                return;
            }

            if (highScoreText == null)
            {
                return;
            }

            highScoreText.text = $"HIGH SCORE {_latestHighScore}";
        }

        private void SetupTitleIntro()
        {
            _highScoreGroup = SetupMenuGroup(highScoreText != null ? highScoreText.gameObject : null);
            _startButtonGroup = SetupMenuGroup(startButton != null ? startButton.gameObject : null);
            _exitButtonGroup = SetupMenuGroup(exitButton != null ? exitButton.gameObject : null);
            SetMenuVisible(false, 0f);

            if (tapScreenText != null)
            {
                tapScreenText.gameObject.SetActive(true);
                SetTextAlpha(tapScreenText, 0.85f);
                _tapBlinkCoroutine = StartCoroutine(BlinkTapScreen());
            }

            if (tapScreenButton != null)
            {
                tapScreenButton.gameObject.SetActive(true);
                tapScreenButton.interactable = true;
                if (tapScreenButton.targetGraphic != null)
                {
                    tapScreenButton.targetGraphic.raycastTarget = true;
                }
            }
            else
            {
                RevealMenu();
            }
        }

        private static CanvasGroup SetupMenuGroup(GameObject menuObject)
        {
            if (menuObject == null)
            {
                return null;
            }

            if (!menuObject.TryGetComponent<CanvasGroup>(out var group))
            {
                group = menuObject.AddComponent<CanvasGroup>();
            }

            return group;
        }

        private IEnumerator BlinkTapScreen()
        {
            while (!_isMenuVisible)
            {
                var phase = Mathf.PingPong(Time.unscaledTime / tapBlinkSeconds, 1f);
                var alpha = Mathf.Lerp(0.25f, 0.9f, Mathf.SmoothStep(0f, 1f, phase));
                SetTextAlpha(tapScreenText, alpha);
                yield return null;
            }
        }

        private void SetupTitleLogoGlitch()
        {
            if (titleLogoImage == null || titleLogoGlitchMaterial == null)
            {
                return;
            }

            _titleLogoGlitchInstance = Instantiate(titleLogoGlitchMaterial);
            titleLogoImage.material = _titleLogoGlitchInstance;
            SetLogoGlitch(0f, 0f);
            _logoGlitchCoroutine = StartCoroutine(PlayLogoGlitchLoop());
        }

        private IEnumerator PlayLogoGlitchLoop()
        {
            yield return new WaitForSecondsRealtime(firstLogoGlitchDelaySeconds);

            while (true)
            {
                yield return PlayLogoGlitchOnce();

                var minInterval = Mathf.Min(logoGlitchIntervalMinSeconds, logoGlitchIntervalMaxSeconds);
                var maxInterval = Mathf.Max(logoGlitchIntervalMinSeconds, logoGlitchIntervalMaxSeconds);
                yield return new WaitForSecondsRealtime(UnityEngine.Random.Range(minInterval, maxInterval));
            }
        }

        private IEnumerator PlayLogoGlitchOnce()
        {
            var elapsedSeconds = 0f;
            var jitter = UnityEngine.Random.Range(0.7f, 1f);
            while (elapsedSeconds < logoGlitchDurationSeconds)
            {
                elapsedSeconds += Time.unscaledDeltaTime;
                var phase = Mathf.Clamp01(elapsedSeconds / logoGlitchDurationSeconds);
                var envelope = Mathf.Sin(phase * Mathf.PI);
                SetLogoGlitch(envelope * logoGlitchStrength, jitter);
                yield return null;
            }

            SetLogoGlitch(0f, 0f);
        }

        private void SetLogoGlitch(float amount, float jitter)
        {
            if (_titleLogoGlitchInstance == null)
            {
                return;
            }

            _titleLogoGlitchInstance.SetFloat(GlitchAmountId, amount);
            _titleLogoGlitchInstance.SetFloat(GlitchJitterId, jitter);
        }

        private void RevealMenu()
        {
            if (_isMenuVisible)
            {
                return;
            }

            _isMenuVisible = true;
            if (_tapBlinkCoroutine != null)
            {
                StopCoroutine(_tapBlinkCoroutine);
                _tapBlinkCoroutine = null;
            }

            if (tapScreenText != null)
            {
                tapScreenText.gameObject.SetActive(false);
            }

            if (tapScreenButton != null)
            {
                tapScreenButton.gameObject.SetActive(false);
            }

            StartCoroutine(FadeMenuIn());
        }

        private void StartGameWithTransition()
        {
            if (_startTransitionCoroutine != null)
            {
                return;
            }

            _startTransitionCoroutine = StartCoroutine(PlayStartTransition());
        }

        private IEnumerator PlayStartTransition()
        {
            SetMenuInteractable(false);

            if (tapScreenButton != null)
            {
                tapScreenButton.interactable = false;
            }

            if (_logoGlitchCoroutine != null)
            {
                StopCoroutine(_logoGlitchCoroutine);
                _logoGlitchCoroutine = null;
            }

            var elapsedSeconds = 0f;
            while (elapsedSeconds < startTransitionSeconds)
            {
                elapsedSeconds += Time.unscaledDeltaTime;
                var phase = Mathf.Clamp01(elapsedSeconds / startTransitionSeconds);
                SetMenuVisible(true, 1f - phase);
                SetLogoGlitch(startTransitionGlitchStrength, 1f);
                yield return null;
            }

            _viewModel.StartGame();
        }

        private IEnumerator FadeMenuIn()
        {
            if (highScoreText != null)
            {
                highScoreText.text = $"HIGH SCORE {_latestHighScore}";
            }

            SetMenuVisible(true, 0f);
            if (menuFadeSeconds <= 0f)
            {
                SetMenuVisible(true, 1f);
                yield break;
            }

            var elapsedSeconds = 0f;
            while (elapsedSeconds < menuFadeSeconds)
            {
                elapsedSeconds += Time.unscaledDeltaTime;
                SetMenuVisible(true, Mathf.Clamp01(elapsedSeconds / menuFadeSeconds));
                yield return null;
            }

            SetMenuVisible(true, 1f);
        }

        private void SetMenuVisible(bool isVisible, float alpha)
        {
            ApplyMenuGroup(_highScoreGroup, isVisible, alpha);
            ApplyMenuGroup(_startButtonGroup, isVisible, alpha);
            ApplyMenuGroup(_exitButtonGroup, isVisible, alpha);
        }

        private void SetMenuInteractable(bool isInteractable)
        {
            ApplyMenuInteractable(_highScoreGroup, isInteractable);
            ApplyMenuInteractable(_startButtonGroup, isInteractable);
            ApplyMenuInteractable(_exitButtonGroup, isInteractable);
        }

        private static void ApplyMenuGroup(CanvasGroup group, bool isVisible, float alpha)
        {
            if (group == null)
            {
                return;
            }

            group.gameObject.SetActive(isVisible || alpha > 0f);
            group.alpha = alpha;
            group.interactable = isVisible && alpha >= 1f;
            group.blocksRaycasts = isVisible && alpha >= 1f;
        }

        private static void ApplyMenuInteractable(CanvasGroup group, bool isInteractable)
        {
            if (group == null)
            {
                return;
            }

            group.interactable = isInteractable;
            group.blocksRaycasts = isInteractable;
        }

        private static void SetTextAlpha(TextMeshProUGUI text, float alpha)
        {
            if (text == null)
            {
                return;
            }

            var color = text.color;
            color.a = alpha;
            text.color = color;
        }
    }
}
