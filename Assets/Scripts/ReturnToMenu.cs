/*
 * Author: Jayden Wong
 * Date: 14 August 2025
 * Description: Provides a simple method to return the player to the Main Menu scene.
 *              Ensures the game time scale is reset and the cursor is unlocked
 *              and made visible before switching scenes.
 */

using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Utility script that transitions the game back to the Main Menu.
/// Resets Time.timeScale, unlocks and shows the cursor, 
/// then loads the "MainMenu" scene.
/// </summary>
public class ReturnToMenu : MonoBehaviour
{
    /// <summary>
    /// Loads the Main Menu scene:
    /// - Resets time scale (in case the game was paused or slowed).
    /// - Unlocks and shows the mouse cursor so the player can interact with menus.
    /// - Switches to the "MainMenu" scene.
    /// </summary>
    public void LoadMenu()
    {
        // Ensure the game runs at normal speed again
        Time.timeScale = 1f;

        // Unlock and show the mouse cursor so the user can interact with UI
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Load the Main Menu scene (must be added in Build Settings)
        SceneManager.LoadScene("MainMenu");
    }
}
