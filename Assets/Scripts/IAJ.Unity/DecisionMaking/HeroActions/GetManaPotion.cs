using Assets.Scripts.Game;
using Assets.Scripts.IAJ.Unity.DecisionMaking.ForwardModel;
using UnityEngine;

namespace Assets.Scripts.IAJ.Unity.DecisionMaking.HeroActions
{
    public class GetManaPotion : WalkToTargetAndExecuteAction
    {
        public GetManaPotion(AutonomousCharacter character, GameObject target) : base("GetManaPotion",character,target)
        {
        }

        public override bool CanExecute()
        {
            if (!base.CanExecute()) return false;
            return Character.baseStats.Mana < Character.baseStats.MaxMana;
        }

        public override bool CanExecute(WorldModel worldModel)
        {
            if (!base.CanExecute(worldModel)) return false;

            var currentMP = (int)worldModel.GetProperty(Properties.MANA);
            var maxMP = (int)worldModel.GetProperty(Properties.MAXMANA);
            return currentMP < maxMP;
        }

        public override void Execute()
        {
            base.Execute();
            GameManager.Instance.GetManaPotion(this.Target);
        }

        public override float GetGoalChange(Goal goal)
        {
            var change = base.GetGoalChange(goal);

            if (goal.Name == AutonomousCharacter.GAIN_LEVEL_GOAL)
            {
                int manaGained = 10 - Character.baseStats.Mana;                  // the potion fills mana to 10
                int skeletonsAlive = GameObject.FindGameObjectsWithTag("Skeleton").Length;
                int smites = Mathf.Min(manaGained / 2, skeletonsAlive);          // 2 mana per smite
                float xpToNextLevel = Character.baseStats.Level * 10;

                // 0.5 = discount: the XP is indirect and only comes after more actions
                change -= 0.5f * smites * 3 / xpToNextLevel;
            }

            return change;
        }

        public override void ApplyActionEffects(WorldModel worldModel)
        {
            base.ApplyActionEffects(worldModel);
            var currentMP = (int)worldModel.GetProperty(Properties.MANA);
            var maxMP = (int)worldModel.GetProperty(Properties.MAXMANA);
            worldModel.SetProperty(Properties.MANA, maxMP);

            //disables the target object so that it can't be reused again
            worldModel.SetProperty(this.Target.name, false);
        }

    }
}
