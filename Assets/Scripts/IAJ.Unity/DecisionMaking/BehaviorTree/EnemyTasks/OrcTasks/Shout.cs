using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.Threading.Tasks;
using UnityEngine.AI;
using Assets.Scripts.Game.NPCs;
using Assets.Scripts.Game;
using Unity.AppUI.UI;
using UnityEngine.Rendering;

namespace Assets.Scripts.IAJ.Unity.DecisionMaking.BehaviorTree.EnemyTasks
{
    class Shout : Task
    {
        protected Monster Character { get; set; }

        public float range;

        public Shout(Monster character)
        {
            this.Character = character;
        }

        public override Result Run()
        {
            if (Character == null)
                return Result.Failure;
            // spawn a ! on orc location
            // counter should be on orcs - 1
            // play sound
            // make action for orcs to go to !
            // if they see player, go to pursuit
            // if they reach ! and they dont see player go back to patrol
            var position = Character.transform.position;

            var exclamationMark = new GameObject("!");
            exclamationMark.transform.position = position;
            exclamationMark.AddComponent<ShoutMarker>().Counter = GameManager.Instance.orcs.Count - 1;
            return Result.Success;
        }

    }
}