using UnityEngine;
using TMPro; // <-- TMP

public enum GameState { Playing, Won, Paused }

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Collection")]
    [Min(1)] public int targetVHS = 20;
    public int currentVHS { get; private set; }

    [Header("HUD")]
    [Tooltip("Drag the HUD TextMeshProUGUI GameObject here (or a parent of it).")]
    public GameObject vhsCounterObject;

    // Cache TMP component
    private TMP_Text vhsCounterText;

    [Header("State")]
    public GameState State { get; private set; } = GameState.Playing;

    public event System.Action<int, int> OnVHSCountChanged;
    public event System.Action OnWin;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        CacheHudTextComponent();
        UpdateVHSCounterUI();
        OnVHSCountChanged?.Invoke(currentVHS, targetVHS);
    }

    void Start()
    {
        if (vhsCounterText == null) CacheHudTextComponent();
        UpdateVHSCounterUI();
    }

    public bool CollectVHS()
    {
        if (State != GameState.Playing) return false;
        if (currentVHS >= targetVHS)     return false;

        currentVHS++;
        OnVHSCountChanged?.Invoke(currentVHS, targetVHS);
        UpdateVHSCounterUI();

        if (currentVHS >= targetVHS)
        {
            State = GameState.Won;
            OnWin?.Invoke();
        }
        return true;
    }

    void CacheHudTextComponent()
    {
        if (vhsCounterObject == null)
        {
            Debug.LogWarning("[GameManager] vhsCounterObject not assigned.");
            return;
        }

        // Try on the object itself first…
        vhsCounterText = vhsCounterObject.GetComponent<TMP_Text>();
        // …then search children (covers cases where the TMP is on a child)
        if (vhsCounterText == null)
            vhsCounterText = vhsCounterObject.GetComponentInChildren<TMP_Text>(true);

        if (vhsCounterText == null)
            Debug.LogWarning("[GameManager] No TMP_Text found on vhsCounterObject or its children.");
    }

    void UpdateVHSCounterUI()
    {
        if (vhsCounterText != null)
            vhsCounterText.text = $"{currentVHS} / {targetVHS}";
    }

    public void PauseGame()
    {
        if (State == GameState.Playing) { State = GameState.Paused; Time.timeScale = 0f; }
    }

    public void ResumeGame()
    {
        if (State == GameState.Paused) { State = GameState.Playing; Time.timeScale = 1f; }
    }
}
