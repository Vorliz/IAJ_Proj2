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
    public class MCTS
    {
        public const float C = 1.4f;
        public bool InProgress { get; private set; }
        protected int MaxIterations { get; set; }
        protected int MaxIterationsPerFrame { get; set; }
        protected int NumberPlayouts { get; set; }
        protected int PlayoutDepthLimit { get; set; }
        public MCTSNode BestFirstChild { get; set; }

        public List<MCTSNode> BestSequence { get; set; }
        public WorldModel BestActionSequenceEndState { get; set; }
        public int CurrentIterations { get; protected set; }
        protected int CurrentDepth { get; set; }
        protected int FrameCurrentIterations { get; set; }
        protected WorldModel InitialState { get; set; }
        protected MCTSNode InitialNode { get; set; }
        protected System.Random RandomGenerator { get; set; }

        //Information and Debug Properties
        public int MaxPlayoutDepthReached { get; set; }
        public int MaxSelectionDepthReached { get; set; }
        public float TotalProcessingTime { get; set; }



        public MCTS(WorldModel currentStateWorldModel, int maxIter, int maxIterFrame, int playouts, int playoutDepthLimit)
        {
            this.InitialState = currentStateWorldModel;
            this.MaxIterations = maxIter;
            this.MaxIterationsPerFrame = maxIterFrame;
            this.NumberPlayouts = playouts;
            this.PlayoutDepthLimit = playoutDepthLimit;
            this.InProgress = false;
            this.RandomGenerator = new System.Random();
        }

        public void InitializeMCTSearch()
        {
            this.InitialState.Initialize();
            this.MaxPlayoutDepthReached = 0;
            this.MaxSelectionDepthReached = 0;
            this.CurrentIterations = 0;
            this.FrameCurrentIterations = 0;
            this.TotalProcessingTime = 0.0f;

            // create root node n0 for state s0
            this.InitialNode = new MCTSNode(this.InitialState)
            {
                Action = null,
                Parent = null,
                PlayerID = 0
            };
            this.InProgress = true;
            this.BestFirstChild = null;
        }

        public Action ChooseAction()
        {
            if (this.InitialNode == null)
            {
                return null;
            }

            var startTime = Time.realtimeSinceStartup;
            this.FrameCurrentIterations = 0;

            while (this.CurrentIterations < this.MaxIterations && this.FrameCurrentIterations < this.MaxIterationsPerFrame)
            {
                MCTSNode selectedNode = this.Selection(this.InitialNode);

                float totalReward = 0.0f;
                for (int i = 0; i < this.NumberPlayouts; i++)
                {
                    totalReward += this.Playout(selectedNode.State);
                }

                float averageReward = totalReward / this.NumberPlayouts;
                if (selectedNode.PlayerID != 0) {
                    averageReward = -averageReward;
                }
                this.Backpropagate(selectedNode, averageReward);

                this.CurrentIterations++;
                this.FrameCurrentIterations++;
            }

            this.TotalProcessingTime = Time.realtimeSinceStartup - startTime;
            if (this.FrameCurrentIterations >= this.MaxIterationsPerFrame)
            {
                return null;
            }
            this.InProgress = false;
            return this.BestAction(this.InitialNode);
        }

        // Selection and Expantion
        protected MCTSNode Selection(MCTSNode initialNode)
        {
            // select a node, if not all expanded, expand, otherwise choose best
            Action nextAction;
            MCTSNode currentNode = initialNode;

            CurrentDepth = 0;
            while (!currentNode.State.IsTerminal())
            {
                nextAction = currentNode.State.GetNextAction();
                if (nextAction != null)
                {
                    return Expand(currentNode, nextAction);
                }
                else
                {
                    MCTSNode bestChild = BestUCTChild(currentNode);
                    if (bestChild == null)
                    {
                        return currentNode;
                    }
                    currentNode = bestChild;
                    CurrentDepth++;
                    MaxSelectionDepthReached = Math.Max(MaxSelectionDepthReached, CurrentDepth);
                }
            }
            return currentNode;
        }

        protected virtual float Playout(WorldModel initialStateForPlayout)
        {
            WorldModel currentState = initialStateForPlayout;
            int playoutDepth = 0;
            while (!currentState.IsTerminal() || playoutDepth <= PlayoutDepthLimit)
            {
                var availableAction = currentState.GetNextAction();
                if (availableAction == null)
                {
                    break;
                }
                playoutDepth++;
                var nextState = currentState.GenerateChildWorldModel();
                availableAction.ApplyActionEffects(nextState);
                currentState = nextState;
                //currentState.CalculateNextPlayer();
            }

            MaxPlayoutDepthReached = Math.Max(MaxPlayoutDepthReached, playoutDepth);
            return currentState.GetScore();
        }

        protected virtual void Backpropagate(MCTSNode node, float reward)
        {
            MCTSNode currentNode = node;
            while (currentNode != null)
            {
                currentNode.N = currentNode.N + 1;
                currentNode.Q = currentNode.Q + reward;
                currentNode = currentNode.Parent;
            }
        }

        protected MCTSNode Expand(MCTSNode parent, Action action)
        {
            MCTSNode currentNode;
            var newState = parent.State.GenerateChildWorldModel();
            action.ApplyActionEffects(newState);
            currentNode = new MCTSNode(newState) { Action = action, Parent = parent };
            parent.ChildNodes.Add(currentNode);
            return currentNode;
        }

        protected virtual MCTSNode BestUCTChild(MCTSNode node)
        {
            MCTSNode bestNode = null;
            double bestUctValue = double.NegativeInfinity;
            double lnParent = Math.Log(node.N);
            foreach (MCTSNode currentNode in node.ChildNodes)
            {
                double uctValue = (currentNode.Q / currentNode.N) + C * Math.Sqrt(lnParent / currentNode.N);
                if (uctValue > bestUctValue) {
                    bestNode = currentNode;
                    bestUctValue = uctValue;
                }
            }
            return bestNode;
        }

        //this method is very similar to the bestUCTChild, but it is used to return the final action of the MCTS search, and so we do not care about
        //the exploration factor
        protected MCTSNode BestChild(MCTSNode node)
        {
            MCTSNode bestNode = null;
            double bestValue = double.NegativeInfinity;
            foreach (MCTSNode currentNode in node.ChildNodes)
            {
                double value = currentNode.Q / currentNode.N;
                if (value > bestValue) {
                    bestNode = currentNode;
                    bestValue = value;
                }
            }
            return bestNode;
        }


        protected Action BestAction(MCTSNode node)
        {
            var bestChild = this.BestChild(node);
            if (bestChild == null) return null;

            this.BestFirstChild = bestChild;

            //this is done for debugging proposes only
            this.BestSequence = new List<MCTSNode> { bestChild };
            node = bestChild;
            this.BestActionSequenceEndState = node.State;

            while(!node.State.IsTerminal())
            {
                bestChild = this.BestChild(node);
                if (bestChild == null) {
                    break;
                }
                this.BestSequence.Add(bestChild);
                node = bestChild;
                this.BestActionSequenceEndState = node.State;
            }
            return this.BestFirstChild.Action;
        }

    }
}
