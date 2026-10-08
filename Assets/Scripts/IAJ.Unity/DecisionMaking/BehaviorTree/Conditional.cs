using System;
using System.Collections.Generic;

namespace Assets.Scripts.IAJ.Unity.DecisionMaking.BehaviorTree
{
    public class Conditional : CompositeTask
    {
        private readonly Func<bool> Condition;
        public Conditional(List<Task> tasks, bool condition) : base(tasks)
        {
            Condition = () => condition;
        }

        public Conditional(List<Task> tasks, Func<bool> condition) : base(tasks)
        {
            Condition = condition;
        }

        public Conditional() { }

        // A Conditional will return immediately with a success status code when one of its children
        // runs fails. As long as its children are Succeding, it will keep on retrying. If it runs out of
        // children completely, it will return a failure status code
        public override Result Run()
        {
            if (children == null || children.Count != 2)
                return Result.Failure;

            if (Condition != null && Condition())
            {
                return children[0].Run();
            }
            return children[1].Run();
        }
    }
}
