using UnityEngine;

public class FaceCamera : MonoBehaviour
{
    Transform _cam;

    void LateUpdate()
    {
        if (_cam == null)
        {
            var main = Camera.main;
            if (main == null) return;
            _cam = main.transform;
        }
        transform.rotation = _cam.rotation;
    }
}
