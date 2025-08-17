/*
 * Author: Jayden Wong
 * Date: 11 August 2025
 * Description: Central game state manager (singleton). 
 *              Persists across scene loads, tracks VHS/key progress, handles HUD visibility, 
 *              player spawn points, timer/score, game pause/resume, and end-game flow (including audio/UI).
 */

using UnityEngine;
using TMPro;
using System;
using UnityEngine.SceneManagement;

public enum GameState { Playing, Paused, Ended }

/// <summary>
/// Global game controller that persists across scenes.
/// - Maintains player progress (VHS count, milestone, key pickup).
/// - Shows/hides HUD elements and instructional prompts.
/// - Spawns player at scene-specific points.
/// - Runs an in-game timer, computes score, and drives end-game presentation.
/// - Exposes events for UI and other systems to react to changes.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public event Action<int> OnDeathCountChanged;

    [Header("Persistent Player Root")]
    public Transform playerRoot;

    [System.Serializable]
    public class SceneSpawn { public string sceneName; public Transform spawn; }

    [Header("Scene Spawn Points")]
    public SceneSpawn[] spawns;

    [Header("HUD - Start Intro ")]
    public GameObject startIntroHud;

    [Header("HUD - VHS Counter visibility")]
    public string[] showCounterInScenes;

    [Header("Collection Goal")]
    [Min(1)] public int targetVHS = 25;
    [SerializeField] public int currentVHS = 0;

    [Header("HUD - Counter")]
    [Tooltip("Drag the HUD TextMeshProUGUI GameObject here (or a parent of it).")]
    public GameObject vhsCounterObject;
    private TMP_Text vhsCounterText;

    [Header("HUD - Milestone Instructions")]
    [Tooltip("Flash panel/text that auto-hides after 'flashDuration'.")]
    public GameObject instructionHudFlash;
    public float flashDuration = 2f;

    [Space(6)]
    [Tooltip("Persistent panel/text that stays visible after milestone.")]
    public GameObject instructionHudPersistent;

    // Key spawn instruction HUDs
    [Header("HUD - Key Spawn Instructions")]
    [Tooltip("Flash panel/text that auto-hides after 'keyFlashDuration'.")]
    public GameObject instructionHud2Flash;
    public float keyFlashDuration = 2f;

    [Space(6)]
    [Tooltip("Persistent panel/text that stays visible after key spawns.")]
    public GameObject instructionHud2Persistent;

    // --- KEY PICKUP HUDs ---
    [Header("HUD - Key Pickup")]
    [Tooltip("Flash panel/text that auto-hides after 'keyPickupFlashDuration'.")]
    public GameObject keyPickupFlash;
    public float keyPickupFlashDuration = 2f;

    [Header("Key Pickup - Show/Enable")]
    public GameObject[] enableOnKeyPickup;

    // --- TIMER HUD ---
    [Header("HUD - Timer (Persistent)")]
    public GameObject timerHudObject;
    public TMP_Text timerHudText;

    [Header("HUD - Game End")]
    public GameObject gameEndHud;
    public TMP_Text endGameTimeText;
    public TMP_Text endGameDeathCounter;
    public TMP_Text endGameScore;
    public EndHudAudioLoop endHudAudio;

    [Header("References")]
    public MonoBehaviour lookScript;

    [Space(6)]
    [Tooltip("Persistent panel/text that stays visible after picking up the key.")]
    public GameObject keyPickupPersistent;

    [Header("State")]
    public GameState State { get; private set; } = GameState.Playing;
    public bool vhsMilestoneReached { get; private set; } = false;
    public bool keyIsSpawned { get; set; } = false;
    public bool hasExitKey { get; private set; } = false;
    public event System.Action<int, int> OnVHSCountChanged;

    [Header("End State - Freeze Stuff")]
    public Transform[] detachOnEnd;
    public Behaviour[] disableOnEnd;

    // Event: VHS milestone reached
    public event System.Action OnVHSMilestone;
    bool _instruction2Shown = false;
    public int DeathCount { get; private set; } = 0;
    private float _elapsedTime = 0f;
    private bool _timerRunning = false;
    public bool IsHandlingDeath { get; set; } = false;
    int _sceneLoadsSinceAwake = 0;
    bool _counterActivated = false;

    /// <summary>
    /// Initializes the singleton, resets timescale, caches HUD references, 
    /// and sets initial HUD visibility. Also ensures timer and counters start in a known state.
    /// </summary>
    void Awake()
    {
        // Stop end screen music if coming from a previous session/end state
        endHudAudio.StopLoop();
        
        // Standard singleton pattern with persistence across scenes
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        Time.timeScale = 1f;        // Ensure gameplay resumes at normal speed when entering a scene
        CacheCounterText();         // Find TMP_Text for the VHS counter once
        if (startIntroHud) startIntroHud.SetActive(true);

        if (timerHudObject) timerHudObject.SetActive(true);
        UpdateTimerUI();            // Render initial timer value (00:00)

        // Ensure both instruction HUD groups start hidden (clean initial UI)
        if (instructionHudFlash) instructionHudFlash.SetActive(false);
        if (instructionHudPersistent) instructionHudPersistent.SetActive(false);

        if (instructionHud2Flash) instructionHud2Flash.SetActive(false);
        if (instructionHud2Persistent) instructionHud2Persistent.SetActive(false);

        if (keyPickupFlash) keyPickupFlash.SetActive(false);
        if (keyPickupPersistent) keyPickupPersistent.SetActive(false);
        if (vhsCounterObject) vhsCounterObject.SetActive(false);

        UpdateVHSCounterUI();       // Show 0/Target if/when counter becomes active
        OnVHSCountChanged?.Invoke(currentVHS, targetVHS); // Inform listeners of initial state
    }

    /// <summary>
    /// Subscribes to scene load events so the player can be repositioned at the correct spawn.
    /// </summary>
    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    /// <summary>
    /// Unsubscribes from scene load events to avoid callbacks after destruction/disable.
    /// </summary>
    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    /// <summary>
    /// Loads a scene by name without additional logic. 
    /// Useful when another system already chose the scene and spawn.
    /// </summary>
    public void LoadSceneWithSpawn(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    /// <summary>
    /// Returns true if the VHS counter HUD should be shown in the given scene.
    /// </summary>
    bool ShouldShowCounterFor(string sceneName)
    {
        if (showCounterInScenes == null) return false;
        for (int i = 0; i < showCounterInScenes.Length; i++)
            if (!string.IsNullOrEmpty(showCounterInScenes[i]) && showCounterInScenes[i] == sceneName)
                return true;
        return false;
    }

    /// <summary>
    /// When a new scene loads, move the player to the configured spawn (if any),
    /// and toggle the counter HUD depending on the scene's rules.
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!playerRoot || spawns == null) return;

        // Find a matching spawn for this scene
        Transform spawn = null;
        foreach (var s in spawns)
            if (s != null && s.spawn && s.sceneName == scene.name) { spawn = s.spawn; break; }
        if (!spawn) return;

        // Temporarily disable CharacterController to avoid collision issues while teleporting
        var cc = playerRoot.GetComponent<CharacterController>();
        if (cc) cc.enabled = false;
        playerRoot.SetPositionAndRotation(spawn.position, spawn.rotation);
        if (cc) cc.enabled = true;

        // Activate the VHS counter the first time we enter an allowed scene
        if (!_counterActivated && ShouldShowCounterFor(scene.name))
        {
            if (startIntroHud) startIntroHud.SetActive(false);
            if (vhsCounterObject) vhsCounterObject.SetActive(true);
            UpdateVHSCounterUI();
            _counterActivated = true;
        }
    }

    /// <summary>
    /// Boot-time setup that ensures counter text is ready and starts the game timer.
    /// </summary>
    void Start()
    {
        if (vhsCounterText == null) CacheCounterText();
        UpdateVHSCounterUI();
        ResetTimer(startRunning: true);
    }

    /// <summary>
    /// Per-frame updates:
    /// - Triggers key-spawn instruction swap once when key appears.
    /// - Advances timer while in active gameplay states.
    /// </summary>
    void Update()
    {
        // Transition instruction sets when the key is spawned (only once)
        if (keyIsSpawned && !_instruction2Shown)
        {
            _instruction2Shown = true;

            // Clean up first instruction set to avoid conflicting guidance
            if (instructionHudPersistent) instructionHudPersistent.SetActive(false);
            if (instructionHudFlash) instructionHudFlash.SetActive(false);

            ShowKeySpawnInstructions();
        }

        // Accumulate timer only during active play
        if (_timerRunning && State != GameState.Paused && State != GameState.Ended)
        {
            _elapsedTime += Time.deltaTime;
            UpdateTimerUI();
        }
    }

    /// <summary>
    /// Increments the death counter and notifies listeners (e.g., HUD).
    /// </summary>
    public void RegisterDeath()
    {
        DeathCount++;
        OnDeathCountChanged?.Invoke(DeathCount);
        Debug.Log($"Deaths: {DeathCount}");
    }

    /// <summary>
    /// Finds a spawn Transform for a given scene by name (or null if none found).
    /// </summary>
    public Transform GetSpawnForScene(string sceneName)
    {
        if (spawns == null) return null;
        foreach (var s in spawns)
            if (s != null && s.spawn && s.sceneName == sceneName)
                return s.spawn;
        return null;
    }

    /// <summary>
    /// Attempts to collect one VHS:
    /// - Fails if not in Playing state or already at target.
    /// - Updates counters and UI; fires milestone and events if target reached.
    /// </summary>
    public bool CollectVHS()
    {
        if (State != GameState.Playing) return false;
        if (currentVHS >= targetVHS) return false;

        currentVHS++;
        OnVHSCountChanged?.Invoke(currentVHS, targetVHS);
        UpdateVHSCounterUI();

        // Reaching target flips milestone and shows instructions for next step
        if (currentVHS >= targetVHS)
        {
            vhsMilestoneReached = true;
            if (vhsCounterObject) vhsCounterObject.SetActive(false);
            ShowMilestoneInstructions();
            OnVHSMilestone?.Invoke();
        }
        return true;
    }

    /// <summary>
    /// Marks the exit key as collected and shows key-related HUD feedback 
    /// (flash + persistent instructions, enabling configured objects).
    /// </summary>
    public bool CollectKey()
    {
        if (hasExitKey) return false;   // prevent double-pickup
        hasExitKey = true;
        HideKeySpawnInstructions();     // Remove key-spawn hints now that key is taken
        SetActiveAll(enableOnKeyPickup, true);

        // Flash HUD for immediate feedback
        if (keyPickupFlash)
        {
            keyPickupFlash.SetActive(true);
            StartCoroutine(HideKeyPickupFlashAfterDelay());
        }

        // Persistent HUD can remain visible to guide next steps
        if (keyPickupPersistent)
            keyPickupPersistent.SetActive(true);

        return true;
    }

    /// <summary>
    /// Sets all objects in the provided list active/inactive (null-safe).
    /// </summary>
    void SetActiveAll(GameObject[] list, bool active)
    {
        if (list == null) return;
        for (int i = 0; i < list.Length; i++)
            if (list[i]) list[i].SetActive(active);
    }

    /// <summary>
    /// Hides the key-pickup flash after a configured delay.
    /// </summary>
    private System.Collections.IEnumerator HideKeyPickupFlashAfterDelay()
    {
        float t = Mathf.Max(0f, keyPickupFlashDuration);
        if (t > 0f)
            yield return new WaitForSeconds(t);

        if (keyPickupFlash)
            keyPickupFlash.SetActive(false);
    }

    /// <summary>
    /// Locates and caches the TMP_Text used by the VHS counter HUD.
    /// Logs a warning if not found to help with scene setup debugging.
    /// </summary>
    void CacheCounterText()
    {
        if (!vhsCounterObject) { Debug.LogWarning("[GameManager] vhsCounterObject not assigned."); return; }
        vhsCounterText = vhsCounterObject.GetComponent<TMP_Text>()
                      ?? vhsCounterObject.GetComponentInChildren<TMP_Text>(true);
        if (!vhsCounterText) Debug.LogWarning("[GameManager] No TMP_Text found for vhsCounterObject.");
    }

    /// <summary>
    /// Updates the VHS counter HUD text to reflect current progress.
    /// </summary>
    void UpdateVHSCounterUI()
    {
        if (vhsCounterText) vhsCounterText.text = $"{currentVHS} / {targetVHS}";
    }

    /// <summary>
    /// Shows milestone instructions (flash + persistent) once the VHS target is met.
    /// Uses a coroutine to auto-hide the flash after a delay.
    /// </summary>
    void ShowMilestoneInstructions()
    {
        // Flash HUD for immediate, temporary guidance
        if (instructionHudFlash)
        {
            instructionHudFlash.SetActive(true);
            var tmp = instructionHudFlash.GetComponent<TMP_Text>()
                   ?? instructionHudFlash.GetComponentInChildren<TMP_Text>(true);

            // Avoid overlapping coroutines if triggered multiple times by mistake
            StopAllCoroutines();
            StartCoroutine(HideFlashAfterDelay());
        }

        // Persistent HUD remains as a reminder of the next step
        if (instructionHudPersistent)
        {
            instructionHudPersistent.SetActive(true);
            var tmp = instructionHudPersistent.GetComponent<TMP_Text>()
                   ?? instructionHudPersistent.GetComponentInChildren<TMP_Text>(true);
        }
    }

    /// <summary>
    /// Hides the first milestone flash after the configured duration.
    /// </summary>
    System.Collections.IEnumerator HideFlashAfterDelay()
    {
        float t = Mathf.Max(0f, flashDuration);
        if (t > 0f) yield return new WaitForSeconds(t);
        if (instructionHudFlash) instructionHudFlash.SetActive(false);
    }

    /// <summary>
    /// Shows the second set of instructions when the key spawns 
    /// (flash + persistent), with auto-hide for the flash.
    /// </summary>
    void ShowKeySpawnInstructions()
    {
        // Flash HUD #2: one-time attention grabber for key spawn
        if (instructionHud2Flash)
        {
            instructionHud2Flash.SetActive(true);
            var tmp = instructionHud2Flash.GetComponent<TMP_Text>()
                   ?? instructionHud2Flash.GetComponentInChildren<TMP_Text>(true);
            StartCoroutine(HideKeyFlashAfterDelay());
        }

        // Persistent HUD #2: remains visible until key pickup
        if (instructionHud2Persistent)
        {
            instructionHud2Persistent.SetActive(true);
            var tmp = instructionHud2Persistent.GetComponent<TMP_Text>()
                   ?? instructionHud2Persistent.GetComponentInChildren<TMP_Text>(true);
        }
    }

    /// <summary>
    /// Hides the key-spawn flash after the configured duration.
    /// </summary>
    System.Collections.IEnumerator HideKeyFlashAfterDelay()
    {
        float t = Mathf.Max(0f, keyFlashDuration);
        if (t > 0f) yield return new WaitForSeconds(t);
        if (instructionHud2Flash) instructionHud2Flash.SetActive(false);
    }

    /// <summary>
    /// Pauses gameplay by setting state and timescale, and halts timer accumulation.
    /// </summary>
    public void PauseGame()
    {
        if (State == GameState.Playing)
        {
            State = GameState.Paused;
            Time.timeScale = 0f;
            StopTimer();
        }
    }

    /// <summary>
    /// Resumes gameplay by restoring state and timescale, and restarts the timer.
    /// </summary>
    public void ResumeGame()
    {
        if (State == GameState.Paused)
        {
            State = GameState.Playing;
            Time.timeScale = 1f;
            StartTimer();
        }
    }

    /// <summary>
    /// Hides key-spawn instruction HUDs (used after key pickup).
    /// </summary>
    void HideKeySpawnInstructions()
    {
        if (instructionHud2Flash) instructionHud2Flash.SetActive(false);
        if (instructionHud2Persistent) instructionHud2Persistent.SetActive(false);
    }

    /// <summary>
    /// Transitions to the end-game state:
    /// - Freezes gameplay, disables look/motion, and detaches configured transforms.
    /// - Updates and shows the end-game HUD with time/deaths/score.
    /// - Unlocks cursor and plays end-screen audio loop.
    /// </summary>
    public void ShowGameEndHUD()
    {
        // Clean up any other HUD elements before showing the end screen
        if (instructionHud2Flash) instructionHud2Flash.SetActive(false);
        if (instructionHud2Persistent) instructionHud2Persistent.SetActive(false);
        if (keyPickupFlash) keyPickupFlash.SetActive(false);
        if (keyPickupPersistent) keyPickupPersistent.SetActive(false);
        if (timerHudObject) timerHudObject.SetActive(false);

        SetActiveAll(enableOnKeyPickup, false);

        // Enter end state and compute final score snapshot
        State = GameState.Ended;
        int score = ComputeScore(_elapsedTime, DeathCount);
        Time.timeScale = 0f;
        StopTimer();

        // Stop camera movement and gameplay behaviours
        if (lookScript != null) lookScript.enabled = false;
        DisableAll(disableOnEnd);
        DetachAll(detachOnEnd);

        // Give control back to the cursor for UI interaction
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Update end HUD stats
        if (endGameDeathCounter != null)
            endGameDeathCounter.text = $"Deaths: {DeathCount}";

        if (endGameTimeText != null)
            endGameTimeText.text = $"Time: {FormatTime(_elapsedTime)}";

        if (endGameScore != null)
            endGameScore.text = $"Score: {score}";

        if (gameEndHud) gameEndHud.SetActive(true);

        // Start looping end-screen audio
        endHudAudio.PlayLoop();
    }

    /// <summary>
    /// Formats seconds into mm:ss for display.
    /// </summary>
    string FormatTime(float t)
    {
        int minutes = Mathf.FloorToInt(t / 60f);
        int seconds = Mathf.FloorToInt(t % 60f);
        return $"{minutes:00}:{seconds:00}";
    }

    /// <summary>
    /// Pushes the current _elapsedTime into the timer HUD text.
    /// </summary>
    void UpdateTimerUI()
    {
        if (timerHudText != null)
            timerHudText.text = FormatTime(_elapsedTime);
    }

    /// <summary>
    /// Resets the timer to zero and optionally starts it immediately.
    /// </summary>
    public void ResetTimer(bool startRunning = true)
    {
        _elapsedTime = 0f;
        _timerRunning = startRunning;
        UpdateTimerUI();
    }

    /// <summary>
    /// Starts accumulating time (used by pause/resume flow).
    /// </summary>
    public void StartTimer() { _timerRunning = true; }

    /// <summary>
    /// Stops accumulating time (used by pause/resume/end flow).
    /// </summary>
    public void StopTimer() { _timerRunning = false; }

    /// <summary>
    /// Computes a final score from elapsed time and deaths using smooth penalty curves.
    /// Designed so longer times and more deaths reduce the score, with gentle falloff.
    /// </summary>
    int ComputeScore(float elapsedSeconds, int deaths)
    {
        const int MAX_SCORE = 999, MIN_SCORE = 1;

        // TIME penalty (gentle, centered around +15 min)
        const float FREE_TIME_SEC = 300f;   // 5:00 free
        const float TIME_HALF_MIN = 15f;    // +15 min over -> ~0.5
        const float KT = 0.22f;             // slope (lower = softer)

        float minutesOver = Mathf.Max(0f, (elapsedSeconds - FREE_TIME_SEC) / 60f);
        float timeFactor = (minutesOver <= 0f)
            ? 1f
            : 1f / (1f + Mathf.Exp(KT * (minutesOver - TIME_HALF_MIN)));

        // DEATHS penalty (smooth, “harder to get high scores”)
        // f(d) = 1 / (1 + (d/D0)^P)
        // Tuned anchors: 10→~0.70, 30→~0.30, 50→~0.16 (and 0→1.0)
        const float D0 = 17.320508f;  // scale
        const float P = 1.5424875f;   // curvature

        float deathsFactor = 1f / (1f + Mathf.Pow(Mathf.Max(0f, deaths) / D0, P));

        // COMBINE
        float combined = timeFactor * deathsFactor;
        int score = Mathf.RoundToInt(MIN_SCORE + (MAX_SCORE - MIN_SCORE) * combined);
        return Mathf.Clamp(score, MIN_SCORE, MAX_SCORE);
    }

    /// <summary>
    /// Disables a list of behaviours safely (used when ending the game).
    /// </summary>
    void DisableAll(Behaviour[] list)
    {
        if (list == null) return;
        foreach (var b in list) if (b) b.enabled = false;
    }

    /// <summary>
    /// Detaches transforms from their parents (e.g., to freeze UI/camera hierarchy on end).
    /// </summary>
    void DetachAll(Transform[] list)
    {
        if (list == null) return;
        foreach (var t in list) if (t) t.SetParent(null, true); // keep world pos
    }
}