using UnityEngine;
using UnityEngine.AI;

public class AIScript : MonoBehaviour {
    NavMeshAgent agent;
    Transform player;
    Animator animator;

    [SerializeField] float stopDistance = 4f;   // set this in Inspector

    void Start() {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        player = GameObject.FindGameObjectWithTag("Player").transform;
        agent.stoppingDistance = stopDistance;
        agent.autoBraking = true;               // helps slow down before stopping
    }

    void Update() {
        agent.SetDestination(player.position);

        // wait until a path is computed before reading remainingDistance
        if (!agent.pathPending) {
            bool moving = agent.remainingDistance > agent.stoppingDistance;
            agent.isStopped = !moving;
            animator.SetBool("Chasing", moving);
        }
    }
}
