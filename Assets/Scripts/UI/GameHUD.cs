using UnityEngine;
using TMPro;

/// <summary>
/// HUD gry — wyświetla HP gracza i zebrane klucze.
/// Zgodnie z wireframe: HP (lewo), Keys X/Y (prawo), celownik (środek).
/// </summary>
public class GameHUD : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI hpText;
    [SerializeField] private TextMeshProUGUI keysText;
    [SerializeField] private GameObject crosshair;

    [Header("Panels")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject gameOverPanel;

    private void OnEnable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnKeysUpdated += UpdateKeysUI;
            GameManager.Instance.OnGameStateChanged += HandleStateChange;
            GameManager.Instance.OnPlayerDied += ShowGameOver;
        }
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnKeysUpdated -= UpdateKeysUI;
            GameManager.Instance.OnGameStateChanged -= HandleStateChange;
            GameManager.Instance.OnPlayerDied -= ShowGameOver;
        }
    }

    private void Start()
    {
        // Початкові значення
        if (GameManager.Instance != null)
        {
            UpdateKeysUI(GameManager.Instance.GetKeysCollected(), GameManager.Instance.GetTotalKeys());
        }

        UpdateHP(100f);
        HideAllPanels();
    }

    // ─────────────── UI Updates ───────────────

    public void UpdateHP(float currentHP)
    {
        if (hpText != null)
            hpText.text = $"HP {Mathf.CeilToInt(currentHP)}";
    }

    private void UpdateKeysUI(int collected, int total)
    {
        if (keysText != null)
            keysText.text = $"Keys {collected}/{total}";
    }

    private void HandleStateChange(GameManager.GameState newState)
    {
        switch (newState)
        {
            case GameManager.GameState.Playing:
                HideAllPanels();
                if (crosshair != null) crosshair.SetActive(true);
                break;
            case GameManager.GameState.Paused:
                ShowPause();
                break;
            case GameManager.GameState.GameOver:
                ShowGameOver();
                break;
        }
    }

    // ─────────────── Panels ───────────────

    private void ShowPause()
    {
        if (pausePanel != null) pausePanel.SetActive(true);
        if (crosshair != null) crosshair.SetActive(false);
    }

    private void ShowGameOver()
    {
        if (gameOverPanel != null) gameOverPanel.SetActive(true);
        if (crosshair != null) crosshair.SetActive(false);
    }

    private void HideAllPanels()
    {
        if (pausePanel != null) pausePanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
    }

    // ─────────────── Button Callbacks ───────────────

    public void OnResumeButton()
    {
        GameManager.Instance?.ResumeGame();
    }

    public void OnRestartButton()
    {
        GameManager.Instance?.LoadGameScene();
    }

    public void OnMainMenuButton()
    {
        GameManager.Instance?.LoadMainMenu();
    }

    public void OnQuitButton()
    {
        GameManager.Instance?.QuitGame();
    }
}
