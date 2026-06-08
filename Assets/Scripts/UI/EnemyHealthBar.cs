using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class EnemyHealthBar : MonoBehaviour
{
    static readonly Color ColBack = new Color(0.02f, 0.015f, 0.012f, 0.85f);
    static readonly Color ColFillHigh = new Color(0.82f, 0.08f, 0.07f, 1f);
    static readonly Color ColFillLow = new Color(0.45f, 0.02f, 0.02f, 1f);

    EnemyHealth _health;
    Canvas _canvas;
    CanvasGroup _group;
    RectTransform _fill;
    Image _fillImage;
    Transform _cam;
    float _height = 2.2f;

    public void Init(EnemyHealth health, float height)
    {
        _health = health;
        _height = height;
        Build();
    }

    void Build()
    {
        var go = new GameObject("HPBar");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = Vector3.up * _height;

        _canvas = go.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.WorldSpace;
        var crt = (RectTransform)_canvas.transform;
        crt.sizeDelta = new Vector2(120f, 16f);
        crt.localScale = Vector3.one * 0.006f;

        _group = go.AddComponent<CanvasGroup>();
        _group.alpha = 0f;
        _group.interactable = false;
        _group.blocksRaycasts = false;

        var bg = NewImage(go.transform, ColBack);
        Stretch(bg.rectTransform);
        var outline = bg.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.9f);
        outline.effectDistance = new Vector2(1.5f, -1.5f);

        var area = new GameObject("FillArea", typeof(RectTransform));
        area.transform.SetParent(bg.transform, false);
        var art = (RectTransform)area.transform;
        art.anchorMin = Vector2.zero; art.anchorMax = Vector2.one;
        art.offsetMin = new Vector2(2f, 2f); art.offsetMax = new Vector2(-2f, -2f);

        _fillImage = NewImage(area.transform, ColFillHigh);
        _fill = _fillImage.rectTransform;
        _fill.anchorMin = Vector2.zero;
        _fill.anchorMax = Vector2.one;
        _fill.offsetMin = Vector2.zero;
        _fill.offsetMax = Vector2.zero;
    }

    static Image NewImage(Transform parent, Color c)
    {
        var go = new GameObject("Img", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = c;
        img.raycastTarget = false;
        return img;
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
    }

    void LateUpdate()
    {
        if (_health == null || _canvas == null) return;

        int max = Mathf.Max(1, _health.maxHP);
        float ratio = Mathf.Clamp01((float)_health.currentHP / max);

        float target = (!_health.IsDead && ratio < 0.999f) ? 1f : 0f;
        _group.alpha = Mathf.MoveTowards(_group.alpha, target, Time.deltaTime * 6f);

        if (_fill != null) _fill.anchorMax = new Vector2(ratio, 1f);
        if (_fillImage != null) _fillImage.color = Color.Lerp(ColFillLow, ColFillHigh, ratio);

        if (_group.alpha <= 0.001f) return;

        if (_cam == null && Camera.main != null) _cam = Camera.main.transform;
        if (_cam != null)
        {
            Vector3 pos = _canvas.transform.position;
            _canvas.transform.rotation = Quaternion.LookRotation(pos - _cam.position);
        }
    }
}
