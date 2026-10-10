namespace Assets.Scripts.IAJ.Unity.DecisionMaking.BehaviorTree
{
    public class Inverter : Task
    {
        private readonly Task task;

        public Inverter(Task task)
        {
            this.task = task;
        }

        public override Result Run()
        {
            Result result = task.Run();
            if (result == Result.Success)
                return Result.Failure;
            if (result == Result.Failure)
                return Result.Success;
            return Result.Running;
        }

        public override void Reset()
        {
            task.Reset();
        }
    }
}