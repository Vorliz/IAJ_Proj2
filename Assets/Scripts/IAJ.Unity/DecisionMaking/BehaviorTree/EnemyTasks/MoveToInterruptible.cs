using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.Threading.Tasks;
using UnityEngine.AI;
using Assets.Scripts.Game.NPCs;
using Assets.Scripts.Game;

namespace Assets.Scripts.IAJ.Unity.DecisionMaking.BehaviorTree.EnemyTasks
{
    class MoveToInterruptible : Task
    {
        protected Monster Character { get; set; }

        public Vector3 Target { get; set; }

        public float range;

        public GameObject Player { get; set; }

        public MoveToInterruptible(Monster character, Vector3 target, float _range, GameObject player)
        {
            this.Character = character;
            this.Target = target;
            this.Player = player;
            range = _range;
        }

        public override Result Run()
        {
            if (Target == null)
                return Result.Failure;

            if (Vector3.Distance(Player.transform.position, Character.transform.position) <= Character.stats.AwakeDistance)
            {
                Debug.Log("switched to pursue");
                return Result.Failure;
            }
            else if (Vector3.Distance(Character.transform.position, this.Target) <= range)
            {
                return Result.Success;
            }
            else
            {
                Character.StartPathfinding(Target);
                return Result.Running;
            }

        }

    }
}
