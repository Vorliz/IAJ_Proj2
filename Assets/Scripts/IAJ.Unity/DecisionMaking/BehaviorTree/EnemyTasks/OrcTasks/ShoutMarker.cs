using System.Collections;
using UnityEngine;

namespace Assets.Scripts.IAJ.Unity.DecisionMaking.BehaviorTree.EnemyTasks
{
    public class ShoutMarker : MonoBehaviour
    {
        private const float FadeDuration = 2f;

        public int Counter;
        public EntityId Id;

        private IEnumerator Start()
        {
            var spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
                yield break;

            var color = spriteRenderer.color;
            color.a = 1f;
            spriteRenderer.color = color;

            float elapsed = 0f;
            while (elapsed < FadeDuration)
            {
                elapsed += Time.deltaTime;
                color.a = 1f - Mathf.Clamp01(elapsed / FadeDuration);
                spriteRenderer.color = color;
                yield return null;
            }

            color.a = 0f;
            spriteRenderer.color = color;
        }

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