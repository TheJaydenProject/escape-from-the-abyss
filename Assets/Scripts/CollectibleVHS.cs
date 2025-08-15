/*
 * Author: Jayden Wong
 * Date: 11/08/2025
 * Description: Handles interaction logic for collecting a VHS object in the scene.
 * Updates inventory, shows a temporary prompt confirmation UI, and removes the object from the world.
 */

/// <summary>
/// Allows the player to collect a VHS and adds it to their inventory.
/// Shows a UI panel temporarily and logs the interaction for debugging.
/// </summary>

using System.Collections;
using UnityEngine;

public class CollectibleVHS : MonoBehaviour, IInteractable
{
    [Header("UI Feedback (optional)")]
    public GameObject collectedPanel;  
    public float collectedPanelTime = 2f;

    [Header("VFX/SFX (optional)")]
    public AudioSource pickupSfx;

    public string PromptText => "Press E to collect VHS";

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

    private IEnumerator HidePanelAfterDelay()
    {
        collectedPanel.SetActive(true);
        yield return new WaitForSeconds(collectedPanelTime);
        if (collectedPanel) collectedPanel.SetActive(false);
    }

    void HideVHS()
    {
        foreach (var c in GetComponentsInChildren<Collider>(true))  c.enabled = false;
        foreach (var r in GetComponentsInChildren<Renderer>(true))  r.enabled = false;
        enabled = false;
    }
}

// Tiny helper extension on Player to hide the panel if you used one
public static class PlayerInteractorExtensions
{
    public static void HideCollectedPanel(this PlayerInteractorRaycast interactor) { }
}
