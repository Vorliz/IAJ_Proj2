using UnityEngine;
using System.Collections;
using System;
using Assets.Scripts.IAJ.Unity.Utils;
using UnityEngine.AI;
using Assets.Scripts.IAJ.Unity.DecisionMaking.BehaviorTree;
using Assets.Scripts.IAJ.Unity.DecisionMaking.BehaviorTree.BehaviourTrees;
//using Assets.Scripts.IAJ.Unity.Formations;
using System.Collections.Generic;
using static GameManager;
using Assets.Scripts.IAJ.Unity.DecisionMaking.StateMachine;

namespace Assets.Scripts.Game.NPCs
{

    public class Orc : Monster
    {
        private const float LeaderSpeedFactor = 0.7f;
        public bool HeardShout;
        public Vector3 ShoutPosition;
        public bool IsAttacking;
        public Vector3 PatrolPoint1;
        public Vector3 PatrolPoint2;
        /*public Sprite AlertSprite;

        private static Sprite defaultAlertSprite;*/

        public Orc()
        {
            this.stats.Type = "Orc";
            this.stats.XPvalue = 8;
            this.stats.AC = 14;
            this.baseStats.HP = 15;
            this.DmgRoll = () => RandomHelper.RollD10() + 2;
            this.stats.SimpleDamage = 6;
            this.stats.AwakeDistance = 15;
            this.stats.WeaponRange = 3;
            this.HeardShout = false;
            this.IsAttacking = false;
        }

        /*private static Sprite CreateDefaultAlertSprite()
        {
            const int textureSize = 128;
            const float center = textureSize / 2f;
            var texture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);
            var pixels = new Color32[textureSize * textureSize];

            for (int y = 0; y < textureSize; y++)
            {
                for (int x = 0; x < textureSize; x++)
                {
                    var point = new Vector2(x + 0.5f, y + 0.5f);
                    var stemPoint = new Vector2(center, Mathf.Clamp(point.y, 52f, 106f));
                    var stemAlpha = 1f - Mathf.SmoothStep(9.25f, 10.75f, Vector2.Distance(point, stemPoint));
                    var dotAlpha = 1f - Mathf.SmoothStep(9.25f, 10.75f, Vector2.Distance(point, new Vector2(center, 23f)));
                    var alpha = Mathf.Max(stemAlpha, dotAlpha);
                    pixels[y * textureSize + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(alpha * 255f));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            return Sprite.Create(texture, new Rect(0f, 0f, textureSize, textureSize), new Vector2(0.5f, 0.5f), 100f);
        }*/

        public override void InitializeStateMachine()
        {
            this.StateMachine = new StateMachine(new Sleep(this));
        }

        public override void InitializeBehaviourTree()
        {
            this.Get2NearestPatrolPoints(out PatrolPoint1, out PatrolPoint2);
            this.BehaviourTree = new PatrolTree(this, Target, this.stats.WeaponRange);

            if (this.formationLeader)
            {
                this.SetFormationLeader();
            }
        }

        public override void SetFormationLeader()
        {
            this.agent.speed *= LeaderSpeedFactor;
        }

        public void HearShout(Vector3 position)
        {
            this.ShoutPosition = position;
            this.HeardShout = true;
        }


        //If you use a folder structure for each pair of Patrolpoints, you can use the following method to get the patrol points of the closest patrol to the orc.
        public void GetPatrolPoints (out Vector3 position1, out Vector3 position2)
        {
            var patrols = GameObject.FindGameObjectsWithTag("Patrol");

            float pos = float.MaxValue;
            GameObject closest = null;
            foreach (var p in patrols)
            {
                if (Vector3.Distance(this.agent.transform.position, p.transform.position) < pos)
                {
                    pos = Vector3.Distance(this.agent.transform.position, p.transform.position);
                    closest = p;
                }

            }

            position1 = closest.transform.GetChild(0).position;
            position2 = closest.transform.GetChild(1).position;
        }

        //If you just spread each patrol point independently, this method will get the two closest patrol points to the orc.
        public void Get2NearestPatrolPoints(out Vector3 position1, out Vector3 position2)
        {
            var patrols = GameObject.FindGameObjectsWithTag("Patrol");

            float closestDistance = float.MaxValue;
            float secondClosestDistance = float.MaxValue;
            GameObject closest = null;
            GameObject secondClosest = null;

            foreach (var p in patrols)
            {
                var distance = Vector3.Distance(this.transform.position, p.transform.position);
                if (distance < closestDistance)
                {
                    secondClosestDistance = closestDistance;
                    secondClosest = closest;
                    closestDistance = distance;
                    closest = p;
                }
                else if (distance < secondClosestDistance)
                {
                    secondClosestDistance = distance;
                    secondClosest = p;
                }
            }

            position1 = closest != null ? closest.transform.position : this.transform.position;
            position2 = secondClosest != null ? secondClosest.transform.position : position1;
        }
    }
}
