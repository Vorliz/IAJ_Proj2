using Assets.Scripts.Game;
using UnityEngine;
using Assets.Scripts.IAJ.Unity.DecisionMaking.ForwardModel;
using System;

namespace Assets.Scripts.IAJ.Unity.DecisionMaking.HeroActions
{
    public class Teleport : Action
    {
        private const int MANA_COST = 5;
        private const int LEVEL_REQUIREMENT = 2;

        protected AutonomousCharacter Character { get; set; }

        public Teleport(AutonomousCharacter character) : base("Teleport")
        {
            this.Character = character;
        }

        public override float GetGoalChange(Goal goal)
        {
            if (goal.Name == AutonomousCharacter.BE_QUICK_GOAL)
            {
                return -1.0f;
            }
            else return 0.0f;
        }

        public override bool CanExecute()
        {
            return Character.baseStats.Mana >= MANA_COST && Character.baseStats.Level >= 2;
        }

        public override bool CanExecute(WorldModel worldModel)
        {
            int mana = (int)worldModel.GetProperty(PropertiesName.MANA);
            int level = (int)worldModel.GetProperty(PropertiesName.LEVEL);

            return mana >= MANA_COST && level >= LEVEL_REQUIREMENT;
        }

        public override void Execute()
        {
            GameManager.Instance.Teleport();
        }

        public override void ApplyActionEffects(WorldModel worldModel)
        {
            int mana = (int)worldModel.GetProperty(PropertiesName.MANA);
            //instant action: overwrite DURATION so the child doesn't inherit the parent's duration
            worldModel.SetProperty(PropertiesName.DURATION, 0.0f);
            worldModel.SetProperty(PropertiesName.MANA, mana - MANA_COST);

            worldModel.SetProperty(PropertiesName.POSITION, this.Character.InitialPosition);
        }
    }
}