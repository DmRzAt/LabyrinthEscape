using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
	[Header("Klucze")]
	public int keysCollected;

	public int keysAvailable;

	public int keysTotal = 3;

	[Tooltip("Count the keys actually present in the level on load (chest Key items + KeyPickups) instead of using the fixed keysTotal. Add a 4th key and it just works.")]
	[SerializeField]
	private bool _autoCountKeys = true;

	[Header("Run stats")]
	public int enemiesKilled;

	private float _runStart;

	public static GameManager Instance { get; private set; }

	public bool IsPaused { get; private set; }

	public bool IsGameOver { get; private set; }

	public float RunSeconds { get; private set; }

	public static event Action<int, int> OnKeysChanged;

	public static event Action OnGameWon;

	public static event Action OnGameLost;

	public static event Action<bool> OnPauseChanged;

	public void AddKill()
	{
		enemiesKilled++;
	}

	private void Awake()
	{
		if (Instance != null && Instance != this)
		{
			UnityEngine.Object.Destroy(this);
			return;
		}
		Instance = this;
		UnityEngine.Object.DontDestroyOnLoad(base.gameObject);
		SceneManager.sceneLoaded += OnSceneLoaded;
		Application.targetFrameRate = -1;
	}

	private void OnDestroy()
	{
		if (Instance == this)
		{
			SceneManager.sceneLoaded -= OnSceneLoaded;
		}
	}

	private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
	{
		if (scene.name == "GameScene")
		{
			if (_autoCountKeys)
			{
				keysTotal = CountKeysInLevel();
			}
			ResetRunState();
			_runStart = Time.time;
		}
		else if (scene.name == "MainMenuScene")
		{
			ResetRunState();
		}
		if (Time.timeScale != 1f)
		{
			Time.timeScale = 1f;
		}
		AudioListener.pause = false;
		IsPaused = false;
	}

	private void ResetRunState()
	{
		keysCollected = 0;
		keysAvailable = 0;
		enemiesKilled = 0;
		IsGameOver = false;
		GameManager.OnKeysChanged?.Invoke(keysCollected, keysTotal);
	}

	private int CountKeysInLevel()
	{
		int num = 0;
		Chest[] array = UnityEngine.Object.FindObjectsByType<Chest>(FindObjectsInactive.Include, FindObjectsSortMode.None);
		for (int i = 0; i < array.Length; i++)
		{
			foreach (Chest.ChestItem item in array[i].items)
			{
				if (item.type == Chest.ItemType.Key)
				{
					num += Mathf.Max(1, item.count);
				}
			}
		}
		num += UnityEngine.Object.FindObjectsByType<KeyPickup>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;
		return Mathf.Max(1, num);
	}

	public void SetPaused(bool paused)
	{
		if ((!paused || !IsGameOver) && IsPaused != paused)
		{
			IsPaused = paused;
			Time.timeScale = (paused ? 0f : 1f);
			AudioListener.pause = paused;
			GameManager.OnPauseChanged?.Invoke(paused);
		}
	}

	public void AddKey()
	{
		keysCollected++;
		keysAvailable++;
		GameManager.OnKeysChanged?.Invoke(keysCollected, keysTotal);
	}

	public bool UseKey()
	{
		if (keysAvailable <= 0)
		{
			return false;
		}
		keysAvailable--;
		GameManager.OnKeysChanged?.Invoke(keysCollected, keysTotal);
		return true;
	}

	public void WinGame()
	{
		if (!IsGameOver)
		{
			IsGameOver = true;
			RunSeconds = Time.time - _runStart;
			GameManager.OnGameWon?.Invoke();
			StartCoroutine(LoadSceneAfter("EndScene", 2f));
		}
	}

	public void LoseGame()
	{
		if (!IsGameOver)
		{
			IsGameOver = true;
			GameManager.OnGameLost?.Invoke();
			StartCoroutine(LoadSceneAfter(SceneManager.GetActiveScene().name, 2f));
		}
	}

	private IEnumerator LoadSceneAfter(string sceneName, float delay)
	{
		yield return new WaitForSecondsRealtime(delay);
		if (Time.timeScale != 1f)
		{
			Time.timeScale = 1f;
		}
		AudioListener.pause = false;
		SceneManager.LoadScene(sceneName);
	}

	public void LoadMainMenu()
	{
		SceneManager.LoadScene("MainMenuScene");
	}

	public void StartGame()
	{
		SceneManager.LoadScene("GameScene");
	}

	public void QuitGame()
	{
#if UNITY_EDITOR
		UnityEditor.EditorApplication.isPlaying = false;
#else
		Application.Quit();
#endif
	}
}
