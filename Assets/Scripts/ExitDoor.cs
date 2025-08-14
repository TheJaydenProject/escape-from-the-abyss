using UnityEngine;

public class ExitDoor : MonoBehaviour, IInteractable
{
    [Header("Locked feedback")]
    public GameObject lockedFlashHud;   // e.g., "Door is locked"
    public float lockedFlashSeconds = 2f;
    public AudioClip lockedSfx;

    [Header("Open (end game)")]
    public AudioClip openSfx;           // optional

    public string PromptText =>
        (GameManager.Instance != null && GameManager.Instance.hasExitKey)
        ? "[E] Open"
        : "[E] Try door";

    public void Interact(PlayerInteractorRaycast interactor)
    {
        if (GameManager.Instance == null) return;

        if (!GameManager.Instance.hasExitKey)
        {
            if (lockedSfx) AudioSource.PlayClipAtPoint(lockedSfx, transform.position);
            if (lockedFlashHud) StartCoroutine(FlashLocked());
            return;
        }

        // Has key → end game
        if (openSfx) AudioSource.PlayClipAtPoint(openSfx, transform.position);
        GameManager.Instance.ShowGameEndHUD();
    }

    private System.Collections.IEnumerator FlashLocked()
    {
        lockedFlashHud.SetActive(true);
        yield return new WaitForSeconds(Mathf.Max(0f, lockedFlashSeconds));
        if (lockedFlashHud) lockedFlashHud.SetActive(false);
    }
}
