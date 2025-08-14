using UnityEngine;
using UnityEngine.UI;

public class PlayerInteractorRaycast : MonoBehaviour
{
    [Header("Raycast")]
    public Camera cam;
    public float interactRange = 3f;

    [Header("Layers")]
    public LayerMask vhsLayer;       
    public LayerMask computerLayer;  
    public LayerMask keyLayer; 

    [Header("VHS Prompt")]
    public GameObject VHSpromptPanel;   

    [Header("Computer Prompts")]
    public GameObject promptHudBeforeMilestone; // shown if milestone FALSE
    public GameObject promptHudAfterMilestone;  // shown if milestone TRUE

    [Header("Key Prompt")]
    public GameObject KeypromptPanel; 

    private IInteractable current;
    private int _vhsMaskValue;
    private int _computerMaskValue;
    private int _keyMaskValue;

    void Awake()
    {
        _vhsMaskValue = vhsLayer.value;
        _computerMaskValue = computerLayer.value;
        _keyMaskValue = keyLayer.value;
    }

    void Update()
    {
        current = null;

        // Raycast against all layers at once
        int combinedMask = _vhsMaskValue | _computerMaskValue | _keyMaskValue;

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
                    if (VHSpromptPanel) VHSpromptPanel.SetActive(true);
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
                    }
                    else
                    {
                        if (promptHudAfterMilestone) promptHudAfterMilestone.SetActive(true);
                    }
                }

                // KEY...
                else if ((hitLayerBit & _keyMaskValue) != 0)
                {
                    if (KeypromptPanel) KeypromptPanel.SetActive(true);
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
        if (VHSpromptPanel) VHSpromptPanel.SetActive(false);
        if (KeypromptPanel) KeypromptPanel.SetActive(false);
        if (promptHudBeforeMilestone) promptHudBeforeMilestone.SetActive(false);
        if (promptHudAfterMilestone)   promptHudAfterMilestone.SetActive(false);
    }
}
