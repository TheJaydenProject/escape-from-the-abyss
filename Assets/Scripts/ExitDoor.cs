/*
 * Author: Jayden Wong
 * Date: 11 August 2025
 * Description: Interactable exit door that either shows a "locked" feedback (SFX + HUD flash)
 *              when the player lacks the exit key, or plays an open SFX and triggers the game
 *              end HUD when the key has been obtained.
 */

using UnityEngine;

/// <summary>
/// Exit door logic:
/// - If the player does not have the exit key, play a "locked" sound and briefly show a HUD message.
/// - If the player has the key, play an "open" sound and display the end-game HUD.
/// The on-screen prompt updates dynamically based on whether the key is owned.
/// </summary>
public class ExitDoor : MonoBehaviour, IInteractable
{
    [Header("Locked feedback")]
    public float lockedFlashSeconds = 2f;   // How long the "locked" HUD should remain visible
    public AudioSource lockedSfx;           // Optional sound effect when the door is locked

    [Header("Open")]
    public AudioSource openSfx;             // Optional sound effect when the door opens

    // Dynamic prompt: shows "[E] Open" only when the GameManager exists and the player has the key
    public string PromptText =>
        (GameManager.Instance != null && GameManager.Instance.hasExitKey)
        ? "[E] Open"
        : "[E] Try door";

    /// <summary>
    /// Handles the player's interaction with the door.
    /// - Safely returns if GameManager is missing.
    /// - If the player lacks the key: plays "locked" SFX and flashes a HUD panel briefly.
    /// - If the player has the key: plays "open" SFX and triggers the end-game HUD.
    /// </summary>
    public void Interact(PlayerInteractorRaycast interactor)
    {
        // Guard: if the global state manager is not present, do nothing to avoid null reference issues
        if (GameManager.Instance == null) return;

        // Player does NOT have the exit key → provide locked feedback and early-exit
        if (!GameManager.Instance.hasExitKey)
        {
            // Play a short "locked" sound if assigned; gives immediate feedback without changing state
            if (lockedSfx) lockedSfx.Play();

            // Try to fetch the "door locked" HUD from the interactor; null-safe access
            var hud = interactor != null ? interactor.doorLockedHud : null;

            // Flash the HUD for a limited time to inform the player why the door won't open
            if (hud) StartCoroutine(FlashLocked(hud));

            return; // Stop here; door stays closed
        }

        // Player HAS the key → play open SFX (if any) then show the end screen/HUD
        if (openSfx) openSfx.Play();
        GameManager.Instance.ShowGameEndHUD();
    }

    /// <summary>
    /// Briefly shows the provided HUD GameObject, waits for the configured duration,
    /// then hides it again. Uses a coroutine so the main thread isn't blocked.
    /// </summary>
    private System.Collections.IEnumerator FlashLocked(GameObject hud)
    {
        // If no HUD was supplied, terminate the coroutine immediately
        if (!hud) yield break;

        hud.SetActive(true); // Make the "locked" message visible

        // Wait for a non-negative duration; Mathf.Max prevents negative waits if misconfigured
        yield return new WaitForSeconds(Mathf.Max(0f, lockedFlashSeconds));

        // Hide the HUD again if it still exists (scene might have changed)
        if (hud) hud.SetActive(false);
    }
}
