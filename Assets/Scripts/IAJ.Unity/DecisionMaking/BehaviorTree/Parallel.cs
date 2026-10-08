using System.Collections.Generic;

namespace Assets.Scripts.IAJ.Unity.DecisionMaking.BehaviorTree
{
    public class Parallel : CompositeTask
    {
        public Parallel(List<Task> tasks) : base(tasks)
        {
        }

        public Parallel() { }

        // A Parallel will return immediately with a success status code when one of its children
        // runs fails. As long as its children are Succeding, it will keep on retrying. If it runs out of
        // children completely, it will return a failure status code
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
                    return Result.Success;
                }

                child.Reset();
            }

            return Result.Running;
        }
    }
}
