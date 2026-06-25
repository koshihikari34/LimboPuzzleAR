using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class SafeArea : MonoBehaviour
{
    private RectTransform _rectTransform;
    private Rect _lastSafeArea = new Rect(0, 0, 0, 0);
    private Vector2 _lastScreenSize = new Vector2(0, 0);

    void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        Refresh();
    }

    void Update()
    {
        // 縦固定のため、画面サイズやセーフエリアの変更（折りたたみスマホの開閉など）のみ監視
        if (_lastSafeArea != Screen.safeArea ||
            _lastScreenSize.x != Screen.width ||
            _lastScreenSize.y != Screen.height)
        {
            Refresh();
        }
    }

    void Refresh()
    {
        Rect safeArea = Screen.safeArea;

        // 現在の状態を記録
        _lastSafeArea = safeArea;
        _lastScreenSize = new Vector2(Screen.width, Screen.height);

        // 画面全体に対するセーフエリアの割合（0.0 〜 1.0）を計算
        Vector2 anchorMin = safeArea.position;
        Vector2 anchorMax = safeArea.position + safeArea.size;

        anchorMin.x /= Screen.width;
        anchorMin.y /= Screen.height;
        anchorMax.x /= Screen.width;
        anchorMax.y /= Screen.height;

        // RectTransformのアンカー（Anchor）に適用する
        _rectTransform.anchorMin = anchorMin;
        _rectTransform.anchorMax = anchorMax;
    }
}