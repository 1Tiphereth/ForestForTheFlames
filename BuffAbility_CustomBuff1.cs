using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ForestForTheFlames
{
    public class BuffAbility_CustomBuff1 : BuffAbility
    {
        public override void OnRoundStart_After_Event(BattleUnitModel unit, BATTLE_EVENT_TIMING timing, BuffInfo info)
        {
            Logger.Log("Hello, from buffs!!!");
            unit.AddShield(100, false, ABILITY_SOURCE_TYPE.BUFF, timing);
            base.OnRoundStart_After_Event(unit, timing, info);
        }

        public override void OnBattleStart(BattleUnitModel unit, BATTLE_EVENT_TIMING timing, BuffInfo info)
        {
            Logger.Log("hiiiii from buff");
            base.OnBattleStart(unit, timing, info);
        }

        public override void OnBattleEnd(BattleUnitModel unit, BATTLE_EVENT_TIMING timing, BuffInfo info)
        {
            Logger.Log("hiiii from end buff");
            base.OnBattleEnd(unit, timing, info);
        }

        public override int GetActionSlotAdder()
        {
            Logger.Log("action, from buffs!!!");
            return 5;
        }
    }
}
