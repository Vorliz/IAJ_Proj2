using UnityEngine;
using UnityEngine.AI;
using System.Linq;

namespace Assets.Scripts.IAJ.Unity.Formations
{
    // Invisible "ghost leader" of a formation: patrols between the two nearest patrol points
    public class FormationAnchor : MonoBehaviour
    {
        private NavMeshAgent agent;
        private Vector3[] patrolPoints;
        private int current;

        void Start()
        {
            agent = GetComponent<NavMeshAgent>();
            patrolPoints = GameObject.FindGameObjectsWithTag("Patrol")
                .OrderBy(p => Vector3.Distance(transform.position, p.transform.position))
                .Take(2)
                .Select(p => p.transform.position)
                .ToArray();

            if (patrolPoints.Length > 0)
                agent.SetDestination(patrolPoints[current]);
        }

        void Update()
        {
            if (patrolPoints.Length < 2 || agent.pathPending) return;

            if (agent.remainingDistance < 1.0f)
            {
                current = (current + 1) % patrolPoints.Length;
                agent.SetDestination(patrolPoints[current]);
            }
        }
    }
}
