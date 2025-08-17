/// 
/// Author: Jayden Wong
/// Date: 11 August 2025
/// Description: Handles the behavior of a collectible key in the game. 
///              When the player interacts with the key, it plays a sound effect, 
///              disables the key’s visual/physical presence, and informs the GameManager 
///              that the player has collected it. The key is then destroyed to prevent reuse.
///

using UnityEngine;

public class CollectibleKey : MonoBehaviour, IInteractable
{
    [Header("SFX")]
    /// <summary>
    /// Audio source that plays when the key is picked up.
    /// </summary>
    public AudioSource pickupSfx;

    public string PromptText => "Press E to pick up Key";

    /// <summary>
    /// Called when the player interacts with this object. 
    /// Handles key collection, sound playback, hiding the object, and destroying it after use.
    /// </summary>
    public void Interact(PlayerInteractorRaycast interactor)
    {
        if (GameManager.Instance == null || !GameManager.Instance.CollectKey()) return;

        if (pickupSfx && pickupSfx.clip)
        {
            // Force audio to play in 2D mode (ignores distance/3D effects).
            pickupSfx.spatialBlend = 0f;


            // Check if the audio source is physically inside this key GameObject 
            // (or one of its children). This matters because we may want to delay 
            // destruction until the sound finishes playing.
            bool sourceInsideKey = pickupSfx.transform == transform || pickupSfx.transform.IsChildOf(transform);

            if (sourceInsideKey)
            {
                pickupSfx.Play();
                HideKey();

                // Calculate how long the clip lasts, factoring in pitch (speed).
                float life = pickupSfx.clip.length / Mathf.Max(0.01f, pickupSfx.pitch);

                // Destroy this GameObject after the sound finishes playing.
                Destroy(gameObject, life);
                return;
            }
            else
            {
                pickupSfx.PlayOneShot(pickupSfx.clip);
            }
        }

        Destroy(gameObject);
    }
    
    /// <summary>
    /// Hides the key visually and physically:
    /// - Disables all colliders so it cannot be interacted with.
    /// - Disables all renderers so it is invisible.
    /// - Disables this script to stop further logic.
    /// </summary>
    void HideKey()
    {
        // Disable all colliders in this object and its children.
        foreach (var c in GetComponentsInChildren<Collider>(true)) c.enabled = false;
        
        // Disable all renderers (removes the key from view).
        foreach (var r in GetComponentsInChildren<Renderer>(true)) r.enabled = false;

        // Disable this script component itself (prevents accidental re-trigger).
        enabled = false;
    }
}
