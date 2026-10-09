using Assets.Scripts.IAJ.Unity.DecisionMaking.ForwardModel;
using UnityEngine;

namespace Assets.Scripts.IAJ.Unity.DecisionMaking.HeroActions
{
    public class ShieldOfFaith : Action
    {

        private const int MANA_COST = 5;
        public AutonomousCharacter Character { get; private set; }

        public ShieldOfFaith(AutonomousCharacter character) : base("ShieldOfFaith")
        {
            this.Character = character;
        }

        public override bool CanExecute()
        {
            return Character.baseStats.Mana >= MANA_COST && Character.baseStats.ShieldHP < Character.baseStats.MaxShieldHp;
        }

        public override bool CanExecute(WorldModel worldModel)
        {
            int mana = (int)worldModel.GetProperty(PropertiesName.MANA);
            int shield = (int)worldModel.GetProperty(PropertiesName.ShieldHP);
            int maxShield = (int)worldModel.GetProperty(PropertiesName.MaxShieldHP);

            return mana >= MANA_COST && shield < maxShield;
        }

        public override void Execute()
        {
            GameManager.Instance.ShieldOfFaith();
        }

        public override void ApplyActionEffects(WorldModel worldModel)
        {
            int mana = (int)worldModel.GetProperty(PropertiesName.MANA);
            int maxShield = (int)worldModel.GetProperty(PropertiesName.MaxShieldHP);
            //instant action: overwrite DURATION so the child doesn't inherit the parent's duration
            worldModel.SetProperty(PropertiesName.DURATION, 0.0f);
            worldModel.SetProperty(PropertiesName.MANA, mana - MANA_COST);

            worldModel.SetProperty(PropertiesName.ShieldHP, maxShield);
        }

        public override float GetGoalChange(Goal goal)
        {
            var change = base.GetGoalChange(goal);

            if (goal.Name == AutonomousCharacter.SURVIVE_GOAL)
            {
                change -= Character.baseStats.MaxShieldHp - Character.baseStats.ShieldHP;
            }

            return change;
        }

    }
}
