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

    void Awake()
    {
        _allPanels = new[] { mainPanel, optionsPanel, creditsPanel, howToPlayPanel };
        ShowMain();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void Update()
    {
        // ESC → back to Main (if not already there)
        if (Input.GetKeyDown(KeyCode.Escape) && mainPanel && !mainPanel.activeSelf)
            ShowMain();
    }

    // ===== Button hooks =====
    public void OnPlay()
    {
        Click();
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (GameManager.Instance) Destroy(GameManager.Instance.gameObject);
        SceneManager.LoadScene(gameplaySceneName, LoadSceneMode.Single);
    }

    public void OnOptions()   { Click(); ShowPanel(optionsPanel); }
    public void OnCredits()   { Click(); ShowPanel(creditsPanel); }
    public void OnHowToPlay() { Click(); ShowPanel(howToPlayPanel); }
    public void OnQuit()
    {
        Click();
        #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
        #else
                Application.Quit();
        #endif
    }

    public void ShowMain() { ShowPanel(mainPanel); }

    // ===== Helpers =====
    void ShowPanel(GameObject target)
    {
        foreach (var p in _allPanels)
            if (p) p.SetActive(p == target);
    }

    void Click() { if (uiClick) uiClick.Play(); }
}
