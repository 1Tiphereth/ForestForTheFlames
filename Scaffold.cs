using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using BepInEx.Logging;
using HarmonyLib;

namespace ForestForTheFlames
{
    public class Scaffold
    {
        internal static ManualLogSource Log = Plugin.Log;

        public static System.Collections.Generic.Dictionary<float, System.Collections.Generic.List<object>> pss = new System.Collections.Generic.Dictionary<float, System.Collections.Generic.List<object>>();

        public static float GetSid(BattleUnitModel model)
        {
            float sid = model.UnitDataModel.ClassInfo.ID;
            return sid;
        }

        public static void AssignPassive(BattleUnitModel model, int id)
        {
            float sid = GetSid(model);
            Logger.Log($"{sid}:{id}");
            if (!pss.ContainsKey(sid))
            {
                pss.Add(sid, new System.Collections.Generic.List<object>());
            }

            //if (id == -1)
            //{
            //    var p = new PassiveAbility_1();
            //    p.Init(model, null, null);
            //    pss[sid].Add(p);
            //}

            var ps = Assembly.GetExecutingAssembly().GetType($"ForestForTheFlames.PassiveAbility_{Math.Abs(id)}");
            if (ps != null)
            {
                Logger.Log(ps);
                var p = (Activator.CreateInstance(ps) as PassiveAbility);
                p.Init(model, null, null);
                pss[sid].Add(p);
            }
            else
            {
                Log.LogFatal($"Passive {id} is null");
            }
        }

        //reset passives + set the passive -1 to the abno part not unit!
        public static void NukePassives(BattleUnitModel model)
        {
            float sid = GetSid(model);
            Logger.Log($"{sid}");
            if (pss.ContainsKey(sid))
            {
                pss.Remove(sid);
            }
        }

        public static System.Collections.Generic.List<PassiveAbility> GetPassives(BattleUnitModel model = null, float bsid = 0f)
        {
            float sid;
            if (model == null)
            {
                sid = bsid;
            }
            else
            {
                sid = GetSid(model);
            }
            System.Collections.Generic.List<PassiveAbility> list = new System.Collections.Generic.List<PassiveAbility>();
            Logger.Log($"{sid}");
            if (pss.ContainsKey(sid))
            {
                foreach (var x in pss[sid])
                {
                    list.Add(x as PassiveAbility);
                }
                return list;
            }
            else
            {
                return list;
            }
        }

        [HarmonyPatch(typeof(BattleUnitModel), "OnStartDuel")]
        [HarmonyPrefix]
        public static void Scaffold_OnStartDuel(BattleUnitModel __instance, BattleActionModel ownerAction, BattleActionModel opponentAction, BATTLE_EVENT_TIMING timing)
        {
            foreach (var ps in GetPassives(__instance))
            {
                ps.OnStartDuel(ownerAction, opponentAction);
            }
        }
        [HarmonyPatch(typeof(BattleUnitModel), "Init")]
        [HarmonyPostfix]
        public static void Scaffold_Init(BattleUnitModel __instance)
        {
            foreach (var psd in __instance._passiveDetail.PassiveList)
            {
                if (psd.GetID() < 0)
                {
                    AssignPassive(__instance, psd.GetID());
                }
            }
        }

        [HarmonyPatch(typeof(BattleUnitModel), "OnDie")]
        [HarmonyPostfix]
        public static void Scaffold_OnDie(BattleUnitModel __instance)
        {
            NukePassives(__instance);
        }

        [HarmonyPatch(typeof(BattleUnitModel), "BeforeGiveAttackDamage")]
        [HarmonyPostfix]
        public static void Scaffold_BeforeGiveAttackDamage(BattleUnitModel __instance, BattleActionModel action, CoinModel coin, BattleUnitModel target, BATTLE_EVENT_TIMING timing)
        {
            foreach (var x in __instance.UnitScripts) {
                Logger.Log(x);
                foreach (var y in x._battleUnitView.ChangedAppearanceList)
                {
                    Logger.Log(y.appearance.name);
                    Logger.Log(y.appearance.GetScriptClassName());
                }
            }
            foreach (var ps in GetPassives(__instance))
            {
                ps.BeforeGiveAttackDamage(action, coin, target, timing);
            }
        }

        [HarmonyPatch(typeof(BattleUnitModel), "OnTakeAttackDamage")]
        [HarmonyPrefix]
        public static void Scaffold_OnTakeAttackDamage(BattleUnitModel __instance, BattleActionModel action, CoinModel coin, ref int realDmg, ref int hpDamage, BATTLE_EVENT_TIMING timing, ref bool isCritical)
        {
            foreach (var ps in GetPassives(__instance))
            {
                ps.OnTakeAttackDamage(action, realDmg, hpDamage, timing);
            }
        }

        [HarmonyPatch(typeof(BattleUnitModel), "GetParryingResultAdder")]
        [HarmonyPostfix]
        public static void Scaffold_GetParryingResultAdder(BattleUnitModel __instance, BattleActionModel action, ref int actorResult, BattleActionModel oppoAction, ref int oppoResult, ref int parryingCount)
        {
            foreach (var ps in GetPassives(__instance))
            {
                actorResult = ps.GetParryingResultAdder(action, actorResult, oppoAction, oppoResult, parryingCount);
            }
        }


        [HarmonyPatch(typeof(BattleUnitModel), "OnBattleStart")]
        [HarmonyPrefix]
        public static void Scaffold_OnBattleStart(BattleUnitModel __instance, BATTLE_EVENT_TIMING timing)
        {
            foreach (var ps in GetPassives(__instance))
            {
                ps.OnBattleStart(timing);
            }
        }

        [HarmonyPatch(typeof(BattleUnitModel), "OnRoundStart_After_Event")]
        [HarmonyPrefix]
        public static void Scaffold_OnRoundStart_After_Event(BattleUnitModel __instance, BATTLE_EVENT_TIMING timing)
        {
            foreach (var ps in GetPassives(__instance))
            {
                ps.OnRoundStart_After_Event(timing);
            }
        }

        [HarmonyPatch(typeof(BattleUnitModel), "OnRoundEnd")]
        [HarmonyPrefix]
        public static void Scaffold_OnRoundEnd(BattleUnitModel __instance, BATTLE_EVENT_TIMING timing)
        {
            foreach (var ps in GetPassives(__instance))
            {
                ps.OnRoundEnd(timing);
            }
        }

        [HarmonyPatch(typeof(PassiveDetail), "OnStartPhase")]
        [HarmonyPrefix]
        public static void Scaffold_OnStartPhase(BattleUnitModel __instance, PHASE phase, BATTLE_EVENT_TIMING timing)
        {
            foreach (var ps in GetPassives(__instance))
            {
                ps.OnStartPhase(phase, timing);
            }
        }

        [HarmonyPatch(typeof(BattleUnitModel), "GetTakeHpDmgAdder")]
        [HarmonyPostfix]
        public static void Scaffold_GetTakeHpDmgAdder(BattleUnitModel __instance, ref int __result)
        {
            foreach (var ps in GetPassives(__instance))
            {
                __result += ps.GetTakeHpDmgAdder();
            }
        }

        [HarmonyPatch(typeof(BattleUnitModel), "GetExpectedTakeHpDmgAdder")]
        [HarmonyPostfix]
        public static void Scaffold_GetExpectedTakeHpDmgAdder(BattleUnitModel __instance, ref int __result)
        {
            foreach (var ps in GetPassives(__instance))
            {
                __result += ps.GetTakeHpDmgAdder();
            }
        }
        // 

        //[HarmonyPatch(typeof(BattleUnitModel), "UseEgo")]
        //[HarmonyPrefix]
        //public static void Scaffold_UseEgo(BattleUnitModel __instance, ref BattleEgoModel ego)
        //{
        //    foreach (var ps in GetPassives(__instance))
        //    {
        //        ps.SetEgoID(ego.ClassInfo.id);
        //    }
        //}

        [HarmonyPatch(typeof(BattleUnitModel), "GetMinSpeedAdder")]
        [HarmonyPostfix]
        public static void Scaffold_GetMinSpeedAdder(BattleUnitModel __instance, ref int __result)
        {
            int total = __result;
            foreach (var ps in GetPassives(__instance))
            {
                total += ps.GetMinSpeedAdder();
            }
            __result = total;
        }

        [HarmonyPatch(typeof(BattleUnitModel), "GetMaxSpeedAdder")]
        [HarmonyPostfix]
        public static void Scaffold_GetMaxSpeedAdder(BattleUnitModel __instance, ref int __result)
        {
            int total = __result;
            foreach (var ps in GetPassives(__instance))
            {
                total += ps.GetMaxSpeedAdder();
            }
            __result = total;
        }

        [HarmonyPatch(typeof(BattleUnitModel), "GetActionSlotAdder")]
        [HarmonyPostfix]
        public static void Scaffold_GetActionSlotAdder(BattleUnitModel __instance, ref int __result)
        {
            bsid = GetSid(__instance);

            int total = __result;
            foreach (var ps in GetPassives(__instance))
            {
                total += ps.GetActionSlotAdder();
            }
            //Log.LogFatal(total);
            //if (__instance._buffDetail != null)
            //{
            //    total += __instance._buffDetail.GetActionSlotAdder();
            //}
            //Log.LogFatal(total);

            //if (__instance._passiveDetail != null)
            //{
            //    total += __instance._passiveDetail.GetActionSlotAdder();
            //}

            Logger.Log("passive slots: " + total);
            __result = total;
            //return false;
        }

        internal static float bsid = 0f;

        [HarmonyPatch(typeof(BuffDetail), "GetActionSlotAdder")]
        [HarmonyPostfix]
        public static void Scaffold_GetActionSlotAdder_Patch(BuffDetail __instance, ref int __result)
        {
            //Log.LogFatal((new System.Diagnostics.StackTrace()).GetFrame(1).GetMethod().Name);
            int num = __result;
            Logger.Log("BSID: " + bsid);
            foreach (var ps in GetPassives(bsid: bsid))
            {
                Logger.Log($"Getting action from BuffDetail (bsid:{bsid})");
                num += ps.GetActionSlotAdder();
            }
            //foreach (BuffModel battleUnitBuff in __instance._grantedBuffList)
            //{
            //    if (battleUnitBuff.IsValid(0) && !battleUnitBuff.IsDestroyed())
            //    {
            //        num += battleUnitBuff.GetActionSlotAdder();
            //    }
            //}
            Logger.Log(num);
            __instance.CheckBuffsDestroyed();
            __result = num;
            //return __result;
        }
    }
}
