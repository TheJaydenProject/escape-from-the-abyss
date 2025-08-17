/* 
 * Author: Jayden Wong
 * Date: 10 Aug 2025
 * Description: Detects when the player enters a trigger zone. 
 *              If the object that entered is the player, it logs the detection 
 *              distance for debugging and then tells the AI controller 
 *              to handle the "player caught" behavior (e.g., death or respawn).
 */

using UnityEngine;

/// <summary>
/// Trigger component that detects when the player enters its collider.
/// If the collider belongs to the Player, it logs the detection distance
/// and notifies the assigned AIChaseController to run the "caught" flow.
/// </summary>

public class CatchTrigger : MonoBehaviour
{
    public AIChaseController ai; // assign in inspector
    // Reference to the AI logic that handles "player caught" behavior.
    // Kept public so you can drag the AI object in the Inspector (avoids runtime lookups).

    void OnTriggerEnter(Collider other)
    {
        // Unity calls this when ANY collider enters this trigger volume.
        // Guard clause: only react to the Player; ignore everything else early.
        if (!other.CompareTag("Player")) return;

        // Diagnostic message to confirm the trigger fired and roughly how far the player was.
        Debug.Log($"Player detected at distance {Vector3.Distance(transform.position, other.transform.position):F2}");

        // Notify the AI system that the player has been caught so it can run its death/respawn flow.
        ai.Caught();
    }
}