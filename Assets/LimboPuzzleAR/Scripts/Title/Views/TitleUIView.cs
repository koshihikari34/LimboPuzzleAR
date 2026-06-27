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
        [SerializeField, Min(0.05f)] private float startTransitionSeconds = 0.22f;
        [SerializeField, Min(0.05f)] private float startTransitionCloseSeconds = 0.35f;
        [SerializeField, Range(0f, 1f)] private float startTransitionGlitchStrength = 1f;

        private TitleViewModel _viewModel;
        private CanvasGroup _highScoreGroup;
        private CanvasGroup _startButtonGroup;
        private CanvasGroup _exitButtonGroup;
        private Canvas _startTransitionOverlayCanvas;
        private CanvasGroup _startTransitionOverlayGroup;
        private RectTransform _startTransitionTopCurtain;
        private RectTransform _startTransitionBottomCurtain;
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
            SetupStartTransitionOverlay();

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
            SetStartTransitionCurtain(0f);

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

            SetMenuVisible(false, 0f);
            if (titleLogoImage != null)
            {
                titleLogoImage.enabled = false;
            }

            BringStartTransitionCurtainToFront();
            elapsedSeconds = 0f;
            while (elapsedSeconds < startTransitionCloseSeconds)
            {
                elapsedSeconds += Time.unscaledDeltaTime;
                var phase = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsedSeconds / startTransitionCloseSeconds));
                SetStartTransitionCurtain(phase);
                yield return null;
            }

            SetStartTransitionCurtain(1f);
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

        private void SetupStartTransitionOverlay()
        {
            var transitionCanvasObject = new GameObject(
                "StartTransitionCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            transitionCanvasObject.layer = gameObject.layer;

            _startTransitionOverlayCanvas = transitionCanvasObject.GetComponent<Canvas>();
            _startTransitionOverlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _startTransitionOverlayCanvas.overrideSorting = true;
            _startTransitionOverlayCanvas.sortingOrder = short.MaxValue;

            var transitionCanvasScaler = transitionCanvasObject.GetComponent<CanvasScaler>();
            transitionCanvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            transitionCanvasScaler.referenceResolution = new Vector2(1080f, 1920f);
            transitionCanvasScaler.matchWidthOrHeight = 1f;

            _startTransitionOverlayGroup = transitionCanvasObject.AddComponent<CanvasGroup>();
            _startTransitionOverlayGroup.alpha = 0f;
            _startTransitionOverlayGroup.interactable = false;
            _startTransitionOverlayGroup.blocksRaycasts = false;

            _startTransitionTopCurtain = CreateStartTransitionCurtain(
                transitionCanvasObject.transform,
                "StartTransitionTopCurtain",
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(0.5f, 1f));
            _startTransitionBottomCurtain = CreateStartTransitionCurtain(
                transitionCanvasObject.transform,
                "StartTransitionBottomCurtain",
                Vector2.zero,
                new Vector2(1f, 0f),
                new Vector2(0.5f, 0f));

            SetStartTransitionCurtain(0f);
        }

        private void BringStartTransitionCurtainToFront()
        {
            if (_startTransitionOverlayCanvas == null)
            {
                return;
            }

            _startTransitionOverlayCanvas.sortingOrder = short.MaxValue;
        }

        private static RectTransform CreateStartTransitionCurtain(
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

        private void SetStartTransitionCurtain(float phase)
        {
            if (_startTransitionOverlayGroup != null)
            {
                _startTransitionOverlayGroup.alpha = phase > 0f ? 1f : 0f;
                _startTransitionOverlayGroup.blocksRaycasts = phase > 0f;
            }

            var height = Mathf.Lerp(0f, 960f, phase);
            if (_startTransitionTopCurtain != null)
            {
                _startTransitionTopCurtain.sizeDelta = new Vector2(0f, height);
            }

            if (_startTransitionBottomCurtain != null)
            {
                _startTransitionBottomCurtain.sizeDelta = new Vector2(0f, height);
            }
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
