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
            while (!currentState.IsTerminal() && playoutDepth < PlayoutDepthLimit)
            {
                var availableActions = currentState.GetExecutableActions();
                if (availableActions == null || availableActions.Length == 0)
                {
                    break;
                }
                playoutDepth++;
                var nextState = currentState.GenerateChildWorldModel();
                ChooseBiasedAction(currentState, availableActions).ApplyActionEffects(nextState);
                nextState.CalculateNextPlayer();
                currentState = nextState;
            }

            MaxPlayoutDepthReached = Math.Max(MaxPlayoutDepthReached, playoutDepth);
            // when the playout is cut by the depth limit, the heuristic estimates the quality of the state
            return currentState.GetScore();
        }

        // Softmax (roulette) over the actions' heuristic values: better actions are more likely,
        // but the choice stays random so that several playouts explore different sequences
        private const float Temperature = 0.05f;

        private Action ChooseBiasedAction(WorldModel currentState, Action[] actions)
        {
            if (actions.Length == 1) return actions[0];

            var hValues = new float[actions.Length];
            float maxH = float.NegativeInfinity;
            for (int i = 0; i < actions.Length; i++)
            {
                hValues[i] = actions[i].GetHValue(currentState);
                maxH = Math.Max(maxH, hValues[i]);
            }

            // subtracting maxH keeps Exp from overflowing
            var weights = new double[actions.Length];
            double totalWeight = 0.0;
            for (int i = 0; i < actions.Length; i++)
            {
                weights[i] = Math.Exp((hValues[i] - maxH) / Temperature);
                totalWeight += weights[i];
            }

            double roll = this.RandomGenerator.NextDouble() * totalWeight;
            for (int i = 0; i < actions.Length; i++)
            {
                roll -= weights[i];
                if (roll <= 0.0) return actions[i];
            }
            return actions[actions.Length - 1];
        }
    }
}