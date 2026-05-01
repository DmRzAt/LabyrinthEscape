using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Klucze")]
    public int keysCollected = 0;
    public int keysTotal = 3;

    public static event System.Action<int, int> OnKeysChanged;
    public static event System.Action OnGameWon;
    public static event System.Action OnGameLost;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void AddKey()
    {
        keysCollected++;
        OnKeysChanged?.Invoke(keysCollected, keysTotal);
    }

    public bool UseKey()
    {
        if (keysCollected <= 0) return false;
        keysCollected--;
        OnKeysChanged?.Invoke(keysCollected, keysTotal);
        return true;
    }

    public void WinGame()
    {
        OnGameWon?.Invoke();
        Invoke(nameof(LoadEndScene), 2f);
    }

    public void LoseGame()
    {
        OnGameLost?.Invoke();
        Invoke(nameof(ReloadGame), 3f);
    }

    void LoadEndScene()   => SceneManager.LoadScene("EndScene");
    void ReloadGame()     => SceneManager.LoadScene(SceneManager.GetActiveScene().name);

    public void LoadMainMenu() => SceneManager.LoadScene("MainMenuScene");
    public void StartGame()    => SceneManager.LoadScene("GameScene");
    public void QuitGame()     => Application.Quit();
}
