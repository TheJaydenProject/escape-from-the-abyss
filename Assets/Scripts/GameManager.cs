using UnityEngine;
using TMPro;

public enum GameState { Playing, MilestoneReached, Paused }

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

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

    [Space(6)]
    [Tooltip("Persistent panel/text that stays visible after picking up the key.")]
    public GameObject keyPickupPersistent;

    public bool hasExitKey { get; private set; } = false;

    [Header("State")]
    public GameState State { get; private set; } = GameState.Playing;
    public bool vhsMilestoneReached { get; private set; } = false;
    public bool keyIsSpawned { get; set; } = false;

    // Event: VHS count updated
    public event System.Action<int, int> OnVHSCountChanged;
    // Event: VHS milestone reached
    public event System.Action OnVHSMilestone;

    bool _instruction2Shown = false;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        CacheCounterText();
        if (vhsCounterObject) vhsCounterObject.SetActive(true);
        UpdateVHSCounterUI();
        OnVHSCountChanged?.Invoke(currentVHS, targetVHS);

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
            State = GameState.MilestoneReached;
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
        if (State == GameState.Playing) { State = GameState.Paused; Time.timeScale = 0f; }
    }

    public void ResumeGame()
    {
        if (State == GameState.Paused) { State = GameState.Playing; Time.timeScale = 1f; }
    }
    

    void HideKeySpawnInstructions()
    {
        if (instructionHud2Flash)      instructionHud2Flash.SetActive(false);
        if (instructionHud2Persistent) instructionHud2Persistent.SetActive(false);
    }

}

