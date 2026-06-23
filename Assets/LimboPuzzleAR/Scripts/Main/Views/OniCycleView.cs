using System.Collections;
using LimboPuzzleAR.Main.Models;
using LimboPuzzleAR.Main.ViewModels;
using R3;
using UnityEngine;
using VContainer;

namespace LimboPuzzleAR.Main.Views
{
    /// <summary>
    /// 鬼ステートを時間経過で進める。
    /// </summary>
    public sealed class OniCycleView : MonoBehaviour
    {
        [SerializeField] private bool autoCycle = true;
        [SerializeField, Min(0.1f)] private float safeSeconds = 3f;
        [SerializeField, Min(0.1f)] private float warningSeconds = 1f;
        [SerializeField, Min(0.1f)] private float watchingSeconds = 2f;
        [SerializeField, Min(0.1f)] private float attackSeconds = 2f;
        [SerializeField, Min(0f)] private float safeReductionPerClearSeconds = 0.2f;
        [SerializeField, Min(0.1f)] private float minimumSafeSeconds = 1.8f;
        [SerializeField, Min(0f)] private float watchingIncreasePerClearSeconds = 0.1f;
        [SerializeField, Min(0.1f)] private float maximumWatchingSeconds = 3f;

        private OniViewModel _oniViewModel;
        private TimeViewModel _timeViewModel;
        private GameStartViewModel _gameStartViewModel;
        private ScoreModel _scoreModel;
        private Coroutine _cycleCoroutine;

        [Inject]
        public void Construct(
            OniViewModel oniViewModel,
            TimeViewModel timeViewModel,
            GameStartViewModel gameStartViewModel,
            ScoreModel scoreModel)
        {
            _oniViewModel = oniViewModel;
            _timeViewModel = timeViewModel;
            _gameStartViewModel = gameStartViewModel;
            _scoreModel = scoreModel;
        }

        private void Start()
        {
            _oniViewModel.CurrentState
                .Subscribe(_ => RestartCycle())
                .AddTo(this);

            _timeViewModel.IsTimeUp
                .Where(isTimeUp => isTimeUp)
                .Subscribe(_ => StopCycle())
                .AddTo(this);

            _gameStartViewModel.IsPlaying
                .Where(isPlaying => isPlaying)
                .Subscribe(_ => StartSafeCycle())
                .AddTo(this);
        }

        private void StartSafeCycle()
        {
            if (_oniViewModel.CurrentState.CurrentValue == OniState.Idle)
            {
                _oniViewModel.SetState(OniState.Safe);
                return;
            }

            RestartCycle();
        }

        private void RestartCycle()
        {
            if (!autoCycle
                || !_gameStartViewModel.IsPlaying.CurrentValue
                || _timeViewModel.IsTimeUp.CurrentValue)
            {
                return;
            }

            if (_cycleCoroutine != null)
            {
                StopCoroutine(_cycleCoroutine);
            }

            _cycleCoroutine = StartCoroutine(WaitAndAdvance());
        }

        private void StopCycle()
        {
            if (_cycleCoroutine == null)
            {
                return;
            }

            StopCoroutine(_cycleCoroutine);
            _cycleCoroutine = null;
        }

        private IEnumerator WaitAndAdvance()
        {
            var state = _oniViewModel.CurrentState.CurrentValue;
            yield return new WaitForSeconds(GetDurationSeconds(state));

            if (!_gameStartViewModel.IsPlaying.CurrentValue || _timeViewModel.IsTimeUp.CurrentValue)
            {
                _cycleCoroutine = null;
                yield break;
            }

            if (state == OniState.Attack)
            {
                _oniViewModel.ReturnToSafe();
            }
            else
            {
                // 状態の順序はOniStateMachineへ寄せ、Viewは時間到達だけを通知する。
                _oniViewModel.AdvanceCycle();
            }

            _cycleCoroutine = null;
        }

        private float GetDurationSeconds(OniState state)
        {
            return state switch
            {
                OniState.Safe => GetScaledSafeSeconds(),
                OniState.Warning => warningSeconds,
                OniState.Watching => GetScaledWatchingSeconds(),
                OniState.Attack => attackSeconds,
                _ => safeSeconds
            };
        }

        private float GetScaledSafeSeconds()
        {
            // クリア数に応じて安全時間を短くし、後半ほど鬼が早く振り返る。
            var scaledSeconds = safeSeconds - _scoreModel.ClearSetCount.CurrentValue * safeReductionPerClearSeconds;
            return Mathf.Max(minimumSafeSeconds, scaledSeconds);
        }

        private float GetScaledWatchingSeconds()
        {
            // クリア数に応じて監視時間を伸ばし、Releaseできない時間を少しずつ増やす。
            var scaledSeconds = watchingSeconds + _scoreModel.ClearSetCount.CurrentValue * watchingIncreasePerClearSeconds;
            return Mathf.Min(maximumWatchingSeconds, scaledSeconds);
        }
    }
}
