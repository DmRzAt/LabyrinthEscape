using UnityEngine;

public class KeyItem : MonoBehaviour
{
    [Header("Obracanie")]
    public float rotateSpeed = 90f;
    public float bobHeight = 0.3f;
    public float bobSpeed = 2f;

    private Vector3 _startPos;

    void Start() => _startPos = transform.position;

    void Update()
    {
        transform.Rotate(Vector3.up * rotateSpeed * Time.deltaTime);
        float newY = _startPos.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        transform.position = new Vector3(_startPos.x, newY, _startPos.z);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        GameManager.Instance?.AddKey();
        Destroy(gameObject);
    }
}
