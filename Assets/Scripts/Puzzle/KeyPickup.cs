using UnityEngine;

public class KeyPickup : MonoBehaviour, IInteractable
{
    [Header("Pickup")]
    [SerializeField] private string _prompt = "Take Key";
    [SerializeField] private bool _pickupOnTrigger = false;
    [SerializeField, Range(0.1f, 5f)] private float _rotateSpeed = 90f;
    [SerializeField, Range(0f, 1f)] private float _bobHeight = 0.2f;
    [SerializeField, Range(0.1f, 5f)] private float _bobSpeed = 2f;
    [SerializeField, Range(0f, 3f)] private float _pickupDelay = 0.75f;

    public string Prompt => _prompt;

    private Vector3 _startPosition;
    private float _spawnTime;

    private void Awake()
    {
        Collider pickupCollider = GetComponent<Collider>();
        if (pickupCollider != null)
        {
            pickupCollider.isTrigger = true;
        }
    }

    private void Start()
    {
        _startPosition = transform.position;
        _spawnTime = Time.time;
    }

    public void SetPickupDelay(float pickupDelay)
    {
        _pickupDelay = pickupDelay;
        _spawnTime = Time.time;
    }

    public void SetPickupOnTrigger(bool pickupOnTrigger)
    {
        _pickupOnTrigger = pickupOnTrigger;
    }

    private void Update()
    {
        transform.Rotate(Vector3.up * _rotateSpeed * Time.deltaTime, Space.World);

        if (_bobHeight <= 0f) return;

        float y = _startPosition.y + Mathf.Sin(Time.time * _bobSpeed) * _bobHeight;
        transform.position = new Vector3(_startPosition.x, y, _startPosition.z);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!_pickupOnTrigger) return;
        if (!other.CompareTag("Player")) return;

        TryPickup();
    }

    private void OnTriggerStay(Collider other)
    {
        if (_pickupOnTrigger || !other.CompareTag("Player")) return;

        if (Input.GetKeyDown(KeyCode.E))
        {
            TryPickup();
        }
    }

    public void Interact()
    {
        TryPickup();
    }

    private void TryPickup()
    {
        if (Time.time < _spawnTime + _pickupDelay) return;

        GameManager.Instance?.AddKey();
        Destroy(gameObject);
    }
}
