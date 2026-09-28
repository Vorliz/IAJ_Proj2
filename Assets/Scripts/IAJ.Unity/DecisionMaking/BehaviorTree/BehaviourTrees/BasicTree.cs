using Assets.Scripts.IAJ.Unity.DecisionMaking.BehaviorTree.EnemyTasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Assets.Scripts.Game;
using Assets.Scripts.Game.NPCs;


namespace Assets.Scripts.IAJ.Unity.DecisionMaking.BehaviorTree.BehaviourTrees
{
   class BasicTree : Sequence
    {
        public BasicTree(Monster monster, GameObject target) {
            // To create a new tree you need to create each branck which is done using the constructors of different tasks
            // Additionally it is possible to create more complex behaviour by combining different tasks and composite tasks...
            this.children = new List<Task>()
            {
                new IsCharacterNearTarget(monster, target, monster.stats.AwakeDistance),
                new Pursue(monster, target, monster.stats.WeaponRange),
                new LightAttack(monster),
                new MoveTo(monster, monster.DefaultPosition, 1.0f)
            };
        }
    }
}
