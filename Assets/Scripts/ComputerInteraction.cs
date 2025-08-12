using UnityEngine;

public class ComputerTerminal : MonoBehaviour, IInteractable
{
    [Header("Key Spawn")]
    public GameObject keyPrefab;     // assign in Inspector
    public Transform keySpawnPoint;  // assign in Inspector

    private bool keySpawned = false;

    public string PromptText => string.Empty; // no prompt shown

    public void Interact(PlayerInteractorRaycast interactor)
    {
        var gm = GameManager.Instance;
        if (gm == null) return;

        // Only allow spawn if milestone reached
        if (!gm.vhsMilestoneReached) return;

        // Spawn the key once
        if (!keySpawned && keyPrefab != null && keySpawnPoint != null)
        {
            Instantiate(keyPrefab, keySpawnPoint.position, keySpawnPoint.rotation);
            keySpawned = true;

            // Let GameManager know
            gm.keyIsSpawned = true;

            Debug.Log("[ComputerTerminal] Key spawned.");
        }
    }
}
