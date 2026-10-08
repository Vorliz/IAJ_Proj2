namespace Assets.Scripts.IAJ.Unity.DecisionMaking.BehaviorTree
{
    public class RepeatUntilFailure : Task
    {
        private readonly Task task;

        public RepeatUntilFailure(Task task)
        {
            this.task = task;
        }

        public override Result Run()
        {
            return task.Run() == Result.Failure ? Result.Failure : Result.Running;
        }

        public override void Reset()
        {
            task.Reset();
        }
    }
}