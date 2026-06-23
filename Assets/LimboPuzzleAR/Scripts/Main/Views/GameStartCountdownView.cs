using LimboPuzzleAR.Main.ViewModels;
using R3;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace LimboPuzzleAR.Main.Views
{
    /// <summary>
    /// 設置完了後のゲーム開始カウントダウンを表示する。
    /// </summary>
    public sealed class GameStartCountdownView : MonoBehaviour
    {
        [SerializeField] private Text countdownText;

        private GameStartViewModel _gameStartViewModel;
        private PlacementViewModel _placementViewModel;

        [Inject]
        public void Construct(
            GameStartViewModel gameStartViewModel,
            PlacementViewModel placementViewModel)
        {
            _gameStartViewModel = gameStartViewModel;
            _placementViewModel = placementViewModel;
        }

        private void Awake()
        {
            ApplyVisibility(false);
        }

        private void Start()
        {
            _placementViewModel.IsPlaced
                .Where(isPlaced => isPlaced)
                .Subscribe(_ => _gameStartViewModel.StartCountdown())
                .AddTo(this);

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
