using Assets.Scripts.Game.NPCs;

namespace Assets.Scripts.IAJ.Unity.DecisionMaking.BehaviorTree.EnemyTasks
{
    class HeardShout : Task
    {
        private readonly Orc orc;

        public HeardShout(Orc orc)
        {
            this.orc = orc;
        }

        public override Result Run()
        {
            return orc != null && orc.HeardShout ? Result.Failure : Result.Running;
        }
    }

}
