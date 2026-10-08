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
            bool heardShout = false;
            this.children = new List<Task>()
            {
                new Parallel(new List<Task>()
                {
                    new Sequence(new List<Task>() {
                        new MoveTo(orc, orc.PatrolPoint1, _range),
                        new MoveTo(orc, orc.PatrolPoint2, _range)
                    }),
                    new IsCharacterFarTarget(orc, player, orc.stats.AwakeDistance),
                    new HeardShout(orc, out heardShout),
                }),
                new Conditional(new List<Task>()
                {
                    new Parallel(new List<Task>()
                    {
                        new IsCharacterFarTarget(orc, player, orc.stats.AwakeDistance),
                        new MoveTo(orc, ShoutLocation, _range),
                    }),
                    new Shout(orc),
                }, heardShout),
                new Sequence(new List<Task>() {
                    new Pursue(orc, player, _range),
                    new LightAttack(orc),
                }),
            };
        }
    }
}
