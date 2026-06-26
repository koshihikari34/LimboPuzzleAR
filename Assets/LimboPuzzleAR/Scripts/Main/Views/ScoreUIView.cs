using System.Collections;
using LimboPuzzleAR.Main.ViewModels;
using R3;
using UnityEngine;
using TMPro;
using VContainer;

namespace LimboPuzzleAR.Main.Views
{
    /// <summary>
    /// クリアセット数を画面上のテキストへ反映する。
    /// </summary>
    public sealed class ScoreUIView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField, Min(1f)] private float scorePunchScale = 1.18f;
        [SerializeField, Min(0f)] private float scorePunchDurationSeconds = 0.18f;

        private StoneViewModel _stoneViewModel;
        private Coroutine _scorePunchCoroutine;
        private Vector3 _initialScoreScale = Vector3.one;

        [Inject]
        public void Construct(StoneViewModel stoneViewModel)
        {
            _stoneViewModel = stoneViewModel;
        }

        private void Start()
        {
            if (scoreText != null)
            {
                _initialScoreScale = scoreText.rectTransform.localScale;
            }

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

            scoreText.text = $"SCORE {clearSetCount}";
            if (clearSetCount > 0)
            {
                PlayScorePunch();
            }
        }

        private void PlayScorePunch()
        {
            if (scoreText == null || scorePunchDurationSeconds <= 0f)
            {
                return;
            }

            if (_scorePunchCoroutine != null)
            {
                StopCoroutine(_scorePunchCoroutine);
            }

            _scorePunchCoroutine = StartCoroutine(AnimateScorePunch());
        }

        private IEnumerator AnimateScorePunch()
        {
            var rectTransform = scoreText.rectTransform;
            var halfDuration = scorePunchDurationSeconds * 0.5f;

            yield return ScaleScore(rectTransform, _initialScoreScale, _initialScoreScale * scorePunchScale, halfDuration);
            yield return ScaleScore(rectTransform, rectTransform.localScale, _initialScoreScale, halfDuration);
            rectTransform.localScale = _initialScoreScale;
            _scorePunchCoroutine = null;
        }

        private static IEnumerator ScaleScore(
            Transform target,
            Vector3 fromScale,
            Vector3 toScale,
            float durationSeconds)
        {
            if (durationSeconds <= 0f)
            {
                target.localScale = toScale;
                yield break;
            }

            var elapsedSeconds = 0f;
            while (elapsedSeconds < durationSeconds)
            {
                elapsedSeconds += Time.unscaledDeltaTime;
                var progress = Mathf.Clamp01(elapsedSeconds / durationSeconds);
                target.localScale = Vector3.Lerp(fromScale, toScale, Mathf.SmoothStep(0f, 1f, progress));
                yield return null;
            }

            target.localScale = toScale;
        }
    }
}
