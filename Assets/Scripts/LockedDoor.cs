/*
 * Author: Jayden Wong
 * Date: 11 August 2025
 * Description: This script controls the behavior of a locked door in the game.
 *              When the player interacts, it provides audio/visual feedback,
 *              briefly flashes a HUD message, and reveals a hidden object the 
 *              first time the door is tried. Used to guide player progression 
 *              and reinforce the idea that a key or other action is required.
 */

using UnityEngine;
using System.Collections;

public class LockedDoor : MonoBehaviour, IInteractable
{
    [Header("Locked Feedback")]
    public AudioSource lockedSfx;        // Plays a sound when the player tries the locked door
    public GameObject lockedFlashHud;    // HUD panel shown briefly to indicate "locked" state
    public float lockedFlashSeconds = 2f; // Duration the HUD stays visible

    [Header("Reveal After First Try")]
    public GameObject objectToShow;      // Optional: an object to reveal after first attempt (e.g., a clue)
    private bool hasTriedDoor = false;   // Tracks if the player has already interacted once

    // Prompt text shown to the player when looking at the door
    public string PromptText => "[E] Try door";

    /// <summary>
    /// Called when the player interacts with the locked door.
    /// Plays feedback (sound + HUD flash), and if it's the first 
    /// attempt, reveals an object in the scene.
    /// </summary>
    public void Interact(PlayerInteractorRaycast interactor)
    {
        // Play locked-door feedback to inform the player they cannot open it
        if (lockedSfx) lockedSfx.Play();

        // Show a temporary HUD message that fades after a few seconds
        if (lockedFlashHud) StartCoroutine(FlashLocked());

        // Handle special logic for the first time the player tries the door
        if (!hasTriedDoor)
        {
            hasTriedDoor = true; // Mark that the player has attempted once

            // Reveal a hidden object if assigned, often used as a hint or progression trigger
            if (objectToShow != null)
                objectToShow.SetActive(true);
        }
    }

    /// <summary>
    /// Shows the "locked" HUD feedback for a set number of seconds,
    /// then hides it again. Runs as a coroutine for timing.
    /// </summary>
    private IEnumerator FlashLocked()
    {
        // Immediately show the HUD message
        lockedFlashHud.SetActive(true);

        // Wait for the defined duration before hiding it again
        yield return new WaitForSeconds(Mathf.Max(0f, lockedFlashSeconds));

        // Ensure the HUD object still exists before disabling
        if (lockedFlashHud) lockedFlashHud.SetActive(false);
    }
}
