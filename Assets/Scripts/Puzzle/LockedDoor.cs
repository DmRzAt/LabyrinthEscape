using UnityEngine;

public class LockedDoor : MonoBehaviour, IInteractable
{
	public enum HingeSide
	{
		Left,
		Right
	}

	[Header("Settings")]
	public int keysRequired = 1;

	public float openAngle = 90f;

	[Tooltip("Angular speed in degrees per second.")]
	public float speed = 180f;

	[SerializeField]
	private bool _debugLogs = true;

	[Tooltip("Assign the door leaf, for example UnlockedLeaf.")]
	public Transform doorLeaf;

	[Tooltip("Hinge side relative to the door leaf.")]
	public HingeSide hingeSide;

	[Tooltip("Doors that should unlock together with this one. Explicit links are preferred over parent/child auto-detect.")]
	public LockedDoor[] linkedDoors;

	[Header("Audio")]
	public AudioClip openClip;

	public AudioClip closeClip;

	public AudioClip lockedClip;

	public AudioClip unlockClip;

	[Range(0f, 1f)]
	public float volume = 0.8f;

	private bool _unlocked;

	private bool _open;

	private bool _sealed;

	private bool _syncingLinkedDoors;

	private Transform _hinge;

	private Quaternion _closedRot;

	private Quaternion _openRot;

	private AudioSource _audio;

	public string Prompt
	{
		get
		{
			if (!_unlocked)
			{
				return $"Locked  (Need: {RequiredKeys}, Have: {AvailableKeys})";
			}
			return "Open Door";
		}
	}

	private int RequiredKeys => Mathf.Max(1, keysRequired);

	private int AvailableKeys
	{
		get
		{
			if (!(GameManager.Instance != null))
			{
				return 0;
			}
			return GameManager.Instance.keysAvailable;
		}
	}

	private void OnValidate()
	{
		keysRequired = Mathf.Max(1, keysRequired);
	}

	private void Start()
	{
		keysRequired = RequiredKeys;
		if (doorLeaf == null)
		{
			doorLeaf = base.transform;
		}
		_audio = GetComponent<AudioSource>();
		if (_audio == null)
		{
			_audio = base.gameObject.AddComponent<AudioSource>();
		}
		_audio.playOnAwake = false;
		_audio.spatialBlend = 1f;
		_audio.maxDistance = 18f;
		_audio.rolloffMode = AudioRolloffMode.Linear;
		_hinge = new GameObject(doorLeaf.name + "_Hinge").transform;
		_hinge.SetParent(doorLeaf.parent, worldPositionStays: false);
		_hinge.position = GetHingeEdgeWorld(doorLeaf, hingeSide);
		_hinge.rotation = doorLeaf.rotation;
		doorLeaf.SetParent(_hinge, worldPositionStays: true);
		_closedRot = _hinge.localRotation;
		_openRot = _closedRot * Quaternion.Euler(0f, openAngle, 0f);
	}

	private void Update()
	{
		if (!(_hinge == null))
		{
			_hinge.localRotation = Quaternion.RotateTowards(_hinge.localRotation, _open ? _openRot : _closedRot, speed * Time.deltaTime);
		}
	}

	public void Seal()
	{
		_sealed = true;
		_open = false;
		PlayDoor(opening: false);
	}

	public void Unseal(bool open)
	{
		_sealed = false;
		_unlocked = true;
		_open = open;
		if (open)
		{
			PlayDoor(opening: true);
		}
	}

	public void Interact()
	{
		if (_sealed)
		{
			return;
		}
		if (!_unlocked)
		{
			if (GameManager.Instance == null)
			{
				return;
			}
			if (_debugLogs)
			{
				Debug.Log($"[LockedDoor] Interact '{base.name}'. Need={RequiredKeys}, available={GameManager.Instance.keysAvailable}, collected={GameManager.Instance.keysCollected}", this);
			}
			if (AvailableKeys < RequiredKeys)
			{
				PlayClip(lockedClip);
				return;
			}
			for (int i = 0; i < RequiredKeys; i++)
			{
				if (!GameManager.Instance.UseKey())
				{
					return;
				}
			}
			UnlockAndOpenLinkedDoors();
		}
		else
		{
			_open = !_open;
			PlayDoor(_open);
		}
	}

	private void PlayDoor(bool opening)
	{
		AudioClip clip = (opening ? openClip : closeClip);
		PlayClip(clip);
	}

	private void PlayClip(AudioClip clip)
	{
		if (_audio != null && clip != null)
		{
			_audio.PlayOneShot(clip, volume);
		}
	}

	private void UnlockAndOpenLinkedDoors()
	{
		SetUnlockedOpen(open: true);
		if (!_syncingLinkedDoors)
		{
			_syncingLinkedDoors = true;
			if (linkedDoors != null && linkedDoors.Length != 0)
			{
				SyncLinkedDoors(linkedDoors);
			}
			else
			{
				SyncLinkedDoors(GetComponentsInParent<LockedDoor>(includeInactive: true));
				SyncLinkedDoors(GetComponentsInChildren<LockedDoor>(includeInactive: true));
			}
			_syncingLinkedDoors = false;
		}
	}

	private void SyncLinkedDoors(LockedDoor[] linkedDoors)
	{
		if (linkedDoors == null)
		{
			return;
		}
		foreach (LockedDoor lockedDoor in linkedDoors)
		{
			if (!(lockedDoor == null) && !(lockedDoor == this))
			{
				lockedDoor.SetUnlockedOpen(open: false);
			}
		}
	}

	private void SetUnlockedOpen(bool open)
	{
		_unlocked = true;
		_open = open;
		PlayClip(unlockClip);
		if (open)
		{
			PlayDoor(opening: true);
		}
	}

	private static Vector3 GetHingeEdgeWorld(Transform leaf, HingeSide side)
	{
		Renderer[] componentsInChildren = leaf.GetComponentsInChildren<Renderer>();
		if (componentsInChildren.Length == 0)
		{
			return leaf.position;
		}
		Bounds bounds = componentsInChildren[0].bounds;
		for (int i = 1; i < componentsInChildren.Length; i++)
		{
			bounds.Encapsulate(componentsInChildren[i].bounds);
		}
		Vector3 right = leaf.right;
		Vector3 center = bounds.center;
		float num = Vector3.Dot(bounds.extents, new Vector3(Mathf.Abs(right.x), Mathf.Abs(right.y), Mathf.Abs(right.z)));
		return center + right * ((side == HingeSide.Left) ? (0f - num) : num);
	}
}
