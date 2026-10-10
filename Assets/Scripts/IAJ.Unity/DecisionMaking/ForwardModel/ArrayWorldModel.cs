using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Assets.Scripts.IAJ.Unity.Utils;
using Assets.Scripts.IAJ.Unity.DecisionMaking.HeroActions;
using UnityEditor;

namespace Assets.Scripts.IAJ.Unity.DecisionMaking.ForwardModel
{
    //Implementation of a WorldModel Class using fixed-size arrays
    public class ArrayWorldModel : WorldModel
    {
        private const int FixedPropertyCount = 14;
        // one array position per disposable object (enemies, chests, potions), built from the real scene objects
        private Dictionary<string, int> DisposableIndices { get; set; }
        private int PropertyCount { get; set; }
        private object[] Properties { get; set; }
        private bool[] PropertyIsSet { get; set; }
        protected ArrayWorldModel Parent { get; set; }

        //This constructor is called to create the first world model,
        //that corresponds to the character's perceptions of the current "real world"
        public ArrayWorldModel(GameManager gameManager, AutonomousCharacter character,  List<Action> actions)
        {
            this.DisposableIndices = new Dictionary<string, int>();
            foreach (var objectName in gameManager.disposableObjects.Keys)
            {
                this.DisposableIndices[objectName] = FixedPropertyCount + this.DisposableIndices.Count;
            }
            this.PropertyCount = FixedPropertyCount + this.DisposableIndices.Count;

            this.Properties = new object[PropertyCount];
            this.PropertyIsSet = new bool[PropertyCount];
            this.Actions = new List<Action>(actions);
            this.Actions.Shuffle();
            this.ActionEnumerator = this.Actions.GetEnumerator();
            this.GameManager = gameManager;
            this.Character = character;
            this.NextPlayer = 0;
        }

        public ArrayWorldModel(ArrayWorldModel parent)
        {
            // children share the index map of the root
            this.DisposableIndices = parent.DisposableIndices;
            this.PropertyCount = parent.PropertyCount;
            this.Properties = new object[PropertyCount];
            this.PropertyIsSet = new bool[PropertyCount];
            this.Actions = new List<Action>(parent.Actions);
            this.Actions.Shuffle();
            this.Parent = parent;
            this.ActionEnumerator = this.Actions.GetEnumerator();
            this.GameManager = parent.GameManager;
            this.Character = parent.Character;
        }

        public override WorldModel GenerateChildWorldModel()
        {
            return new ArrayWorldModel(this);
        }

        public override void Initialize()
        {
            base.Initialize();

            if (this.Parent != null) return;

            System.Array.Clear(this.Properties, 0, this.Properties.Length);
            System.Array.Clear(this.PropertyIsSet, 0, this.PropertyIsSet.Length);
        }


        public override object GetProperty(string propertyName)
        {
            int propertyIndex = this.GetPropertyIndex(propertyName);
            if (propertyIndex >= 0 && this.PropertyIsSet[propertyIndex])
            {
                return this.Properties[propertyIndex];
            }
            else if (this.Parent != null)
            {
                return this.Parent.GetProperty(propertyName);
            }
            else   //we are at the base WorldModel, that corresponds to the charactr's perceptions
            {
                if (propertyIndex >= FixedPropertyCount)
                    return this.GameManager.disposableObjects.ContainsKey(propertyName);

                return propertyIndex switch
                {
                    0 => this.GameManager.Character.baseStats.Mana,
                    1 => this.GameManager.Character.baseStats.MaxMana,
                    2 => this.GameManager.Character.baseStats.XP,
                    3 => this.GameManager.Character.baseStats.MaxHP,
                    4 => this.GameManager.Character.baseStats.HP,
                    5 => this.GameManager.Character.baseStats.ShieldHP,
                    6 => this.GameManager.Character.baseStats.MaxShieldHp,
                    7 => this.GameManager.Character.baseStats.Money,
                    8 => this.GameManager.Character.baseStats.Time,
                    9 => this.GameManager.Character.baseStats.Level,
                    10 => this.GameManager.Character.gameObject.transform.position,
                    11 => 0.0f,
                    12 => this.GameManager.Character.baseStats.Level,
                    13 => this.GameManager.Character.baseStats.Money,
                    _ => this.GameManager.disposableObjects.ContainsKey(propertyName),
                };
            }
        }

        public override void SetProperty(string propertyName, object value)
        {
            int propertyIndex = this.GetPropertyIndex(propertyName);
            if (propertyIndex < 0)
                return;

            this.Properties[propertyIndex] = value;
            this.PropertyIsSet[propertyIndex] = true;
        }

        private int GetPropertyIndex(string propertyName)
        {
            int fixedIndex = propertyName switch
            {
                PropertiesName.MANA => 0,
                PropertiesName.MAXMANA => 1,
                PropertiesName.XP => 2,
                PropertiesName.MAXHP => 3,
                PropertiesName.HP => 4,
                PropertiesName.ShieldHP => 5,
                PropertiesName.MaxShieldHP => 6,
                PropertiesName.MONEY => 7,
                PropertiesName.TIME => 8,
                PropertiesName.LEVEL => 9,
                PropertiesName.POSITION => 10,
                PropertiesName.DURATION => 11,
                PropertiesName.PreviousLEVEL => 12,
                PropertiesName.PreviousMONEY => 13,
                _ => -1,
            };

            if (fixedIndex >= 0)
                return fixedIndex;

            // disposable objects (e.g. "Skelleton0", "Orc3", "Chest1") use the map built from the scene
            return this.DisposableIndices.TryGetValue(propertyName, out int disposableIndex) ? disposableIndex : -1;
        }

        public override void SetGoalValue(string goalName, float goalValue)
        {
            // pass
        }

        public override float GaetGoalInsistence(string goalName)
        {
            return 0.0f;
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

        public override float GetScore()
        {
            int money = (int)this.GetProperty(PropertiesName.MONEY);
            int HP = (int)this.GetProperty(PropertiesName.HP);
            float time = (float)this.GetProperty(PropertiesName.TIME);

            if (HP <= 0 || time >= GameManager.GameConstants.TIME_LIMIT) //lose
                return 0.0f;
            else if (this.NextPlayer == 0 && money == 25 && HP > 0) //win
                return 1.0f;
            else
            { // non-terminal state
                return this.NonTerminalHeuristicScore();
            }
        }

        public override float GetFinalScore()
        {
            int money = (int)this.GetProperty(PropertiesName.MONEY);
            int HP = (int)this.GetProperty(PropertiesName.HP);
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