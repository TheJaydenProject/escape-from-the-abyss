/*
 * Author: Jayden Wong
 * Date: 11/08/2025
 * Description: Handles player interaction with the computer terminal. 
 *              Once the VHS milestone is reached, the terminal:
 *              - Plays a sound effect
 *              - Updates the screen material to show progress
 *              - Spawns a key at a designated location
 */

using UnityEngine;

/// <summary>
/// Represents an interactive computer terminal that responds when the VHS milestone is reached. 
/// On interaction, it can:
/// - Play an optional sound effect
/// - Update the screen’s material to indicate progress
/// - Spawn a key at the given spawn point
/// </summary>
/// 

public class ComputerTerminal : MonoBehaviour, IInteractable
{
    [Header("Key Spawn")]
    /// <summary>
    /// Prefab of the key to spawn after the milestone is reached.
    /// </summary>
    public GameObject keyPrefab;

    /// <summary>
    /// Transform location where the key will appear.
    /// </summary>
    public Transform keySpawnPoint;

    private bool keySpawned; // Tracks whether the key has already been spawned

    [Header("SFX")]
    /// <summary>
    /// Audio source to play when the terminal is used.
    /// </summary>
    public AudioSource interactSfx;

    [Header("Material Swap")]
    /// <summary>
    /// Renderer of the terminal screen to update when milestone is reached.
    /// </summary>
    public Renderer screenRenderer;

    /// <summary>
    /// Index of the material slot on the screen renderer to replace.
    /// </summary>
    [Min(0)] public int screenMaterialIndex = 0;

    /// <summary>
    /// Material to swap in after the VHS milestone is reached.
    /// </summary>
    public Material materialAfterMilestone;

    private bool visualsUpdated; // Ensures material is swapped only once

    /// <summary>
    /// Prompt text for this interactable (empty since the terminal auto-triggers at milestone).
    /// </summary>
    public string PromptText => string.Empty;

    /// <summary>
    /// Prepares the interact SFX: disables play on awake, forces 2D sound, and preloads the clip.
    /// </summary>
    void Awake()
    {
        if (interactSfx && interactSfx.clip)
        {
            interactSfx.playOnAwake = false;
            interactSfx.spatialBlend = 0f;
            interactSfx.clip.LoadAudioData();
        }
    }

    /// <summary>
    /// Handles interaction with the terminal. If the VHS milestone is complete:
    /// - Plays sound
    /// - Updates the screen material
    /// - Spawns a key (only once)
    /// </summary>

    public void Interact(PlayerInteractorRaycast interactor)
    {
        var gm = GameManager.Instance;
        if (gm == null || !gm.vhsMilestoneReached) return;

        // 1) Play SFX
        if (interactSfx && interactSfx.clip)
        {
            bool onSelf = interactSfx.transform == transform || interactSfx.transform.IsChildOf(transform);
            if (onSelf) interactSfx.Play();
            else        interactSfx.PlayOneShot(interactSfx.clip);
        }

        // 2) Swap ONLY the chosen material slot (once)
        if (!visualsUpdated && screenRenderer && materialAfterMilestone)
        {
            var mats = screenRenderer.materials; // instanced array (safe to modify at runtime)
            if (screenMaterialIndex >= 0 && screenMaterialIndex < mats.Length)
            {
                mats[screenMaterialIndex] = materialAfterMilestone;
                screenRenderer.materials = mats;
                visualsUpdated = true;
            }
            else
            {
                Debug.LogWarning($"[ComputerTerminal] screenMaterialIndex {screenMaterialIndex} out of range (0..{mats.Length - 1}).");
            }
        }

        // 3) Spawn key (once)
        if (!keySpawned && keyPrefab && keySpawnPoint)
        {
            Instantiate(keyPrefab, keySpawnPoint.position, keySpawnPoint.rotation);
            keySpawned = true;
            gm.keyIsSpawned = true;
            Debug.Log("[ComputerTerminal] Key spawned.");
        }
    }
}
