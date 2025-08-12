using UnityEngine;
using UnityEngine.UI;

public class PlayerInteractorRaycast : MonoBehaviour
{
    [Header("Raycast Settings")]
    public Camera cam;
    public float interactRange = 3f;
    public LayerMask interactLayer; // Only hit Interactables

    [Header("UI Prompt")]
    public GameObject promptPanel;
    public Text promptText;

    private IInteractable current;

    void Update()
    {
        current = null;

        if (cam != null)
        {
            if (Physics.Raycast(cam.transform.position, cam.transform.forward,
                                out RaycastHit hit, interactRange,
                                interactLayer, QueryTriggerInteraction.Collide))
            {
                current = hit.collider.GetComponentInParent<IInteractable>();
            }
        }

        // Show/hide prompt
        if (current != null)
        {
            if (promptPanel) promptPanel.SetActive(true);
            if (promptText)  promptText.text = current.PromptText;

            if (Input.GetKeyDown(KeyCode.E))
            {
                current.Interact(this);
            }
        }
        else
        {
            if (promptPanel) promptPanel.SetActive(false);
        }
    }
}
