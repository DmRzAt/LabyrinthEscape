using UnityEngine;
using TMPro;

public class ControlsPanel : MonoBehaviour
{
    static readonly (string key, string action)[] Rows =
    {
        ("WASD / Arrows", "Move"),
        ("Mouse",         "Look"),
        ("Shift",         "Sprint"),
        ("C",             "Crouch"),
        ("Space",         "Jump"),
        ("Left Ctrl",     "Dash"),
        ("Left Mouse",    "Attack  (hold = heavy)"),
        ("Right Mouse",   "Block / Parry"),
        ("E",             "Interact"),
        ("1 - 6",         "Hotbar slots"),
        ("Esc",           "Pause"),
    };

    public static GameObject Create(Transform parent, System.Action onBack)
    {
        var box = UIKit.Box("ControlsPanel", parent, new Vector2(820f, 760f));
        var sp = box.AddComponent<ControlsPanel>();
        sp.Build(box.transform, onBack);
        return box;
    }

    void Build(Transform panel, System.Action onBack)
    {
        UIKit.Title(panel, "CONTROLS", 320f);

        float top = 230f, step = 50f;
        for (int i = 0; i < Rows.Length; i++)
        {
            float y = top - i * step;

            var key = UIKit.Text(panel, Rows[i].key, 24f, FontStyles.Bold, TextAlignmentOptions.Right);
            var krt = key.rectTransform;
            krt.anchorMin = krt.anchorMax = new Vector2(0.5f, 0.5f); krt.pivot = new Vector2(1f, 0.5f);
            krt.anchoredPosition = new Vector2(-30f, y); krt.sizeDelta = new Vector2(340f, 34f);
            key.color = UIKit.Edge;

            var act = UIKit.Text(panel, Rows[i].action, 24f, FontStyles.Normal, TextAlignmentOptions.Left);
            var art = act.rectTransform;
            art.anchorMin = art.anchorMax = new Vector2(0.5f, 0.5f); art.pivot = new Vector2(0f, 0.5f);
            art.anchoredPosition = new Vector2(30f, y); art.sizeDelta = new Vector2(380f, 34f);
        }

        UIKit.Button(panel, 0f, -330f, 320f, 64f, "Back", () => onBack?.Invoke());
    }
}
