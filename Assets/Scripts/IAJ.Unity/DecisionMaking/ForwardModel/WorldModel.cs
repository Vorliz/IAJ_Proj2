using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Assets.Scripts.IAJ.Unity.Utils;
using Assets.Scripts.IAJ.Unity.DecisionMaking.HeroActions;

namespace Assets.Scripts.IAJ.Unity.DecisionMaking.ForwardModel
{
    //This Abstract Class defines the methods any WorldModel should support
    public abstract class WorldModel
    {
        protected List<Action> Actions { get; set; }
        protected IEnumerator<Action> ActionEnumerator { get; set; }
        protected GameManager GameManager { get; set; }
        public AutonomousCharacter Character { get; protected set; }
        public Dictionary<string, float> GoalValues { get; set; }
        protected int NextPlayer { get; set; }
        protected Action NextEnemyAction { get; set; }
        protected Action[] NextEnemyActions { get; set; }

        public abstract WorldModel GenerateChildWorldModel();

        public abstract float GaetGoalInsistence(string goalName);

        public abstract void SetGoalValue(string goalName, float goalValue);

        public abstract object GetProperty(string propertyName);

        public abstract void  SetProperty(string propertyName, object value);

        public abstract float GetScore();

        public abstract float GetFinalScore();

        public abstract bool IsTerminal();

        public abstract bool IsWinningState();

        public abstract bool IsLosingState();

        public virtual void Initialize()
        {
            // Reset the action enumeration by obtaining a fresh enumerator instance.
            // Calling Reset() on an existing enumerator can be unreliable depending on
            // the concrete IEnumerator implementation, so recreate it instead.
            if (this.Actions != null)
                this.ActionEnumerator = this.Actions.GetEnumerator();
        }

        public virtual int GetNextPlayer()
        {
            return this.NextPlayer;
        }

        public virtual void CalculateNextPlayer()
        {
            Vector3 position = (Vector3)this.GetProperty(PropertiesName.POSITION);
            bool enemyEnabled;

            //basically if the character is close enough to an enemy, the next player will be the enemy.
            if (GameManager.monsterControl != GameManager.MonsterControl.SleepingMonsters)
            {
                foreach (var enemy in this.GameManager.enemies)
                {
                    enemyEnabled = (bool)this.GetProperty(enemy.name);
                    if (enemyEnabled && (enemy.transform.position - position).sqrMagnitude <= 100)
                    {
                        this.NextPlayer = 1;
                        this.NextEnemyAction = new EnemyAttack(this.GameManager.Character, enemy);
                        this.NextEnemyActions = new Action[] { this.NextEnemyAction };
                        return;
                    }
                }
            }
            this.NextPlayer = 0;
            //if not, then the next player will be player 0

        }

        public virtual Action GetNextAction()
        {
            Action action = null;
            //returns the next action that can be executed or null if no more executable actions exist
            if (this.NextPlayer == 0)
            {
                if (this.ActionEnumerator.MoveNext())
                    action = ActionEnumerator.Current;

                while (action != null && !action.CanExecute(this))
                {
                    if (this.ActionEnumerator.MoveNext())
                        action = ActionEnumerator.Current;
                    else
                        action = null;
                }
            }
            else
            {
                action = this.NextEnemyAction;
                this.NextEnemyAction = null;
            }

            return action;
        }

        // Heuristic value of a non-terminal state, used by the biased/limited playouts.
        // Each factor is normalized to [0,1] and the total is scaled to [0, 0.9],
        // so a non-terminal state is always worth less than a victory (1.0).
        private const int MaxLevel = 4; // same cap as the LevelUp action

        protected float NonTerminalHeuristicScore()
        {
            int money = (int)this.GetProperty(PropertiesName.MONEY);
            int hp = (int)this.GetProperty(PropertiesName.HP);
            int shieldHP = (int)this.GetProperty(PropertiesName.ShieldHP);
            int maxHP = (int)this.GetProperty(PropertiesName.MAXHP);
            int level = (int)this.GetProperty(PropertiesName.LEVEL);
            float time = (float)this.GetProperty(PropertiesName.TIME);

            float moneyScore = Mathf.Clamp01(money / 25.0f);
            float hpScore = Mathf.Clamp01((hp + shieldHP) / (float)Mathf.Max(1, maxHP));
            float levelScore = Mathf.Clamp01((level - 1) / (float)(MaxLevel - 1));
            float timeScore = Mathf.Clamp01(1.0f - time / GameManager.GameConstants.TIME_LIMIT);

            return 0.9f * (0.4f * moneyScore + 0.2f * hpScore + 0.3f * levelScore + 0.1f * timeScore);
        }

        public virtual Action[] GetExecutableActions()
        {
            if (this.NextPlayer == 0)
                return this.Actions.Where(a => a.CanExecute(this)).ToArray();
            else
                return this.NextEnemyActions;
        }

    }
}
