using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Zarządza stanem gry: start, pauza, koniec poziomu, przejścia między scenami.
/// Singleton — jeden na całą grę.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum GameState { MainMenu, Playing, Paused, GameOver, Win }

    [Header("Game State")]
    [SerializeField] private GameState currentState = GameState.MainMenu;
    public GameState CurrentState => currentState;

    [Header("Level Settings")]
    [SerializeField] private int totalKeysRequired = 5;
    private int keysCollected = 0;

    // Events — UI та інші системи підписуються
    public event System.Action<GameState> OnGameStateChanged;
    public event System.Action<int, int> OnKeysUpdated; // (collected, total)
    public event System.Action OnPlayerDied;
    public event System.Action OnLevelCompleted;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        // Визначаємо початковий стан за поточною сценою
        string sceneName = SceneManager.GetActiveScene().name;
        if (sceneName == "GameScene")
        {
            StartGame();
        }
    }

    private void Update()
    {
        // Пауза по Escape
        if (currentState == GameState.Playing && Input.GetKeyDown(KeyCode.Escape))
        {
            PauseGame();
        }
        else if (currentState == GameState.Paused && Input.GetKeyDown(KeyCode.Escape))
        {
            ResumeGame();
        }
    }

    // ─────────────── Управління станом ───────────────

    public void StartGame()
    {
        keysCollected = 0;
        SetState(GameState.Playing);
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        OnKeysUpdated?.Invoke(keysCollected, totalKeysRequired);
    }

    public void PauseGame()
    {
        if (currentState != GameState.Playing) return;

        SetState(GameState.Paused);
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void ResumeGame()
    {
        if (currentState != GameState.Paused) return;

        SetState(GameState.Playing);
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void PlayerDied()
    {
        SetState(GameState.GameOver);
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        OnPlayerDied?.Invoke();
    }

    public void LevelCompleted()
    {
        SetState(GameState.Win);
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        OnLevelCompleted?.Invoke();
    }

    // ─────────────── Klucze (Keys) ───────────────

    public void CollectKey()
    {
        keysCollected++;
        OnKeysUpdated?.Invoke(keysCollected, totalKeysRequired);
        Debug.Log($"[GameManager] Key collected: {keysCollected}/{totalKeysRequired}");
    }

    public bool HasAllKeys()
    {
        return keysCollected >= totalKeysRequired;
    }

    public int GetKeysCollected() => keysCollected;
    public int GetTotalKeys() => totalKeysRequired;

    // ─────────────── Przejścia scen ───────────────

    public void LoadMainMenu()
    {
        Time.timeScale = 1f;
        SetState(GameState.MainMenu);
        SceneManager.LoadScene("MainMenuScene");
    }

    public void LoadGameScene()
    {
        SceneManager.LoadScene("GameScene");
    }

    public void LoadEndScene()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("EndScene");
    }

    public void QuitGame()
    {
        Debug.Log("[GameManager] Quitting game...");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ─────────────── Helpers ───────────────

    private void SetState(GameState newState)
    {
        currentState = newState;
        Debug.Log($"[GameManager] State changed to: {newState}");
        OnGameStateChanged?.Invoke(newState);
    }
}
