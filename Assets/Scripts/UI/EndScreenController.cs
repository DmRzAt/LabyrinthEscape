using UnityEngine;
using TMPro;

/// <summary>
/// Ekran końcowy — "YOU WIN" + przycisk RESTART.
/// </summary>
public class EndScreenController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI statsText;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (titleText != null)
            titleText.text = "YOU WIN";
    }

    public void OnRestartButton()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.LoadGameScene();
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("GameScene");
        }
    }

    public void OnMainMenuButton()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.LoadMainMenu();
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenuScene");
        }
    }
}
