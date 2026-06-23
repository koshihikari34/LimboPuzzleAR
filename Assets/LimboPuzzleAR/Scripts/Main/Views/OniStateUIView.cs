using LimboPuzzleAR.Main.Models;
using LimboPuzzleAR.Main.ViewModels;
using R3;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace LimboPuzzleAR.Main.Views
{
    /// <summary>
    /// 鬼ステートを画面上のテキストへ反映する。
    /// </summary>
    public sealed class OniStateUIView : MonoBehaviour
    {
        [SerializeField] private Text stateText;

        private OniViewModel _oniViewModel;
        private TimeViewModel _timeViewModel;

        [Inject]
        public void Construct(
            OniViewModel oniViewModel,
            TimeViewModel timeViewModel)
        {
            _oniViewModel = oniViewModel;
            _timeViewModel = timeViewModel;
        }

        private void Start()
        {
            _oniViewModel.CurrentState
                .Subscribe(ApplyState)
                .AddTo(this);

            _timeViewModel.IsTimeUp
                .Where(isTimeUp => isTimeUp)
                .Subscribe(_ => ApplyTimeUp())
                .AddTo(this);
        }

        private void ApplyState(OniState state)
        {
            if (stateText == null)
            {
                return;
            }

            if (_timeViewModel.IsTimeUp.CurrentValue)
            {
                ApplyTimeUp();
                return;
            }

            stateText.text = state switch
            {
                OniState.Safe => "積め！",
                OniState.Warning => "来るぞ...",
                OniState.Watching => "離すな！",
                OniState.Attack => "アウト！",
                _ => string.Empty
            };
        }

        private void ApplyTimeUp()
        {
            if (stateText == null)
            {
                return;
            }

            stateText.text = "終了！";
        }
    }
}
