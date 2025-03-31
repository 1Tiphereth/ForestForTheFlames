using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ForestForTheFlames
{
    // Nuovo Fabric
    public class PassiveAbility_100 : PassiveAbility
    {
        internal static int min = -3;
        internal static int max = -7;

        public override int GetExpectedTakeHpDmgAdder()
        {
            return -5;
        }

        public override int GetTakeHpDmgAdder()
        {
            return -5;
        }
    }
}
