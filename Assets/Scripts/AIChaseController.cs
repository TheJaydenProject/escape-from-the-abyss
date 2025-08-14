using UnityEngine;
using UnityEngine.AI;
using System.Collections;

[RequireComponent(typeof(NavMeshAgent))]
public class AIChaseController : MonoBehaviour
{
    public SightSensor sight;
    public float caughtDistance = 4f; // also used as stop distance
    public float repathEvery = 0.2f;
    public float caughtCooldown = 4f; // seconds
    public Behaviour playerMovementToDisable;

    [Header("HUD")]
    [SerializeField] private GameObject[] hudRoots;

    private bool[] _hudWasActive;   
    private bool _hudHiddenForDeath;


    [Header("Cameras")]
    public Camera mainCam;
    public Camera deathCam;
    [Header("Respawn Settings")]
    public Transform respawnPoint;

    [Header("Patrol")]
    public Transform[] patrolPoints;
    public float waypointTolerance = 0.3f;
    public float waitAtWaypoint = 0.5f;
    private int _previousPatrolIndex = -1;

    private int PickNextIndexNoRepeat(int current, int previous, int length)
    {
        if (length <= 1) return 0;
        int next;
        do { next = Random.Range(0, length); }
        while ((length > 2) && (next == current || next == previous));
        return next;
    }

    NavMeshAgent _agent;
    Animator _anim;

    Coroutine _chaseRoutine;
    Coroutine _patrolRoutine;
    bool _cooldownActive;
    int _patrolIndex;


    void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _anim  = GetComponent<Animator>();
        if (sight == null) sight = GetComponent<SightSensor>();

        // Ensure agent’s stopping distance matches our caught distance
        _agent.stoppingDistance = caughtDistance;
    }

    void Update()
    {
        if (sight.PlayerSeen && !_cooldownActive)
        {
            if (_patrolRoutine != null) { StopCoroutine(_patrolRoutine); _patrolRoutine = null; }
            if (_chaseRoutine == null) _chaseRoutine = StartCoroutine(ChaseLoop());
            return;
        }

        if (!sight.PlayerSeen)
        {
            if (_chaseRoutine != null)
            {
                StopCoroutine(_chaseRoutine);
                _chaseRoutine = null;
                _agent.isStopped = true;
                _anim?.SetBool("Chasing", false);
            }

            if (_patrolRoutine == null && patrolPoints != null && patrolPoints.Length > 0)
                _patrolRoutine = StartCoroutine(PatrolLoop());
        }
    }


    void SetNextPatrolIndex()
    {
        int last = _patrolIndex;  // remember current
        _patrolIndex = PickNextIndexNoRepeat(last, _previousPatrolIndex, patrolPoints.Length);
        _previousPatrolIndex = last;  // store the last, not the new
    }

    IEnumerator PatrolLoop()
    {
        while (!sight.PlayerSeen)
        {
            if (patrolPoints == null || patrolPoints.Length == 0) break;

            Transform point = patrolPoints[_patrolIndex];
            if (point == null)
            {
                yield return null;
                SetNextPatrolIndex();
                continue;
            }

            _anim?.SetBool("Patrolling", true);
            _agent.isStopped = false;
            _agent.SetDestination(point.position);

            while (!sight.PlayerSeen)
            {
                if (!_agent.pathPending && 
                    _agent.remainingDistance <= Mathf.Max(waypointTolerance, _agent.stoppingDistance + _agent.radius))
                    break; // arrived

                yield return null;
            }

            _anim?.SetBool("Patrolling", false);

            if (sight.PlayerSeen) break;

            _agent.isStopped = true;
            float t = 0f;
            while (t < waitAtWaypoint && !sight.PlayerSeen)
            {
                t += Time.deltaTime;
                yield return null;
            }

            SetNextPatrolIndex();
        }

        _anim?.SetBool("Patrolling", false);
        _agent.isStopped = true;
        _patrolRoutine = null;
    }

    IEnumerator ChaseLoop()
    {
        _agent.isStopped = false;

        while (sight.PlayerSeen && !_cooldownActive)
        {
            // keep updating destination
            _agent.SetDestination(sight.LastSeenPosition);

            // set chasing anim only if we are actually moving
            bool moving = _agent.velocity.sqrMagnitude > 0.04f &&
                        (_agent.pathPending || _agent.remainingDistance > _agent.stoppingDistance + 0.05f);
            _anim?.SetBool("Chasing", moving);

            // small wait to avoid hammering SetDestination
            float t = 0f;
            while (t < repathEvery)
            {
                if (!sight.PlayerSeen || _cooldownActive) break;
                t += Time.deltaTime;
                yield return null;
            }
        }

        // clean up when chase ends
        _anim?.SetBool("Chasing", false);
        _agent.isStopped = true;
        _chaseRoutine = null;
    }


    public void Caught()
    {
        if (_cooldownActive) return;
        _cooldownActive = true;
        GameManager.Instance?.RegisterDeath();

        // Stop any movement/state coroutines
        if (_chaseRoutine != null) { StopCoroutine(_chaseRoutine); _chaseRoutine = null; }
        if (_patrolRoutine != null) { StopCoroutine(_patrolRoutine); _patrolRoutine = null; }

        _agent.isStopped = true;
        _agent.ResetPath();

        _anim?.SetBool("Chasing", false);
        _anim?.SetBool("Patrolling", false);
        _anim?.SetTrigger("Caught");

        Debug.Log("Player caught!");

        StartCoroutine(HandleDeathAndRespawn());
    }

    void HideHUDForDeath()
    {
        if (hudRoots == null || _hudHiddenForDeath) return;

        if (_hudWasActive == null || _hudWasActive.Length != hudRoots.Length)
            _hudWasActive = new bool[hudRoots.Length];

        for (int i = 0; i < hudRoots.Length; i++)
        {
            var go = hudRoots[i];
            if (!go) continue;

            // remember the state the dev set in the scene / before death
            _hudWasActive[i] = go.activeSelf;

            // hide for death sequence
            go.SetActive(false);
        }

        _hudHiddenForDeath = true;
    }

    void RestoreHUDAfterDeath()
    {
        if (hudRoots == null || !_hudHiddenForDeath) return;

        for (int i = 0; i < hudRoots.Length; i++)
        {
            var go = hudRoots[i];
            if (!go) continue;

            // restore exactly what it was before HideHUDForDeath()
            go.SetActive(_hudWasActive[i]);
        }

        _hudHiddenForDeath = false;
    }
    private IEnumerator HandleDeathAndRespawn()
    {
        // Disable player movement
        if (playerMovementToDisable != null) playerMovementToDisable.enabled = false;

        // Switch to death cam
        if (mainCam != null) { mainCam.enabled = false; mainCam.gameObject.SetActive(false); }
        if (deathCam != null) { deathCam.gameObject.SetActive(true); deathCam.enabled = true; }
        HideHUDForDeath();

        // Stay on death cam for Xs
        yield return new WaitForSeconds(1.8f);

        // Teleport to respawn
        var t = playerMovementToDisable.transform;
        var cc = t.GetComponent<CharacterController>();

        if (cc != null)
        {
            cc.enabled = false;
            t.SetPositionAndRotation(respawnPoint.position, respawnPoint.rotation);
            cc.enabled = true;
        }
        else
        {
            t.SetPositionAndRotation(respawnPoint.position, respawnPoint.rotation);
        }

        // Switch back to main cam
        if (deathCam != null) { deathCam.enabled = false; deathCam.gameObject.SetActive(false); }
        if (mainCam != null) { mainCam.gameObject.SetActive(true); mainCam.enabled = true; }
        RestoreHUDAfterDeath();

        // Re-enable player movement
        if (playerMovementToDisable != null) playerMovementToDisable.enabled = true;

        // Start cooldown last
        StartCoroutine(CooldownTimer());
    }

    IEnumerator CooldownTimer()
    {
        yield return new WaitForSeconds(caughtCooldown);
        _cooldownActive = false;

        // If the player is still visible after cooldown, resume chase
        if (sight.PlayerSeen && _chaseRoutine == null)
            _chaseRoutine = StartCoroutine(ChaseLoop());
    }
}
