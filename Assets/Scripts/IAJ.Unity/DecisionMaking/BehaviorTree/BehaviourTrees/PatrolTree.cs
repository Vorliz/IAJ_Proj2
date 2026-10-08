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
    class PatrolTree : Sequence
    {
        private Orc orc;

        public PatrolTree(Orc orc, GameObject player, float _range) {
            this.orc = orc;
            this.children = new List<Task>()
            {
                new Parallel(new List<Task>()
                {
                    new Sequence(new List<Task>() {
                        new MoveTo(orc, orc.PatrolPoint1, _range),
                        new MoveTo(orc, orc.PatrolPoint2, _range)
                    }),
                    new IsCharacterFarTarget(orc, player, orc.stats.AwakeDistance),
                    new HeardShout(orc),
                }),
                new MoveToShout(orc, player, _range),
                new Shout(orc),
                new RepeatUntilFailure(new Sequence(new List<Task>() {
                    new Pursue(orc, player, _range),
                    new LightAttack(orc),
                })),
            };
        }

        public override void Reset()
        {
            base.Reset();
            this.orc.HeardShout = false;
        }
    }
}
