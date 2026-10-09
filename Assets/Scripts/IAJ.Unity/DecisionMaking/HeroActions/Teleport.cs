using Assets.Scripts.Game;
using UnityEngine;
using Assets.Scripts.IAJ.Unity.DecisionMaking.ForwardModel;
using System;

namespace Assets.Scripts.IAJ.Unity.DecisionMaking.HeroActions
{
    public class Teleport : Action
    {
        private const int MANA_COST = 5;

        protected AutonomousCharacter Character { get; set; }

        public GameObject Target { get; set; }

        public Teleport(AutonomousCharacter character) : base("Teleport")
        {
            this.Character = character;
        }

        public override float GetGoalChange(Goal goal)
        {
            return 0.0f;
        }

        public override bool CanExecute()
        {
            return Character.baseStats.Mana >= MANA_COST;
        }

        public override bool CanExecute(WorldModel worldModel)
        {
            int mana = (int)worldModel.GetProperty(PropertiesName.MANA);

            return mana >= MANA_COST;
        }

        public override void Execute()
        {
            this.Character.transform.position = this.Character.InitialPosition;
        }

        public override void ApplyActionEffects(WorldModel worldModel)
        {
            worldModel.SetProperty(PropertiesName.POSITION, Target.transform.position);
        }
    }
}