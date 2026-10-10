using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AI;
using Assets.Scripts.IAJ.Unity.DecisionMaking.ForwardModel;
using Assets.Scripts.IAJ.Unity.DecisionMaking.GOB;
using Assets.Scripts.IAJ.Unity.DecisionMaking.MCTS;
using Assets.Scripts.IAJ.Unity.DecisionMaking.HeroActions;
using Assets.Scripts.Game;
using Assets.Scripts.Game.NPCs;
using Assets.Scripts.IAJ.Unity.Utils;
using System;
using static GameManager;
using Action = Assets.Scripts.IAJ.Unity.DecisionMaking.HeroActions.Action;

public class AutonomousCharacter : NPC
{
    //constants
    public const string SURVIVE_GOAL = "Survive";
    public const string GAIN_LEVEL_GOAL = "GainLevel";
    public const string BE_QUICK_GOAL = "BeQuick";
    public const string GET_RICH_GOAL = "GetRich";

    public const float DECISION_MAKING_INTERVAL = 30.0f;
    public const float RESTING_INTERVAL = 5.0f;
    public const float LEVELING_INTERVAL = 10.0f;
    public const float LAYONHANDS_INTERVAL = 2.0f;
    public const float ENEMY_NEAR_CHECK_INTERVAL = 0.5f;
    public const float ENEMY_DETECTION_RADIUS = 10.0f;
    public const int REST_HP_RECOVERY = 2;
    public const float SPEED = 2.0f;
    // Dying is the worst possible outcome
    public const float DEATH_DISCONTENTMENT = 1000000.0f;



    //UI Variables
    private Text SurviveGoalText;
    private Text GainXPGoalText;
    private Text BeQuickGoalText;
    private Text GetRichGoalText;
    private Text DiscontentmentText;
    private Text TotalProcessingTimeText;
    private Text BestDiscontentmentText;
    private Text ProcessedActionsText;
    private Text BestActionText;
    private Text BestActionSequence;
    private Text DiaryText;

    [Serializable]
    public enum CharacterControlType
    {
        ControlledByPlayer,
        GOB,
        GOAP,
        MCTS,
        MCTS_BiasedPlayout,
        MCTS_LimitedBiasedPlayout   // kept last so the values already saved in the scene don't shift
    }

    public enum MCTSWorldModelType
    {
        DictionaryWorldModel,
        ArrayWorldModel
    }

    [Header("Character Behaviour Algorithm")]
    [Tooltip("Here you choose what algorithm control Sir Uthgard")]
    [SerializeField]
    public CharacterControlType characterControl;
    public MCTSWorldModelType mctsWorldModelType;

    public bool ReactToEnemy;
    [Tooltip("Finding an enemy makes the character replan")]

    [Header("Goal Weights")]
    public float SurviveGoalWeight = 0.0f;
    public float GainLevelGoalWeight = 0.0f;
    public float BeQuickGoalWeight = 0.0f;
    public float GetRichWeight = 0.0f;

    [Header("Goal Initial Insistences")]
    public float GainLevelInitialInsistence = 1;
    public float GetRichInitialInsistence = 25;

    public int MCTS_MaxIterations = 1000;
    public int MCTS_MaxIterationsPerFrame = 500;
    public int MCTS_MaxPlayoutDepth = 100;
    [Tooltip("Playout depth used by MCTS_LimitedBiasedPlayout; the heuristic (GetScore) evaluates the state where the playout stops")]
    public int MCTS_LimitedPlayoutDepth = 5;
    public int MCTS_NumberPlayouts = 1;
    public int GOAP_MaxDepth = DepthLimitedGOAPDecisionMaking.DEFAULT_MAX_DEPTH;
    public int GOAP_ActionCombinationsPerFrame = 2000;

    [Header("Decision Algorithm Options")]

    //[Header("Hero Actions")]
    public bool LevelUp = true;
    public bool GetHealthPotion = true;
    public bool SwordAttack = true;
    public bool GetManaPotion = true;
    public bool ShieldOfFaith = false;
    public bool DivineSmite = true;
    public bool Teleport = false;
    public bool LayOnHands = true;



    public bool ControlledByPlayer { get; private set; }
    public bool GOBActive { get; private set; }
    public bool GOAPActive { get; private set; }
    public bool MCTSActive { get; private set; }
    public bool MCTSBiasedPlayoutActive { get; private set; }


    public Goal BeQuickGoal { get; private set; }
    public Goal SurviveGoal { get; private set; }
    public Goal GetRichGoal { get; private set; }
    public Goal GainLevelGoal { get; private set; }
    public List<Goal> Goals { get; set; }
    public List<Action> Actions { get; set; }
    public Action CurrentAction { get; private set; }
    public GOBDecisionMaking GOBDecisionMaking { get; set; }
    public DepthLimitedGOAPDecisionMaking GOAPDecisionMaking { get; set; }
    public MCTS MCTSDecisionMaking { get; set; }
    public MCTSBiasedPlayout MCTSBiasedDecisionMaking { get; set; }
    public Vector3 InitialPosition { get; set; }

    public GameObject nearEnemy { get; private set; }

    public float StopTime { get; set; }

    //Status fields for timing issues

    public bool Resting { get; set; }
    public bool LevelingUp { get; set; }

    //private fields for internal use only

    private float nextUpdateTime = 0.0f;
    private float lastUpdateTime = 0.0f;
    private float lastEnemyCheckTime = 0.0f;
    private float previousGold = 0.0f;
    private int previousLevel = 1;
    public TextMesh playerText;
    private GameObject closestObject;

    // Draw path settings
    private LineRenderer lineRenderer;


    public void Start()
    {
        //This is the actual speed of the agent
        lineRenderer = this.GetComponent<LineRenderer>();
        playerText.text = "";


        // Initializing UI Text
        this.BeQuickGoalText = GameObject.Find("BeQuickGoal").GetComponent<Text>();
        this.SurviveGoalText = GameObject.Find("SurviveGoal").GetComponent<Text>();
        this.GainXPGoalText = GameObject.Find("GainXP").GetComponent<Text>();
        this.GetRichGoalText = GameObject.Find("GetRichGoal").GetComponent<Text>();
        this.DiscontentmentText = GameObject.Find("Discontentment").GetComponent<Text>();
        this.TotalProcessingTimeText = GameObject.Find("ProcessTime").GetComponent<Text>();
        this.BestDiscontentmentText = GameObject.Find("BestDicont").GetComponent<Text>();
        this.ProcessedActionsText = GameObject.Find("ProcComb").GetComponent<Text>();
        this.BestActionText = GameObject.Find("BestAction").GetComponent<Text>();
        this.BestActionSequence = GameObject.Find("BestActionSequence").GetComponent<Text>();
        DiaryText = GameObject.Find("DiaryText").GetComponent<Text>();

        InitialPosition = this.gameObject.transform.position;
        nearEnemy = null;

        ControlledByPlayer = (characterControl == CharacterControlType.ControlledByPlayer);
        GOBActive = (characterControl == CharacterControlType.GOB);
        GOAPActive = (characterControl == CharacterControlType.GOAP);
        MCTSActive = (characterControl == CharacterControlType.MCTS);
        // the limited variant is the biased playout with a small depth limit
        MCTSBiasedPlayoutActive = (characterControl == CharacterControlType.MCTS_BiasedPlayout
                                   || characterControl == CharacterControlType.MCTS_LimitedBiasedPlayout);

        bool showGoalDiscontentment = !MCTSActive && !MCTSBiasedPlayoutActive;
        Text[] goalDiscontentmentTexts =
        {
            this.SurviveGoalText,
            this.GainXPGoalText,
            this.BeQuickGoalText,
            this.GetRichGoalText,
            this.DiscontentmentText
        };
        foreach (Text goalText in goalDiscontentmentTexts)
        {
            goalText.gameObject.SetActive(showGoalDiscontentment);
            if (!showGoalDiscontentment)
            {
                Image background = goalText.GetComponentInParent<Image>();
                if (background != null) background.enabled = false;
            }
        }


        //initialization of the GOB decision making
        //let's start by creating 4 main goals

        this.SurviveGoal = new Goal(SURVIVE_GOAL, SurviveGoalWeight, 0.0f, this.baseStats.MaxHP);

        this.GainLevelGoal = new Goal(GAIN_LEVEL_GOAL, GainLevelGoalWeight, 1.0f, 4.0f)
        {
            InsistenceValue = GainLevelInitialInsistence, //Initial value
            ChangeRate = 0
        };

        this.GetRichGoal = new Goal(GET_RICH_GOAL, GetRichWeight, 0.0f, 25.0f)
        {
            InsistenceValue = GetRichInitialInsistence, //Initial value
            ChangeRate = 0
        };

        this.BeQuickGoal = new Goal(BE_QUICK_GOAL, BeQuickGoalWeight, 0.0f, (float)GameManager.GameConstants.TIME_LIMIT)
        {
            ChangeRate = 1
        };

        this.Goals = new List<Goal>
        {
            this.SurviveGoal,
            this.BeQuickGoal,
            this.GetRichGoal,
            this.GainLevelGoal
        };

        //initialize the available actions
        //Uncomment commented actions after you implement them

        this.Actions = new List<Action>();

        //First it is necessary to add the actions made available by the elements in the scene
        //Some actions might be off due to not yet being implemented, or for
        //debuging purposes
        foreach (var enemy in GameObject.FindGameObjectsWithTag("Skeleton"))
        {
            if (SwordAttack) { this.Actions.Add(new SwordAttack(this, enemy)); };

            if (DivineSmite) this.Actions.Add(new DivineSmite(this, enemy));
        }

        foreach (var enemy in GameObject.FindGameObjectsWithTag("Orc"))
        {
            if (SwordAttack) { this.Actions.Add(new SwordAttack(this, enemy)); };
        }

        foreach (var enemy in GameObject.FindGameObjectsWithTag("Dragon"))
        {
            if (SwordAttack) { this.Actions.Add(new SwordAttack(this, enemy)); };
        }

        foreach (var chest in GameObject.FindGameObjectsWithTag("Chest"))
        {
            //This action always needs to be active for the game to end
            this.Actions.Add(new PickUpChest(this, chest));
        }

        foreach (var potion in GameObject.FindGameObjectsWithTag("HealthPotion"))
        {
            if (GetHealthPotion) this.Actions.Add(new GetHealthPotion(this, potion));
        }

        foreach (var potion in GameObject.FindGameObjectsWithTag("ManaPotion"))
        {
            if (GetManaPotion) this.Actions.Add(new GetManaPotion(this, potion));
        }

        //Then we have a series of extra actions available to Sir Uthgard
        if (ShieldOfFaith) this.Actions.Add(new ShieldOfFaith(this));
        if (LevelUp) this.Actions.Add(new LevelUp(this));
        if (LayOnHands) this.Actions.Add(new LayOnHands(this));
        if (Teleport) this.Actions.Add(new Teleport(this));
        /*if (Rest) this.Actions.Add(new Rest(this)); rest doesn't seem that it should exist */


        // Initialization of Decision Making Algorithms
        if (!this.ControlledByPlayer)
        {
            if (this.GOBActive) this.GOBDecisionMaking = new GOBDecisionMaking(this.Actions, this.Goals);
            else if (this.GOAPActive)
            {
                // the WorldModel is necessary for the GOAP and MCTS algorithms that need to predict action effects on the world...
                var worldModel = new DictionaryWorldModel(GameManager.Instance, this, this.Actions, this.Goals);
                //var WorldModelImproved = new CurrentStateWorldModelImproved(GameManager.Instance, this.Actions, this.Goals);
                this.GOAPDecisionMaking = new DepthLimitedGOAPDecisionMaking(worldModel, this, GOAP_MaxDepth);
            }
            else if (this.MCTSActive)
            {
                WorldModel worldModel = this.mctsWorldModelType == MCTSWorldModelType.ArrayWorldModel
                    ? new ArrayWorldModel(GameManager.Instance, this, this.Actions)
                    : new DictionaryWorldModel(GameManager.Instance, this, this.Actions, this.Goals);
                this.MCTSDecisionMaking = new MCTS(worldModel, MCTS_MaxIterations, MCTS_MaxIterationsPerFrame, MCTS_NumberPlayouts, MCTS_MaxPlayoutDepth);
            }
            else if (this.MCTSBiasedPlayoutActive)
            {
                WorldModel worldModel = this.mctsWorldModelType == MCTSWorldModelType.ArrayWorldModel
                    ? new ArrayWorldModel(GameManager.Instance, this, this.Actions)
                    : new DictionaryWorldModel(GameManager.Instance, this, this.Actions, this.Goals);
                int playoutDepth = this.characterControl == CharacterControlType.MCTS_LimitedBiasedPlayout
                    ? MCTS_LimitedPlayoutDepth
                    : MCTS_MaxPlayoutDepth;
                this.MCTSBiasedDecisionMaking = new MCTSBiasedPlayout(worldModel, MCTS_MaxIterations, MCTS_MaxIterationsPerFrame, MCTS_NumberPlayouts, playoutDepth);
            }
        }

        string intro = "My Diary \n I awoke. What a wonderful day to kill Monsters! \n";
        FullDiary.Append(intro);
        DiaryText.text += intro;
    }

    void FixedUpdate()
    {
        if (GameManager.Instance.gameEnded) return;

        //Agent Perception
        if (Time.time > this.lastEnemyCheckTime + ENEMY_NEAR_CHECK_INTERVAL)
        {
            GameObject enemy = CheckEnemies(ENEMY_DETECTION_RADIUS);
            if (enemy != null)
            {
                if (ReactToEnemy) GameManager.Instance.WorldChanged = true;
                AddToDiary(" There is " + enemy.name + " in front of me!");
                this.nearEnemy = enemy;
            }
            else
            {
                this.nearEnemy = null;
            }
            this.lastEnemyCheckTime = Time.time;
        }

        if (Time.time > this.nextUpdateTime || GameManager.Instance.WorldChanged)
        {
            GameManager.Instance.WorldChanged = false;
            this.nextUpdateTime = Time.time + DECISION_MAKING_INTERVAL;

            //first step, perceptions
            //update the agent's goals based on the state of the world
            UpdateGoalsInsistence();

            //Write in the interface
            this.SurviveGoalText.text = "Survive: " + this.SurviveGoal.NormalizedInsistenceValue + " (" + this.SurviveGoal.Weight + ")";
            this.GainXPGoalText.text = "Gain Level: " + this.GainLevelGoal.NormalizedInsistenceValue.ToString("F1") + " (" + this.GainLevelGoal.Weight + ")";
            this.BeQuickGoalText.text = "Be Quick: " + this.BeQuickGoal.NormalizedInsistenceValue.ToString("F1") + " (" + this.BeQuickGoal.Weight + ")";
            this.GetRichGoalText.text = "GetRich: " + this.GetRichGoal.NormalizedInsistenceValue.ToString("F1") + " (" + this.GetRichGoal.Weight + ")";
            this.DiscontentmentText.text = "Discontentment: " + this.CalculateDiscontentment().ToString("F1");

            this.lastUpdateTime = Time.time;

            //To have a new decision lets initialize Decision Making Proccess
            this.CurrentAction = null;
            if (GOBActive)
            {
                this.GOBDecisionMaking.InProgress = true;
            }
            else if (GOAPActive)
            {
                // read every decision so the value can be tuned in the Inspector during Play mode
                this.GOAPDecisionMaking.ActionCombinationsProcessedPerFrame = Mathf.Max(1, GOAP_ActionCombinationsPerFrame);
                this.GOAPDecisionMaking.InitializeDecisionMakingProcess();
                this.goapDecisionFrames = 0;
                this.goapRootExecutableActions = this.GOAPDecisionMaking.InitialWorldModel.GetExecutableActions().Length;
            }
            else if (MCTSActive)
            {
                this.MCTSDecisionMaking.InitializeMCTSearch();
                this.mctsDecisionFrames = 0;
            }
            else if (MCTSBiasedPlayoutActive)
            {
                this.MCTSBiasedDecisionMaking.InitializeMCTSearch();
                this.mctsDecisionFrames = 0;
            }
        }

        if (this.ControlledByPlayer)
        {
            //Using the old Input System
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
                this.transform.position += new Vector3(0.0f, 0.0f, 0.1f) * SPEED;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
                this.transform.position += new Vector3(0.0f, 0.0f, -0.1f) * SPEED;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
                this.transform.position += new Vector3(-0.1f, 0.0f, 0.0f) * SPEED;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
                this.transform.position += new Vector3(0.1f, 0.0f, 0.0f) * SPEED;
            if (Input.GetKey(KeyCode.F))
                if (closestObject != null)
                {
                    //Simple way of checking which object is closest to Sir Uthgard
                    var s = playerText.text.ToString();
                    if (s.Contains("Health Potion"))
                        PickUpHealthPotion();
                    else if (s.Contains("Mana Potion"))
                        PickUpManaPotion();
                    else if (s.Contains("Chest"))
                        PickUpChest();
                    else if (s.Contains("Enemy"))
                        AttackEnemy();
                }
            if (Input.GetKey(KeyCode.L) || this.LevelingUp)
            {
                LevelUpAction();
            }
        }

        else if (this.GOAPActive)
        {
            this.UpdateDLGOAP();
        }
        else if (this.GOBActive)
        {
            this.UpdateGOB();
        }
        else if (this.MCTSActive)
        {
            this.UpdateMCTS(this.MCTSDecisionMaking);
        }
        else if (this.MCTSBiasedPlayoutActive)
        {
            this.UpdateMCTS(this.MCTSBiasedDecisionMaking);
        }

        if (this.CurrentAction != null)
        {
            if (this.CurrentAction.CanExecute())
            {
                this.CurrentAction.Execute();
            }
        }

        if (navMeshAgent.hasPath)
        {
            DrawPath();
        }


    }

    private void UpdateGoalsInsistence()
    {
        var duration = Time.time - this.lastUpdateTime;
        // Max Health minus current Health
        this.SurviveGoal.Max = baseStats.MaxHP;
        this.SurviveGoal.InsistenceValue = (float)(baseStats.MaxHP - baseStats.HP) / baseStats.MaxHP * 17.5f;

        // How much time has passed...
        this.BeQuickGoal.InsistenceValue += this.BeQuickGoal.ChangeRate * duration;

        // This can go up as time passes, and then go down when the character gains a level
        this.GainLevelGoal.InsistenceValue += this.GainLevelGoal.ChangeRate * duration; //increase in goal over time
        if (baseStats.Level > this.previousLevel)
        {
            this.GainLevelGoal.InsistenceValue -= (baseStats.Level - this.previousLevel);
            this.previousLevel = baseStats.Level;
        }

        // This can start high or go up as time passes, but is reduced by how much treasure the character already got
        this.GetRichGoal.InsistenceValue += this.GetRichGoal.ChangeRate * duration;
        var goldDiff = baseStats.Money - this.previousGold;
        if (goldDiff > 0)
        {
            this.GetRichGoal.InsistenceValue -= goldDiff;
            this.previousGold = baseStats.Money;
        }

        // Normalize goals to 0-10
        foreach (Goal goal in Goals) { goal.NormalizeGoalValue(goal.InsistenceValue, goal.Min, goal.Max); }
    }

    public void UpdateGoalsInsistence(WorldModel wM)
    {
        float duration = (float) wM.GetProperty(PropertiesName.DURATION);

        // Max Health minus current Health
        var surviveValue = (int)wM.GetProperty(PropertiesName.MAXHP) - (int)wM.GetProperty(PropertiesName.HP);
        wM.SetGoalValue(SURVIVE_GOAL, surviveValue);

        // How much time has passed...
        float beQuickValue = (float) wM.GaetGoalInsistence(BE_QUICK_GOAL) + this.BeQuickGoal.ChangeRate * duration;
        wM.SetGoalValue(BE_QUICK_GOAL, beQuickValue);

        var gainLevelValue = wM.GaetGoalInsistence(GAIN_LEVEL_GOAL) + this.GainLevelGoal.ChangeRate * duration;
        if ( (int)wM.GetProperty(PropertiesName.LEVEL) > (int)wM.GetProperty(PropertiesName.PreviousLEVEL) )
        {
            wM.SetGoalValue(GAIN_LEVEL_GOAL, gainLevelValue - 1);
            wM.SetProperty(PropertiesName.PreviousLEVEL, wM.GetProperty(PropertiesName.LEVEL));
        }

        // This can start high or go up as time passes, but is reduced by how much treasure the character already got
        var richGoalValue = wM.GaetGoalInsistence(GET_RICH_GOAL) + this.GetRichGoal.ChangeRate * duration;
        var goldDiff = (int)wM.GetProperty(PropertiesName.MONEY) - (int)wM.GetProperty(PropertiesName.PreviousMONEY);
        if (goldDiff > 0)
        {
            wM.SetGoalValue(GET_RICH_GOAL, richGoalValue - goldDiff);
            wM.SetProperty(PropertiesName.PreviousMONEY, wM.GetProperty(PropertiesName.MONEY));
        }
        else
        {
            wM.SetGoalValue(GET_RICH_GOAL, richGoalValue);
        }
    }

    public float CalculateDiscontentment()
    {
        var discontentment = 0.0f;

        foreach (var goal in this.Goals)
        {
            discontentment += goal.GetDiscontentment();
        }
        return discontentment;
    }

    public float CalculateDiscontentment(WorldModel worldModel)
    {
        if (worldModel.IsWinningState())
        {
            return 0.0f;
        }

        // dying (or running out of time) is the worst thing that can happen
        if (worldModel.IsLosingState())
        {
            return DEATH_DISCONTENTMENT;
        }

        var discontentment = 0.0f;

        foreach (var goal in this.Goals)
        {
            var max = (goal.Name == SURVIVE_GOAL) ? (int)worldModel.GetProperty(PropertiesName.MAXHP) : goal.Max;
            var normalizedValue = goal.NormalizeGoalValue( worldModel.GaetGoalInsistence(goal.Name), goal.Min, max);
            discontentment += goal.GetDiscontentment(normalizedValue);
        }
        return discontentment;
    }

    // Normalize different goal values to 0-10 ranges according to their max


    private GameObject CheckEnemies(float detectionRadius)
    {
        foreach (var enemy in GameManager.Instance.enemies)
        {
            Transform characterTransform = this.navMeshAgent.GetComponent<Transform>();
            Vector3 enemyDirection = enemy.GetComponent<Transform>().position - characterTransform.position;
            Vector3 characterDirection = navMeshAgent.velocity.normalized;
            //actually it checks if the enemy is in front of the character... it returns the first one...
            if (enemyDirection.sqrMagnitude < detectionRadius*detectionRadius &&
                Mathf.Cos(MathHelper.ConvertVectorToOrientation(characterDirection) - MathHelper.ConvertVectorToOrientation(enemyDirection.normalized)) > 0)
            {
                return enemy;
            }
        }
        return null;
    }

    public void AddToDiary(string s)
    {
        string entry = Time.time + s + "\n";
        FullDiary.Append(entry);
        DiaryText.text += entry;

        if (DiaryText.text.Length > 1000)
            DiaryText.text = DiaryText.text.Substring(500);
    }

    // The UI diary is truncated to its last ~1000 characters, so the full history is kept here
    private readonly System.Text.StringBuilder FullDiary = new System.Text.StringBuilder();

    private bool reportPrinted;

    // print data to the console after the game ends
    private void OnApplicationQuit()
    {
        if (!this.reportPrinted) PrintDiaryToConsole();
    }

    public void PrintDiaryToConsole()
    {
        this.reportPrinted = true;
        // a single log, so everything can be copied at once
        var report = "===== FULL DIARY =====\n" + FullDiary + "\n" + BuildRunSummary();
        Debug.Log(report);
        AppendRunResultCsv();
    }

    private string GetResult()
    {
        if (baseStats.HP <= 0) return "LOST (died)";
        if (baseStats.Time >= GameConstants.TIME_LIMIT) return "LOST (time limit)";
        if (baseStats.Money >= 25) return "WON";
        return "game not finished";
    }

    private int GetMCTSPlayoutDepth()
    {
        return this.characterControl == CharacterControlType.MCTS_LimitedBiasedPlayout ? MCTS_LimitedPlayoutDepth : MCTS_MaxPlayoutDepth;
    }

    // End of game summary for every algorithm: result, settings and search statistics (used for the algorithms comparison)
    private string BuildRunSummary()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("===== " + this.characterControl + " SUMMARY =====");
        sb.AppendLine("Result: " + GetResult());
        sb.AppendLine("Final state: HP " + baseStats.HP + "/" + baseStats.MaxHP
            + " | Money " + baseStats.Money
            + " | XP " + baseStats.XP
            + " | Level " + baseStats.Level
            + " | Mana " + baseStats.Mana + "/" + baseStats.MaxMana
            + " | Time " + baseStats.Time.ToString("F1"));

        sb.AppendLine("-- Settings --");
        sb.AppendLine("Monsters: " + GameManager.Instance.monsterControl + " | Stochastic world: " + GameManager.Instance.StochasticWorld
            + " | React to enemy: " + this.ReactToEnemy);

        if (this.GOBActive || this.GOAPActive)
        {
            sb.Append("Goal weights:");
            foreach (var goal in this.Goals)
            {
                sb.Append(" " + goal.Name + " " + goal.Weight);
            }
            sb.AppendLine();
        }

        if (this.GOAPActive) sb.Append(BuildGOAPSummary());
        else if (this.MCTSActive || this.MCTSBiasedPlayoutActive) sb.Append(BuildMCTSSummary());
        else if (this.GOBActive) sb.Append(BuildGOBSummary());

        return sb.ToString();
    }

    // one line per game, so the runs of each algorithm (win rate, times...) can be compared in a spreadsheet
    private void AppendRunResultCsv()
    {
        if (this.ControlledByPlayer) return;

        int decisions;
        float totalTime;
        if (this.GOAPActive) { decisions = this.goapDecisionCount; totalTime = this.goapTotalProcessingTime; }
        else if (this.GOBActive) { decisions = this.gobDecisionCount; totalTime = this.gobTotalProcessingTime; }
        else { decisions = this.mctsDecisionCount; totalTime = this.mctsTotalProcessingTime; }

        bool isMCTS = this.MCTSActive || this.MCTSBiasedPlayoutActive;
        var inv = System.Globalization.CultureInfo.InvariantCulture;
        var line = string.Join(";",
            DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            this.characterControl,
            isMCTS ? this.mctsWorldModelType.ToString() : "-",
            isMCTS ? MCTS_MaxIterations.ToString() : "-",
            isMCTS ? GetMCTSPlayoutDepth().ToString() : (this.GOAPActive ? GOAP_MaxDepth.ToString() : "-"),
            GameManager.Instance.monsterControl,
            GameManager.Instance.StochasticWorld,
            this.ReactToEnemy,
            GetResult(),
            baseStats.Money,
            baseStats.HP,
            baseStats.Time.ToString("F1", inv),
            decisions,
            (decisions > 0 ? totalTime / decisions : 0f).ToString("F4", inv));

        try
        {
            var path = System.IO.Path.Combine(Application.dataPath, "..", "RunResults.csv");
            if (!System.IO.File.Exists(path))
            {
                System.IO.File.AppendAllText(path, "Date;Algorithm;WorldModel;MaxIterations;Depth;Monsters;Stochastic;ReactToEnemy;Result;Money;HP;GameTime;Decisions;AvgDecisionTime\n");
            }
            System.IO.File.AppendAllText(path, line + "\n");
        }
        catch (Exception e)
        {
            Debug.LogWarning("Could not write RunResults.csv: " + e.Message);
        }
    }

    // MCTS statistics (MCTS, Biased and Limited), accumulated after each completed decision
    private int mctsDecisionFrames;
    private int mctsDecisionCount;
    private long mctsTotalIterations;
    private int mctsMinIterations = int.MaxValue;
    private int mctsMaxIterations;
    private float mctsTotalProcessingTime;
    private float mctsMaxProcessingTime;
    private long mctsTotalFrames;
    private int mctsMaxFrames;
    private int mctsMaxPlayoutDepth;
    private int mctsMaxSelectionDepth;

    private void RecordMCTSDecision(MCTS mCTS)
    {
        this.mctsDecisionCount++;
        this.mctsTotalIterations += mCTS.CurrentIterations;
        this.mctsMinIterations = Mathf.Min(this.mctsMinIterations, mCTS.CurrentIterations);
        this.mctsMaxIterations = Mathf.Max(this.mctsMaxIterations, mCTS.CurrentIterations);
        this.mctsTotalProcessingTime += mCTS.TotalProcessingTime;
        this.mctsMaxProcessingTime = Mathf.Max(this.mctsMaxProcessingTime, mCTS.TotalProcessingTime);
        this.mctsTotalFrames += this.mctsDecisionFrames;
        this.mctsMaxFrames = Mathf.Max(this.mctsMaxFrames, this.mctsDecisionFrames);
        this.mctsMaxPlayoutDepth = Mathf.Max(this.mctsMaxPlayoutDepth, mCTS.MaxPlayoutDepthReached);
        this.mctsMaxSelectionDepth = Mathf.Max(this.mctsMaxSelectionDepth, mCTS.MaxSelectionDepthReached);
    }

    private string BuildMCTSSummary()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("World model: " + this.mctsWorldModelType + " | Max iterations: " + MCTS_MaxIterations
            + " | Iterations per frame: " + MCTS_MaxIterationsPerFrame + " | Playouts per node: " + MCTS_NumberPlayouts
            + " | Playout depth limit: " + GetMCTSPlayoutDepth());

        sb.AppendLine("-- Search statistics --");
        if (this.mctsDecisionCount == 0)
        {
            sb.Append("No MCTS decision was completed.");
            return sb.ToString();
        }

        sb.AppendLine("Decisions completed: " + this.mctsDecisionCount);
        sb.AppendLine("Iterations per decision: average " + (this.mctsTotalIterations / this.mctsDecisionCount)
            + " | min " + this.mctsMinIterations
            + " | max " + this.mctsMaxIterations);
        sb.AppendLine("Processing time per decision: average " + (this.mctsTotalProcessingTime / this.mctsDecisionCount).ToString("F4") + "s"
            + " | max " + this.mctsMaxProcessingTime.ToString("F4") + "s"
            + " | total " + this.mctsTotalProcessingTime.ToString("F3") + "s");
        sb.AppendLine("Frames per decision: average " + ((float)this.mctsTotalFrames / this.mctsDecisionCount).ToString("F1")
            + " | max " + this.mctsMaxFrames);
        sb.Append("Max selection depth: " + this.mctsMaxSelectionDepth + " | Max playout depth: " + this.mctsMaxPlayoutDepth);

        return sb.ToString();
    }

    // GOB statistics: GOB decides in a single frame, so only the number of decisions and the time are relevant
    private int gobDecisionCount;
    private float gobTotalProcessingTime;
    private float gobMaxProcessingTime;

    private void RecordGOBDecision(float processingTime)
    {
        this.gobDecisionCount++;
        this.gobTotalProcessingTime += processingTime;
        this.gobMaxProcessingTime = Mathf.Max(this.gobMaxProcessingTime, processingTime);
    }

    private string BuildGOBSummary()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("-- Search statistics --");
        if (this.gobDecisionCount == 0)
        {
            sb.Append("No GOB decision was completed.");
            return sb.ToString();
        }

        sb.AppendLine("Decisions completed: " + this.gobDecisionCount);
        sb.Append("Processing time per decision: average " + (this.gobTotalProcessingTime / this.gobDecisionCount).ToString("F6") + "s"
            + " | max " + this.gobMaxProcessingTime.ToString("F6") + "s"
            + " | total " + this.gobTotalProcessingTime.ToString("F4") + "s");

        return sb.ToString();
    }

    // GOAP statistics, accumulated after each completed decision and printed once at the end of the game
    private int goapDecisionFrames;
    private int goapRootExecutableActions;
    private int goapDecisionCount;
    private long goapTotalCombinations;
    private int goapMinCombinations = int.MaxValue;
    private int goapMaxCombinations;
    private float goapTotalProcessingTime;
    private float goapMaxProcessingTime;
    private long goapTotalFrames;
    private int goapMaxFrames;
    private long goapTotalRootActions;
    private int goapMaxRootActions;

    private void RecordGOAPDecision()
    {
        var combinations = this.GOAPDecisionMaking.TotalActionCombinationsProcessed;
        var processingTime = this.GOAPDecisionMaking.TotalProcessingTime;

        this.goapDecisionCount++;
        this.goapTotalCombinations += combinations;
        this.goapMinCombinations = Mathf.Min(this.goapMinCombinations, combinations);
        this.goapMaxCombinations = Mathf.Max(this.goapMaxCombinations, combinations);
        this.goapTotalProcessingTime += processingTime;
        this.goapMaxProcessingTime = Mathf.Max(this.goapMaxProcessingTime, processingTime);
        this.goapTotalFrames += this.goapDecisionFrames;
        this.goapMaxFrames = Mathf.Max(this.goapMaxFrames, this.goapDecisionFrames);
        this.goapTotalRootActions += this.goapRootExecutableActions;
        this.goapMaxRootActions = Mathf.Max(this.goapMaxRootActions, this.goapRootExecutableActions);
    }

    private string BuildGOAPSummary()
    {
        var goap = this.GOAPDecisionMaking;
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Max depth: " + goap.MaxDepth + " | Combinations per frame: " + goap.ActionCombinationsProcessedPerFrame);

        sb.AppendLine("-- Search statistics --");
        if (this.goapDecisionCount == 0)
        {
            sb.Append("No GOAP decision was completed.");
            return sb.ToString();
        }

        float avgRootActions = (float)this.goapTotalRootActions / this.goapDecisionCount;
        sb.AppendLine("Decisions completed: " + this.goapDecisionCount);
        sb.AppendLine("Executable actions at root (N): average " + avgRootActions.ToString("F1") + " | max " + this.goapMaxRootActions
            + " | N^depth with average N: " + Mathf.Pow(avgRootActions, goap.MaxDepth).ToString("F0"));
        sb.AppendLine("Action combinations processed: average " + (this.goapTotalCombinations / this.goapDecisionCount)
            + " | min " + this.goapMinCombinations
            + " | max " + this.goapMaxCombinations
            + " | total " + this.goapTotalCombinations);
        sb.AppendLine("Processing time per decision: average " + (this.goapTotalProcessingTime / this.goapDecisionCount).ToString("F4") + "s"
            + " | max " + this.goapMaxProcessingTime.ToString("F4") + "s"
            + " | total " + this.goapTotalProcessingTime.ToString("F3") + "s");
        sb.Append("Frames per decision: average " + ((float)this.goapTotalFrames / this.goapDecisionCount).ToString("F1")
            + " | max " + this.goapMaxFrames);

        return sb.ToString();
    }


    private void UpdateGOB()
    {

        bool newDecision = false;
        if (this.GOBDecisionMaking.InProgress)
        {
            //choose an action using the GOB Decision Making process
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var action = this.GOBDecisionMaking.ChooseAction(this);
            stopwatch.Stop();
            if (!this.GOBDecisionMaking.InProgress) RecordGOBDecision((float)stopwatch.Elapsed.TotalSeconds);
            if (action != null && action != this.CurrentAction)
            {
                this.BeQuickGoal.InsistenceValue += action.GetDuration() * this.BeQuickGoal.ChangeRate;
                this.CurrentAction = action;
                newDecision = true;
                if (newDecision)
                {
                    var bestDiscont = this.GOBDecisionMaking.ActionDiscontentment[action];
                    Action secondBestAction = this.GOBDecisionMaking.secondBestAction;
                    var secondBestDiscont = secondBestAction != null ? this.GOBDecisionMaking.ActionDiscontentment[secondBestAction] : 0.0f;
                    Action thirdBestAction = this.GOBDecisionMaking.thirdBestAction;
                    var thirdBestDiscont = thirdBestAction != null ? this.GOBDecisionMaking.ActionDiscontentment[thirdBestAction] : 0.0f;
                    AddToDiary(" I decided to " + action.Name);
                    this.BestActionText.text = "Best Action:\n " + action.Name + ":" + bestDiscont.ToString("F2") + "\n";
                    this.BestActionSequence.text = " Second Best:\n" + (secondBestAction != null ? secondBestAction.Name : "null") + ":" + secondBestDiscont.ToString("F2") + "\n"
                        + " Third Best:\n" + (thirdBestAction != null ? thirdBestAction.Name : "null") + ":" + thirdBestDiscont.ToString("F2") + "\n";
                }
            }
        }
    }

    private void UpdateDLGOAP()
    {
        bool newDecision = false;
        if (this.GOAPDecisionMaking.InProgress)
        {
            //choose an action using the GOB Decision Making process
            var action = this.GOAPDecisionMaking.ChooseAction();
            this.goapDecisionFrames++;
            if (action != null && action != this.CurrentAction)
            {
                this.CurrentAction = action;
                newDecision = true;
            }

            // the search just finished in this frame
            if (!this.GOAPDecisionMaking.InProgress) RecordGOAPDecision();
        }

        this.TotalProcessingTimeText.text = "Process. Time: " + this.GOAPDecisionMaking.TotalProcessingTime.ToString("F");
        this.BestDiscontentmentText.text = "Best Discontentment: " + this.GOAPDecisionMaking.BestDiscontentmentValue.ToString("F");
        this.ProcessedActionsText.text = "Act. comb. processed: " + this.GOAPDecisionMaking.TotalActionCombinationsProcessed;

        if (this.GOAPDecisionMaking.BestAction != null)
        {
            if (newDecision)
            {
                AddToDiary(" I decided to " + GOAPDecisionMaking.BestAction.Name);
            }
            var actionText = "";
            foreach (var action in this.GOAPDecisionMaking.BestActionSequence)
            {
                if (action != null) actionText += "\n" + action.Name;
            }
            this.BestActionSequence.text = "Best Action Sequence: " + actionText;
            //this.BestActionText.text = "Best Action: " + GOAPDecisionMaking.BestAction.Name;
            //What is the predicted state of the world?
            var endState = GOAPDecisionMaking.BestActionSequenceEndState;
            var text = "";
            if (endState != null)
            {
                text += "Predicted World State:\n";
                text += "My Level:" + endState.GetProperty(PropertiesName.LEVEL) + "\n";
                text += "My HP:" + endState.GetProperty(PropertiesName.HP) + "\n";
                text += "My Money:" + endState.GetProperty(PropertiesName.MONEY) + "\n";
                text += "Time Passsed:" + endState.GetProperty(PropertiesName.TIME) + "\n";
                this.BestActionText.text = text;
                if ((int)endState.GetProperty(PropertiesName.HP) < 0 ||
                    (float)endState.GetProperty(PropertiesName.TIME) > GameConstants.TIME_LIMIT)
                {
                    //stop here
                }
            }
            else this.BestActionText.text = "No EndState was found";
        }
        else
        {
            this.BestActionSequence.text = "Best Action Sequence:\nNone";
            this.BestActionText.text = "Best Action: \n Node";
        }
    }

    private void UpdateMCTS(MCTS mCTS)
    {
        if (mCTS.InProgress)
        {
            var action = mCTS.ChooseAction();
            this.mctsDecisionFrames++;
            // the search just finished in this frame
            if (!mCTS.InProgress) RecordMCTSDecision(mCTS);
            if (action != null)
            {
                this.CurrentAction = action;
                AddToDiary(" I decided to " + action.Name);
            }
        }
        //Statistical and Debug Data
        this.TotalProcessingTimeText.text = "Process. Time: " + mCTS.TotalProcessingTime.ToString("F");

        this.ProcessedActionsText.text = "Iterations: "
            + mCTS.CurrentIterations.ToString()
            + "\n Max Sel Depth: "
            + mCTS.MaxSelectionDepthReached.ToString()
            + "\n Max Playout Depth: "
            + mCTS.MaxPlayoutDepthReached.ToString();

        if (mCTS.BestFirstChild != null)
        {
            var q = mCTS.BestFirstChild.Q / mCTS.BestFirstChild.N;
            this.BestDiscontentmentText.text = "Best Exp. Q value: " + q.ToString("F05");
            var actionText = "";

            foreach (var node in mCTS.BestSequence)
            {
                actionText += "\n" + node.Action.Name + " (" + node.Q + "/" + node.N + ")";
            }
            this.BestActionSequence.text = "Best Action Sequence: " + actionText;

            //What is the predicted state of the world?
            var endState = mCTS.BestActionSequenceEndState;
            var text = "";
            if (endState != null)
            {
                text += "Predicted World State:\n";
                text += "My Level:" + endState.GetProperty(PropertiesName.LEVEL) + "\n";
                text += "My HP:" + endState.GetProperty(PropertiesName.HP) + "\n";
                text += "My Money:" + endState.GetProperty(PropertiesName.MONEY) + "\n";
                text += "Time Passed:" + endState.GetProperty(PropertiesName.TIME) + "\n";
                this.BestActionText.text = text;
                if ((int)endState.GetProperty(PropertiesName.HP) < 0 ||
                    (float)endState.GetProperty(PropertiesName.TIME) > GameConstants.TIME_LIMIT)
                {
                    //stop here
                }
            }
            else this.BestActionText.text = "No EndState was found";
        }
        else
        {
            this.BestActionSequence.text = "Best Action Sequence:\nNone";
            this.BestActionText.text = "";
        }


    }



    void DrawPath()
    {

        lineRenderer.positionCount = navMeshAgent.path.corners.Length;
        lineRenderer.SetPosition(0, this.transform.position);

        if (navMeshAgent.path.corners.Length < 2)
        {
            return;
        }

        for (int i = 1; i < navMeshAgent.path.corners.Length; i++)
        {
            Vector3 pointPosition = new Vector3(navMeshAgent.path.corners[i].x, navMeshAgent.path.corners[i].y, navMeshAgent.path.corners[i].z);
            lineRenderer.SetPosition(i, pointPosition);
        }

    }



    //Player Controlled Character Stuff - Do not change

    //Functions designed for when the Player has control of the character
    void OnTriggerEnter(Collider col)
    {
        if (this.ControlledByPlayer)
        {
            if (col.gameObject.tag.ToString().Contains("HealthPotion"))
            {
                playerText.text = "Pickup Health Potion";
                closestObject = col.gameObject;
            }
            else if (col.gameObject.tag.ToString().Contains("ManaPotion"))
            {
                playerText.text = "Pickup Mana Potion";
                closestObject = col.gameObject;
            }
            else if (col.gameObject.tag.ToString().Contains("Chest"))
            {
                playerText.text = "Pickup Chest";
                closestObject = col.gameObject;
            }
            else if (col.gameObject.tag.ToString().Contains("Orc") || col.gameObject.tag.ToString().Contains("Skeleton"))
            {
                playerText.text = "Attack Enemy";
                closestObject = col.gameObject;
            }
        }
    }

    void OnTriggerExit(Collider col)
    {
        if (col.gameObject.tag.ToString() != "")
            playerText.text = "";
    }


    //Actions designed for when the Player has control of the character
    void PickUpHealthPotion()
    {
        if (closestObject != null)
            if (GameManager.Instance.InPotionRange(closestObject))
            {
                GameManager.Instance.GetHealthPotion(closestObject);
                closestObject = null;
                playerText.text = "";
            }
    }

    void PickUpManaPotion()
    {
        if (closestObject != null)
            if (GameManager.Instance.InPotionRange(closestObject))
            {
                GameManager.Instance.GetManaPotion(closestObject);
                closestObject = null;
                playerText.text = "";
            }
    }


    void PickUpChest()
    {
        if (closestObject != null)
            if (GameManager.Instance.InChestRange(closestObject))
            {
                GameManager.Instance.PickUpChest(closestObject);
                closestObject = null;
                playerText.text = "";
            }
    }

    void AttackEnemy()
    {
        if (closestObject != null)
            if (GameManager.Instance.InMeleeRange(closestObject))
            {
                GameManager.Instance.SwordAttack(closestObject);
                closestObject = null;
                playerText.text = "";
            }
    }

    void LevelUpAction()
    {
        GameManager.Instance.LevelUp();
    }
}
