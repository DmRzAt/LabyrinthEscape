using System.Collections;
using UnityEngine;

public class CombatFeedback : MonoBehaviour
{
    private static CombatFeedback _instance;
    private bool _running;
    private float _restore = 1f;

    private static CombatFeedback Instance
    {
        get
        {
            if (_instance == null)
            {
                var go = new GameObject("CombatFeedback");
                _instance = go.AddComponent<CombatFeedback>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    public static void HitStop(float seconds)
    {
        if (seconds <= 0f) return;
        Instance.Begin(seconds);
    }

    private void Begin(float seconds)
    {
        if (Time.timeScale <= 0.01f) return;
        if (_running) return;
        StartCoroutine(Routine(seconds));
    }

    private IEnumerator Routine(float seconds)
    {
        _running = true;
        _restore = Time.timeScale;
        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(seconds);
        bool paused = GameManager.Instance != null && GameManager.Instance.IsPaused;
        if (Time.timeScale <= 0.01f && !paused) Time.timeScale = _restore;
        _running = false;
    }
}
