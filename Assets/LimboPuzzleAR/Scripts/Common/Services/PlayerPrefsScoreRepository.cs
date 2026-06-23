using UnityEngine;

namespace LimboPuzzleAR.Common.Services
{
    /// <summary>
    /// PlayerPrefsを使ってハイスコアを保存する。
    /// </summary>
    public sealed class PlayerPrefsScoreRepository : IScoreRepository
    {
        private const string HighScoreKey = "LimboPuzzleAR.HighScore";

        public int LoadHighScore()
        {
            return PlayerPrefs.GetInt(HighScoreKey, 0);
        }

        public void SaveHighScore(int score)
        {
            PlayerPrefs.SetInt(HighScoreKey, score);
            PlayerPrefs.Save();
        }
    }
}
