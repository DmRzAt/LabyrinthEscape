using UnityEngine;

public class EndScene : MonoBehaviour
{
    void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Restart()  => GameManager.Instance?.StartGame();
    public void MainMenu() => GameManager.Instance?.LoadMainMenu();
}
