using Assets.Scripts.IAJ.Unity.DecisionMaking.ForwardModel;
using UnityEngine;

namespace Assets.Scripts.IAJ.Unity.DecisionMaking.HeroActions
{
    public class LayOnHands : Action
    {

        internal const int MANA_COST = 7;
        internal const int LEVEL_REQUIREMENT = 2;
        public AutonomousCharacter Character { get; private set; }

        public LayOnHands(AutonomousCharacter character) : base("LayOnHands")
        {
            this.Character = character;
            this.Duration = AutonomousCharacter.LAYONHANDS_INTERVAL;
        }

        public override bool CanExecute()
        {
            return Character.baseStats.Mana >= MANA_COST && Character.baseStats.HP < Character.baseStats.MaxHP
            && Character.baseStats.Level >= LEVEL_REQUIREMENT;
        }

        public override bool CanExecute(WorldModel worldModel)
        {
            int mana = (int)worldModel.GetProperty(PropertiesName.MANA);
            int hp = (int)worldModel.GetProperty(PropertiesName.HP);
            int maxHP = (int)worldModel.GetProperty(PropertiesName.MAXHP);
            int level = (int)worldModel.GetProperty(PropertiesName.LEVEL);

            return mana >= MANA_COST && hp < maxHP && level >= LEVEL_REQUIREMENT;
        }

        public override void Execute()
        {
            GameManager.Instance.LayOnHands();
        }

        public override void ApplyActionEffects(WorldModel worldModel)
        {
            int mana = (int)worldModel.GetProperty(PropertiesName.MANA);
            int maxHP = (int)worldModel.GetProperty(PropertiesName.MAXHP);
            float time = (float)worldModel.GetProperty(PropertiesName.TIME);

            // the hero stands still for LAYONHANDS_INTERVAL while healing (see GameManager.LayOnHands)
            worldModel.SetProperty(PropertiesName.DURATION, AutonomousCharacter.LAYONHANDS_INTERVAL);
            worldModel.SetProperty(PropertiesName.TIME, time + AutonomousCharacter.LAYONHANDS_INTERVAL);
            worldModel.SetProperty(PropertiesName.MANA, mana - MANA_COST);

            worldModel.SetProperty(PropertiesName.HP, maxHP);
        }

        public override float GetGoalChange(Goal goal)
        {
            /* is it needed?*/
            var change = base.GetGoalChange(goal);

            if (goal.Name == AutonomousCharacter.SURVIVE_GOAL)
            {
                change -= Character.baseStats.MaxHP - Character.baseStats.HP;
            }

            return change;
        }

    }
}
