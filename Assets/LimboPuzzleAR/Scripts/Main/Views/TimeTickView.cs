using LimboPuzzleAR.Main.ViewModels;
using UnityEngine;
using VContainer;

namespace LimboPuzzleAR.Main.Views
{
    /// <summary>
    /// Unityのフレーム更新を残り時間の更新へ橋渡しする。
    /// </summary>
    public sealed class TimeTickView : MonoBehaviour
    {
        private TimeViewModel _timeViewModel;
        private GameStartViewModel _gameStartViewModel;

        [Inject]
        public void Construct(
            TimeViewModel timeViewModel,
            GameStartViewModel gameStartViewModel)
        {
            _timeViewModel = timeViewModel;
            _gameStartViewModel = gameStartViewModel;
        }

        private void Update()
        {
            if (!_gameStartViewModel.IsPlaying.CurrentValue)
            {
                return;
            }

            _timeViewModel.Tick(Time.deltaTime);
        }
    }
}
