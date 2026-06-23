using LimboPuzzleAR.Main.ViewModels;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;

namespace LimboPuzzleAR.Main.Views
{
    /// <summary>
    /// Gameビュー上の設置入力をViewModelへ渡す。
    /// 設置位置の計算やゲーム状態の判断は行わない。
    /// </summary>
    public sealed class ARInputView : MonoBehaviour
    {
        private PlacementViewModel _viewModel;

        [Inject]
        public void Construct(PlacementViewModel viewModel)
        {
            _viewModel = viewModel;
        }

        private void Update()
        {
            if (_viewModel == null || _viewModel.IsPlaced.CurrentValue)
            {
                return;
            }

            if (TryGetPointerDownPosition(out var screenPosition))
            {
                _viewModel.TryPlace(screenPosition);
            }
        }

        private static bool TryGetPointerDownPosition(out Vector2 screenPosition)
        {
            var touchscreen = Touchscreen.current;
            if (touchscreen != null && touchscreen.primaryTouch.press.wasPressedThisFrame)
            {
                screenPosition = touchscreen.primaryTouch.position.ReadValue();
                return true;
            }

            var mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            {
                screenPosition = mouse.position.ReadValue();
                return true;
            }

            screenPosition = default;
            return false;
        }
    }
}
