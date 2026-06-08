using UnityEngine;

public class ExitZone : MonoBehaviour
{
	[Tooltip("Require every key to be collected before the exit counts as a win — guarantees the player can't skip the puzzles even if the maze geometry has a shortcut.")]
	[SerializeField]
	private bool _requireAllKeys = true;

	private bool _triggered;

	private void OnTriggerEnter(Collider other)
	{
		if (_triggered || !other.CompareTag("Player"))
		{
			return;
		}
		GameManager instance = GameManager.Instance;
		if (!(instance == null) && !instance.IsGameOver)
		{
			if (_requireAllKeys && instance.keysCollected < instance.keysTotal)
			{
				PickupFeedback.ShowMessage($"Need all keys  ({instance.keysCollected}/{instance.keysTotal})");
				return;
			}
			_triggered = true;
			instance.WinGame();
		}
	}
}
