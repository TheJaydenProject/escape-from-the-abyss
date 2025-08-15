using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using UnityEngine.SceneManagement;

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

    [Header("Audio")]
    public AudioSource caughtSfx;

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

    Transform _playerT;
    CharacterController _playerCC;
    AudioListener _mainListener;
    AudioListener _deathListener;
    
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


    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded_ResetHudState;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded_ResetHudState;
    }

    void OnSceneLoaded_ResetHudState(Scene scene, LoadSceneMode mode)
    {
        // Clear cached references & state so the next death can hide HUD again
        hudRoots = null;
        _hudWasActive = null;
        _hudHiddenForDeath = false;
    }

    GameObject[] ResolveHudRoots()
    {
        // If already set up, use what we have
        if (hudRoots != null && hudRoots.Length > 0) return hudRoots;

        var gm = GameManager.Instance;
        if (!gm) return hudRoots; // stay null if no GM

        // Grab all canvases under the persistent GameManager
        var canvases = gm.GetComponentsInChildren<Canvas>(true);
        if (canvases != null && canvases.Length > 0)
        {
            hudRoots = new GameObject[canvases.Length];
            for (int i = 0; i < canvases.Length; i++)
                hudRoots[i] = canvases[i] ? canvases[i].gameObject : null;
        }

        return hudRoots;
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

    bool ResolvePlayer()
    {
        var gm = GameManager.Instance;
        if (!gm || !gm.playerRoot) return false;

        _playerT  = gm.playerRoot;                         
        _playerCC = _playerT.GetComponent<CharacterController>();

        // OPTIONAL: movement script to disable — leave unassigned in Inspector
        if (!playerMovementToDisable)
            playerMovementToDisable = _playerT.GetComponent<Behaviour>();

        return true;
    }

    public void Caught()
    {
        if (_cooldownActive || GameManager.Instance.IsHandlingDeath) return;
        if (!ResolvePlayer()) return;
        _cooldownActive = true;
        GameManager.Instance.IsHandlingDeath = true;
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

        // Play caught SFX if assigned
        if (caughtSfx && caughtSfx.clip)
        {
            caughtSfx.Play();
        }

        StartCoroutine(HandleDeathAndRespawn());
    }

    void HideHUDForDeath()
    {
        var roots = ResolveHudRoots();
        if (roots == null || roots.Length == 0 || _hudHiddenForDeath) return;

        if (_hudWasActive == null || _hudWasActive.Length != roots.Length)
            _hudWasActive = new bool[roots.Length];

        for (int i = 0; i < roots.Length; i++)
        {
            var go = roots[i];
            if (!go) continue;
            _hudWasActive[i] = go.activeSelf; // remember state
            go.SetActive(false);
        }

        _hudHiddenForDeath = true;
    }

    void RestoreHUDAfterDeath()
    {
        var roots = ResolveHudRoots();
        if (roots == null || roots.Length == 0 || !_hudHiddenForDeath) return;

        for (int i = 0; i < roots.Length; i++)
        {
            var go = roots[i];
            if (!go) continue;
            bool was = (_hudWasActive != null && i < _hudWasActive.Length) ? _hudWasActive[i] : true;
            go.SetActive(was);
        }

        _hudHiddenForDeath = false;
    }
    
    void EnableDeathCam()
    {
        // turn off main cam + its listener
        if (mainCam)
        {
            if (!_mainListener) _mainListener = mainCam.GetComponent<AudioListener>();
            if (_mainListener)  _mainListener.enabled = false;

            mainCam.enabled = false;
            mainCam.gameObject.SetActive(false);
        }

        // turn on death cam + ensure it has a listener
        if (deathCam)
        {
            deathCam.gameObject.SetActive(true);
            deathCam.enabled = true;

            if (!_deathListener) _deathListener = deathCam.GetComponent<AudioListener>();
            if (!_deathListener) _deathListener = deathCam.gameObject.AddComponent<AudioListener>();
            _deathListener.enabled = true;   // <- force-enable here
        }
    }

    private IEnumerator HandleDeathAndRespawn()
    {
        if (!ResolvePlayer()) yield break;

        // Disable player movement
        if (playerMovementToDisable) playerMovementToDisable.enabled = false;

        var t = _playerT;
        var cc = _playerCC;

        // Switch to death cam
        EnableDeathCam();
        HideHUDForDeath();

        // Stay on death cam for Xs
        yield return new WaitForSeconds(1.8f);

        // Teleport
        Transform target = respawnPoint;
        if (target == null && GameManager.Instance)
        {
            string sceneName = SceneManager.GetActiveScene().name;
            target = GameManager.Instance.GetSpawnForScene(sceneName);
        }

        if (target != null)
        {
            if (cc) cc.enabled = false;
            t.SetPositionAndRotation(target.position, target.rotation);
            if (cc) cc.enabled = true;
        }
        else
        {
            Debug.LogWarning("[AIChaseController] No respawn target found (respawnPoint null and no GM spawn for this scene).");
        }

        // Switch back to main cam
        if (deathCam)
        {
            if (!_deathListener) _deathListener = deathCam.GetComponent<AudioListener>();
            if (_deathListener)  _deathListener.enabled = false;

            deathCam.enabled = false;
            deathCam.gameObject.SetActive(false);
        }

        if (mainCam)
        {
            mainCam.gameObject.SetActive(true);
            mainCam.enabled = true;

            if (!_mainListener) _mainListener = mainCam.GetComponent<AudioListener>();
            if (!_mainListener) _mainListener = mainCam.gameObject.AddComponent<AudioListener>();
            _mainListener.enabled = true;       // <- force-enable here
        }

        RestoreHUDAfterDeath();

        // Re-enable player movement
        if (playerMovementToDisable) playerMovementToDisable.enabled = true;

        GameManager.Instance.IsHandlingDeath = false;

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
