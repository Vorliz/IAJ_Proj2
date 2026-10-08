using UnityEngine;
using Assets.Scripts.Game.NPCs;

namespace Assets.Scripts.IAJ.Unity.DecisionMaking.BehaviorTree.EnemyTasks
{
    class MoveToShout : Task
    {
        private readonly Orc orc;
        private readonly float range;

        private readonly GameObject Target;

        public MoveToShout(Orc orc, GameObject target, float range)
        {
            this.orc = orc;
            this.range = range;
            this.Target = target;
        }

        public override Result Run()
        {
            if (orc == null) {
                return Result.Failure;
            }

            if (!orc.HeardShout)
            {
                return Result.Success;
            }

            if (Vector3.Distance(orc.transform.position, this.Target.transform.position) <= orc.stats.AwakeDistance)
            {
                return Result.Success;
            }

            if (Vector3.Distance(orc.transform.position, orc.ShoutPosition) <= range)
            {
                return Result.Failure;
            }

            orc.StartPathfinding(orc.ShoutPosition);
            return Result.Running;
        }
    }
}