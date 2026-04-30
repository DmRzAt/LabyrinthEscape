using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MainMenu : MonoBehaviour
{
    [Header("Panels")]
    public GameObject mainPanel;
    public GameObject settingsPanel;

    [Header("Settings Controls")]
    public Slider volumeSlider;
    public Slider sensitivitySlider;
    public Toggle fullscreenToggle;
    public TextMeshProUGUI volumeLabel;
    public TextMeshProUGUI sensitivityLabel;

    const string KEY_VOLUME = "opt_volume";
    const string KEY_SENS   = "opt_sensitivity";
    const string KEY_FULL   = "opt_fullscreen";

    void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (GameManager.Instance == null)
            new GameObject("GameManager").AddComponent<GameManager>();

        ShowMain();
        LoadSettings();
    }

    public void StartGame() => GameManager.Instance.StartGame();
    public void QuitGame()  => GameManager.Instance.QuitGame();

    public void OpenSettings()
    {
        if (mainPanel != null) mainPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(true);
    }

    public void ShowMain()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (mainPanel != null) mainPanel.SetActive(true);
    }

    public void OnVolumeChanged(float v)
    {
        AudioListener.volume = v;
        if (volumeLabel != null) volumeLabel.text = $"Volume: {Mathf.RoundToInt(v * 100)}%";
        PlayerPrefs.SetFloat(KEY_VOLUME, v);
    }

    public void OnSensitivityChanged(float v)
    {
        if (sensitivityLabel != null) sensitivityLabel.text = $"Sensitivity: {v:F1}";
        PlayerPrefs.SetFloat(KEY_SENS, v);
    }

    public void OnFullscreenChanged(bool b)
    {
        Screen.fullScreen = b;
        PlayerPrefs.SetInt(KEY_FULL, b ? 1 : 0);
    }

    void LoadSettings()
    {
        float vol = PlayerPrefs.GetFloat(KEY_VOLUME, 0.8f);
        float sen = PlayerPrefs.GetFloat(KEY_SENS, 2f);
        bool full = PlayerPrefs.GetInt(KEY_FULL, 1) == 1;

        if (volumeSlider != null)      { volumeSlider.value = vol; OnVolumeChanged(vol); }
        if (sensitivitySlider != null) { sensitivitySlider.value = sen; OnSensitivityChanged(sen); }
        if (fullscreenToggle != null)  { fullscreenToggle.isOn = full; OnFullscreenChanged(full); }
    }
}
