using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Assets.Scripts.IAJ.Unity.Utils;
using Assets.Scripts.IAJ.Unity.DecisionMaking.HeroActions;
using UnityEditor;

namespace Assets.Scripts.IAJ.Unity.DecisionMaking.ForwardModel
{
    //Implementation of a WorldModel Class using a recursive dictionary
    public class DictionaryWorldModel : WorldModel
    {
        private Dictionary<string, object> Properties { get; set; }
        private List<Goal> Goals { get; set; }
        protected DictionaryWorldModel Parent { get; set; }

        //This constructor is called to create the first world model,
        //that corresponds to the character's perceptions of the current "real world"
        public DictionaryWorldModel(GameManager gameManager, AutonomousCharacter character,  List<Action> actions, List<Goal> goals)
        {
            this.Properties = new Dictionary<string, object>();
            this.GoalValues = new Dictionary<string, float>();
            this.Goals = goals;
            this.Actions = new List<Action>(actions);
            this.Actions.Shuffle();
            this.ActionEnumerator = this.Actions.GetEnumerator();
            this.GameManager = gameManager;
            this.Character = character;
            this.NextPlayer = 0;

            foreach (var goal in goals)
            {
                this.GoalValues.Add(goal.Name, goal.InsistenceValue);
            }
        }

        public DictionaryWorldModel(DictionaryWorldModel parent)
        {
            this.Properties = new Dictionary<string, object>();
            this.GoalValues = new Dictionary<string, float>();
            this.Goals = parent.Goals;
            this.Actions = new List<Action>(parent.Actions);
            this.Actions.Shuffle();
            this.Parent = parent;
            this.ActionEnumerator = this.Actions.GetEnumerator();
            this.GameManager = parent.GameManager;
            this.Character = parent.Character;
        }

        public override WorldModel GenerateChildWorldModel()
        {
            return new DictionaryWorldModel(this);
        }

        public override void Initialize()
        {
            base.Initialize();

            if (this.Parent != null) return;

            this.Properties.Clear();
            this.GoalValues.Clear();
            foreach (var goal in this.Goals)
            {
                this.GoalValues.Add(goal.Name, goal.InsistenceValue);
            }
        }


        public override object GetProperty(string propertyName)
        {
            //recursive implementation of WorldModel
            if (this.Properties.ContainsKey(propertyName))
            {
                return this.Properties[propertyName];
            }
            else if (this.Parent != null)
            {
                return this.Parent.GetProperty(propertyName);
            }
            else   //we are at the base WorldModel, that corresponds to the charactr's perceptions
            {
                return propertyName switch
                {
                    PropertiesName.MANA =>      this.GameManager.Character.baseStats.Mana,
                    PropertiesName.MAXMANA =>   this.GameManager.Character.baseStats.MaxMana,
                    PropertiesName.XP =>        this.GameManager.Character.baseStats.XP,
                    PropertiesName.MAXHP =>     this.GameManager.Character.baseStats.MaxHP,
                    PropertiesName.HP =>        this.GameManager.Character.baseStats.HP,
                    PropertiesName.ShieldHP =>  this.GameManager.Character.baseStats.ShieldHP,
                    PropertiesName.MaxShieldHP => this.GameManager.Character.baseStats.MaxShieldHp,
                    PropertiesName.MONEY =>     this.GameManager.Character.baseStats.Money,
                    PropertiesName.TIME =>      this.GameManager.Character.baseStats.Time,
                    PropertiesName.LEVEL =>     this.GameManager.Character.baseStats.Level,
                    PropertiesName.POSITION =>  this.GameManager.Character.gameObject.transform.position,
                    PropertiesName.DURATION =>  0.0f,
                    PropertiesName.PreviousLEVEL => this.GameManager.Character.baseStats.Level,
                    PropertiesName.PreviousMONEY => this.GameManager.Character.baseStats.Money,
                    _ => this.GameManager.disposableObjects.ContainsKey(propertyName),
                    // If an object name is found in the dictionary of disposable objects,
                    // then the object still exists. The object has been removed/destroyed otherwise.
                };
            }
        }

        public override void SetProperty(string propertyName, object value)
        {
            this.Properties[propertyName] = value;
        }

        public override float GaetGoalInsistence(string goalName)
        {
            //recursive implementation of WorldModel
            if (this.GoalValues.ContainsKey(goalName))
            {
                return this.GoalValues[goalName];
            }
            else if (this.Parent != null)
            {
                return this.Parent.GaetGoalInsistence(goalName);
            }
            else  //we are at the base WorldModel, that corresponds to the charactr's perceptions
            {
                // Base world model should have the initial goal values set in the constructor.
                // Be defensive: if the key is missing, return 0.
                return this.GoalValues.ContainsKey(goalName) ? this.GoalValues[goalName] : 0.0f;

            }
        }

        public override void SetGoalValue(string goalName, float value)
        {
            this.GoalValues[goalName] = value;
        }



        public override bool IsTerminal()
        {
            return this.IsWinningState() || this.IsLosingState();
        }

        public override bool IsWinningState()
        {
            int money = (int)this.GetProperty(PropertiesName.MONEY);

            return this.NextPlayer == 0 && money == 25;
        }

        public override bool IsLosingState()
        {
            int HP = (int)this.GetProperty(PropertiesName.HP);
            float time = (float)this.GetProperty(PropertiesName.TIME);

            return HP <= 0 || time >= GameManager.GameConstants.TIME_LIMIT;
        }

        public override float GetFinalScore()
        {
            int money = (int)this.GetProperty(PropertiesName.MONEY);
            int HP = (int)this.GetProperty(PropertiesName.HP);
            int ShieldHP = (int)this.GetProperty(PropertiesName.ShieldHP);
            int lvl = (int)this.GetProperty(PropertiesName.LEVEL);
            float time = (float)this.GetProperty(PropertiesName.TIME);

            if (HP <= 0 || time >= GameManager.GameConstants.TIME_LIMIT) //lose
                return 0.0f;
            else if (this.NextPlayer == 0 && money == 25 && HP > 0) //win
                return 1.0f;
            else
            { // non-terminal state
                return 0.0f;
            }
        }

        public override float GetScore()
        {
            int money = (int)this.GetProperty(PropertiesName.MONEY);
            int HP = (int)this.GetProperty(PropertiesName.HP);
            int ShieldHP = (int)this.GetProperty(PropertiesName.ShieldHP);
            int lvl = (int)this.GetProperty(PropertiesName.LEVEL);
            float time = (float)this.GetProperty(PropertiesName.TIME);

            if (HP <= 0 || time >= GameManager.GameConstants.TIME_LIMIT) //lose
                return 0.0f;
            else if (this.NextPlayer == 0 && money == 25 && HP > 0) //win
                return 1.0f;
            else
            { // non-terminal state
                return timeAndMoneyScore(time, money) + levelScore(lvl) + hpScore(HP + ShieldHP) + timeScore(time);
            }
        }

        private float timeAndMoneyScore(float time, int money)
        {
            return 0.01f * (money / time);
        }

        private float levelScore(int lvl)
        {
            return 0.03f * lvl;
        }

        private float hpScore(int HP)
        {
            return 0.11f * HP;
        }

        private float timeScore(float time)
        {
            return -0.01f * (time - GameManager.GameConstants.TIME_LIMIT);
        }

        /*public virtual int GetNextPlayer()
        {
            return this.NextPlayer;
        }

        public virtual void CalculateNextPlayer()
        {
            Vector3 position = (Vector3)this.GetProperty(PropertiesName.POSITION);
            bool enemyEnabled;

            //basically if the character is close enough to an enemy, the next player will be the enemy.
            foreach (var enemy in this.GameManager.enemies)
            {
                enemyEnabled = (bool)this.GetProperty(enemy.name);
                if (enemyEnabled && (enemy.transform.position - position).sqrMagnitude <= 100)
                {
                    this.NextPlayer = 1;
                    this.NextEnemyAction = new SwordAttack(this.GameManager.Character, enemy);
                    this.NextEnemyActions = new Action[] { this.NextEnemyAction };
                    return;
                }
            }
            this.NextPlayer = 0;
            //if not, then the next player will be player 0

        }*/
    }
}