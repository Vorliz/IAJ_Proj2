using UnityEngine;

namespace Assets.Scripts.IAJ.Unity.DecisionMaking.BehaviorTree.EnemyTasks
{
    public class ShoutMarker : MonoBehaviour
    {
        public int Counter;

        public void updateCounter() {
            Counter--;
            if (Counter <= 0)
            {
                this.destroy();
            }
        }

        public void destroy()
        {
            GameObject.Destroy(GameObject.FindAnyObjectByType<ShoutMarker>());
        }
    }
}