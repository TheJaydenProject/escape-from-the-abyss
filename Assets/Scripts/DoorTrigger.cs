/*
 * Author: Jayden Wong
 * Date: 11 August 2025
 * Description: A one-shot trigger that swaps the visibility of two objects. 
 *              When the player enters the trigger zone, one object is hidden 
 *              and another is shown. This only happens once, and the trigger 
 *              disables itself afterward to prevent repeat activations.
 */

using UnityEngine;

[RequireComponent(typeof(Collider))]
public class OneShotTriggerSwap : MonoBehaviour
{
    [Header("Who can trigger")]
    public LayerMask playerLayer;     // LayerMask that defines which objects (e.g., Player) can activate this trigger

    [Header("Swap")]
    public GameObject objectToHide;   // The object that will be hidden when the trigger is activated
    public GameObject objectToShow;   // The object that will be revealed when the trigger is activated

    bool _fired; // Tracks whether this trigger has already been activated (so it only fires once)

    /// <summary>
    /// Ensures that the attached collider is set as a trigger when this component is added/reset.
    /// This allows it to detect overlaps instead of blocking movement.
    /// </summary>
    void Reset()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true; // Make sure collider behaves as a trigger
    }

    /// <summary>
    /// Called automatically by Unity when another collider enters this trigger zone.
    /// If the entering object belongs to the player layer and this trigger has not yet fired,
    /// it swaps the objects (hide one, show another) and then disables itself to prevent reuse.
    /// </summary>
    void OnTriggerEnter(Collider other)
    {
        // Stop immediately if the trigger has already been fired before
        if (_fired) return;

        // Convert the entering object's layer to a bitmask and check against the allowed playerLayer
        int otherBit = 1 << other.gameObject.layer;
        if ((playerLayer.value & otherBit) == 0) return;  // Ignore if it's not the player layer

        // Mark as fired so this block cannot run again
        _fired = true;

        // Perform the object swap: hide one, show the other
        if (objectToHide) objectToHide.SetActive(false);
        if (objectToShow) objectToShow.SetActive(true);

        // Disable the collider to prevent retriggering in the future
        var col = GetComponent<Collider>();
        if (col) col.enabled = false;

        // Disable this script entirely since its job is done
        enabled = false;
    }
}
