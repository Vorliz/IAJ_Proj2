using Assets.Scripts.IAJ.Unity.DecisionMaking.HeroActions;
using Assets.Scripts.IAJ.Unity.DecisionMaking.ForwardModel;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Action = Assets.Scripts.IAJ.Unity.DecisionMaking.HeroActions.Action;
using Assets.Scripts.IAJ.Unity.Utils;
using UnityEditor.Animations;
using NUnit.Framework.Interfaces;

namespace Assets.Scripts.IAJ.Unity.DecisionMaking.MCTS
{
    public class MCTSBiasedPlayout : MCTS
    {
        public MCTSBiasedPlayout(WorldModel currentStateWorldModel, int maxIter, int maxIterFrame, int playouts, int playoutDepthLimit)
            : base(currentStateWorldModel, maxIter, maxIterFrame, playouts, playoutDepthLimit)
        {
        }

        // Selection and Expantion

        protected override float Playout(WorldModel initialStateForPlayout)
        {
            WorldModel currentState = initialStateForPlayout;
            int playoutDepth = 0;
            while (!currentState.IsTerminal() || playoutDepth <= PlayoutDepthLimit)
            {
                var availableActions = currentState.GetExecutableActions();
                if (availableActions == null || availableActions.Count() == 0)
                {
                    break;
                }
                playoutDepth++;
                var nextState = currentState.GenerateChildWorldModel();
                GetBestAction(currentState, availableActions).ApplyActionEffects(nextState);
                currentState = nextState;
                //currentState.CalculateNextPlayer();
            }

            MaxPlayoutDepthReached = Math.Max(MaxPlayoutDepthReached, playoutDepth);
            return currentState.GetScore();
        }

        private Action GetBestAction(WorldModel currentState, Action[] actions)
        {
            Action bestAction = null;
            float bestActionScore = float.NegativeInfinity;
            foreach (Action action in actions)
            {
                float actionScore = action.GetHValue(currentState);
                if (actionScore > bestActionScore)
                {
                    bestAction = action;
                    bestActionScore = actionScore;
                }
            }
            return bestAction;
        }
    }
}