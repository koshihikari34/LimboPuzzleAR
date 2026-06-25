using System.Collections;
using LimboPuzzleAR.Main.ViewModels;
using R3;
using UnityEngine;
using VContainer;

namespace LimboPuzzleAR.Main.Views
{
    /// <summary>
    /// プレイエリア設置後、鬼のスポーン演出を終えてから開始カウントダウンへ進める。
    /// </summary>
    public sealed class OniSpawnView : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private RuntimeAnimatorController spawnController;
        [SerializeField, Min(0f)] private float spawnSeconds = 1.5f;

        private PlacementViewModel _placementViewModel;
        private GameStartViewModel _gameStartViewModel;
        private Coroutine _spawnCoroutine;
        private bool _hasStartedSpawn;

        [Inject]
        public void Construct(
            PlacementViewModel placementViewModel,
            GameStartViewModel gameStartViewModel)
        {
            _placementViewModel = placementViewModel;
            _gameStartViewModel = gameStartViewModel;
        }

        private void Start()
        {
            _placementViewModel.IsPlaced
                .Where(isPlaced => isPlaced)
                .Subscribe(_ => StartSpawnSequence())
                .AddTo(this);
        }

        private void StartSpawnSequence()
        {
            if (_hasStartedSpawn)
            {
                return;
            }

            _hasStartedSpawn = true;
            if (_spawnCoroutine != null)
            {
                StopCoroutine(_spawnCoroutine);
            }

            _spawnCoroutine = StartCoroutine(SpawnAndStartCountdown());
        }

        private IEnumerator SpawnAndStartCountdown()
        {
            if (animator != null && spawnController != null)
            {
                animator.runtimeAnimatorController = spawnController;
                animator.Play("anim", 0, 0f);
            }

            if (spawnSeconds > 0f)
            {
                yield return new WaitForSeconds(spawnSeconds);
            }

            _gameStartViewModel.StartCountdown();
            _spawnCoroutine = null;
        }
    }
}
