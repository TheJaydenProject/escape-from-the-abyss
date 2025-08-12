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


using UnityEngine;

public class CollectibleVHS : MonoBehaviour, IInteractable
{
    [Header("UI Feedback (optional)")]
    public GameObject collectedPanel;   // small “Collected!” panel (optional)
    public float collectedPanelTime = 2f;

    [Header("VFX/SFX (optional)")]
    public ParticleSystem pickupVfx;
    public AudioSource pickupSfx;

    public string PromptText => "Press E to collect VHS";

    public void Interact(PlayerInteractorRaycast interactor)
    {
        if (GameManager.Instance != null && GameManager.Instance.CollectVHS())
        {
            if (pickupVfx) Instantiate(pickupVfx, transform.position, Quaternion.identity);
            if (pickupSfx) pickupSfx.Play();

            if (collectedPanel != null)
                StartCoroutine(HidePanelAfterDelay());

            Destroy(gameObject);
        }
        else
        {
            Debug.LogWarning("[CollectibleVHS] GameManager missing or could not collect.");
        }
    }
    
    private System.Collections.IEnumerator HidePanelAfterDelay()
    {
        collectedPanel.SetActive(true);
        yield return new WaitForSeconds(collectedPanelTime);
        collectedPanel.SetActive(false);
    }
}

// Tiny helper extension on Player to hide the panel if you used one
public static class PlayerInteractorExtensions
{
    public static void HideCollectedPanel(this PlayerInteractorRaycast interactor)
    {
        // no-op; keep if you want to wire a global collected panel on HUD instead
    }
}
