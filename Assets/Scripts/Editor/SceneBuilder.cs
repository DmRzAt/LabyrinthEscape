using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using System.IO;

public static class SceneBuilder
{
    const string SCENES_DIR = "Assets/Scenes";

    [MenuItem("Tools/Scene Builder/Build MainMenuScene")]
    public static void BuildMainMenu()
    {
        EnsureDir();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        AddBasicLighting();
        AddGameManager();

        var canvas = CreateCanvas("MainMenuCanvas", out var es);

        var bg = CreateImage(canvas.transform, "Background", Vector2.zero, new Vector2(1920, 1080), new Color(0.05f, 0.05f, 0.08f, 1f));
        var bgRT = bg.GetComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero; bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = Vector2.zero; bgRT.offsetMax = Vector2.zero;
        bg.name = "Background_DropImageHere";

        var vignette = CreateImage(canvas.transform, "Vignette", Vector2.zero, new Vector2(1920, 1080), new Color(0, 0, 0, 0.55f));
        var vRT = vignette.GetComponent<RectTransform>();
        vRT.anchorMin = Vector2.zero; vRT.anchorMax = Vector2.one;
        vRT.offsetMin = Vector2.zero; vRT.offsetMax = Vector2.zero;

        var menuObj = new GameObject("MainMenu");
        menuObj.transform.SetParent(canvas.transform, false);
        var mm = menuObj.AddComponent<MainMenu>();

        var mainPanel = CreatePanel(canvas.transform, "MainPanel");
        var title = CreateText(mainPanel.transform, "Title", "LABYRINTH ESCAPE", 90, new Vector2(0, 320));
        title.fontStyle = FontStyles.Bold;
        title.color = new Color(0.95f, 0.85f, 0.55f);
        var titleRT = title.GetComponent<RectTransform>();
        titleRT.sizeDelta = new Vector2(1600, 130);

        var subtitle = CreateText(mainPanel.transform, "Subtitle", "- Find your way out -", 30, new Vector2(0, 230));
        subtitle.fontStyle = FontStyles.Italic;
        subtitle.color = new Color(0.7f, 0.65f, 0.5f);
        var subRT = subtitle.GetComponent<RectTransform>();
        subRT.sizeDelta = new Vector2(800, 60);

        var startBtn    = CreateButton(mainPanel.transform, "StartBtn",    "START",    new Vector2(0,   60));
        var settingsBtn = CreateButton(mainPanel.transform, "SettingsBtn", "SETTINGS", new Vector2(0,  -50));
        var quitBtn     = CreateButton(mainPanel.transform, "QuitBtn",     "QUIT",     new Vector2(0, -160));

        var settingsPanel = CreatePanel(canvas.transform, "SettingsPanel");
        settingsPanel.SetActive(false);
        CreateText(settingsPanel.transform, "SettingsTitle", "SETTINGS", 80, new Vector2(0, 280)).fontStyle = FontStyles.Bold;

        var box = CreateImage(settingsPanel.transform, "Box", new Vector2(0, 0), new Vector2(700, 460), new Color(0.05f, 0.05f, 0.08f, 0.85f));

        var volLabel = CreateText(settingsPanel.transform, "VolumeLabel", "Volume: 80%", 36, new Vector2(0, 130), TextAlignmentOptions.Center);
        var volSlider = CreateSlider(settingsPanel.transform, "VolumeSlider", new Vector2(0, 80), new Vector2(500, 30));
        volSlider.minValue = 0; volSlider.maxValue = 1; volSlider.value = 0.8f;
        UnityEditor.Events.UnityEventTools.AddPersistentListener(volSlider.onValueChanged, mm.OnVolumeChanged);

        var senLabel = CreateText(settingsPanel.transform, "SensitivityLabel", "Sensitivity: 2.0", 36, new Vector2(0, 20), TextAlignmentOptions.Center);
        var senSlider = CreateSlider(settingsPanel.transform, "SensitivitySlider", new Vector2(0, -30), new Vector2(500, 30));
        senSlider.minValue = 0.5f; senSlider.maxValue = 6f; senSlider.value = 2f;
        UnityEditor.Events.UnityEventTools.AddPersistentListener(senSlider.onValueChanged, mm.OnSensitivityChanged);

        var fsToggle = CreateToggle(settingsPanel.transform, "FullscreenToggle", "Fullscreen", new Vector2(0, -110));
        UnityEditor.Events.UnityEventTools.AddPersistentListener<bool>(fsToggle.onValueChanged, mm.OnFullscreenChanged);

        var backBtn = CreateButton(settingsPanel.transform, "BackBtn", "BACK", new Vector2(0, -240));

        mm.mainPanel = mainPanel;
        mm.settingsPanel = settingsPanel;
        mm.volumeSlider = volSlider;
        mm.sensitivitySlider = senSlider;
        mm.fullscreenToggle = fsToggle;
        mm.volumeLabel = volLabel;
        mm.sensitivityLabel = senLabel;

        UnityEditor.Events.UnityEventTools.AddPersistentListener(startBtn.onClick,    mm.StartGame);
        UnityEditor.Events.UnityEventTools.AddPersistentListener(settingsBtn.onClick, mm.OpenSettings);
        UnityEditor.Events.UnityEventTools.AddPersistentListener(quitBtn.onClick,     mm.QuitGame);
        UnityEditor.Events.UnityEventTools.AddPersistentListener(backBtn.onClick,     mm.ShowMain);

        SaveScene(scene, "MainMenuScene");
    }

    [MenuItem("Tools/Scene Builder/Build EndScene")]
    public static void BuildEnd()
    {
        EnsureDir();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        AddBasicLighting();

        var canvas = CreateCanvas("EndCanvas", out var es);
        var end = new GameObject("EndScene");
        end.transform.SetParent(canvas.transform, false);
        end.AddComponent<EndScene>();

        CreateText(canvas.transform, "WinText", "YOU WIN!", 120, new Vector2(0, 200));

        var restart = CreateButton(canvas.transform, "RestartBtn",  "RESTART",   new Vector2(0, 0));
        var menu    = CreateButton(canvas.transform, "MenuBtn",     "MAIN MENU", new Vector2(0, -120));

        var es2 = end.GetComponent<EndScene>();
        UnityEditor.Events.UnityEventTools.AddPersistentListener(restart.onClick, es2.Restart);
        UnityEditor.Events.UnityEventTools.AddPersistentListener(menu.onClick,    es2.MainMenu);

        SaveScene(scene, "EndScene");
    }

    [MenuItem("Tools/Scene Builder/Add HUD to GameScene")]
    public static void AddHUDToGameScene()
    {
        var oldCanvas = GameObject.Find("HUD_Canvas");
        if (oldCanvas != null) GameObject.DestroyImmediate(oldCanvas);

        var canvas = CreateCanvas("HUD_Canvas", out var es);
        var hudGO = new GameObject("HUD");
        hudGO.transform.SetParent(canvas.transform, false);
        var hud = hudGO.AddComponent<HUD>();

        var hpText = CreateText(canvas.transform, "HPText", "HP 100/100", 32, Vector2.zero, TextAlignmentOptions.Left);
        AnchorTo(hpText.rectTransform, new Vector2(0, 0), new Vector2(40, 90), new Vector2(320, 40));

        var hpBar = CreateSlider(canvas.transform, "HPSlider", Vector2.zero, new Vector2(320, 24));
        AnchorTo(hpBar.GetComponent<RectTransform>(), new Vector2(0, 0), new Vector2(40, 50), new Vector2(320, 24));

        var keysText = CreateText(canvas.transform, "KeysText", "Keys 0/3", 44, Vector2.zero, TextAlignmentOptions.Right);
        AnchorTo(keysText.rectTransform, new Vector2(1, 1), new Vector2(-40, -50), new Vector2(360, 60));

        var msg = CreateText(canvas.transform, "MessageText", "", 96, Vector2.zero);
        msg.gameObject.SetActive(false);

        hud.hpSlider = hpBar;
        hud.hpText = hpText;
        hud.keysText = keysText;
        hud.messageText = msg;

        AddGameManager();
        var gmGO = GameObject.Find("GameManager");
        if (gmGO != null)
        {
            var gm = gmGO.GetComponent<GameManager>();
            if (gm != null) gm.keysTotal = 3;
        }

        EditorSceneManager.MarkAllScenesDirty();
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("HUD updated: HP bottom left, keys top right, 3 keys total.");
    }

    static void AnchorTo(RectTransform rt, Vector2 anchor, Vector2 anchoredPos, Vector2 size)
    {
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = anchor;
        rt.sizeDelta = size;
        rt.anchoredPosition = anchoredPos;
    }

    [MenuItem("Tools/Scene Builder/Add All Scenes To Build")]
    public static void AddScenesToBuild()
    {
        var paths = new[] { "MainMenuScene", "GameScene", "EndScene" };
        var list = new System.Collections.Generic.List<EditorBuildSettingsScene>();
        foreach (var n in paths)
        {
            string p = $"{SCENES_DIR}/{n}.unity";
            if (File.Exists(p)) list.Add(new EditorBuildSettingsScene(p, true));
        }
        EditorBuildSettings.scenes = list.ToArray();
        Debug.Log($"Scenes added to Build: {list.Count}");
    }

    [MenuItem("Tools/Scene Builder/Set MainMenu as Play Start")]
    public static void SetMainMenuAsPlayStart()
    {
        string p = $"{SCENES_DIR}/MainMenuScene.unity";
        var asset = AssetDatabase.LoadAssetAtPath<SceneAsset>(p);
        if (asset == null) { Debug.LogError("MainMenuScene not found. Build MainMenuScene first."); return; }
        EditorSceneManager.playModeStartScene = asset;
        Debug.Log("Play mode now always starts from MainMenuScene.");
    }

    [MenuItem("Tools/Scene Builder/Reset Play Start")]
    public static void ResetPlayStart()
    {
        EditorSceneManager.playModeStartScene = null;
        Debug.Log("Play mode starts from the active scene.");
    }

    static void EnsureDir()
    {
        if (!Directory.Exists(SCENES_DIR)) Directory.CreateDirectory(SCENES_DIR);
    }

    static void SaveScene(UnityEngine.SceneManagement.Scene scene, string name)
    {
        string path = $"{SCENES_DIR}/{name}.unity";
        EditorSceneManager.SaveScene(scene, path);
        Debug.Log($"Saved: {path}");
    }

    static void AddBasicLighting()
    {
        var lightGO = new GameObject("Directional Light");
        var light = lightGO.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1f;
        lightGO.transform.rotation = Quaternion.Euler(50, -30, 0);

        var camGO = new GameObject("Main Camera");
        camGO.tag = "MainCamera";
        var cam = camGO.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        cam.orthographic = false;
        camGO.AddComponent<AudioListener>();
        camGO.transform.position = new Vector3(0, 1, -10);
    }

    static void AddGameManager()
    {
        if (Object.FindFirstObjectByType<GameManager>() != null) return;
        var go = new GameObject("GameManager");
        go.AddComponent<GameManager>();
    }

    static GameObject CreateCanvas(string name, out EventSystem es)
    {
        var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        es = Object.FindFirstObjectByType<EventSystem>();
        if (es == null)
        {
            var esGO = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            es = esGO.GetComponent<EventSystem>();
        }
        return go;
    }

    static TextMeshProUGUI CreateText(Transform parent, string name, string text, int size, Vector2 anchoredPos, TextAlignmentOptions align = TextAlignmentOptions.Center)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text = text;
        t.fontSize = size;
        t.alignment = align;
        t.color = Color.white;
        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(800, 120);
        rt.anchoredPosition = anchoredPos;
        return t;
    }

    static Button CreateButton(Transform parent, string name, string label, Vector2 anchoredPos)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.color = new Color(0.15f, 0.15f, 0.2f, 0.9f);
        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(360, 80);
        rt.anchoredPosition = anchoredPos;

        CreateText(go.transform, "Label", label, 48, Vector2.zero);
        return go.GetComponent<Button>();
    }

    static GameObject CreatePanel(Transform parent, string name)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        return go;
    }

    static Image CreateImage(Transform parent, string name, Vector2 anchoredPos, Vector2 size, Color c)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = size;
        rt.anchoredPosition = anchoredPos;
        var img = go.GetComponent<Image>();
        img.color = c;
        img.raycastTarget = false;
        return img;
    }

    static Toggle CreateToggle(Transform parent, string name, string label, Vector2 anchoredPos)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Toggle));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(360, 50);
        rt.anchoredPosition = anchoredPos;

        var bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
        bg.transform.SetParent(go.transform, false);
        var bgRT = bg.GetComponent<RectTransform>();
        bgRT.anchorMin = new Vector2(0, 0.5f); bgRT.anchorMax = new Vector2(0, 0.5f);
        bgRT.sizeDelta = new Vector2(40, 40);
        bgRT.anchoredPosition = new Vector2(20, 0);
        bg.GetComponent<Image>().color = new Color(0.15f, 0.15f, 0.2f);

        var check = new GameObject("Checkmark", typeof(RectTransform), typeof(Image));
        check.transform.SetParent(bg.transform, false);
        var cRT = check.GetComponent<RectTransform>();
        cRT.anchorMin = Vector2.zero; cRT.anchorMax = Vector2.one;
        cRT.offsetMin = new Vector2(6, 6); cRT.offsetMax = new Vector2(-6, -6);
        check.GetComponent<Image>().color = new Color(0.85f, 0.65f, 0.25f);

        var lbl = CreateText(go.transform, "Label", label, 32, new Vector2(60, 0), TextAlignmentOptions.Left);
        var lblRT = lbl.GetComponent<RectTransform>();
        lblRT.sizeDelta = new Vector2(280, 50);

        var t = go.GetComponent<Toggle>();
        t.targetGraphic = bg.GetComponent<Image>();
        t.graphic = check.GetComponent<Image>();
        t.isOn = true;
        return t;
    }

    static Slider CreateSlider(Transform parent, string name, Vector2 anchoredPos, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Slider));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = size;
        rt.anchoredPosition = anchoredPos;

        var bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
        bg.transform.SetParent(go.transform, false);
        var bgRT = bg.GetComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero; bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = Vector2.zero; bgRT.offsetMax = Vector2.zero;
        bg.GetComponent<Image>().color = new Color(0.2f, 0.05f, 0.05f);

        var fillArea = new GameObject("FillArea", typeof(RectTransform));
        fillArea.transform.SetParent(go.transform, false);
        var faRT = fillArea.GetComponent<RectTransform>();
        faRT.anchorMin = Vector2.zero; faRT.anchorMax = Vector2.one;
        faRT.offsetMin = Vector2.zero; faRT.offsetMax = Vector2.zero;

        var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fill.transform.SetParent(fillArea.transform, false);
        var fillRT = fill.GetComponent<RectTransform>();
        fillRT.anchorMin = Vector2.zero; fillRT.anchorMax = Vector2.one;
        fillRT.offsetMin = Vector2.zero; fillRT.offsetMax = Vector2.zero;
        fill.GetComponent<Image>().color = new Color(0.85f, 0.15f, 0.15f);

        var slider = go.GetComponent<Slider>();
        slider.fillRect = fillRT;
        slider.targetGraphic = bg.GetComponent<Image>();
        slider.minValue = 0;
        slider.maxValue = 100;
        slider.value = 100;
        slider.transition = Selectable.Transition.None;
        return slider;
    }
}
