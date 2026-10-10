using System.Collections.Generic;

namespace Assets.Scripts.IAJ.Unity.DecisionMaking.BehaviorTree
{
    public class Parallel : CompositeTask
    {
        public Parallel(List<Task> tasks) : base(tasks)
        {
        }

        public Parallel() { }

        // A Parallel returns failure when one of its children fails. While children succeed,
        // it keeps retrying them and remains running.
        public override Result Run()
        {
            if (children == null || children.Count == 0)
                return Result.Failure;

            foreach (Task child in children)
            {
                Result result = child.Run();
                if (result == Result.Running)
                {
                    continue;
                }

                if (result == Result.Failure)
                {
                    Reset();
                    return Result.Failure;
                }

                child.Reset();
            }

            return Result.Running;
        }
    }
}
