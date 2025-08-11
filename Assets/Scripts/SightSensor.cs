using UnityEngine;

public class SightSensor : MonoBehaviour
{
    [Header("Tuning")]
    public float detectionRadius = 15f;
    [Range(1f, 179f)] public float fovAngle = 90f;       // total cone angle
    public float eyeHeight = 1.7f;
    public float memoryTime = 2.0f;                       // how long AI remembers after LOS lost
    public float tickInterval = 0.1f;                     // run checks 10x/sec
    public LayerMask losObstacles;                        // walls, props, etc.
    public LayerMask targetMask;                          // Player layer

    [Header("Multi-point LOS (optional)")]
    public bool sampleMultiplePoints = true;
    public Vector3[] localSampleOffsets = new Vector3[]   // chest, head, hips
    {
        new Vector3(0f, 1.6f, 0f),
        new Vector3(0f, 1.2f, 0f),
        new Vector3(0f, 0.9f, 0f)
    };

    // Public state
    public bool PlayerSeen { get; private set; }
    public Vector3 LastSeenPosition { get; private set; }
    public Transform CurrentTarget { get; private set; }

    // internals
    Collider[] _hits = new Collider[8];
    float _forgetAt = -1f;

    void OnEnable() => InvokeRepeating(nameof(Tick), Random.Range(0f, tickInterval), tickInterval);
    void OnDisable() => CancelInvoke(nameof(Tick));

    void Tick()
    {
        PlayerSeen = false;
        CurrentTarget = null;

        // 1) Range prefilter
        int count = Physics.OverlapSphereNonAlloc(transform.position, detectionRadius, _hits, targetMask, QueryTriggerInteraction.Ignore);
        if (count == 0)
        {
            CheckMemory();
            return;
        }

        float halfFov = fovAngle * 0.5f;
        float bestDist = Mathf.Infinity;
        Transform best = null;

        Vector3 eye = GetEyePosition();
        Vector3 fwd = transform.forward;

        for (int i = 0; i < count; i++)
        {
            if (_hits[i] == null) continue;
            Transform tgt = _hits[i].transform;

            Vector3 to = (TargetCenter(tgt) - eye);
            float dist = to.magnitude;
            Vector3 dir = to / dist;

            // 2) FOV check
            float dot = Vector3.Dot(fwd, dir);
            if (dot < Mathf.Cos(halfFov * Mathf.Deg2Rad)) continue;

            // 3) LOS check
            if (HasLineOfSight(eye, tgt, dist))
            {
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = tgt;
                }
            }
        }

        if (best != null)
        {
            PlayerSeen = true;
            CurrentTarget = best;
            LastSeenPosition = best.position;
            _forgetAt = Time.time + memoryTime; // refresh memory
        }
        else
        {
            CheckMemory();
        }

        for (int i = 0; i < count; i++) _hits[i] = null;
    }

    bool HasLineOfSight(Vector3 eye, Transform target, float rawDist)
    {
        if (!sampleMultiplePoints)
        {
            Vector3 p = TargetCenter(target);
            return !Physics.Raycast(eye, (p - eye).normalized, out RaycastHit hit, rawDist, losObstacles, QueryTriggerInteraction.Ignore)
                   || hit.collider.transform.IsChildOf(target);
        }

        for (int i = 0; i < localSampleOffsets.Length; i++)
        {
            Vector3 p = target.TransformPoint(localSampleOffsets[i]);
            Vector3 dir = (p - eye);
            float dist = dir.magnitude;
            if (dist <= 0.001f) continue;

            if (!Physics.Raycast(eye, dir / dist, out RaycastHit hit, dist, losObstacles, QueryTriggerInteraction.Ignore)
                || hit.collider.transform.IsChildOf(target))
                return true;
        }
        return false;
    }

    Vector3 GetEyePosition() => new Vector3(transform.position.x, transform.position.y + eyeHeight, transform.position.z);

    Vector3 TargetCenter(Transform t)
    {
        if (t.TryGetComponent<Collider>(out var col)) return col.bounds.center;
        return t.position + Vector3.up * 1.2f;
    }

    void CheckMemory()
    {
        if (Time.time <= _forgetAt) PlayerSeen = true;
        else PlayerSeen = false;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        Vector3 eye = Application.isPlaying ? GetEyePosition() : transform.position + Vector3.up * eyeHeight;
        float half = fovAngle * 0.5f;
        Quaternion left = Quaternion.AngleAxis(-half, Vector3.up);
        Quaternion right = Quaternion.AngleAxis(half, Vector3.up);
        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(eye, left * transform.forward * detectionRadius);
        Gizmos.DrawRay(eye, right * transform.forward * detectionRadius);
        Gizmos.DrawRay(eye, transform.forward * detectionRadius);
    }
#endif
}
