using System;
using System.Collections.Generic;
using UnityEngine;

// Token: 0x02000478 RID: 1144
namespace ForestForTheFlames;
public class PassiveAbility_1 : PassiveAbility
{
    public override void OnRoundStart_After_Event(BATTLE_EVENT_TIMING timing)
    {
        Logger.Log(timing);
        Singleton<StageController>.Instance.DanteAbilityManager.AddBonusCount(5);
        //Singleton<StageController>.Instance._abilityLogManager.CreateMpHealLogBySystem(45, _owner.InstanceID, timing);
        //_owner.AddAbilityThisRound(SYSTEM_ABILITY_KEYWORD.SystemAbility_Vergil, 2);

        //MediatedBuffData mbd = new MediatedBuffData(BUFF_UNIQUE_KEYWORD.RealPaperBear);
        //_owner.AddBuff_Variable(mbd, 5);

        //SlotWeightAdder
    }

    public override void OnBattleStart(BATTLE_EVENT_TIMING timing)
    {
        base.OnBattleStart(timing);
        Logger.Log(base.GetOwner().GetAppearanceID());

        //try { 
        //    base.GetOwner().UseEgo(base.GetOwner().GetEgoModel(20601));
        //    Logger.Log("ego set worked!!");
        //} catch {
        //    Logger.Log("Setting ego failed");
        //}
    }

    public override void OnStartDuel(BattleActionModel ownerAction, BattleActionModel opponentAction)
    {
        //try
        //{
        //    base.GetOwner().UseEgo(base.GetOwner().GetEgoModel(20601));
        //    Logger.Log("ego set worked!!");
        //}
        //catch
        //{
        //    Logger.Log("Setting ego failed 2");
        //}
        base.OnStartDuel(ownerAction, opponentAction);
        //opponentAction.Model?.TakeAbsHpDamage(null, 77, out _, out _, BATTLE_EVENT_TIMING.ON_START_DUEL, DAMAGE_SOURCE_TYPE.PASSIVE);
    }

    public override int GetMinSpeedAdder()
    {
        return 100;
    }

    public override int GetMaxSpeedAdder()
    {
        return 200;
    }

    public override int GetActionSlotAdder()
    {
        return 5;
    }
}
