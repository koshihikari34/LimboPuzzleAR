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

        [Inject]
        public void Construct(TimeViewModel timeViewModel)
        {
            _timeViewModel = timeViewModel;
        }

        private void Update()
        {
            _timeViewModel.Tick(Time.deltaTime);
        }
    }
}
