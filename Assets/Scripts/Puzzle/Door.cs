using UnityEngine;

public class Door : MonoBehaviour, IInteractable
{
	public enum HingeSide
	{
		Left,
		Right
	}

	[Header("Settings")]
	public float openAngle = 90f;

	[Tooltip("Angular speed in degrees per second.")]
	public float speed = 180f;

	public bool locked;

	public string prompt = "Open Door";

	[Tooltip("Assign the door leaf, for example UnlockedLeaf.")]
	public Transform doorLeaf;

	[Tooltip("Hinge side relative to the door leaf.")]
	public HingeSide hingeSide;

	[Header("Audio")]
	public AudioClip openClip;

	public AudioClip closeClip;

	[Range(0f, 1f)]
	public float volume = 0.8f;

	private Transform _hinge;

	private Quaternion _closedRot;

	private Quaternion _openRot;

	private bool _isOpen;

	private AudioSource _audio;

	public string Prompt
	{
		get
		{
			if (!locked)
			{
				return prompt;
			}
			return "Locked";
		}
	}

	private void Start()
	{
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
		Vector3 hingeEdgeWorld = GetHingeEdgeWorld(doorLeaf, hingeSide);
		_hinge.position = hingeEdgeWorld;
		_hinge.rotation = doorLeaf.rotation;
		doorLeaf.SetParent(_hinge, worldPositionStays: true);
		_closedRot = _hinge.localRotation;
		_openRot = _closedRot * Quaternion.Euler(0f, openAngle, 0f);
	}

	private void Update()
	{
		if (!(_hinge == null))
		{
			_hinge.localRotation = Quaternion.RotateTowards(_hinge.localRotation, _isOpen ? _openRot : _closedRot, speed * Time.deltaTime);
		}
	}

	public void Interact()
	{
		if (!locked)
		{
			_isOpen = !_isOpen;
			PlayDoor(_isOpen);
		}
	}

	public void Unlock()
	{
		locked = false;
	}

	public void SetLocked(bool isLocked)
	{
		locked = isLocked;
	}

	public void Open()
	{
		if (!locked && !_isOpen)
		{
			_isOpen = true;
			PlayDoor(opening: true);
		}
	}

	public void Close()
	{
		if (_isOpen)
		{
			_isOpen = false;
			PlayDoor(opening: false);
		}
	}

	private void PlayDoor(bool opening)
	{
		AudioClip audioClip = (opening ? openClip : closeClip);
		if (_audio != null && audioClip != null)
		{
			_audio.PlayOneShot(audioClip, volume);
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
