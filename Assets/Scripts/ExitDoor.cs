using UnityEngine;

public class ExitDoor : MonoBehaviour, IInteractable
{
    [Header("Locked feedback")]
    public GameObject lockedFlashHud; 
    public float lockedFlashSeconds = 2f;
    public AudioSource lockedSfx;

    [Header("Open")]
    public AudioSource openSfx;       

    public string PromptText =>
        (GameManager.Instance != null && GameManager.Instance.hasExitKey)
        ? "[E] Open"
        : "[E] Try door";

    public void Interact(PlayerInteractorRaycast interactor)
    {
        if (GameManager.Instance == null) return;

        if (!GameManager.Instance.hasExitKey)
        {
            if (lockedSfx) lockedSfx.Play();
            if (lockedFlashHud) StartCoroutine(FlashLocked());
            return;
        }

        // Has key → end game
        if (openSfx) openSfx.Play();
        GameManager.Instance.ShowGameEndHUD();
    }

    private System.Collections.IEnumerator FlashLocked()
    {
        lockedFlashHud.SetActive(true);
        yield return new WaitForSeconds(Mathf.Max(0f, lockedFlashSeconds));
        if (lockedFlashHud) lockedFlashHud.SetActive(false);
    }
}
