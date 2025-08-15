using UnityEngine;

[RequireComponent(typeof(Collider))]
public class OneShotTriggerSwap : MonoBehaviour
{
    [Header("Who can trigger")]
    public LayerMask playerLayer;     // set this to your Player layer

    [Header("Swap")]
    public GameObject objectToHide;   // assign in Inspector
    public GameObject objectToShow;   // assign in Inspector

    bool _fired;

    void Reset()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (_fired) return;

        int otherBit = 1 << other.gameObject.layer;
        if ((playerLayer.value & otherBit) == 0) return;  // not player layer

        _fired = true;

        if (objectToHide) objectToHide.SetActive(false);
        if (objectToShow) objectToShow.SetActive(true);

        // prevent re-triggering
        var col = GetComponent<Collider>();
        if (col) col.enabled = false;
        enabled = false;
    }
}
