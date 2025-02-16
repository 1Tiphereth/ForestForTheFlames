using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
            Log.LogInfo($"{sid}:{id}");
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
            Log.LogInfo($"{sid}");
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
            Log.LogInfo($"{sid}");
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
        [HarmonyPrefix]
        public static void Scaffold_BeforeGiveAttackDamage(BattleUnitModel __instance, BattleActionModel action, CoinModel coin, BattleUnitModel target, BATTLE_EVENT_TIMING timing)
        {
            foreach (var ps in GetPassives(__instance))
            {
                ps.BeforeGiveAttackDamage(action, coin, target, timing);
            }
        }

        [HarmonyPatch(typeof(BattleUnitModel), "OnTakeAttackDamage")]
        [HarmonyPrefix]
        public static void Scaffold_OnTakeAttackDamage(BattleUnitModel __instance, BattleActionModel action, CoinModel coin, int realDmg, int hpDamage, BATTLE_EVENT_TIMING timing, bool isCritical)
        {
            foreach (var ps in GetPassives(__instance))
            {
                ps.OnTakeAttackDamage(action, realDmg, hpDamage, timing);
            }
        }

        [HarmonyPatch(typeof(BattleUnitModel), "GetParryingResultAdder")]
        [HarmonyPrefix]
        public static void Scaffold_GetParryingResultAdder(BattleUnitModel __instance, BattleActionModel action, int actorResult, BattleActionModel oppoAction, int oppoResult, int parryingCount)
        {
            foreach (var ps in GetPassives(__instance))
            {
                actorResult = ps.GetParryingResultAdder(action, actorResult, oppoAction, oppoResult, parryingCount);
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
        //

        [HarmonyPatch(typeof(BattleUnitModel), "GetActionSlotAdder")]
        [HarmonyPrefix]
        public static bool Scaffold_GetActionSlotAdder(BattleUnitModel __instance, ref int __result)
        {
            //bsid = GetSid(__instance);

            int total = 0;
            foreach (var ps in GetPassives(__instance))
            {
                total += ps.GetActionSlotAdder();
            }
            Log.LogFatal(total);
            if (__instance._buffDetail != null)
            {
                total += __instance._buffDetail.GetActionSlotAdder();
            }
            Log.LogFatal(total);

            if (__instance._passiveDetail != null)
            {
                total += __instance._passiveDetail.GetActionSlotAdder();
            }

            Log.LogFatal(total);
            __result = total;
            return false;
        }

        internal static float bsid = 0f;

        [HarmonyPatch(typeof(BuffDetail), "GetActionSlotAdder")]
        [HarmonyPrefix]
        public static bool Scaffold_GetActionSlotAdder_Patch(BuffDetail __instance, ref int __result)
        {
            Log.LogFatal((new System.Diagnostics.StackTrace()).GetFrame(1).GetMethod().Name);
            int num = 0;

            foreach (var ps in GetPassives(bsid: bsid))
            {
                num += ps.GetActionSlotAdder();
            }

            foreach (BuffModel battleUnitBuff in __instance._grantedBuffList)
            {
                if (battleUnitBuff.IsValid(0) && !battleUnitBuff.IsDestroyed())
                {
                    num += battleUnitBuff.GetActionSlotAdder();
                }
            }
            __instance.CheckBuffsDestroyed();
            __result = num;
            return false;
        }
    }
}
