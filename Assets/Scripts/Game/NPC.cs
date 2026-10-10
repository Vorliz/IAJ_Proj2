using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;

namespace Assets.Scripts.Game
{
    public class NPC : MonoBehaviour
    {
        [Serializable]
        public struct Stats
        {
            public string Name;
            public int HP;
            public int ShieldHP;
            public int MaxShieldHp;
            public int MaxHP;
            public int Mana;
            public int MaxMana;
            public int XP;
            public float Time;
            public int Money;
            public int Level;

        }

        protected GameObject character;

        // Pathfinding
        protected UnityEngine.AI.NavMeshAgent navMeshAgent;
        private Vector3 previousTarget;

        public Stats baseStats;


        void Awake()
        {
            previousTarget = new Vector3(0.0f, 0.0f, 0.0f);
            this.character = this.gameObject;
            navMeshAgent = this.GetComponent<NavMeshAgent>();
        }



        #region Navmesh Pathfinding Methods

        public void StartPathfinding(Vector3 targetPosition)
        {
            //if the targetPosition received is the same as a previous target, then this a request for the same target
            //no need to redo the pathfinding search
            navMeshAgent.isStopped = false;
            if (!this.previousTarget.Equals(targetPosition))
            {

                this.previousTarget = targetPosition;

                navMeshAgent.SetDestination(targetPosition);

            }
        }

        // Setting transform.position directly is overridden by the NavMeshAgent, so the agent must be warped
        public void TeleportTo(Vector3 position)
        {
            navMeshAgent.Warp(position);
            navMeshAgent.ResetPath();
            // the agent is already at "position": the next StartPathfinding to any other target sets a new destination
            this.previousTarget = position;
        }

        public void StopPathfinding()
        {
            navMeshAgent.isStopped = true;
        }

        // Simple way of calculating distance left to target using Unity's navmesh
        public float GetDistanceToTarget(Vector3 originalPosition, Vector3 targetPosition)
        {
            var distance = 0.0f;

            NavMeshPath result = new();
            // Path from originalPosition (the imagined position in the plan), not from the agent's real position.
            // Both points are snapped onto the NavMesh because objects may sit slightly off it.
            if (NavMesh.SamplePosition(originalPosition, out NavMeshHit startHit, 2.0f, navMeshAgent.areaMask) &&
                NavMesh.SamplePosition(targetPosition, out NavMeshHit endHit, 2.0f, navMeshAgent.areaMask) &&
                NavMesh.CalculatePath(startHit.position, endHit.position, navMeshAgent.areaMask, result))
            {
                var currentPosition = originalPosition;
                foreach (var c in result.corners)
                {
                    //Rough estimate, it does not account for shortcuts so we have to multiply it
                    distance += Vector3.Distance(currentPosition, c) * 0.5f;
                    currentPosition = c;
                }
                return distance;
            }

            //Default value
            return 100;
        }

        // For ranged actions: follows the NavMesh path from originalPosition towards targetPosition and stops at the first
        // point whose straight-line distance to the target is <= range (that is when the action fires in the game).
        // Returns the distance walked (same 0.5 scale as GetDistanceToTarget) and the position where the character stops.
        public float GetDistanceUntilInRange(Vector3 originalPosition, Vector3 targetPosition, float range, out Vector3 stopPosition)
        {
            stopPosition = originalPosition;
            if ((targetPosition - originalPosition).sqrMagnitude <= range * range) return 0.0f;

            NavMeshPath result = new();
            if (NavMesh.SamplePosition(originalPosition, out NavMeshHit startHit, 2.0f, navMeshAgent.areaMask) &&
                NavMesh.SamplePosition(targetPosition, out NavMeshHit endHit, 2.0f, navMeshAgent.areaMask) &&
                NavMesh.CalculatePath(startHit.position, endHit.position, navMeshAgent.areaMask, result))
            {
                var distance = 0.0f;
                var currentPosition = originalPosition;
                foreach (var c in result.corners)
                {
                    if ((targetPosition - c).sqrMagnitude <= range * range)
                    {
                        // the range is entered somewhere in the segment currentPosition -> c
                        var entryPoint = GetRangeEntryPoint(currentPosition, c, targetPosition, range);
                        distance += Vector3.Distance(currentPosition, entryPoint) * 0.5f;
                        stopPosition = entryPoint;
                        return distance;
                    }
                    distance += Vector3.Distance(currentPosition, c) * 0.5f;
                    currentPosition = c;
                }

                // the path ended before entering the range: the character stops at the end of the path
                stopPosition = currentPosition;
                return distance;
            }

            //Default value
            stopPosition = targetPosition;
            return 100;
        }

        // First point of the segment start -> end at distance "range" from center (start is outside the range, end inside)
        private static Vector3 GetRangeEntryPoint(Vector3 start, Vector3 end, Vector3 center, float range)
        {
            var d = end - start;
            var f = start - center;
            var a = Vector3.Dot(d, d);
            if (a < 1e-6f) return start;

            var b = 2.0f * Vector3.Dot(f, d);
            var c = Vector3.Dot(f, f) - range * range;
            var discriminant = Mathf.Max(0.0f, b * b - 4.0f * a * c);
            var t = Mathf.Clamp01((-b - Mathf.Sqrt(discriminant)) / (2.0f * a));
            return start + t * d;
        }

        #endregion

    }
}
