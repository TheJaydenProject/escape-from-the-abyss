using UnityEngine;
using System.Collections;

public class LockedDoor : MonoBehaviour, IInteractable
{
    [Header("Locked Feedback")]
    public AudioSource lockedSfx;        // plays when trying the door
    public GameObject lockedFlashHud;    // HUD panel to show when locked
    public float lockedFlashSeconds = 2f;

    [Header("Reveal After First Try")]
    public GameObject objectToShow;      // assign object in Inspector
    private bool hasTriedDoor = false;

    public string PromptText => "[E] Try door";

    public void Interact(PlayerInteractorRaycast interactor)
    {
        // Play locked feedback
        if (lockedSfx) lockedSfx.Play();
        if (lockedFlashHud) StartCoroutine(FlashLocked());

        // First-time interaction logic
        if (!hasTriedDoor)
        {
            hasTriedDoor = true;

            if (objectToShow != null)
                objectToShow.SetActive(true);
        }
    }

    private IEnumerator FlashLocked()
    {
        lockedFlashHud.SetActive(true);
        yield return new WaitForSeconds(Mathf.Max(0f, lockedFlashSeconds));
        if (lockedFlashHud) lockedFlashHud.SetActive(false);
    }
}
