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

    [Header("Cameras")]
    public Camera mainCam;
    public Camera deathCam;
    [Header("Respawn Settings")]
    public Transform respawnPoint;

    NavMeshAgent _agent;
    Animator _anim;

    Coroutine _chaseRoutine;
    bool _cooldownActive;

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
        // Start chasing when seen (unless in cooldown); stop when not seen
        if (sight.PlayerSeen && !_cooldownActive && _chaseRoutine == null)
        {
            _chaseRoutine = StartCoroutine(ChaseLoop());
        }
        else if (!sight.PlayerSeen && _chaseRoutine != null)
        {
            StopCoroutine(_chaseRoutine);
            _chaseRoutine = null;
            _agent.isStopped = true;
            _anim?.SetBool("Chasing", false);
        }
    }

    IEnumerator ChaseLoop()
    {
        _anim?.SetBool("Chasing", true);

        while (sight.PlayerSeen && !_cooldownActive)
        {
            _agent.isStopped = false;
            _agent.SetDestination(sight.LastSeenPosition);

            float t = 0f;
            while (t < repathEvery)
            {
                if (!sight.PlayerSeen) break;
                t += Time.deltaTime;
                yield return null;
            }
        }

        _anim?.SetBool("Chasing", false);
        _agent.isStopped = true;
        _chaseRoutine = null;
    }

    public void Caught()
    {
        if (_cooldownActive) return;
        _cooldownActive = true;

        _agent.isStopped = true;
        _agent.ResetPath();

        _anim?.SetBool("Chasing", false);
        _anim?.SetTrigger("Caught");

        Debug.Log("Player caught!");

        // run the sequence withdeath-cam hold
        StartCoroutine(HandleDeathAndRespawn());
    }

    private IEnumerator HandleDeathAndRespawn()
    {
        // Disable player movement
        if (playerMovementToDisable != null) playerMovementToDisable.enabled = false;

        // Switch to death cam
        if (mainCam != null) { mainCam.enabled = false; mainCam.gameObject.SetActive(false); }
        if (deathCam != null) { deathCam.gameObject.SetActive(true); deathCam.enabled = true; }

        // Stay on death cam for Xs
        yield return new WaitForSeconds(1.8f);

        // Teleport to respawn
        var t  = playerMovementToDisable.transform;
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
