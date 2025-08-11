using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class AIScript : MonoBehaviour
{
    private NavMeshAgent agent;
    private Transform player;
    private float distanceToPlayer;
    private Animator animator;

    // public variables
    public float stopDistance = 2.0f;

    // Start is called before the first frame update
    void Start()
    {
        animator = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();
        player = GameObject.FindGameObjectWithTag("Player").transform;
    }

    // Update is called once per frame
    void Update()
    {
        distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer > stopDistance)
        {
            agent.isStopped = false;
            agent.destination = player.position;
            animator.SetBool("Chasing", true);
        }
        else
        {
            agent.isStopped = true;
            animator.SetBool("Chasing", false);
        }
    }
}
