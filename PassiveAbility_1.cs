using System;
using System.Collections.Generic;
using UnityEngine;

// Token: 0x02000478 RID: 1144
namespace ForestForTheFlames;
public class PassiveAbility_1 : PassiveAbility
{
    // Token: 0x170004B7 RID: 1207
    // (get) Token: 0x06001E99 RID: 7833 RVA: 0x0000BCF8 File Offset: 0x00009EF8

    internal static int max = 6;
    internal static int min = 1;

    public override void OnStartPhase(PHASE phase, BATTLE_EVENT_TIMING timing)
    {
        base.OnStartPhase(phase, timing);
        Debug.Log(phase.ToString() + " : " + timing);
    }

    public override void OnRoundEnd(BATTLE_EVENT_TIMING timing)
    {
        Debug.Log("round ended");
        Debug.Log(min);
        //var bf = new BuffModel(Singleton<StaticDataManager>.Instance.BuffList.GetData("SlotAdder"), base.GetOwner().InstanceID);
        //BuffAbilityStaticData td = new BuffAbilityStaticData { value = min };
        //bf._abilityList[0].MergeData(td);
        //base.GetOwner()._buffDetail._grantedBuffList.Add(bf);
        //base.GetOwner().AddBuff_NonGiver(BUFF_UNIQUE_KEYWORD.AaCePaBa, min, 0, 0, ABILITY_SOURCE_TYPE.PASSIVE, BATTLE_EVENT_TIMING.ON_END_ROUND, null, out _, out _);
        if (min < max)
        {
            min += 1;
        }
        base.OnRoundEnd(timing);
    }

    public override void OnRoundStart_After_Event(BATTLE_EVENT_TIMING timing)
    {
        Debug.Log("round started lol");
        base.GetOwner().InitActionSlots();
        base.OnRoundStart_After_Event(timing);
    }


    public override int GetActionSlotAdder()
    {

        //MediatedBuffData x = new MediatedBuffData();
        //base.GetOwner()._buffDetail._grantedBuffList.Add(new BuffModel())
        //base.GetOwner()._buffDetail.AddBuff(base.GetOwner(), x, null, BATTLE_EVENT_TIMING.NONE);
        base.GetActionSlotAdder();
        Debug.Log("get slot adder");
        Debug.Log(min);
        Debug.Log(max);
        base.GetOwner().InitActionSlots();
        return min;
    }

    //public override int GetParryingResultAdder(BattleActionModel action, int actorResult, BattleActionModel oppoAction, int oppoResult, int parryingCount)
    //{
    //    base.GetOwner().AddBuff_NonGiver(BUFF_UNIQUE_KEYWORD.Charge, 10, 20, 0, ABILITY_SOURCE_TYPE.PASSIVE, BATTLE_EVENT_TIMING.ON_START_PARRYING, null, out _, out _);
    //    //actorResult += 777;
    //    //oppoResult = 1;
    //    //parryingCount = 50;
    //    //return base.GetParryingResultAdder(action, actorResult, oppoAction, oppoResult, parryingCount);
    //    return 100;
    //}
}
