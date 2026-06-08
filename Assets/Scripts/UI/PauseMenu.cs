using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    GameObject _root, _mainPanel, _settingsPanel, _controlsPanel, _resumeBtn;
    bool _open;

    void Awake()
    {
        EnsureEventSystem();
        BuildUI();
        _root.SetActive(false);
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null || !kb.escapeKey.wasPressedThisFrame) return;
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver) return;

        if (!_open && (InventoryUI.IsOpen || MazeMap.IsOpen || (ChestUI.Instance != null && ChestUI.Instance.IsOpen))) return;

        if (_open && (_settingsPanel.activeSelf || _controlsPanel.activeSelf)) { ShowMain(); return; }
        SetOpen(!_open);
    }

    void SetOpen(bool open)
    {
        _open = open;
        _root.SetActive(open);
        if (open) ShowMain();
        if (GameManager.Instance != null) GameManager.Instance.SetPaused(open);
        Cursor.lockState = open ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = open;
    }

    void ShowMain()
    {
        _mainPanel.SetActive(true);
        _settingsPanel.SetActive(false);
        _controlsPanel.SetActive(false);
        UIKit.SelectFirst(_resumeBtn);
    }

    void ShowSettings()
    {
        _mainPanel.SetActive(false);
        _controlsPanel.SetActive(false);
        _settingsPanel.SetActive(true);
        UIKit.SelectFirst(_settingsPanel);
    }

    void ShowControls()
    {
        _mainPanel.SetActive(false);
        _settingsPanel.SetActive(false);
        _controlsPanel.SetActive(true);
        UIKit.SelectFirst(_controlsPanel);
    }

    void BuildUI()
    {
        var canvasGo = new GameObject("PauseCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        _root = UIKit.NewRect("Root", canvasGo.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        UIKit.Stretch((RectTransform)_root.transform);
        _root.AddComponent<Image>().color = UIKit.Dim;

        _mainPanel = UIKit.Box("MainPanel", _root.transform, new Vector2(460f, 620f));
        UIKit.Title(_mainPanel.transform, "PAUSED", 250f);
        _resumeBtn = UIKit.Button(_mainPanel.transform, 0f, 150f,  320f, 60f, "Resume",    () => SetOpen(false)).gameObject;
        UIKit.Button(_mainPanel.transform, 0f, 80f,   320f, 60f, "Controls",  ShowControls);
        UIKit.Button(_mainPanel.transform, 0f, 10f,   320f, 60f, "Settings",  ShowSettings);
        UIKit.Button(_mainPanel.transform, 0f, -60f,  320f, 60f, "Restart",
            () => UIKit.Confirm(_root.transform, "Restart level? Progress will be lost.", RestartLevel));
        UIKit.Button(_mainPanel.transform, 0f, -130f, 320f, 60f, "Main Menu",
            () => UIKit.Confirm(_root.transform, "Return to main menu? Progress will be lost.", () => LoadScene(GameScenes.MainMenu)));
        UIKit.Button(_mainPanel.transform, 0f, -200f, 320f, 60f, "Quit",
            () => UIKit.Confirm(_root.transform, "Quit the game?", Quit));

        _settingsPanel = SettingsPanel.Create(_root.transform, ShowMain);
        _controlsPanel = ControlsPanel.Create(_root.transform, ShowMain);
    }

    void RestartLevel()
    {
        if (GameManager.Instance != null) GameManager.Instance.SetPaused(false);
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    void EnsureEventSystem()
    {
        if (EventSystem.current != null) return;
        if (FindFirstObjectByType<EventSystem>() != null) return;
        var es = new GameObject("EventSystem", typeof(EventSystem));
        es.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
    }

    void LoadScene(string sceneName)
    {
        if (GameManager.Instance != null) GameManager.Instance.SetPaused(false);
        Time.timeScale = 1f;
        SceneManager.LoadScene(sceneName);
    }

    void Quit()
    {
        Time.timeScale = 1f;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
