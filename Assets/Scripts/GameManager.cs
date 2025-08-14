using UnityEngine;
using TMPro;
using System;

public enum GameState { Playing, Paused, Ended }

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public event Action<int> OnDeathCountChanged;

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

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        CacheCounterText();
        if (vhsCounterObject) vhsCounterObject.SetActive(true);
        UpdateVHSCounterUI();
        OnVHSCountChanged?.Invoke(currentVHS, targetVHS);

        if (timerHudObject) timerHudObject.SetActive(true);
        UpdateTimerUI();

        // Ensure both instruction HUDs start hidden
        if (instructionHudFlash) instructionHudFlash.SetActive(false);
        if (instructionHudPersistent) instructionHudPersistent.SetActive(false);

        if (instructionHud2Flash) instructionHud2Flash.SetActive(false);
        if (instructionHud2Persistent) instructionHud2Persistent.SetActive(false);

        if (keyPickupFlash) keyPickupFlash.SetActive(false);
        if (keyPickupPersistent) keyPickupPersistent.SetActive(false);
    }

    void Start()
    {
        if (vhsCounterText == null) CacheCounterText();
        UpdateVHSCounterUI();
        ResetTimer(startRunning: true);
    }

    void Update()
    {
        // When the key spawns, hide the first persistent HUD and show instruction HUD #2 (once)
        if (keyIsSpawned && !_instruction2Shown)
        {
            _instruction2Shown = true;

            // Hide the first persistent instructions if they are showing
            if (instructionHudPersistent) instructionHudPersistent.SetActive(false);
            if (instructionHudFlash) instructionHudFlash.SetActive(false);

            ShowKeySpawnInstructions();
        }

        // TIMER
        if (_timerRunning && State != GameState.Paused && State != GameState.Ended)
        {
            _elapsedTime += Time.deltaTime;
            UpdateTimerUI();
        }
    }

    public void RegisterDeath()
    {
        DeathCount++;
        OnDeathCountChanged?.Invoke(DeathCount);
        // Debug.Log($"Deaths: {DeathCount}");
    }

    public bool CollectVHS()
    {
        if (State != GameState.Playing) return false;
        if (currentVHS >= targetVHS) return false;

        currentVHS++;
        OnVHSCountChanged?.Invoke(currentVHS, targetVHS);
        UpdateVHSCounterUI();

        if (currentVHS >= targetVHS)
        {
            vhsMilestoneReached = true;
            if (vhsCounterObject) vhsCounterObject.SetActive(false);
            ShowMilestoneInstructions();
            OnVHSMilestone?.Invoke();
        }
        return true;
    }


    public bool CollectKey()
    {
        if (hasExitKey) return false;   // prevent double-pickup
        hasExitKey = true;
        HideKeySpawnInstructions();
        SetActiveAll(enableOnKeyPickup, true);

        // Flash HUD
        if (keyPickupFlash)
        {
            keyPickupFlash.SetActive(true);
            StartCoroutine(HideKeyPickupFlashAfterDelay());
        }

        // Persistent HUD
        if (keyPickupPersistent)
            keyPickupPersistent.SetActive(true);

        return true;
    }

    void SetActiveAll(GameObject[] list, bool active)
    {
        if (list == null) return;
        for (int i = 0; i < list.Length; i++)
            if (list[i]) list[i].SetActive(active);
    }

    private System.Collections.IEnumerator HideKeyPickupFlashAfterDelay()
    {
        float t = Mathf.Max(0f, keyPickupFlashDuration);
        if (t > 0f)
            yield return new WaitForSeconds(t);

        if (keyPickupFlash)
            keyPickupFlash.SetActive(false);
    }



    void CacheCounterText()
    {
        if (!vhsCounterObject) { Debug.LogWarning("[GameManager] vhsCounterObject not assigned."); return; }
        vhsCounterText = vhsCounterObject.GetComponent<TMP_Text>()
                      ?? vhsCounterObject.GetComponentInChildren<TMP_Text>(true);
        if (!vhsCounterText) Debug.LogWarning("[GameManager] No TMP_Text found for vhsCounterObject.");
    }

    void UpdateVHSCounterUI()
    {
        if (vhsCounterText) vhsCounterText.text = $"{currentVHS} / {targetVHS}";
    }

    void ShowMilestoneInstructions()
    {
        // Flash HUD
        if (instructionHudFlash)
        {
            instructionHudFlash.SetActive(true);
            var tmp = instructionHudFlash.GetComponent<TMP_Text>()
                   ?? instructionHudFlash.GetComponentInChildren<TMP_Text>(true);
            StopAllCoroutines(); // in case of multiple triggers, be safe
            StartCoroutine(HideFlashAfterDelay());
        }

        // Persistent HUD
        if (instructionHudPersistent)
        {
            instructionHudPersistent.SetActive(true);
            var tmp = instructionHudPersistent.GetComponent<TMP_Text>()
                   ?? instructionHudPersistent.GetComponentInChildren<TMP_Text>(true);
        }
    }

    System.Collections.IEnumerator HideFlashAfterDelay()
    {
        float t = Mathf.Max(0f, flashDuration);
        if (t > 0f) yield return new WaitForSeconds(t);
        if (instructionHudFlash) instructionHudFlash.SetActive(false);
    }

    // Show instruction HUDs for key spawn
    void ShowKeySpawnInstructions()
    {
        // Flash HUD 2
        if (instructionHud2Flash)
        {
            instructionHud2Flash.SetActive(true);
            var tmp = instructionHud2Flash.GetComponent<TMP_Text>()
                   ?? instructionHud2Flash.GetComponentInChildren<TMP_Text>(true);
            StartCoroutine(HideKeyFlashAfterDelay());
        }

        // Persistent HUD 2
        if (instructionHud2Persistent)
        {
            instructionHud2Persistent.SetActive(true);
            var tmp = instructionHud2Persistent.GetComponent<TMP_Text>()
                   ?? instructionHud2Persistent.GetComponentInChildren<TMP_Text>(true);
        }
    }

    System.Collections.IEnumerator HideKeyFlashAfterDelay()
    {
        float t = Mathf.Max(0f, keyFlashDuration);
        if (t > 0f) yield return new WaitForSeconds(t);
        if (instructionHud2Flash) instructionHud2Flash.SetActive(false);
    }

    public void PauseGame()
    {
        if (State == GameState.Playing)
        {
            State = GameState.Paused;
            Time.timeScale = 0f;
            StopTimer();
        }
    }

    public void ResumeGame()
    {
        if (State == GameState.Paused)
        {
            State = GameState.Playing;
            Time.timeScale = 1f;
            StartTimer();
        }
    }


    void HideKeySpawnInstructions()
    {
        if (instructionHud2Flash) instructionHud2Flash.SetActive(false);
        if (instructionHud2Persistent) instructionHud2Persistent.SetActive(false);
    }

    public void ShowGameEndHUD()
    {
        // Hide any other HUDs
        if (instructionHud2Flash) instructionHud2Flash.SetActive(false);
        if (instructionHud2Persistent) instructionHud2Persistent.SetActive(false);
        if (keyPickupFlash) keyPickupFlash.SetActive(false);
        if (keyPickupPersistent) keyPickupPersistent.SetActive(false);
        if (timerHudObject) timerHudObject.SetActive(false);


        // Stop the game
        State = GameState.Ended;
        Time.timeScale = 0f;
        StopTimer();

        // Stop camera movement
        if (lookScript != null) lookScript.enabled = false;
        DisableAll(disableOnEnd);         
        DetachAll(detachOnEnd);

        // Unlock & show cursor
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;


        // Update death count in end screen
        if (endGameDeathCounter != null)
            endGameDeathCounter.text = $"Deaths: {DeathCount}";

        // Update time in end screen
        if (endGameTimeText != null)
            endGameTimeText.text = $"Time: {FormatTime(_elapsedTime)}";

        // Show end screen
        if (gameEndHud) gameEndHud.SetActive(true);
    }

    string FormatTime(float t)
    {
        int minutes = Mathf.FloorToInt(t / 60f);
        int seconds = Mathf.FloorToInt(t % 60f);
        return $"{minutes:00}:{seconds:00}";
    }

    void UpdateTimerUI()
    {
        if (timerHudText != null)
            timerHudText.text = FormatTime(_elapsedTime);
    }

    public void ResetTimer(bool startRunning = true)
    {
        _elapsedTime = 0f;
        _timerRunning = startRunning;
        UpdateTimerUI();
    }

    public void StartTimer() { _timerRunning = true; }
    public void StopTimer() { _timerRunning = false; }
    

    void DisableAll(Behaviour[] list)
    {
        if (list == null) return;
        foreach (var b in list) if (b) b.enabled = false;
    }

    void DetachAll(Transform[] list)
    {
        if (list == null) return;
        foreach (var t in list) if (t) t.SetParent(null, true); // keep world pos
    }


}

