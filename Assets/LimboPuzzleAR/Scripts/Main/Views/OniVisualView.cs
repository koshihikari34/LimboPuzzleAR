using LimboPuzzleAR.Main.Models;
using LimboPuzzleAR.Main.ViewModels;
using R3;
using UnityEngine;
using VContainer;

namespace LimboPuzzleAR.Main.Views
{
    /// <summary>
    /// 鬼ステートを3Dモデルの向きと仮アニメーションへ反映する。
    /// </summary>
    public sealed class OniVisualView : MonoBehaviour
    {
        [SerializeField] private Transform visualRoot;
        [SerializeField] private Animator animator;
        [SerializeField] private RuntimeAnimatorController idleController;
        [SerializeField] private RuntimeAnimatorController safeController;
        [SerializeField] private RuntimeAnimatorController warningController;
        [SerializeField] private RuntimeAnimatorController watchingController;
        [SerializeField] private RuntimeAnimatorController attackController;
        [SerializeField] private Vector3 idleEulerAngles = Vector3.zero;
        [SerializeField] private Vector3 safeEulerAngles = Vector3.zero;
        [SerializeField] private Vector3 warningEulerAngles = new(0f, -180f, 0f);
        [SerializeField] private Vector3 watchingEulerAngles = new(0f, -180f, 0f);
        [SerializeField] private Vector3 attackEulerAngles = new(0f, -180f, 0f);
        [SerializeField] private Vector3 initialEulerAngles = new(0f, 180f, 0f);
        [SerializeField, Min(1f)] private float rotationDegreesPerSecond = 240f;
        [SerializeField, Min(1f)] private float warningRotationDegreesPerSecond = 90f;
        [SerializeField, Min(0.01f)] private float stopAngleThresholdDegrees = 0.5f;

        private OniViewModel _oniViewModel;
        private PlacementModel _placementModel;
        private GameStartViewModel _gameStartViewModel;
        private OniState _currentState = OniState.Idle;
        private Vector3 _targetEulerAngles;
        private float _currentYawDegrees;
        private float _targetYawDegrees;

        [Inject]
        public void Construct(
            OniViewModel oniViewModel,
            PlacementModel placementModel,
            GameStartViewModel gameStartViewModel)
        {
            _oniViewModel = oniViewModel;
            _placementModel = placementModel;
            _gameStartViewModel = gameStartViewModel;
        }

        private void Start()
        {
            _targetEulerAngles = initialEulerAngles;
            _currentYawDegrees = initialEulerAngles.y;
            _targetYawDegrees = initialEulerAngles.y;
            ApplyCurrentRotation();

            _oniViewModel.CurrentState
                .Subscribe(ApplyState)
                .AddTo(this);

            _gameStartViewModel.IsCountdownVisible
                .Subscribe(_ => ApplyState(_currentState))
                .AddTo(this);

            _gameStartViewModel.IsPlaying
                .Subscribe(_ => ApplyState(_currentState))
                .AddTo(this);
        }

        private void Update()
        {
            if (visualRoot == null)
            {
                return;
            }

            var remainingAngle = Mathf.Abs(_targetYawDegrees - _currentYawDegrees);
            if (remainingAngle <= stopAngleThresholdDegrees)
            {
                _currentYawDegrees = _targetYawDegrees;
                ApplyCurrentRotation();
                return;
            }

            _currentYawDegrees = Mathf.MoveTowards(
                _currentYawDegrees,
                _targetYawDegrees,
                GetCurrentRotationDegreesPerSecond() * Time.deltaTime);
            ApplyCurrentRotation();
        }

        private void ApplyState(OniState state)
        {
            _currentState = state;
            RefreshTargetRotation();

            if (animator == null)
            {
                return;
            }

            var controller = GetController(state);
            if (controller == null)
            {
                return;
            }

            if (animator.runtimeAnimatorController != controller)
            {
                animator.runtimeAnimatorController = controller;
            }

            animator.Play("anim", 0, 0f);
        }

        private void RefreshTargetRotation()
        {
            // 仕様: 鬼ステートに応じて背を向ける/振り返る/攻撃する見た目へ切り替える。
            if (visualRoot != null)
            {
                _targetEulerAngles = GetEulerAngles(_currentState);
                _targetYawDegrees = GetTargetYawDegrees(_targetEulerAngles.y);
            }
        }

        private void ApplyCurrentRotation()
        {
            if (visualRoot == null)
            {
                return;
            }

            visualRoot.localRotation = Quaternion.Euler(
                _targetEulerAngles.x,
                _currentYawDegrees,
                _targetEulerAngles.z);
        }

        private Vector3 GetEulerAngles(OniState state)
        {
            if (state == OniState.Idle
                && !_gameStartViewModel.IsCountdownVisible.CurrentValue
                && !_gameStartViewModel.IsPlaying.CurrentValue)
            {
                return initialEulerAngles;
            }

            if (state == OniState.Attack && TryGetAttackEulerAngles(out var attackLookEulerAngles))
            {
                return attackLookEulerAngles;
            }

            return state switch
            {
                OniState.Idle => idleEulerAngles,
                OniState.Safe => safeEulerAngles,
                OniState.Warning => warningEulerAngles,
                OniState.Watching => watchingEulerAngles,
                OniState.Attack => attackEulerAngles,
                _ => safeEulerAngles
            };
        }

        private float GetTargetYawDegrees(float rawTargetYawDegrees)
        {
            if (_currentState == OniState.Attack)
            {
                return _currentYawDegrees + Mathf.DeltaAngle(_currentYawDegrees, rawTargetYawDegrees);
            }

            // 通常サイクルは毎回同じ向きへ回し、Warning中にWatching正面まで近づける。
            var targetYawDegrees = rawTargetYawDegrees;
            while (targetYawDegrees < _currentYawDegrees)
            {
                targetYawDegrees += 360f;
            }

            return targetYawDegrees;
        }

        private float GetCurrentRotationDegreesPerSecond()
        {
            return _currentState == OniState.Warning
                ? warningRotationDegreesPerSecond
                : rotationDegreesPerSecond;
        }

        private bool TryGetAttackEulerAngles(out Vector3 eulerAngles)
        {
            eulerAngles = attackEulerAngles;

            if (!_placementModel.IsPlaced.CurrentValue || visualRoot == null)
            {
                return false;
            }

            var direction = _placementModel.StackPose.CurrentValue.position - visualRoot.position;
            direction.y = 0f;
            if (direction.sqrMagnitude <= Mathf.Epsilon)
            {
                return false;
            }

            var localDirection = visualRoot.parent != null
                ? visualRoot.parent.InverseTransformDirection(direction)
                : direction;

            // Attackはプレイヤーではなく、設置済みの積み場へ向いて石を崩す予備動作にする。
            var attackYawDegrees = Mathf.Atan2(localDirection.x, localDirection.z) * Mathf.Rad2Deg;
            eulerAngles = new Vector3(attackEulerAngles.x, attackYawDegrees, attackEulerAngles.z);
            return true;
        }

        private RuntimeAnimatorController GetController(OniState state)
        {
            return state switch
            {
                OniState.Idle => idleController,
                OniState.Safe => safeController,
                OniState.Warning => warningController,
                OniState.Watching => watchingController,
                OniState.Attack => attackController,
                _ => safeController
            };
        }
    }
}
