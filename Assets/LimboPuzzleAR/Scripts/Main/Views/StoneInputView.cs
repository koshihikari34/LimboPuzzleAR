using LimboPuzzleAR.Main.ViewModels;
using R3;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using VContainer;

namespace LimboPuzzleAR.Main.Views
{
    /// <summary>
    /// 石操作用のポインター入力をViewModelへ渡す。
    /// </summary>
    public sealed class StoneInputView : MonoBehaviour
    {
        [SerializeField] private Button nextStoneButton;

        private StoneViewModel _viewModel;
        private PlacementViewModel _placementViewModel;
        private GameStartViewModel _gameStartViewModel;
        private bool _isDragging;
        private int _placementCompletedFrame = -1;

        [Inject]
        public void Construct(
            StoneViewModel viewModel,
            PlacementViewModel placementViewModel,
            GameStartViewModel gameStartViewModel)
        {
            _viewModel = viewModel;
            _placementViewModel = placementViewModel;
            _gameStartViewModel = gameStartViewModel;
        }

        private void Start()
        {
            _placementViewModel.IsPlaced
                .Where(isPlaced => isPlaced)
                .Subscribe(_ => _placementCompletedFrame = Time.frameCount)
                .AddTo(this);

            _viewModel.CanSpawnStone
                .Subscribe(ApplyNextStoneButton)
                .AddTo(this);

            _gameStartViewModel.IsPlaying
                .Subscribe(_ => ApplyNextStoneButton(_viewModel.CanSpawnStone.CurrentValue))
                .AddTo(this);
        }

        private void Update()
        {
            if (TryGetPointerDownPosition(out var downPosition))
            {
                TryBeginDrag(downPosition);
                return;
            }

            if (!_isDragging)
            {
                return;
            }

            if (TryGetPointerPosition(out var position))
            {
                _viewModel.TryMoveHold(position);
            }

            if (WasPointerReleased())
            {
                _viewModel.TryRelease();
                _isDragging = false;
            }
        }

        private void TryBeginDrag(Vector2 screenPosition)
        {
            if (_placementCompletedFrame == Time.frameCount)
            {
                return;
            }

            // 仕様: 石の供給はNextStoneボタンを押した位置から開始する。
            if (!IsInNextStoneButton(screenPosition))
            {
                return;
            }

            _isDragging = _viewModel.TryBeginHold(screenPosition);
        }

        private bool IsInNextStoneButton(Vector2 screenPosition)
        {
            if (nextStoneButton == null)
            {
                return false;
            }

            return nextStoneButton.transform is RectTransform buttonRect
                && RectTransformUtility.RectangleContainsScreenPoint(buttonRect, screenPosition);
        }

        private void ApplyNextStoneButton(bool canSpawnStone)
        {
            if (nextStoneButton == null)
            {
                return;
            }

            nextStoneButton.interactable = canSpawnStone && _gameStartViewModel.IsPlaying.CurrentValue;
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

        private static bool TryGetPointerPosition(out Vector2 screenPosition)
        {
            var touchscreen = Touchscreen.current;
            if (touchscreen != null && touchscreen.primaryTouch.press.isPressed)
            {
                screenPosition = touchscreen.primaryTouch.position.ReadValue();
                return true;
            }

            var mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.isPressed)
            {
                screenPosition = mouse.position.ReadValue();
                return true;
            }

            screenPosition = default;
            return false;
        }

        private static bool WasPointerReleased()
        {
            var touchscreen = Touchscreen.current;
            if (touchscreen != null && touchscreen.primaryTouch.press.wasReleasedThisFrame)
            {
                return true;
            }

            var mouse = Mouse.current;
            return mouse != null && mouse.leftButton.wasReleasedThisFrame;
        }
    }
}
