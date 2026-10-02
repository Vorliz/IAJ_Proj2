using Assets.Scripts.IAJ.Unity.DecisionMaking.BehaviorTree.EnemyTasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Assets.Scripts.Game;
using Assets.Scripts.Game.NPCs;
using System.Numerics;
using System.Runtime.CompilerServices;


namespace Assets.Scripts.IAJ.Unity.DecisionMaking.BehaviorTree.BehaviourTrees
{
   class PatrolTree : Selector
    {
        public PatrolTree(Orc orc, GameObject player, float _range) {
            // To create a new tree you need to create each branch which is done using the constructors of different tasks
            // Additionally it is possible to create more complex behaviour by combining different tasks and composite tasks...
            UnityEngine.Vector3 patrolPoint1, patrolPoint2;
            orc.Get2NearestPatrolPoints(out patrolPoint1, out patrolPoint2);
            this.children = new List<Task>()
            {
                new Sequence(new List<Task>() {new MoveToInterruptible(orc, patrolPoint1, _range, player), new MoveToInterruptible(orc, patrolPoint2, _range, player)}),
                //new Shout(orc, player); (sequence between shout and pursue?)
                new Pursue(orc, player, _range),
            };
        }
    }
}
