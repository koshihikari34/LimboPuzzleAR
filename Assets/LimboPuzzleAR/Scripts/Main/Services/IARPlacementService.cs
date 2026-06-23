using UnityEngine;

namespace LimboPuzzleAR.Main.Services
{
    /// <summary>
    /// 画面座標をプレイエリアの設置候補へ変換する境界。
    /// 実機のAR RaycastとEditor用入力を同じ契約で扱う。
    /// </summary>
    public interface IARPlacementService
    {
        bool TryGetPlacementPose(Vector2 screenPosition, out Pose pose);
    }
}
