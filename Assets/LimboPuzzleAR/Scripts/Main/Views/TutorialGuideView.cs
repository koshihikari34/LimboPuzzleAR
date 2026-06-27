using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace LimboPuzzleAR.Main.Views
{
    /// <summary>
    /// 初回起動時とヘルプ表示で、遊び方をページ式モーダルとして表示する。
    /// </summary>
    public sealed class TutorialGuideView : MonoBehaviour
    {
        private const string ViewedKey = "LimboPuzzleAR.Tutorial.Viewed";

        [SerializeField] private Font guideFont;
        [SerializeField] private Sprite modalSprite;
        [SerializeField] private Sprite[] illustrationSprites;

        private static float _previousTimeScale = 1f;
        private static bool _isPausingGameplay;

        private static readonly string[] Titles =
        {
            "平面を探して置く",
            "石をタップしながら積む",
            "石が止まるまで待つ",
            "鬼が見ている時は離さない",
            "高さに届いたらスコア",
            "アウトになると鬼が崩す"
        };

        private static readonly string[] Bodies =
        {
            "ARで平面を見つけたら\nゲームエリアを置きたい場所をタップします",
            "右上の石ボタンをタップしたまま動かして\nガイドの高さを目指して積みます",
            "置いた石が安定するまでは次の石を押せません\nボタンがグレーの間は待ちます",
            "「離すな！」の間に指を離すとアウトです\n「！」と「来るぞ...」になったら注意しましょう",
            "石がガイドの高さに届くとスコアが加算され\n次の高さに進みます",
            "アウトになると鬼が石を崩します\n落ち着いてもう一度挑戦しましょう"
        };

        private GameObject _modalRoot;
        private Image _illustrationImage;
        private Text _titleText;
        private Text _bodyText;
        private Text _pageText;
        private Text _nextButtonText;
        private GameObject _helpButtonObject;
        private Button _backButton;
        private Button _nextButton;
        private int _pageIndex;

        private void Start()
        {
            if (guideFont == null)
            {
                guideFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }

            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                canvas = FindTutorialCanvas();
            }

            if (canvas == null)
            {
                Debug.LogWarning("Tutorial guide skipped. Missing parent Canvas.");
                return;
            }

            BuildModal(canvas.transform);
            BuildHelpButton(canvas.transform);

            if (!PlayerPrefs.HasKey(ViewedKey))
            {
                StartCoroutine(OpenInitialTutorialNextFrame());
            }
            else
            {
                Close(saveViewed: false);
            }
        }

        private IEnumerator OpenInitialTutorialNextFrame()
        {
            yield return null;
            Open();
        }

        private static Canvas FindTutorialCanvas()
        {
            var canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            Canvas fallbackCanvas = null;

            foreach (var canvas in canvases)
            {
                if (!IsTutorialCanvasCandidate(canvas))
                {
                    continue;
                }

                if (canvas.name == "MainCanvas")
                {
                    return canvas;
                }

                fallbackCanvas ??= canvas;
            }

            return fallbackCanvas;
        }

        private static bool IsTutorialCanvasCandidate(Canvas canvas)
        {
            if (canvas == null || !canvas.isActiveAndEnabled || !canvas.gameObject.activeInHierarchy)
            {
                return false;
            }

            if (canvas.name.Contains("Transition"))
            {
                return false;
            }

            return !canvas.TryGetComponent<CanvasGroup>(out var canvasGroup)
                || canvasGroup.alpha > 0f;
        }

        public void Open()
        {
            if (_modalRoot == null)
            {
                return;
            }

            _pageIndex = 0;
            _modalRoot.SetActive(true);
            PauseGameplay();
            if (_helpButtonObject != null)
            {
                _helpButtonObject.SetActive(false);
            }

            ApplyPage();
        }

        private void Close(bool saveViewed)
        {
            if (saveViewed)
            {
                PlayerPrefs.SetInt(ViewedKey, 1);
                PlayerPrefs.Save();
            }

            if (_modalRoot != null)
            {
                _modalRoot.SetActive(false);
            }

            ResumeGameplay();

            if (_helpButtonObject != null)
            {
                _helpButtonObject.SetActive(true);
            }
        }

        private void OnDestroy()
        {
            ResumeGameplay();
        }

        private void MovePage(int direction)
        {
            _pageIndex = Mathf.Clamp(_pageIndex + direction, 0, Titles.Length - 1);
            ApplyPage();
        }

        private void ApplyPage()
        {
            _titleText.text = Titles[_pageIndex];
            _bodyText.text = Bodies[_pageIndex];
            _pageText.text = $"{_pageIndex + 1}/{Titles.Length}";
            _nextButtonText.text = _pageIndex >= Titles.Length - 1 ? "はじめる" : "次へ";
            _backButton.interactable = _pageIndex > 0;

            var sprite = GetIllustrationSprite(_pageIndex);
            _illustrationImage.sprite = sprite;
            _illustrationImage.enabled = sprite != null;
            _illustrationImage.color = Color.white;
        }

        private Sprite GetIllustrationSprite(int index)
        {
            if (illustrationSprites == null
                || index < 0
                || index >= illustrationSprites.Length)
            {
                return null;
            }

            return illustrationSprites[index];
        }

        private static void PauseGameplay()
        {
            if (_isPausingGameplay)
            {
                return;
            }

            _previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            _isPausingGameplay = true;
        }

        private static void ResumeGameplay()
        {
            if (!_isPausingGameplay)
            {
                return;
            }

            Time.timeScale = _previousTimeScale;
            _isPausingGameplay = false;
        }

        private void BuildModal(Transform parent)
        {
            _modalRoot = CreateObject("TutorialModal", parent);
            var rootRect = _modalRoot.AddComponent<RectTransform>();
            Stretch(rootRect);
            var overlayImage = _modalRoot.AddComponent<Image>();
            overlayImage.color = new Color(0f, 0f, 0f, 0.58f);
            overlayImage.raycastTarget = true;

            var panel = CreateObject("Panel", _modalRoot.transform);
            var panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.08f, 0.5f);
            panelRect.anchorMax = new Vector2(0.92f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta = new Vector2(0f, 940f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            var panelImage = panel.AddComponent<Image>();
            panelImage.sprite = modalSprite;
            panelImage.type = modalSprite == null ? Image.Type.Simple : Image.Type.Sliced;
            panelImage.color = new Color(0.98f, 0.98f, 1f, 0.96f);

            _illustrationImage = CreateImage("Illustration", panel.transform);
            var illustrationRect = _illustrationImage.rectTransform;
            illustrationRect.anchorMin = new Vector2(0.5f, 1f);
            illustrationRect.anchorMax = new Vector2(0.5f, 1f);
            illustrationRect.anchoredPosition = new Vector2(0f, -220f);
            illustrationRect.sizeDelta = new Vector2(440f, 280f);
            _illustrationImage.preserveAspect = true;

            _titleText = CreateText("Title", panel.transform, 52, FontStyle.Bold, new Color32(0x07, 0x07, 0x6D, 0xFF));
            SetTextRect(_titleText.rectTransform, new Vector2(0.08f, 1f), new Vector2(0.92f, 1f), new Vector2(0f, -410f), new Vector2(0f, 90f));

            _bodyText = CreateText("Body", panel.transform, 34, FontStyle.Normal, new Color32(0x20, 0x20, 0x38, 0xFF));
            _bodyText.lineSpacing = 1.15f;
            SetTextRect(_bodyText.rectTransform, new Vector2(0.1f, 0.5f), new Vector2(0.9f, 0.5f), new Vector2(0f, -40f), new Vector2(0f, 230f));

            _pageText = CreateText("Page", panel.transform, 28, FontStyle.Normal, new Color32(0x60, 0x60, 0x7A, 0xFF));
            SetTextRect(_pageText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 190f), new Vector2(180f, 50f));

            _backButton = CreateButton("BackButton", panel.transform, "戻る");
            SetTextRect(_backButton.GetComponent<RectTransform>(), new Vector2(0.08f, 0f), new Vector2(0.36f, 0f), new Vector2(0f, 90f), new Vector2(0f, 86f));
            _backButton.onClick.AddListener(() => MovePage(-1));

            _nextButton = CreateButton("NextButton", panel.transform, "次へ");
            SetTextRect(_nextButton.GetComponent<RectTransform>(), new Vector2(0.64f, 0f), new Vector2(0.92f, 0f), new Vector2(0f, 90f), new Vector2(0f, 86f));
            _nextButton.onClick.AddListener(() =>
            {
                if (_pageIndex >= Titles.Length - 1)
                {
                    Close(saveViewed: true);
                    return;
                }

                MovePage(1);
            });
            _nextButtonText = _nextButton.GetComponentInChildren<Text>();

            _modalRoot.SetActive(false);
        }

        private void BuildHelpButton(Transform parent)
        {
            var helpButton = CreateButton("TutorialHelpButton", parent, "?");
            _helpButtonObject = helpButton.gameObject;
            var rect = helpButton.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 0f);
            rect.anchoredPosition = new Vector2(78f, 78f);
            rect.sizeDelta = new Vector2(92f, 92f);
            helpButton.onClick.AddListener(Open);
        }

        private Button CreateButton(string objectName, Transform parent, string label)
        {
            var buttonObject = CreateObject(objectName, parent);
            var rect = buttonObject.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(220f, 80f);
            var image = buttonObject.AddComponent<Image>();
            image.color = new Color32(0x07, 0x07, 0x6D, 0xFF);
            var button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;

            var labelText = CreateText("Label", buttonObject.transform, 30, FontStyle.Bold, Color.white);
            Stretch(labelText.rectTransform);
            labelText.text = label;
            return button;
        }

        private Image CreateImage(string objectName, Transform parent)
        {
            var imageObject = CreateObject(objectName, parent);
            imageObject.AddComponent<CanvasRenderer>();
            var image = imageObject.AddComponent<Image>();
            image.raycastTarget = false;
            return image;
        }

        private Text CreateText(
            string objectName,
            Transform parent,
            float fontSize,
            FontStyle fontStyle,
            Color color)
        {
            var textObject = CreateObject(objectName, parent);
            textObject.AddComponent<CanvasRenderer>();
            var text = textObject.AddComponent<Text>();
            text.font = guideFont;
            text.text = string.Empty;
            text.fontStyle = fontStyle;
            text.color = color;
            text.alignment = TextAnchor.MiddleCenter;
            text.fontSize = Mathf.RoundToInt(fontSize);
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 22;
            text.resizeTextMaxSize = Mathf.RoundToInt(fontSize);
            text.raycastTarget = false;
            return text;
        }

        private static GameObject CreateObject(string objectName, Transform parent)
        {
            var gameObject = new GameObject(objectName);
            gameObject.transform.SetParent(parent, false);
            return gameObject;
        }

        private static void Stretch(RectTransform rectTransform)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = Vector2.zero;
        }

        private static void SetTextRect(
            RectTransform rectTransform,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 anchoredPosition,
            Vector2 sizeDelta)
        {
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.sizeDelta = sizeDelta;
        }
    }
}
