using UnityEngine;

namespace Assets.Scripts.IAJ.Unity.DecisionMaking.BehaviorTree.EnemyTasks
{
    // Visual clue for the orc that shouted: a red "!" drawn on screen right above its model.
    // Pure GUI (no scene objects, no colliders), so it never interferes with physics or raycasts.
    public class ShoutMarker : MonoBehaviour
    {
        private const float Duration = 3f;           // real seconds, independent of Time Scale
        private const float PixelsAboveModel = 4f;   // small gap between the top of the model and the "!"
        private const int FontSize = 22;

        private float endTime;
        private GUIStyle style;
        private Renderer modelRenderer;

        public void Show()
        {
            this.endTime = Time.unscaledTime + Duration;
            if (this.modelRenderer == null)
                this.modelRenderer = GetComponentInChildren<Renderer>();
        }

        private void OnGUI()
        {
            if (Time.unscaledTime > this.endTime || Camera.main == null) return;

            // top of the orc's model, so the mark follows its real size on screen
            var top = this.transform.position;
            if (this.modelRenderer != null)
            {
                var bounds = this.modelRenderer.bounds;
                top = bounds.center + Vector3.up * bounds.extents.y;
            }

            var screen = Camera.main.WorldToScreenPoint(top);
            if (screen.z < 0) return; // behind the camera

            if (this.style == null)
            {
                this.style = new GUIStyle(GUI.skin.label)
                {
                    fontSize = FontSize,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.LowerCenter
                };
                this.style.normal.textColor = Color.red;
            }

            // GUI coordinates have y pointing down; the rect's bottom edge sits just above the model
            const float size = 30f;
            var rect = new Rect(screen.x - size / 2f, Screen.height - screen.y - PixelsAboveModel - size, size, size);
            GUI.Label(rect, "!", this.style);
        }
    }
}
