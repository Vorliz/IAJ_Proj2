using UnityEngine;
using Assets.Scripts.Game.NPCs;
using Assets.Scripts.Game;

namespace Assets.Scripts.IAJ.Unity.DecisionMaking.BehaviorTree.EnemyTasks
{
    class Shout : Task
    {
        protected Orc Orc { get; set; }

        public Shout(Orc orc)
        {
            this.Orc = orc;
        }

        public override Result Run()
        {
            if (Orc == null)
                return Result.Failure;

            var position = Orc.transform.position;
            Orc.AudioSource.PlayOneShot(GameManager.Instance.ShoutAudio);

            // visual clue: a red "!" on screen above the orc that shouted
            var marker = Orc.GetComponent<ShoutMarker>();
            if (marker == null)
                marker = Orc.gameObject.AddComponent<ShoutMarker>();
            marker.Show();

            foreach (var orcObject in GameManager.Instance.orcs)
            {
                if (orcObject == null || orcObject == Orc.gameObject)
                    continue;

                var receiver = orcObject.GetComponent<Orc>();
                if (receiver != null)
                    receiver.HearShout(position);
            }

            return Result.Success;
        }

    }
}