using Assets.Scripts.Game;
using Assets.Scripts.IAJ.Unity.DecisionMaking.ForwardModel;
using UnityEngine;

namespace Assets.Scripts.IAJ.Unity.DecisionMaking.HeroActions
{
    public class GetManaPotion : WalkToTargetAndExecuteAction
    {
        // MaxMana is 10
        private const int POTION_MANA = 10;

        public GetManaPotion(AutonomousCharacter character, GameObject target) : base("GetManaPotion",character,target)
        {
        }

        public override bool CanExecute()
        {
            if (!base.CanExecute()) return false;
            return Character.baseStats.Mana < POTION_MANA;
        }

        public override bool CanExecute(WorldModel worldModel)
        {
            if (!base.CanExecute(worldModel)) return false;

            var currentMP = (int)worldModel.GetProperty(Properties.MANA);
            return currentMP < POTION_MANA;
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
                int manaGained = POTION_MANA - Character.baseStats.Mana;         // the potion fills mana to 10
                int skeletonsAlive = GameObject.FindGameObjectsWithTag("Skeleton").Length;
                int smites = Mathf.Min(manaGained / 2, skeletonsAlive);          // 2 mana per smite
                float xpToNextLevel = Character.baseStats.Level * 10;

                // 0.5 = discount: the XP is indirect and only comes after more actions
                change -= 0.5f * smites * 3 / xpToNextLevel;
            }
            else if (goal.Name == AutonomousCharacter.SURVIVE_GOAL &&
                     Character.baseStats.Level >= LayOnHands.LEVEL_REQUIREMENT &&
                     Character.baseStats.Mana < LayOnHands.MANA_COST)
            {
                int missingHP = Character.baseStats.MaxHP - Character.baseStats.HP;
                change -= 0.3f * Mathf.Max(0, missingHP);
            }

            return change;
        }

        public override void ApplyActionEffects(WorldModel worldModel)
        {
            base.ApplyActionEffects(worldModel);
            worldModel.SetProperty(Properties.MANA, POTION_MANA);

            //disables the target object so that it can't be reused again
            worldModel.SetProperty(this.Target.name, false);
        }

    }
}
