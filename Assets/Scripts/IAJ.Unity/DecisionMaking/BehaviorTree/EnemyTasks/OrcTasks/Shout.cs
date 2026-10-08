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