using TMPro;
using UnityEngine;

public class EndScene : MonoBehaviour
{
	[SerializeField]
	private TMP_Text statsText;

	private void Start()
	{
		Cursor.lockState = CursorLockMode.None;
		Cursor.visible = true;
		Time.timeScale = 1f;
		if (GameManager.Instance == null)
		{
			new GameObject("GameManager").AddComponent<GameManager>();
		}
		GameManager.Instance.SetPaused(paused: false);
		ShowStats();
	}

	private void ShowStats()
	{
		if (!(statsText == null))
		{
			GameManager instance = GameManager.Instance;
			if (instance == null)
			{
				statsText.text = string.Empty;
				return;
			}
			int num = Mathf.Max(0, Mathf.RoundToInt(instance.RunSeconds));
			int num2 = num / 60;
			int num3 = num % 60;
			statsText.text = $"Time  {num2:00}:{num3:00}        Keys  {instance.keysCollected}/{instance.keysTotal}        Slain  {instance.enemiesKilled}";
		}
	}

	public void Restart()
	{
		GameManager.Instance?.StartGame();
	}

	public void MainMenu()
	{
		GameManager.Instance?.LoadMainMenu();
	}
}
