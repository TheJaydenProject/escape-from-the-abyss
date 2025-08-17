/* 
 * Author: Jayden Wong
 * Date: 10 Aug 2025
 * Description: Controls enemy AI behavior for patrolling, chasing the player when detected,
 *              handling player capture (death), camera switching, HUD hiding/restoring,
 *              and respawn logic.
 */


using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using UnityEngine.SceneManagement;

/// <summary>
/// Handles enemy AI logic, including:
/// - Patrolling between waypoints when the player is not detected
/// - Chasing the player when spotted by the SightSensor
/// - Capturing the player and triggering a death sequence
/// - Switching between main and death cameras
/// - Hiding and restoring the HUD during death/respawn
/// - Respawning the player at a designated point and applying cooldowns
/// </summary>

[RequireComponent(typeof(NavMeshAgent))]
public class AIChaseController : MonoBehaviour
{
    /// <summary> Detects if/where the player is seen. </summary>
    public SightSensor sight;

    
    /// <summary> Distance where the AI considers the player “caught”. </summary>
    public float caughtDistance = 4f;

    /// <summary> Seconds between chase path updates. </summary>
    public float repathEvery = 0.2f;

    /// <summary> Seconds before AI can catch again after death. </summary>
    public float caughtCooldown = 4f;

    /// <summary> Player movement script to disable on death. </summary>
    public Behaviour playerMovementToDisable;

    [Header("HUD")]
    /// <summary> HUD roots toggled during death/respawn. </summary>
    [SerializeField] private GameObject[] hudRoots;

    /// <summary> Tracks which HUD roots were active before death. </summary>
    private bool[] _hudWasActive;   

    /// <summary> Flag to indicate HUD has been hidden for current death. </summary>
    private bool _hudHiddenForDeath;


    [Header("Cameras")]
    /// <summary> Main gameplay camera. </summary>
    public Camera mainCam;

    /// <summary> Death camera shown on capture. </summary>
    public Camera deathCam;
    [Header("Respawn Settings")]
    
    /// <summary> Where the player respawns. </summary>
    public Transform respawnPoint;

    [Header("Patrol")]
    /// <summary> Patrol waypoints for the AI. </summary>
    public Transform[] patrolPoints;

    
    /// <summary> Arrival threshold for waypoints. </summary>
    public float waypointTolerance = 0.3f;

    /// <summary> Pause duration at each waypoint. </summary>
    public float waitAtWaypoint = 0.5f;
    
    
    /// <summary> Last patrol index used to avoid immediate repeats. </summary>
    private int _previousPatrolIndex = -1;

    [Header("Audio")]
    /// <summary> Sound to play when the player is caught. </summary>
    public AudioSource caughtSfx;

    /// <summary> Cached NavMeshAgent used for pathfinding. </summary>
    NavMeshAgent _agent;

    /// <summary> Animator for the AI state animations. </summary>
    Animator _anim;    

    /// <summary> Handle to the active chase coroutine. </summary>
    Coroutine _chaseRoutine;

    /// <summary> Handle to the active patrol coroutine. </summary>
    Coroutine _patrolRoutine;

    /// <summary> True while post-catch cooldown is active. </summary>
    bool _cooldownActive;

    /// <summary> Current patrol waypoint index. </summary>
    int _patrolIndex;

    /// <summary> Cached player Transform. </summary>
    Transform _playerT;  

    /// <summary> Cached player CharacterController. </summary>
    CharacterController _playerCC;

    /// <summary> AudioListener attached to the main camera. </summary>
    AudioListener _mainListener;

    /// <summary> AudioListener attached to the death camera. </summary>
    AudioListener _deathListener;


    /// <summary> Picks a random next patrol index without repeating last two. </summary>
    private int PickNextIndexNoRepeat(int current, int previous, int length)
    {
        if (length <= 1) return 0;
        int next;
        do { next = Random.Range(0, length); }
        while ((length > 2) && (next == current || next == previous));
        return next;
    }

    /// <summary> Initializes references and sets stopping distance. </summary>
    void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _anim  = GetComponent<Animator>();
        if (sight == null) sight = GetComponent<SightSensor>();

        // Ensure agent’s stopping distance matches our caught distance
        _agent.stoppingDistance = caughtDistance;
    }

    /// <summary> Checks each frame whether to patrol, chase, or idle. </summary>
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


    /// <summary> Registers scene load event to reset HUD state. </summary>
    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded_ResetHudState;
    }

    /// <summary> Unregisters scene load event. </summary>
    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded_ResetHudState;
    }

    /// <summary> Resets HUD state after a new scene loads. </summary>
    void OnSceneLoaded_ResetHudState(Scene scene, LoadSceneMode mode)
    {
        // Clear cached references & state so the next death can hide HUD again
        hudRoots = null;
        _hudWasActive = null;
        _hudHiddenForDeath = false;
    }

    /// <summary> Finds and caches HUD root objects under GameManager. </summary>
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

    /// <summary> Updates patrol index ensuring no repeats. </summary>
    void SetNextPatrolIndex()
    {
        // Pick a new patrol point that’s not the same as last 2
        int last = _patrolIndex;  // remember current
        _patrolIndex = PickNextIndexNoRepeat(last, _previousPatrolIndex, patrolPoints.Length);
        _previousPatrolIndex = last;  // store the last, not the new
    }

    /// <summary> Coroutine that moves AI between patrol points. </summary>
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


            // Keep walking until we arrive at the patrol point or until the player is seen
            while (!sight.PlayerSeen)
            {
                if (!_agent.pathPending &&
                    _agent.remainingDistance <= Mathf.Max(waypointTolerance, _agent.stoppingDistance + _agent.radius))
                    break; // arrived

                yield return null;
            }

            _anim?.SetBool("Patrolling", false);

            if (sight.PlayerSeen) break;

            // Wait briefly at waypoint before moving to next
            _agent.isStopped = true;
            float t = 0f;
            while (t < waitAtWaypoint && !sight.PlayerSeen)
            {
                t += Time.deltaTime;
                yield return null;
            }

            SetNextPatrolIndex();
        }

        // Patrol ended
        _anim?.SetBool("Patrolling", false);
        _agent.isStopped = true;
        _patrolRoutine = null;
    }


    /// <summary> Coroutine that makes AI chase the player. </summary>
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

            // Recalculate path every "repathEvery" seconds, small wait to avoid hammering SetDestination
            float t = 0f;
            while (t < repathEvery)
            {
                if (!sight.PlayerSeen || _cooldownActive) break;
                t += Time.deltaTime;
                yield return null;
            }
        }

        // Stop chasing once player is lost
        _anim?.SetBool("Chasing", false);
        _agent.isStopped = true;
        _chaseRoutine = null;
    }


    /// <summary> Finds and caches references to the player and movement script. </summary>
    bool ResolvePlayer()
    {
        var gm = GameManager.Instance;
        if (!gm || !gm.playerRoot) return false;

        _playerT  = gm.playerRoot;                         
        _playerCC = _playerT.GetComponent<CharacterController>();

        // If no movement script manually assigned, try to auto-grab one
        if (!playerMovementToDisable)
            playerMovementToDisable = _playerT.GetComponent<Behaviour>();

        return true;
    }

    /// <summary> Handles when player is caught: stop AI, play animation, trigger death. </summary>
    public void Caught()
    {
        // Ignore if cooldown active or already processing death
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

        // Update animation states
        _anim?.SetBool("Chasing", false);
        _anim?.SetBool("Patrolling", false);
        _anim?.SetTrigger("Caught");

        Debug.Log("Player caught!");

        // Play caught SFX
        if (caughtSfx && caughtSfx.clip)
        {
            caughtSfx.Play();
        }

        // Begin death handling process
        StartCoroutine(HandleDeathAndRespawn());
    }

    /// <summary> Hides HUD elements during death sequence. </summary>
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

    /// <summary> Restores HUD elements after respawn. </summary>
    void RestoreHUDAfterDeath()
    {
        var roots = ResolveHudRoots();
        if (roots == null || roots.Length == 0 || !_hudHiddenForDeath) return;

        // Remember current HUD state, then disable everything
        for (int i = 0; i < roots.Length; i++)
        {
            var go = roots[i];
            if (!go) continue;
            bool was = (_hudWasActive != null && i < _hudWasActive.Length) ? _hudWasActive[i] : true;
            go.SetActive(was);
        }

        _hudHiddenForDeath = false;
    }
    
    /// <summary> Enables death camera and disables main camera. </summary>
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
            _deathListener.enabled = true;   // force-enable 
        }
    }

    /// <summary> Handles full death and respawn sequence with camera/HUD changes. </summary>
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

        // Find respawn point (either assigned or from GameManager)
        Transform target = respawnPoint;
        if (target == null && GameManager.Instance)
        {
            string sceneName = SceneManager.GetActiveScene().name;
            target = GameManager.Instance.GetSpawnForScene(sceneName);
        }

        // Teleport player to respawn location
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

        // Disable death cam, re-enable main cam
        if (deathCam)
        {
            if (!_deathListener) _deathListener = deathCam.GetComponent<AudioListener>();
            if (_deathListener) _deathListener.enabled = false;

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

        // Restore HUD visibility
        RestoreHUDAfterDeath();

        // Re-enable player movement
        if (playerMovementToDisable) playerMovementToDisable.enabled = true;

        GameManager.Instance.IsHandlingDeath = false;

        // Start cooldown so enemy doesn’t instantly catch player again
        StartCoroutine(CooldownTimer());
    }


    /// <summary> Waits for cooldown before AI can catch player again. </summary>
    IEnumerator CooldownTimer()
    {
        yield return new WaitForSeconds(caughtCooldown);
        _cooldownActive = false;

        // If the player is still visible after cooldown, resume chase
        if (sight.PlayerSeen && _chaseRoutine == null)
            _chaseRoutine = StartCoroutine(ChaseLoop());
    }
}
