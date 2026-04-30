using UnityEngine;

public class Chest : MonoBehaviour
{
    [Header("Lid")]
    public Transform lid;
    public string lidChildName = "Chest_Top";
    public Vector3 lidOpenEuler = new Vector3(-90f, 0f, 0f);

    [Header("Trigger")]
    public float triggerDistance = 2.2f;
    public float openSpeed = 3f;
    public bool oneShot = true;

    Quaternion _lidClosed;
    Quaternion _lidOpen;
    Transform _player;
    bool _opened;
    bool _animating;

    void Start()
    {
        if (lid == null) lid = FindLid(transform);
        if (lid != null)
        {
            _lidClosed = lid.localRotation;
            _lidOpen = _lidClosed * Quaternion.Euler(lidOpenEuler);
        }

        var p = GameObject.FindWithTag("Player");
        if (p != null) _player = p.transform;
    }

    Transform FindLid(Transform root)
    {
        foreach (Transform t in root)
        {
            if (t.name.Contains(lidChildName)) return t;
            var nested = FindLid(t);
            if (nested != null) return nested;
        }
        return null;
    }

    void Update()
    {
        if (lid == null) return;

        if (!_opened && _player != null)
        {
            float dist = Vector3.Distance(transform.position, _player.position);
            if (dist < triggerDistance)
            {
                _opened = true;
                _animating = true;
            }
            else if (!oneShot && _animating)
            {
                _animating = true;
            }
        }

        if (_animating)
        {
            Quaternion target = _opened ? _lidOpen : _lidClosed;
            lid.localRotation = Quaternion.Slerp(lid.localRotation, target, Time.deltaTime * openSpeed);
            if (Quaternion.Angle(lid.localRotation, target) < 0.5f)
            {
                lid.localRotation = target;
                _animating = false;
            }
        }
    }
}
