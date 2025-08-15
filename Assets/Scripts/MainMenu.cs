using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    [Header("Scene")]
    [Tooltip("Name of the scene to load when pressing Play.")]
    public string gameplaySceneName = "Lcorridor";

    [Header("Panels")]
    public GameObject mainPanel;
    public GameObject optionsPanel;
    public GameObject creditsPanel;
    public GameObject howToPlayPanel;

    [Header("First Selected (optional, for keyboard/controller)")]
    public Selectable firstMain;
    public Selectable firstOptions;
    public Selectable firstCredits;
    public Selectable firstHowTo;

    [Header("SFX (optional)")]
    public AudioSource uiClick;

    GameObject[] _allPanels;

    void Awake()
    {
        _allPanels = new[] { mainPanel, optionsPanel, creditsPanel, howToPlayPanel };

        // Default to Main panel on start
        ShowMain();

        // Ensure cursor visible on menu
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void Update()
    {
        // ESC: back to Main (unless already there)
        if (Input.GetKeyDown(KeyCode.Escape) && mainPanel && !mainPanel.activeSelf)
            ShowMain();
    }

    // ===== Button hooks =====

    public void OnPlay()
    {
        Click();

        // make sure we're not stuck in a paused/ended timescale
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // nuke old persistent state for a truly fresh run
        if (GameManager.Instance != null)
            Destroy(GameManager.Instance.gameObject);

        SceneManager.LoadScene(gameplaySceneName, LoadSceneMode.Single);
    }

    public void OnOptions()   { Click(); ShowPanel(optionsPanel, firstOptions); }
    public void OnCredits()   { Click(); ShowPanel(creditsPanel, firstCredits); }
    public void OnHowToPlay() { Click(); ShowPanel(howToPlayPanel, firstHowTo); }
    public void OnQuit()
    {
        Click();
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }

    public void ShowMain() { ShowPanel(mainPanel, firstMain); }

    // ===== Helpers =====
    void ShowPanel(GameObject target, Selectable first = null)
    {
        foreach (var p in _allPanels)
            if (p) p.SetActive(p == target);

        // set UI focus
        if (first)
        {
            EventSystem.current?.SetSelectedGameObject(null);
            first.Select();
        }
    }

    void Click() { if (uiClick) uiClick.Play(); }
}
