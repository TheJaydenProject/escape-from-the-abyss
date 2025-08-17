/*
 * Author: Jayden Wong
 * Date: 11/08/2025
 * Description: Handles the logic for collecting a VHS object. 
 *              Plays optional sound and UI feedback, updates the GameManager inventory, 
 *              hides the object from the scene, and destroys it after a short delay.
 */

using System.Collections;
using UnityEngine;

/// <summary>
/// Allows the player to collect a VHS and add it to the GameManager’s inventory. 
/// Plays optional sound effects, shows a temporary UI panel if assigned, 
/// and removes the VHS object from the scene after a short delay.
/// </summary>

public class CollectibleVHS : MonoBehaviour, IInteractable
{
    [Header("UI Feedback (optional)")]
    public GameObject collectedPanel;
    public float collectedPanelTime = 2f;

    [Header("VFX/SFX (optional)")]
    public AudioSource pickupSfx;

    public string PromptText => "Press E to collect VHS";


    
    /// <summary>
    /// Called when the script instance is being loaded.
    /// Preloads audio data and ensures the sound is set to 2D 
    /// so that the first playback is instant and non-positional.
    /// </summary>
    void Awake()
    {
        // Preload & force 2D so first-play is instant and non-positional
        if (pickupSfx && pickupSfx.clip)
        {
            pickupSfx.playOnAwake = false;
            pickupSfx.spatialBlend = 0f;
            pickupSfx.clip.LoadAudioData();
        }
    }


    /// <summary>
    /// Handles player interaction with the VHS:
    /// - Updates GameManager script
    /// - Plays sound effects
    /// - Hides colliders/renderers immediately
    /// - Destroys the object after feedback is complete
    /// </summary>
    public void Interact(PlayerInteractorRaycast interactor)
    {
        if (GameManager.Instance == null || !GameManager.Instance.CollectVHS())
        {
            Debug.LogWarning("[CollectibleVHS] GameManager missing or could not collect.");
            return;
        }

        float destroyDelay = 0.05f; // tiny safety window by default

        // Instant SFX pattern (mirrors CollectibleKey)
        if (pickupSfx && pickupSfx.clip)
        {
            pickupSfx.spatialBlend = 0f;

            bool sourceOnSelf = pickupSfx.transform == transform
                             || pickupSfx.transform.IsChildOf(transform);

            if (sourceOnSelf)
            {
                pickupSfx.Play();
                float clipTail = pickupSfx.clip.length / Mathf.Max(0.01f, pickupSfx.pitch);
                destroyDelay = Mathf.Max(destroyDelay, clipTail);
            }
            else
            {
                // Fire-and-forget from a separate source (e.g., UI)
                pickupSfx.PlayOneShot(pickupSfx.clip);
                // keep min delay so PlayOneShot actually fires before destroy
                destroyDelay = Mathf.Max(destroyDelay, 0.05f);
            }
        }

        // Show panel if assigned (coroutine runs on THIS object)
        if (collectedPanel != null)
        {
            StartCoroutine(HidePanelAfterDelay());
            // keep this object alive long enough for the panel to auto-hide
            destroyDelay = Mathf.Max(destroyDelay, collectedPanelTime);
        }

        // Hide visuals/colliders immediately so it feels collected
        HideVHS();

        // Single exit point
        Destroy(gameObject, destroyDelay);
    }


    /// <summary>
    /// Shows the collected panel and automatically hides it after a delay.
    /// </summary>
    private IEnumerator HidePanelAfterDelay()
    {
        collectedPanel.SetActive(true);
        yield return new WaitForSeconds(collectedPanelTime);
        if (collectedPanel) collectedPanel.SetActive(false);
    }


    /// <summary>
    /// Hides the VHS object visually and physically:
    /// - Disables colliders
    /// - Disables renderers
    /// - Disables this script
    /// </summary>
    void HideVHS()
    {
        foreach (var c in GetComponentsInChildren<Collider>(true)) c.enabled = false;
        foreach (var r in GetComponentsInChildren<Renderer>(true)) r.enabled = false;
        enabled = false;
    }
}

// Tiny helper extension on Player to hide the panel if you used one
public static class PlayerInteractorExtensions
{
    public static void HideCollectedPanel(this PlayerInteractorRaycast interactor) { }
}
