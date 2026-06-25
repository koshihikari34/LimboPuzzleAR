using LimboPuzzleAR.Common.Services;
using R3;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace LimboPuzzleAR.Title.ViewModels
{
    /// <summary>
    /// Title画面の表示情報とSTART操作を扱う。
    /// </summary>
    public sealed class TitleViewModel
    {
        private readonly IScoreRepository _scoreRepository;
        private readonly string _mainSceneName;
        private readonly ReactiveProperty<int> _highScore = new();

        public TitleViewModel(
            IScoreRepository scoreRepository,
            string mainSceneName)
        {
            _scoreRepository = scoreRepository;
            _mainSceneName = mainSceneName;
            _highScore.Value = _scoreRepository.LoadHighScore();
        }

        public ReadOnlyReactiveProperty<int> HighScore => _highScore;

        public void StartGame()
        {
            SceneManager.LoadScene(_mainSceneName);
        }

        public void ExitGame()
        {
#if UNITY_EDITOR
            // Editor確認ではアプリ終了の代わりにPlayを止める。
            EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
