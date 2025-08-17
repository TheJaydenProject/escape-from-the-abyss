/*
 * Author: Jayden Wong
 * Date: 14 August 2025
 * Description: Interactable door that changes the scene after an optional door SFX.
 *              Prevents double-triggering, optionally disables colliders while waiting,
 *              and supports editor-only scene picking with build-safe string fallback.
 */

using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Scene-changing door:
/// - Plays an optional SFX, waits for its duration (or a fallback), then loads a target scene.
/// - Can disable colliders to prevent re-triggering while waiting.
/// - Editor-only SceneAsset helps pick a scene; a string is used at runtime.
/// </summary>
public class SceneChangeDoor : MonoBehaviour, IInteractable
{
    [Header("SFX (optional)")]
    public AudioSource doorSfx; // plays once on interact

    [Header("Scene")]
#if UNITY_EDITOR
    [SerializeField] private SceneAsset sceneAsset; // visible only in Editor; used to auto-fill sceneToLoad
#endif
    [SerializeField] private string sceneToLoad; // actual name used in build

    [Header("Timing")]
    [Tooltip("If no AudioSource/clip is available, wait this long before loading.")]
    public float fallbackWaitSeconds = 1.0f;
    [Tooltip("Extra delay added after SFX (or fallback) before loading.")]
    public float extraDelay = 0f;

    [Header("One-shot Control")]
    [Tooltip("Prevent multiple triggers while waiting.")]
    public bool disableCollidersOnUse = true;

    public string PromptText => "[E] Open";

    bool _triggered; // guards against double-activation while SFX/delays are running

#if UNITY_EDITOR
    /// <summary>
    /// Editor-only convenience: whenever a SceneAsset is assigned,
    /// copy its filename (without extension) into the build-safe string field.
    /// </summary>
    void OnValidate()
    {
        // Auto-update scene name when you pick a scene in Inspector
        if (sceneAsset != null)
        {
            string path = AssetDatabase.GetAssetPath(sceneAsset);
            sceneToLoad = System.IO.Path.GetFileNameWithoutExtension(path);
        }
    }
#endif

    /// <summary>
    /// Entry point when the player interacts with the door:
    /// - Ensures the door runs only once (sets _triggered and optionally disables colliders).
    /// - Plays SFX if available (forces sensible 2D blend if out-of-range).
    /// - Starts a coroutine to load the scene after the computed wait.
    /// </summary>
    public void Interact(PlayerInteractorRaycast interactor)
    {
        if (_triggered) return;   // stop duplicate scene loads or repeated SFX
        _triggered = true;

        // Optional safety: turn off colliders so walking through won't re-trigger
        if (disableCollidersOnUse)
        {
            foreach (var c in GetComponentsInChildren<Collider>(true))
                c.enabled = false;
        }

        // If we have an SFX, play it now; use the clip length as part of the wait time
        if (doorSfx && doorSfx.clip)
        {
            doorSfx.playOnAwake = false;
            // If spatialBlend got an invalid value elsewhere, coerce to 2D so it always plays
            if (doorSfx.spatialBlend < 0f || doorSfx.spatialBlend > 1f)
                doorSfx.spatialBlend = 0f;
            doorSfx.Play();
        }

        // Defer scene load to a coroutine so we can wait for SFX/extra delays
        StartCoroutine(LoadAfterDelay());
    }

    /// <summary>
    /// Waits for either the SFX duration (or fallback), plus any extra delay,
    /// then loads the configured scene if valid. Warns if no scene name is set.
    /// </summary>
    IEnumerator LoadAfterDelay()
    {
        float wait = ComputeWaitSeconds(); // derive time based on SFX or fallback
        if (wait > 0f) yield return new WaitForSeconds(wait);

        if (extraDelay > 0f) yield return new WaitForSeconds(extraDelay);

        if (!string.IsNullOrEmpty(sceneToLoad))
        {
            // Single = replace current scene; avoids stacking scenes by mistake
            SceneManager.LoadScene(sceneToLoad, LoadSceneMode.Single);
        }
        else
        {
            Debug.LogWarning("[SceneChangeDoor] No sceneToLoad set.");
        }
    }

    /// <summary>
    /// Computes how long to wait before loading:
    /// - If there is a door SFX, return its clip length adjusted by pitch (so higher pitch plays faster).
    /// - Otherwise, use a clamped non-negative fallback delay.
    /// </summary>
    float ComputeWaitSeconds()
    {
        if (doorSfx && doorSfx.clip)
        {
            float p = Mathf.Max(0.01f, doorSfx.pitch); // avoid divide-by-zero
            return doorSfx.clip.length / p;
        }
        return Mathf.Max(0f, fallbackWaitSeconds);
    }
}
