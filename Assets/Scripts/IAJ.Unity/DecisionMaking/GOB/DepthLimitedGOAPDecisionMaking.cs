using System.Collections.Generic;
using UnityEngine;
using Assets.Scripts.IAJ.Unity.DecisionMaking.HeroActions;
using Assets.Scripts.IAJ.Unity.DecisionMaking.ForwardModel;
using Assets.Scripts.Game;

namespace Assets.Scripts.IAJ.Unity.DecisionMaking.GOB
{
    public class DepthLimitedGOAPDecisionMaking
    {
        public const int DEFAULT_MAX_DEPTH = 3;
        public int ActionCombinationsProcessedPerFrame { get; set; }
        public float TotalProcessingTime { get; set; }
        public int TotalActionCombinationsProcessed { get; set; }
        public bool InProgress { get; set; }
        public int MaxDepth { get; private set; }

        public WorldModel InitialWorldModel { get; set; }
        public WorldModel BestActionSequenceEndState { get; set; }
        private List<Goal> Goals { get; set; }
        private WorldModel[] Models { get; set; }
        private Action[] LevelAction { get; set; }
        public Action[] BestActionSequence { get; private set; }
        public Action BestAction { get; private set; }
        public float BestDiscontentmentValue { get; private set; }
        private int CurrentDepth {  get; set; }

        public DepthLimitedGOAPDecisionMaking(WorldModel currentStateWorldModel, AutonomousCharacter character, int maxDepth)
        {
            this.ActionCombinationsProcessedPerFrame = 2000;
            this.Goals = character.Goals;
            this.InitialWorldModel = currentStateWorldModel;
            this.MaxDepth = Mathf.Max(1, maxDepth);
        }

        public void InitializeDecisionMakingProcess()
        {
            this.InProgress = true;
            this.TotalProcessingTime = 0.0f;
            this.TotalActionCombinationsProcessed = 0;
            this.CurrentDepth = 0;
            // Recreate a fresh initial world model from the current game state so the
            // decision process always uses up-to-date perceptions (disposable objects, character stats, etc.). 
            this.InitialWorldModel = new DictionaryWorldModel(GameManager.Instance, this.InitialWorldModel.Character, this.InitialWorldModel.Character.Actions, this.Goals);
            this.Models = new DictionaryWorldModel[this.MaxDepth + 1];
            this.Models[0] = (DictionaryWorldModel)this.InitialWorldModel;
            this.LevelAction = new Action[this.MaxDepth];
            this.BestActionSequence = new Action[this.MaxDepth];
            this.BestAction = null;
            this.BestDiscontentmentValue = float.MaxValue;
            this.InitialWorldModel.Initialize();
        }

        public Action ChooseAction()
        {
            var processedActions = 0;
            var startTime = Time.realtimeSinceStartup;
            Debug.Log("DepthLimitedGOAP: ChooseAction started. InProgress=" + this.InProgress + " MaxDepth=" + this.MaxDepth + " CurrentDepth=" + this.CurrentDepth);

            while (this.CurrentDepth >= 0)
            {
                //ToDo Implement
            }

            // Finished full search: update processing time, mark as not in progress and return best action found
            this.TotalProcessingTime += Time.realtimeSinceStartup - startTime;
            this.InProgress = false;
            return this.BestAction;

        }
    }
}
