using UnityEngine;

public class CatchTrigger : MonoBehaviour
{
    public AIChaseController ai; // assign in inspector

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        Debug.Log($"Player detected at distance {Vector3.Distance(transform.position, other.transform.position):F2}");
        ai.Caught();
    }
}