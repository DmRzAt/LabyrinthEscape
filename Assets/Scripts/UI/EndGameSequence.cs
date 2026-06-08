using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class EndGameSequence : MonoBehaviour
{
    static EndGameSequence _instance;

    CanvasGroup _group;
    AudioSource _audio;
    float _targetAlpha;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => _instance = null;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (_instance != null) return;
        var go = new GameObject("EndGameSequence");
        DontDestroyOnLoad(go);
        _instance = go.AddComponent<EndGameSequence>();
        _instance.Build();
    }

    void Build()
    {
        var canvasGO = new GameObject("FadeCanvas");
        canvasGO.transform.SetParent(transform, false);
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 5000;

        var imgGO = new GameObject("Fade", typeof(RectTransform));
        imgGO.transform.SetParent(canvasGO.transform, false);
        var img = imgGO.AddComponent<Image>();
        img.color = Color.black;
        img.raycastTarget = false;
        var rt = img.rectTransform;
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

        _group = imgGO.AddComponent<CanvasGroup>();
        _group.alpha = 0f;
        _group.blocksRaycasts = false;
        _group.interactable = false;

        _audio = gameObject.AddComponent<AudioSource>();
        _audio.playOnAwake = false;
        _audio.spatialBlend = 0f;
    }

    void OnEnable()
    {
        GameManager.OnGameWon += OnWon;
        GameManager.OnGameLost += OnLost;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        GameManager.OnGameWon -= OnWon;
        GameManager.OnGameLost -= OnLost;
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnWon()
    {
        _targetAlpha = 1f;
        if (_audio != null) _audio.PlayOneShot(ProceduralSfx.Chime(523f, 1568f, 0.7f, 0.6f));
    }

    void OnLost() => _targetAlpha = 1f;

    void OnSceneLoaded(Scene s, LoadSceneMode m) => _targetAlpha = 0f;

    void Update()
    {
        if (_group == null) return;
        _group.alpha = Mathf.MoveTowards(_group.alpha, _targetAlpha, Time.unscaledDeltaTime / 1.2f);
    }
}
