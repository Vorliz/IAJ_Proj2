using UnityEngine;
using System.Collections;
using System;
using Assets.Scripts.IAJ.Unity.Utils;
using UnityEngine.AI;
using Assets.Scripts.IAJ.Unity.DecisionMaking.BehaviorTree;
using Assets.Scripts.IAJ.Unity.DecisionMaking.BehaviorTree.BehaviourTrees;
//using Assets.Scripts.IAJ.Unity.Formations;
using System.Collections.Generic;
using static GameManager;
using Assets.Scripts.IAJ.Unity.DecisionMaking.StateMachine;

namespace Assets.Scripts.Game.NPCs
{

    public class Orc : Monster
    {
        private const float LeaderSpeedFactor = 0.7f;
        public bool HeardShout;
        public Vector3 ShoutPosition;
        public bool IsAttacking;
        public Vector3 PatrolPoint1 = Vector3.zero;
        public Vector3 PatrolPoint2 = Vector3.zero;

        public Orc()
        {
            this.stats.Type = "Orc";
            this.stats.XPvalue = 8;
            this.stats.AC = 14;
            this.baseStats.HP = 15;
            this.DmgRoll = () => RandomHelper.RollD10() + 2;
            this.stats.SimpleDamage = 6;
            this.stats.AwakeDistance = 15;
            this.stats.WeaponRange = 3;
            this.HeardShout = false;
            this.IsAttacking = false;
        }

        public override void InitializeStateMachine()
        {
            this.StateMachine = new StateMachine(new Sleep(this));
        }

        public override void InitializeBehaviourTree()
        {
            if (PatrolPoint1 == Vector3.zero || PatrolPoint2 == Vector3.zero)
            {
                this.Get2NearestPatrolPoints(out PatrolPoint1, out PatrolPoint2);
            }

            this.BehaviourTree = new PatrolTree(this, Target, this.stats.WeaponRange);

            if (this.formationLeader)
            {
                this.SetFormationLeader();
            }
        }

        public override void SetFormationLeader()
        {
            this.agent.speed *= LeaderSpeedFactor;
        }

        public void HearShout(Vector3 position)
        {
            this.ShoutPosition = position;
            this.HeardShout = true;
        }


        //If you use a folder structure for each pair of Patrolpoints, you can use the following method to get the patrol points of the closest patrol to the orc.
        public void GetPatrolPoints (out Vector3 position1, out Vector3 position2)
        {
            var patrols = GameObject.FindGameObjectsWithTag("Patrol");

            float pos = float.MaxValue;
            GameObject closest = null;
            foreach (var p in patrols)
            {
                if (Vector3.Distance(this.agent.transform.position, p.transform.position) < pos)
                {
                    pos = Vector3.Distance(this.agent.transform.position, p.transform.position);
                    closest = p;
                }

            }

            position1 = closest.transform.GetChild(0).position;
            position2 = closest.transform.GetChild(1).position;
        }

        //If you just spread each patrol point independently, this method will get the two closest patrol points to the orc.
        public void Get2NearestPatrolPoints(out Vector3 position1, out Vector3 position2)
        {
            var patrols = GameObject.FindGameObjectsWithTag("Patrol");

            float closestDistance = float.MaxValue;
            float secondClosestDistance = float.MaxValue;
            GameObject closest = null;
            GameObject secondClosest = null;

            foreach (var p in patrols)
            {
                var distance = Vector3.Distance(this.transform.position, p.transform.position);
                if (distance < closestDistance)
                {
                    secondClosestDistance = closestDistance;
                    secondClosest = closest;
                    closestDistance = distance;
                    closest = p;
                }
                else if (distance < secondClosestDistance)
                {
                    secondClosestDistance = distance;
                    secondClosest = p;
                }
            }

            position1 = closest != null ? closest.transform.position : this.transform.position;
            position2 = secondClosest != null ? secondClosest.transform.position : position1;
        }
    }
}
