using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.Threading.Tasks;
using UnityEngine.AI;
using Assets.Scripts.Game.NPCs;
using Assets.Scripts.Game;
using Unity.AppUI.UI;
using UnityEngine.Rendering;

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
            // spawn a ! on orc location
            // counter should be on orcs - 1
            // play sound
            // make action for orcs to go to !
            // if they see player, go to pursuit
            // if they reach ! and they dont see player go back to patrol
            var position = Orc.transform.position;

            Orc.AudioSource.PlayOneShot(GameManager.Instance.ShoutAudio);

            /*if (Orc.AlertSprite == null)
                return Result.Failure;*/

            /*var exclamationMark = new GameObject("!");
            exclamationMark.transform.position = position;
            exclamationMark.AddComponent<SpriteRenderer>().sprite = Orc.AlertSprite;

            var marker = exclamationMark.GetComponent<ShoutMarker>() ?? exclamationMark.AddComponent<ShoutMarker>();
            marker.Counter = GameManager.Instance.orcs.Count - 1;
            marker.Id = marker.GetEntityId();*/
            return Result.Failure;
        }

    }
}