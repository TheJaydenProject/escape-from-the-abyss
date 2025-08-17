/*
 * Author: Jayden Wong
 * Date: 11 August 2025
 * Description: Vision sensor for AI agents. Continuously checks for targets (e.g., Player)
 *              within a radius, field of view, and line of sight. Stores whether the player
 *              is currently seen, the last known position, and remembers the target for a
 *              short memory time after losing direct sight.
 */

using UnityEngine;

/// <summary>
/// Handles field-of-view based vision detection for AI.
/// - Checks for targets within a detection radius.
/// - Confirms they are inside the FOV cone.
/// - Verifies line of sight (raycasts against obstacles).
/// - Remembers targets for a short duration even if they move out of sight.
/// </summary>
public class SightSensor : MonoBehaviour
{
    [Header("Tuning")]
    public float detectionRadius = 15f;
    [Range(1f, 179f)] public float fovAngle = 90f;       // Angle of the vision cone
    public float eyeHeight = 1.7f;
    public float memoryTime = 2.0f;                       // How long the AI "remembers" a lost target
    public float tickInterval = 0.1f;                     // Frequency of vision checks
    public LayerMask losObstacles;                        // Obstacles that can block vision
    public LayerMask targetMask;                          // Layer mask for targets (e.g., Player)

    [Header("Multi-point LOS (optional)")]
    public bool sampleMultiplePoints = true;              // Test multiple points (head, chest, hips) for LOS
    public Vector3[] localSampleOffsets = new Vector3[]   // Local offsets used when checking LOS
    {
        new Vector3(0f, 1.6f, 0f),
        new Vector3(0f, 1.2f, 0f),
        new Vector3(0f, 0.9f, 0f)
    };

    // Public detection state
    public bool PlayerSeen { get; private set; }
    public Vector3 LastSeenPosition { get; private set; }
    public Transform CurrentTarget { get; private set; }

    // Internal helpers
    Collider[] _hits = new Collider[8];   // Buffer for nearby colliders
    float _forgetAt = -1f;                // Timestamp when memory expires

    /// <summary>
    /// Starts invoking the vision check on a repeating interval.
    /// Uses a small random offset to avoid synchronization spikes if multiple AIs exist.
    /// </summary>
    void OnEnable() => InvokeRepeating(nameof(Tick), Random.Range(0f, tickInterval), tickInterval);

    /// <summary>
    /// Cancels the repeating vision checks when disabled.
    /// </summary>
    void OnDisable() => CancelInvoke(nameof(Tick));

    /// <summary>
    /// Performs a single vision "tick":
    /// - Finds potential targets in range.
    /// - Filters them by field of view.
    /// - Verifies line of sight with raycasts.
    /// - Updates the detection state and memory timer.
    /// </summary>
    void Tick()
    {
        PlayerSeen = false;
        CurrentTarget = null;

        // 1) Broad phase: check colliders in radius that belong to the target mask
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

        // 2) Narrow phase: iterate all potential targets
        for (int i = 0; i < count; i++)
        {
            if (_hits[i] == null) continue;
            Transform tgt = _hits[i].transform;

            Vector3 to = (TargetCenter(tgt) - eye);
            float dist = to.magnitude;
            Vector3 dir = to / dist;

            // Check field of view angle using dot product
            float dot = Vector3.Dot(fwd, dir);
            if (dot < Mathf.Cos(halfFov * Mathf.Deg2Rad)) continue;

            // Check line of sight with raycasts
            if (HasLineOfSight(eye, tgt, dist))
            {
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = tgt;
                }
            }
        }

        // If at least one target was seen, update state and memory
        if (best != null)
        {
            PlayerSeen = true;
            CurrentTarget = best;
            LastSeenPosition = best.position;
            _forgetAt = Time.time + memoryTime;
        }
        else
        {
            CheckMemory();
        }

        // Clear collider buffer to avoid stale references
        for (int i = 0; i < count; i++) _hits[i] = null;
    }

    /// <summary>
    /// Checks whether the AI has a clear line of sight to the target.
    /// Uses either a single ray to the target center or multiple sample points.
    /// </summary>
    bool HasLineOfSight(Vector3 eye, Transform target, float rawDist)
    {
        if (!sampleMultiplePoints)
        {
            // Single ray from eye to target center
            Vector3 p = TargetCenter(target);
            return !Physics.Raycast(eye, (p - eye).normalized, out RaycastHit hit, rawDist, losObstacles, QueryTriggerInteraction.Ignore)
                   || hit.collider.transform.IsChildOf(target);
        }

        // Multi-point sampling for more robust detection
        for (int i = 0; i < localSampleOffsets.Length; i++)
        {
            Vector3 p = target.TransformPoint(localSampleOffsets[i]);
            Vector3 dir = (p - eye);
            float dist = dir.magnitude;
            if (dist <= 0.001f) continue;

            // If any sample point has clear LOS, consider the target visible
            if (!Physics.Raycast(eye, dir / dist, out RaycastHit hit, dist, losObstacles, QueryTriggerInteraction.Ignore)
                || hit.collider.transform.IsChildOf(target))
                return true;
        }
        return false;
    }

    /// <summary>
    /// Returns the "eye" position of this sensor (camera height).
    /// </summary>
    Vector3 GetEyePosition() => new Vector3(transform.position.x, transform.position.y + eyeHeight, transform.position.z);

    /// <summary>
    /// Returns the center point of a target for aiming LOS checks.
    /// Uses the collider center if available, otherwise an estimated offset.
    /// </summary>
    Vector3 TargetCenter(Transform t)
    {
        if (t.TryGetComponent<Collider>(out var col)) return col.bounds.center;
        return t.position + Vector3.up * 1.2f;
    }

    /// <summary>
    /// Maintains "memory" of the target after it goes out of sight,
    /// until the configured memory time expires.
    /// </summary>
    void CheckMemory()
    {
        if (Time.time <= _forgetAt) PlayerSeen = true;
        else PlayerSeen = false;
    }

#if UNITY_EDITOR
    /// <summary>
    /// Draws gizmos in the Scene view to visualize detection radius and FOV cone.
    /// Only runs in the Unity Editor.
    /// </summary>
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
