using Assets.Scripts.IAJ.Unity.DecisionMaking.ForwardModel;
using UnityEngine;

namespace Assets.Scripts.IAJ.Unity.DecisionMaking.HeroActions
{
    public class DivineSmite : WalkToTargetAndExecuteAction
    {
        private const int XP_CHANGE = 3;
        private const int MANA_COST = 2;

        public DivineSmite(AutonomousCharacter character, GameObject target) : base("DivineSmite", character, target)
        {
        }

        public override bool CanExecute()
        {
            if (!base.CanExecute()) return false;
            return this.Target.CompareTag("Skeleton") && Character.baseStats.Mana >= MANA_COST;
        }

        public override bool CanExecute(WorldModel worldModel)
        {
            if (!base.CanExecute(worldModel)) return false;

            var mana = (int)worldModel.GetProperty(PropertiesName.MANA);
            return this.Target.CompareTag("Skeleton") && mana >= MANA_COST;
        }

        public override float GetGoalChange(Goal goal)
        {
            var change = base.GetGoalChange(goal);

            if (goal.Name == AutonomousCharacter.GAIN_LEVEL_GOAL)
            {
                change -= (float)XP_CHANGE / (Character.baseStats.Level * 10);
            }

            return change;
        }

        public override void Execute()
        {
            base.Execute();
            GameManager.Instance.DivineSmite(this.Target);
        }


        public override void ApplyActionEffects(WorldModel worldModel)
        {
            base.ApplyActionEffects(worldModel);

            int xp = (int)worldModel.GetProperty(PropertiesName.XP);
            int mana = (int)worldModel.GetProperty(PropertiesName.MANA);

            worldModel.SetProperty(PropertiesName.XP, xp + XP_CHANGE);
            worldModel.SetProperty(PropertiesName.MANA, mana - MANA_COST);

            worldModel.SetProperty(this.Target.name, false);
        }

    }
}
