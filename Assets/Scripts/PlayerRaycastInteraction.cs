using UnityEngine;
using UnityEngine.UI;

public class PlayerInteractorRaycast : MonoBehaviour
{
    [Header("Raycast")]
    public Camera cam;
    public float interactRange = 3f;

    [Header("Layers")]
    public LayerMask vhsLayer;       // set to your VHS layer
    public LayerMask computerLayer;  // set to your Computer layer

    [Header("VHS Prompt (generic)")]
    public GameObject promptPanel;   // e.g., “[E] Pick up”
    public Text promptText;

    [Header("Computer Prompts (milestone-based)")]
    public GameObject promptHudBeforeMilestone; // shown if milestone FALSE
    public Text promptTextBefore;               // optional
    public GameObject promptHudAfterMilestone;  // shown if milestone TRUE
    public Text promptTextAfter;                // optional

    private IInteractable current;
    private int _vhsMaskValue;
    private int _computerMaskValue;

    void Awake()
    {
        _vhsMaskValue = vhsLayer.value;
        _computerMaskValue = computerLayer.value;
    }

    void Update()
    {
        current = null;

        // Raycast against both layers at once
        int combinedMask = _vhsMaskValue | _computerMaskValue;

        if (cam != null && Physics.Raycast(
                cam.transform.position, cam.transform.forward,
                out RaycastHit hit, interactRange,
                combinedMask, QueryTriggerInteraction.Collide))
        {
            current = hit.collider.GetComponentInParent<IInteractable>();

            // Hide everything first; we’ll enable what we need
            HideAllPrompts();

            if (current != null)
            {
                int hitLayerBit = 1 << hit.collider.gameObject.layer;

                // VHS LAYER BEHAVIOUR
                if ( (hitLayerBit & _vhsMaskValue) != 0 )
                {
                    if (promptPanel) promptPanel.SetActive(true);
                    if (promptText)  promptText.text = current.PromptText;
                }

                // COMPUTER LAYER BEHAVIOUR
                else if ( (hitLayerBit & _computerMaskValue) != 0 )
                {
                    bool milestone =
                        GameManager.Instance != null &&
                        GameManager.Instance.vhsMilestoneReached;

                    if (!milestone)
                    {
                        if (promptHudBeforeMilestone) promptHudBeforeMilestone.SetActive(true);
                        if (promptTextBefore) promptTextBefore.text = current.PromptText;
                    }
                    else
                    {
                        if (promptHudAfterMilestone) promptHudAfterMilestone.SetActive(true);
                        if (promptTextAfter) promptTextAfter.text = current.PromptText;
                    }
                }

                // Interact
                if (Input.GetKeyDown(KeyCode.E))
                {
                    current.Interact(this);
                }
            }
        }
        else
        {
            HideAllPrompts();
        }
    }

    void HideAllPrompts()
    {
        if (promptPanel)               promptPanel.SetActive(false);
        if (promptHudBeforeMilestone)  promptHudBeforeMilestone.SetActive(false);
        if (promptHudAfterMilestone)   promptHudAfterMilestone.SetActive(false);
    }
}
