/*
 * Author: Jayden Wong
 * Date: 9 August 2025
 * Description: Handles player interaction using a forward raycast from the camera. 
 *              Detects objects on specific layers (VHS, Computer, Key, Door),
 *              shows the correct prompt HUD, and triggers interaction when the
 *              player presses the interaction key (E). 
 *              Includes logic to lock the computer prompt after it has been used.
 */

using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Player interaction controller using a camera raycast:
/// - Casts a ray each frame to detect nearby interactable objects.
/// - Shows the correct HUD prompt depending on what was hit.
/// - Calls the object's Interact method when the player presses [E].
/// - Tracks milestone logic for computers so prompts only appear once.
/// </summary>
public class PlayerInteractorRaycast : MonoBehaviour
{
    [Header("Raycast")]
    public Camera cam;
    public float interactRange = 3f;

    [Header("Layers")]
    public LayerMask vhsLayer;       
    public LayerMask computerLayer;  
    public LayerMask keyLayer; 
    public LayerMask doorLayer;

    [Header("VHS Prompt")]
    public GameObject VHSpromptPanel;   

    [Header("Computer Prompts")]
    public GameObject promptHudBeforeMilestone; // shown if milestone FALSE
    public GameObject promptHudAfterMilestone;  // shown if milestone TRUE

    [Header("Key Prompt")]
    public GameObject KeypromptPanel; 

    [Header("Door Prompt")]
    public GameObject DoorPromptPanel;
    
    [Header("ExitDoorLocked")]
    public GameObject doorLockedHud;

    private IInteractable current;
    private int _vhsMaskValue;
    private int _computerMaskValue;
    private int _keyMaskValue;
    private int _doorMaskValue;

    private bool _computerUsed = false; // locks the computer prompt after interaction

    /// <summary>
    /// Pre-computes layer mask values for efficiency.
    /// </summary>
    void Awake()
    {
        _vhsMaskValue = vhsLayer.value;
        _computerMaskValue = computerLayer.value;
        _keyMaskValue = keyLayer.value;
        _doorMaskValue = doorLayer.value;
    }

    /// <summary>
    /// Runs every frame:
    /// - Shoots a ray forward from the camera.
    /// - Detects which interactable object is hit.
    /// - Shows the correct HUD prompt for that object.
    /// - Calls Interact() when [E] is pressed.
    /// - Locks the computer prompt if used after milestone.
    /// </summary>
    void Update()
    {
        current = null;

        // Combine all interaction layers into one mask for a single raycast
        int combinedMask = _vhsMaskValue | _computerMaskValue | _keyMaskValue | _doorMaskValue;

        if (cam != null && Physics.Raycast(
                cam.transform.position, cam.transform.forward,
                out RaycastHit hit, interactRange,
                combinedMask, QueryTriggerInteraction.Collide))
        {
            current = hit.collider.GetComponentInParent<IInteractable>();

            // Always hide everything first; only show the relevant prompt
            HideAllPrompts();

            if (current != null)
            {
                // Determine which layer the object belongs to
                int hitLayerBit = 1 << hit.collider.gameObject.layer;

                // VHS OBJECT
                if ((hitLayerBit & _vhsMaskValue) != 0)
                {
                    if (VHSpromptPanel) VHSpromptPanel.SetActive(true);
                }
                // COMPUTER OBJECT
                else if ((hitLayerBit & _computerMaskValue) != 0)
                {
                    bool milestone = GameManager.Instance != null && GameManager.Instance.vhsMilestoneReached;

                    if (!milestone)
                    {
                        // Show "before milestone" prompt
                        if (promptHudBeforeMilestone) promptHudBeforeMilestone.SetActive(true);
                    }
                    else
                    {
                        // Show "after milestone" prompt only if not already used
                        if (!_computerUsed && promptHudAfterMilestone) 
                            promptHudAfterMilestone.SetActive(true);
                    }
                }
                // KEY OBJECT
                else if ((hitLayerBit & _keyMaskValue) != 0)
                {
                    if (KeypromptPanel) KeypromptPanel.SetActive(true);
                }
                // DOOR OBJECT
                else if ((hitLayerBit & _doorMaskValue) != 0)
                {
                    if (DoorPromptPanel) DoorPromptPanel.SetActive(true);
                }

                // Handle interaction when player presses [E]
                if (Input.GetKeyDown(KeyCode.E))
                {
                    current.Interact(this);

                    // If interacting with a computer AFTER milestone, lock it so prompt won’t show again
                    if ((hitLayerBit & _computerMaskValue) != 0)
                    {
                        bool milestone = GameManager.Instance != null && GameManager.Instance.vhsMilestoneReached;
                        if (milestone) _computerUsed = true;
                    }
                }
            }
        }
        else
        {
            // No hit = hide all prompts
            HideAllPrompts();
        }
    }

    /// <summary>
    /// Hides all interaction prompts to ensure only one prompt is visible at a time.
    /// </summary>
    void HideAllPrompts()
    {
        if (VHSpromptPanel) VHSpromptPanel.SetActive(false);
        if (KeypromptPanel) KeypromptPanel.SetActive(false);
        if (promptHudBeforeMilestone) promptHudBeforeMilestone.SetActive(false);
        if (promptHudAfterMilestone) promptHudAfterMilestone.SetActive(false);
        if (DoorPromptPanel) DoorPromptPanel.SetActive(false);
    }
}
