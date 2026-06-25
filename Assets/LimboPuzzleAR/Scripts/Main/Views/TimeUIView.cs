using LimboPuzzleAR.Main.ViewModels;
using R3;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VContainer;

namespace LimboPuzzleAR.Main.Views
{
    /// <summary>
    /// 残り時間を画面上のテキストへ反映する。
    /// </summary>
    public sealed class TimeUIView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI timeText;

        private TimeViewModel _timeViewModel;

        [Inject]
        public void Construct(TimeViewModel timeViewModel)
        {
            _timeViewModel = timeViewModel;
        }

        private void Start()
        {
            _timeViewModel.RemainingSeconds
                .Subscribe(ApplyTime)
                .AddTo(this);
        }

        private void ApplyTime(float remainingSeconds)
        {
            if (timeText == null)
            {
                return;
            }

            timeText.text = $"TIME {Mathf.CeilToInt(remainingSeconds)}";
        }
    }
}
