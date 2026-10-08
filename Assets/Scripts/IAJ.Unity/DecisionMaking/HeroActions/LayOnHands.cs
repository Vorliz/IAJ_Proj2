using Assets.Scripts.IAJ.Unity.DecisionMaking.ForwardModel;
using UnityEngine;

namespace Assets.Scripts.IAJ.Unity.DecisionMaking.HeroActions
{
    public class LayOnHands : Action
    {

        private const int MANA_COST = 7;
        public AutonomousCharacter Character { get; private set; }

        public LayOnHands(AutonomousCharacter character) : base("LayOnHands")
        {
            this.Character = character;
        }

        public override bool CanExecute()
        {
            return Character.baseStats.Mana >= MANA_COST && Character.baseStats.HP < Character.baseStats.MaxHP;
        }

        public override bool CanExecute(WorldModel worldModel)
        {
            int mana = (int)worldModel.GetProperty(PropertiesName.MANA);
            int hp = (int)worldModel.GetProperty(PropertiesName.HP);
            int maxHP = (int)worldModel.GetProperty(PropertiesName.MAXHP);

            return mana >= MANA_COST && hp < maxHP;
        }

        public override void Execute()
        {
            GameManager.Instance.LayOnHands();
        }

        public override void ApplyActionEffects(WorldModel worldModel)
        {
            int mana = (int)worldModel.GetProperty(PropertiesName.MANA);
            int maxHP = (int)worldModel.GetProperty(PropertiesName.MAXHP);
            worldModel.SetProperty(PropertiesName.MANA, mana - MANA_COST);

            worldModel.SetProperty(PropertiesName.HP, maxHP);
        }

        public override float GetGoalChange(Goal goal)
        {
            /* is it needed?*/
            return 0.0f;
        }

    }
}
