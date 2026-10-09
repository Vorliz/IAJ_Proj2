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

        private const float TIE_TOLERANCE = 0.1f;
        private float BestFirstActionDuration { get; set; }

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
            this.BestFirstActionDuration = float.MaxValue;
            this.InitialWorldModel.Initialize();
        }

        public Action ChooseAction()
        {
            var processedActions = 0;
            var startTime = Time.realtimeSinceStartup;
            // Debug.Log("DepthLimitedGOAP: ChooseAction started. InProgress=" + this.InProgress + " MaxDepth=" + this.MaxDepth + " CurrentDepth=" + this.CurrentDepth);

            while (this.CurrentDepth >= 0)
            {
                if(processedActions >= this.ActionCombinationsProcessedPerFrame)
                {
                    this.TotalProcessingTime += Time.realtimeSinceStartup - startTime;
                    return null;
                }

                var currentModel = this.Models[this.CurrentDepth];

                // || (this.CurrentDepth > 0 && currentModel.IsTerminal()) is used for when only two actions remain and the second action is terminal, so we can still evaluate the first action
                if(this.CurrentDepth >= this.MaxDepth || (this.CurrentDepth > 0 && currentModel.IsTerminal()))
                {
                    var currentValue = currentModel.Character.CalculateDiscontentment(currentModel);
                    processedActions++;
                    this.TotalActionCombinationsProcessed++;

                    var firstActionDuration = (float)this.Models[1].GetProperty(PropertiesName.DURATION);

                    bool clearlyBetter = currentValue < this.BestDiscontentmentValue - TIE_TOLERANCE;
                    bool tiedButQuicker = Mathf.Abs(currentValue - this.BestDiscontentmentValue) <= TIE_TOLERANCE && firstActionDuration < this.BestFirstActionDuration;

                    if (clearlyBetter || tiedButQuicker)
                    {
                        BestDiscontentmentValue = currentValue;
                        this.BestFirstActionDuration = firstActionDuration;
                        BestAction = this.LevelAction[0];
                        this.BestActionSequenceEndState = currentModel;

                        System.Array.Clear(this.BestActionSequence, 0, this.MaxDepth);
                        System.Array.Copy(this.LevelAction, this.BestActionSequence, this.CurrentDepth);
                    }

                    this.CurrentDepth--;
                    continue;
                }

                var nextAction = currentModel.GetNextAction();

                if(nextAction != null)
                {
                    var child = currentModel.GenerateChildWorldModel();
                    nextAction.ApplyActionEffects(child);
                    child.Character.UpdateGoalsInsistence(child);
                    this.Models[this.CurrentDepth + 1] = child;
                    this.LevelAction[this.CurrentDepth] = nextAction;
                    this.CurrentDepth++;
                }
                else
                {
                    this.CurrentDepth--;
                }
            }

            // Finished full search: update processing time, mark as not in progress and return best action found
            this.TotalProcessingTime += Time.realtimeSinceStartup - startTime;
            this.InProgress = false;
            return this.BestAction;

        }
    }
}
