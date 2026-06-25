using LimboPuzzleAR.Main.ViewModels;
using R3;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace LimboPuzzleAR.Main.Views
{
    /// <summary>
    /// ゲーム開始カウントダウンを表示する。
    /// </summary>
    public sealed class GameStartCountdownView : MonoBehaviour
    {
        [SerializeField] private Text countdownText;

        private GameStartViewModel _gameStartViewModel;

        [Inject]
        public void Construct(GameStartViewModel gameStartViewModel)
        {
            _gameStartViewModel = gameStartViewModel;
        }

        private void Awake()
        {
            ApplyVisibility(false);
        }

        private void Start()
        {
            _gameStartViewModel.IsCountdownVisible
                .Subscribe(ApplyVisibility)
                .AddTo(this);

            _gameStartViewModel.CountdownLabel
                .Subscribe(ApplyLabel)
                .AddTo(this);
        }

        private void Update()
        {
            _gameStartViewModel.Tick(Time.deltaTime);
        }

        private void ApplyVisibility(bool isVisible)
        {
            if (countdownText == null)
            {
                return;
            }

            countdownText.gameObject.SetActive(isVisible);
        }

        private void ApplyLabel(string label)
        {
            if (countdownText == null)
            {
                return;
            }

            countdownText.text = label;
        }
    }
}
