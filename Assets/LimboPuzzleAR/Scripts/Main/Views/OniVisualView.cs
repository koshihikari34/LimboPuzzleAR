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
        [SerializeField] private Vector3 idleEulerAngles = new(0f, 180f, 0f);
        [SerializeField] private Vector3 safeEulerAngles = new(0f, 180f, 0f);
        [SerializeField] private Vector3 warningEulerAngles = new(0f, 90f, 0f);
        [SerializeField] private Vector3 watchingEulerAngles = Vector3.zero;
        [SerializeField] private Vector3 attackEulerAngles = Vector3.zero;
        [SerializeField, Min(0.01f)] private float rotationSpeed = 8f;

        private OniViewModel _oniViewModel;
        private Quaternion _targetRotation;

        [Inject]
        public void Construct(OniViewModel oniViewModel)
        {
            _oniViewModel = oniViewModel;
        }

        private void Start()
        {
            _targetRotation = visualRoot != null ? visualRoot.localRotation : Quaternion.identity;

            _oniViewModel.CurrentState
                .Subscribe(ApplyState)
                .AddTo(this);
        }

        private void Update()
        {
            if (visualRoot == null)
            {
                return;
            }

            visualRoot.localRotation = Quaternion.Slerp(
                visualRoot.localRotation,
                _targetRotation,
                rotationSpeed * Time.deltaTime);
        }

        private void ApplyState(OniState state)
        {
            // 仕様: 鬼ステートに応じて背を向ける/振り返る/攻撃する見た目へ切り替える。
            if (visualRoot != null)
            {
                _targetRotation = Quaternion.Euler(GetEulerAngles(state));
            }

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

        private Vector3 GetEulerAngles(OniState state)
        {
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
