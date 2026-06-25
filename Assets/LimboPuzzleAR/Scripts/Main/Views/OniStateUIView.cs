using LimboPuzzleAR.Main.Models;
using LimboPuzzleAR.Main.ViewModels;
using R3;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VContainer;

namespace LimboPuzzleAR.Main.Views
{
    /// <summary>
    /// 鬼ステートを画面上のテキストへ反映する。
    /// </summary>
    public sealed class OniStateUIView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI stateText;
        [SerializeField] private Image leftStatusImage;
        [SerializeField] private Image statusIconImage;
        [SerializeField] private Sprite idleSprite;
        [SerializeField] private Sprite safeSprite;
        [SerializeField] private Sprite warningSprite;
        [SerializeField] private Sprite watchingSprite;
        [SerializeField] private Sprite attackSprite;

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
                .Subscribe(ApplyTimeUpState)
                .AddTo(this);
        }

        private void ApplyState(OniState state)
        {
            if (_timeViewModel.IsTimeUp.CurrentValue)
            {
                ApplyTimeUp();
                return;
            }

            ApplyStateText(state);
            ApplyStateVisual(state);
        }

        private void ApplyStateText(OniState state)
        {
            if (stateText == null)
            {
                return;
            }

            stateText.text = state switch
            {
                OniState.Idle => "待機中",
                OniState.Safe => "積め！",
                OniState.Warning => "来るぞ...",
                OniState.Watching => "離すな！",
                OniState.Attack => "アウト！",
                _ => string.Empty
            };
        }

        private void ApplyStateVisual(OniState state)
        {
            // 仕様: 鬼ステートに応じてステータス背景色とアイコンを切り替える。
            if (leftStatusImage != null)
            {
                leftStatusImage.color = GetStatusColor(state);
            }

            if (statusIconImage == null)
            {
                return;
            }

            var sprite = GetStatusSprite(state);
            if (sprite != null)
            {
                statusIconImage.sprite = sprite;
            }
        }

        private void ApplyTimeUp()
        {
            if (stateText == null)
            {
                return;
            }

            stateText.text = "終了！";
        }

        private void ApplyTimeUpState(bool isTimeUp)
        {
            if (isTimeUp)
            {
                ApplyTimeUp();
                return;
            }

            ApplyState(_oniViewModel.CurrentState.CurrentValue);
        }

        private static Color32 GetStatusColor(OniState state)
        {
            return state switch
            {
                OniState.Idle => new Color32(0xD9, 0xD9, 0xD9, 0xFF),
                OniState.Safe => new Color32(0x2F, 0xFF, 0x00, 0xFF),
                OniState.Warning => new Color32(0xFB, 0xFF, 0x00, 0xFF),
                OniState.Watching => new Color32(0xFF, 0x22, 0x00, 0xFF),
                OniState.Attack => new Color32(0xFF, 0x22, 0x00, 0xFF),
                _ => Color.white
            };
        }

        private Sprite GetStatusSprite(OniState state)
        {
            return state switch
            {
                OniState.Idle => idleSprite,
                OniState.Safe => safeSprite,
                OniState.Warning => warningSprite,
                OniState.Watching => watchingSprite,
                OniState.Attack => attackSprite,
                _ => null
            };
        }
    }
}
