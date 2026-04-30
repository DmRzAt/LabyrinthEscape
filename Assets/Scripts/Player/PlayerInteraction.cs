using UnityEngine;
using TMPro;

public class PlayerInteraction : MonoBehaviour
{
    public float interactDistance = 2.5f;
    public Transform cameraTransform;
    public TextMeshProUGUI hintText;

    private IInteractable _current;

    void Update()
    {
        Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, interactDistance))
            _current = hit.collider.GetComponentInParent<IInteractable>();
        else
            _current = null;

        if (hintText != null)
        {
            if (_current != null)
            {
                hintText.text = $"{_current.Prompt}  [E]";
                hintText.gameObject.SetActive(true);
            }
            else
            {
                hintText.gameObject.SetActive(false);
            }
        }

        if (_current != null && Input.GetKeyDown(KeyCode.E))
            _current.Interact();
    }
}
