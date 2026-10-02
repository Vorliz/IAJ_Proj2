using System.Collections.Generic;
using Assets.Scripts.IAJ.Unity.DecisionMaking.HeroActions;
using Assets.Scripts.IAJ.Unity.DecisionMaking.ForwardModel;
using UnityEngine;
using System.Linq;

namespace Assets.Scripts.IAJ.Unity.DecisionMaking.GOB
{
    public class GOBDecisionMaking
    {
        public bool InProgress { get; set; }
        private List<Goal> goals { get; set; }
        private List<Action> actions { get; set; }

        public Dictionary<Action,float> ActionDiscontentment { get; set; }

        public Action secondBestAction;
        public Action thirdBestAction;

        // Utility based GOB
        public GOBDecisionMaking(List<Action> _actions, List<Goal> goals)
        {
            this.actions = _actions;
            this.goals = goals;
            secondBestAction = new Action("yo");
            thirdBestAction = new Action("yo too");
            this.ActionDiscontentment = new Dictionary<Action,float>();
        }

        //Predicting the Discontentment after executing the action
        public static float CalculateDiscontentment(Action action, List<Goal> goals, AutonomousCharacter character)
        {
            // Keep a running total
            var discontentment = 0.0f;
            var duration = action.GetDuration();

            foreach (var goal in goals)
            {
                 // Calculate the new value after the action
                 //+ duration * goal.ChangeRate retirada porque o be quick já é calculado noutro lado
                float changeValue = action.GetGoalChange(goal);
                
                // The change rate is how much the goals changes per time
                var newValue = goal.NormalizeGoalValue(goal.InsistenceValue + changeValue, goal.Min, goal.Max);

                discontentment += goal.GetDiscontentment(newValue);
            }

            return discontentment;
        }

        public Action ChooseAction(AutonomousCharacter character)
        {
            // Find the action leading to the lowest discontentment
            Action bestAction = null;
            secondBestAction = null;
            thirdBestAction = null;
            ActionDiscontentment.Clear();

            foreach (var action in actions)
            {
                if (action.CanExecute())
                {
                    var discontentmentValue = CalculateDiscontentment(action, goals, character);
                    ActionDiscontentment[action] = discontentmentValue;
                    // The values are read from ActionDiscontentment, so only the actions need to be shifted down
                    if (bestAction == null || discontentmentValue < ActionDiscontentment[bestAction])
                    {
                        thirdBestAction = secondBestAction;
                        secondBestAction = bestAction;
                        bestAction = action;
                    } else if (secondBestAction == null || discontentmentValue < ActionDiscontentment[secondBestAction])
                    {
                        thirdBestAction = secondBestAction;
                        secondBestAction = action;
                    } else if (thirdBestAction == null || discontentmentValue < ActionDiscontentment[thirdBestAction])
                    {
                        thirdBestAction = action;
                    }

                }
            } 

            InProgress = false;
            return bestAction;
        }
    }
}
