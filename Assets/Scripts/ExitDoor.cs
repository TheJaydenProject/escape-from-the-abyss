using UnityEngine;

public class ExitDoor : MonoBehaviour, IInteractable
{
    [Header("Locked feedback")]
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

            var hud = interactor != null ? interactor.doorLockedHud : null;
            if (hud) StartCoroutine(FlashLocked(hud));

            return;
        }

        if (openSfx) openSfx.Play();
        GameManager.Instance.ShowGameEndHUD();
    }

    private System.Collections.IEnumerator FlashLocked(GameObject hud)
    {
        if (!hud) yield break;
        hud.SetActive(true);
        yield return new WaitForSeconds(Mathf.Max(0f, lockedFlashSeconds));
        if (hud) hud.SetActive(false);
    }
}
