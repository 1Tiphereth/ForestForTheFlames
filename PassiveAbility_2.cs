using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ForestForTheFlames
{
    public class PassiveAbility_2 : PassiveAbility
    {
        public override int GetCoinScaleAdder(BattleActionModel action, CoinModel coin)
        {
            //action._targetDataDetail.GetMainTarget().HasBuff(BUFF_UNIQUE_KEYWORD.)
            return 2;
        }
    }
}
