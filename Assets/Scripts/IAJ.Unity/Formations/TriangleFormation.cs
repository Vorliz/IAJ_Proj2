using System.Collections;
using System.Linq;
using UnityEngine;

namespace Assets.Scripts.IAJ.Unity.Formations
{
    public class TriangleFormation : FormationPattern
    {
        // A wedge: the leader (slot 0) is the tip, the others stand behind it, one on each side.
        private static readonly float offset = 2.5f;

        public TriangleFormation()
        {
            this.FreeAnchor = true;
        }

        public override Vector3 GetOrientation(FormationManager formation )
        {
            //In this formation, the orientation is defined by the first character's transform rotation...

            return formation.Orientation;

        }

        public override Vector3 GetSlotLocation(FormationManager formation, int slotNumber)
        {
            var forward = this.GetOrientation(formation);
            var right = Vector3.Cross(Vector3.up, forward);
            var anchor = formation.AnchorPosition;

            return slotNumber switch
            {
                0 => anchor,
                1 => anchor - forward * offset - right * offset,   // back-left
                2 => anchor - forward * offset + right * offset,   // back-right
                _ => anchor
            };
        }

        public override  bool SupportSlot(int slotCount)
        {
            return (slotCount <= 3);
        }


    }
}
