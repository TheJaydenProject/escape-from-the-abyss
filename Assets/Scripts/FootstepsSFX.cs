/*
 * Author: Jayden Wong
 * Date: 14 August 2025
 * Description: Controls looping walking sounds in a first-person game.
 *              Detects player movement and plays a looping audio clip when
 *              walking, with optional fade-out when stopping. Integrates with
 *              GameManager state so sounds stop during pauses or death handling.
 */

using UnityEngine;

/// <summary>
/// Handles continuous playback of walking loop audio in an FPS setup.
/// - Starts playback after the player has moved for a short delay.
/// - Stops or fades the sound when the player stops moving.
/// - Resets properly if the game state is paused or the player is dead.
/// </summary>
public class WalkingLoopControllerFPS : MonoBehaviour
{
    [Header("Audio")]
    public AudioSource walkingLoopSource; // AudioSource assigned with the walking loop clip

    [Header("Movement")]
    public float speedThreshold = 0.30f;  // Movement speed required before considering "walking"
    public float startDelay = 0.1f;       // Must move continuously this long before audio starts

    [Header("Polish (optional)")]
    public bool fadeOutOnStop = false;    // If true, fade out the loop instead of cutting instantly
    public float fadeTime = 0.08f;        // Fade-out duration

    Vector3 _lastPos;            // Tracks previous frame’s position
    bool _fading;                // Whether a fade-out coroutine is currently running
    float _movementHeldTime = 0f; // How long the player has been moving

    /// <summary>
    /// Initializes the AudioSource settings and sets baseline values.
    /// Ensures the sound is set up for 2D FPS playback and not positional in 3D space.
    /// </summary>
    void Awake()
    {
        // If no AudioSource is assigned, try to attach one to the main camera
        if (!walkingLoopSource)
        {
            var cam = Camera.main;
            if (cam)
                walkingLoopSource = cam.GetComponent<AudioSource>() ?? cam.gameObject.AddComponent<AudioSource>();
        }

        // Configure the AudioSource for FPS walking loop playback
        if (walkingLoopSource)
        {
            walkingLoopSource.playOnAwake = false; // Do not play automatically
            walkingLoopSource.loop = true;        // Loop continuously while walking
            walkingLoopSource.spatialBlend = 0f;  // Force 2D audio (non-positional)
            walkingLoopSource.dopplerLevel = 0f;  // Remove doppler effect
        }

        // Record initial position for movement detection
        _lastPos = transform.position;
    }

    /// <summary>
    /// Called every frame to check movement state and manage walking loop playback.
    /// - If game is paused or handling death, stop sound immediately.
    /// - Detects movement speed and starts/stops audio accordingly.
    /// </summary>
    void Update()
    {
        // GameManager check: stop all walking sounds if game isn’t actively playing
        var gm = GameManager.Instance;
        if (gm && (gm.State != GameState.Playing || gm.IsHandlingDeath))
        {
            StopAndResetImmediate();
            _movementHeldTime = 0f;
            return;
        }

        // Calculate horizontal movement speed (ignores vertical axis)
        Vector3 pos = transform.position;
        Vector3 delta = pos - _lastPos;
        delta.y = 0f;
        float speed = delta.magnitude / Mathf.Max(Time.deltaTime, 0.0001f);
        _lastPos = pos;

        // True if player movement speed exceeds the threshold
        bool moving = speed > speedThreshold;

        if (moving)
        {
            _movementHeldTime += Time.deltaTime;

            // Start playback only if movement is sustained beyond startDelay
            if (_movementHeldTime >= startDelay && walkingLoopSource && !walkingLoopSource.isPlaying && !_fading)
            {
                walkingLoopSource.time = 0f; // Restart audio from beginning
                walkingLoopSource.Play();
            }
        }
        else
        {
            // Reset the sustained movement timer when player stops
            _movementHeldTime = 0f;

            // If sound is playing, stop it either instantly or via fade-out
            if (walkingLoopSource && walkingLoopSource.isPlaying && !_fading)
            {
                if (fadeOutOnStop) StartCoroutine(FadeOutThenStop());
                else               StopAndResetImmediate();
            }
        }
    }

    /// <summary>
    /// Coroutine that gradually lowers volume over fadeTime, then stops the audio.
    /// Prevents audio clicks/pops when abruptly stopping loop sounds.
    /// </summary>
    System.Collections.IEnumerator FadeOutThenStop()
    {
        _fading = true;
        float startVol = walkingLoopSource.volume; // Save initial volume
        float t = 0f;

        // Gradually reduce volume until fadeTime elapses
        while (t < fadeTime)
        {
            t += Time.unscaledDeltaTime;
            walkingLoopSource.volume = Mathf.Lerp(startVol, 0f, Mathf.Clamp01(t / fadeTime));
            yield return null;
        }

        // Fully stop playback and restore original volume for next use
        StopAndResetImmediate();
        walkingLoopSource.volume = startVol;
        _fading = false;
    }

    /// <summary>
    /// Immediately stops the walking loop playback and resets the AudioSource.
    /// Called when stopping without fade-out, or when resetting on state changes.
    /// </summary>
    void StopAndResetImmediate()
    {
        if (!walkingLoopSource) return;
        walkingLoopSource.Stop();
    }
}
