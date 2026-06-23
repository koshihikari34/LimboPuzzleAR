namespace LimboPuzzleAR.Common.Services
{
    /// <summary>
    /// スコア永続化の境界を表す。
    /// </summary>
    public interface IScoreRepository
    {
        int LoadHighScore();

        void SaveHighScore(int score);
    }
}
