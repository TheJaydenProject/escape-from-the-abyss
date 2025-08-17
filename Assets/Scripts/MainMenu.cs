/*
 * Author: Jayden Wong
 * Date: 15 August 2025
 * Description: Controls the main menu flow: shows/hides panels, handles button clicks,
 *              plays optional UI click SFX, and starts/quits the game. Also manages
 *              cursor and time scale so the transition into gameplay is clean.
 */

using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    [Header("Scene")]
    public string gameplaySceneName = "Lcorridor";

    [Header("Panels")]
    public GameObject mainPanel;
    public GameObject optionsPanel;
    public GameObject creditsPanel;
    public GameObject howToPlayPanel;

    [Header("SFX (optional)")]
    public AudioSource uiClick;

    GameObject[] _allPanels;

    /// <summary>
    /// Initializes the menu:
    /// - Caches all panels for easy toggling.
    /// - Shows the Main panel by default.
    /// - Ensures the cursor is visible and unlocked for menu navigation.
    /// </summary>
    void Awake()
    {
        // Cache all panels so we can toggle them as a group
        _allPanels = new[] { mainPanel, optionsPanel, creditsPanel, howToPlayPanel };
        ShowMain(); // Land on the main screen first

        // Menus need a free cursor for UI interaction
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    /// <summary>
    /// Listens for the ESC key to quickly return to the Main panel
    /// when on any sub-panel (Options/Credits/HowToPlay).
    /// </summary>
    void Update()
    {
        // ESC → back to Main (if not already there)
        if (Input.GetKeyDown(KeyCode.Escape) && mainPanel && !mainPanel.activeSelf)
            ShowMain();
    }

    // ===== Button hooks =====

    /// <summary>
    /// Starts gameplay:
    /// - Plays click SFX (optional).
    /// - Resets time scale (in case menus were paused).
    /// - Locks and hides the cursor for FPS control.
    /// - Destroys any existing GameManager instance to avoid duplicates.
    /// - Loads the gameplay scene.
    /// </summary>
    public void OnPlay()
    {
        Click();
        Time.timeScale = 1f;                    // Ensure normal game speed
        Cursor.lockState = CursorLockMode.Locked; // FPS: lock mouse to the center
        Cursor.visible = false;                 // Hide cursor during gameplay

        // If a persistent GameManager exists from a prior session, remove it before loading
        if (GameManager.Instance) Destroy(GameManager.Instance.gameObject);
        SceneManager.LoadScene(gameplaySceneName, LoadSceneMode.Single);
    }

    /// <summary>
    /// Opens the Options panel and plays click SFX (optional).
    /// </summary>
    public void OnOptions()   { Click(); ShowPanel(optionsPanel); }

    /// <summary>
    /// Opens the Credits panel and plays click SFX (optional).
    /// </summary>
    public void OnCredits()   { Click(); ShowPanel(creditsPanel); }

    /// <summary>
    /// Opens the How-To-Play panel and plays click SFX (optional).
    /// </summary>
    public void OnHowToPlay() { Click(); ShowPanel(howToPlayPanel); }

    /// <summary>
    /// Quits the game:
    /// - In Editor: stops Play Mode.
    /// - In Build: exits the application.
    /// Plays click SFX (optional) before quitting.
    /// </summary>
    public void OnQuit()
    {
        Click();
        #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false; // Stop play mode
        #else
                Application.Quit(); // Close the application
        #endif
    }

    /// <summary>
    /// Shows the Main panel (used by ESC or Main button), hiding others.
    /// </summary>
    public void ShowMain() { ShowPanel(mainPanel); }

    // ===== Helpers =====

    /// <summary>
    /// Shows exactly one target panel and hides all others.
    /// Keeps UI state consistent no matter which button was pressed.
    /// </summary>
    void ShowPanel(GameObject target)
    {
        foreach (var p in _allPanels)
            if (p) p.SetActive(p == target);
    }

    /// <summary>
    /// Plays a UI click sound if an AudioSource is assigned.
    /// Kept separate so all buttons share the same feedback.
    /// </summary>
    void Click() { if (uiClick) uiClick.Play(); }
}
