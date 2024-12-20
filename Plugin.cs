using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using MainUI;
using Server;
using UnityEngine;
using Addressable;
using SimpleJSON;
using System.IO;
using BattleUI;
using SD;
using ServerConfig;
using Dungeon;
using Il2CppSystem.Collections.Generic;

namespace ForestForTheFlames;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, "YumYum Enterprises", MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BasePlugin
{
    internal static new ManualLogSource Log;
    protected static string SERVER_URL = " http://127.0.0.1:21000";
    internal static string DATA_PATH = BepInEx.Paths.PluginPath + "\\ForestForTheFlames";
    internal static bool patched = false;
    internal static bool networkKill = false;
    internal static bool _asPatched = false;
    internal static bool _skip1 = false;
    internal static bool _egostage = false;

    public override void Load()
    {
        Harmony.CreateAndPatchAll(typeof(Plugin));

        Log = base.Log;
        Log.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
        //Log.LogInfo($"Using custom server at {SERVER_URL}");

        //foreach (var x in System.Reflection.Assembly.GetExecutingAssembly().GetTypes())
        //{
        //    Log.LogInfo(x.Name);
        //}

        //egolist.Add("Abnormality", new System.Collections.Generic.List<(int, int)>());
        //egolist.Add("Abnormality_Part", new System.Collections.Generic.List<(int, int)>());
        //egolist.Add("Ally", new System.Collections.Generic.List<(int, int)>());
        //egolist.Add("Assistant", new System.Collections.Generic.List<(int, int)>());
        //egolist.Add("Opponent", new System.Collections.Generic.List<(int, int)>());
        egolist.Add("Enemy", new System.Collections.Generic.List<(int, int, int)>());
        egolist.Add("Player", new System.Collections.Generic.List<(int, int, int)>());
    }

    [HarmonyPatch(typeof(HttpApiRequester), "AddRequest")]
    [HarmonyPrefix]
    public static bool AddRequest(HttpApiRequester __instance, HttpApiSchema httpApiSchema, int priority = 0)
    {
        if (!_skip1)
        {
            //httpApiSchema._url.Replace("https://www.limbuscompanyapi.com", SERVER_URL);
            Log.LogInfo(httpApiSchema._url + " : " + httpApiSchema.RequestJson);

            // change httpApiSchema._url you redirect it to your own host
            // _url -> full url
            __instance._requestQueue.Enqueue(httpApiSchema, priority);
            __instance.ProceedRequest();
        }
        return false;
    }

    public static void Callback(object any)
    {
        Log.LogInfo((string)any);
    }

    public static void ReplaceSkill(SkillStaticData x, SkillStaticData y)
    {
        x.skillData = y.skillData;
        x.skillTier = y.skillTier;
        x.skillType = y.skillType;
        x.textID = y.textID;
    }

    public static Il2CppSystem.Collections.Generic.List<JSONNode> jlist = new Il2CppSystem.Collections.Generic.List<JSONNode>();
    public static Il2CppSystem.Collections.Generic.List<JSONNode> lclist = new Il2CppSystem.Collections.Generic.List<JSONNode>();
    public static System.Collections.Generic.Dictionary<int, (string, string)> aplist = new System.Collections.Generic.Dictionary<int, (string, string)> { };
    public static System.Collections.Generic.Dictionary<int, JSONNode> eslist = new System.Collections.Generic.Dictionary<int, JSONNode> { };
    public static System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<(int, int, int)>> egolist = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<(int, int, int)>> { };
    // charid, id, who, egoid

    public static void PrepareSkillFromLocalJson(string path)
    {
        Log.LogInfo($"Skill: Preparing to load {path}");
        string contents = File.ReadAllText($@"{DATA_PATH}\json\{path}");
        jlist.Add(JSONNode.Parse(contents));
    }

    public static void InitSkills()
    {
        Log.LogInfo("Loading skills");
        foreach (var x in jlist)
        {
            var y = JsonUtility.FromJson<SkillStaticData>(x.ToString());
            if (Singleton<StaticDataManager>.Instance.SkillList.dict.ContainsKey(y.ID))
            {
                Singleton<StaticDataManager>.Instance.SkillList.dict[y.ID] = y;
            }
            else
            {
                //Singleton<StaticDataManager>.Instance.SkillList.list.Add(y);
                Singleton<StaticDataManager>.Instance.SkillList.dict.Add(y.ID, y);

            }

        }
        Log.LogInfo("Finished loading skills");
        //Log.LogInfo("Clearing [jlist]");
        //jlist.Clear();
    }

    public static void PrepareLocalize(string path)
    {
        Log.LogInfo($"Localize: Preparing to load {path}");
        var p = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        string contents = File.ReadAllText($@"{DATA_PATH}\json\{path}");
        jlist.Add(JSONNode.Parse(contents));
    }

    public static void InitLocalize()
    {
        Log.LogInfo("Loading localization");
        foreach (var x in jlist)
        {
            var y = JsonUtility.FromJson<TextData_Skill>(x.ToString());
            if (Singleton<TextDataSet>.Instance.SkillList._dic.ContainsKey(y.ID))
            {
                Singleton<TextDataSet>.Instance.SkillList._dic[y.ID] = y;
            }
            else
            {
                Singleton<TextDataSet>.Instance.SkillList._dic.Add(y.ID, y);
            }
        }
        Log.LogInfo("Finished loading localization");
        //Log.LogInfo("Clearing [lclist]");
        //lclist.Clear();
    }

    public static void HotPatch()
    {
        Log.LogWarning("Hotpatching in progress!");
        InitSkills();
        InitLocalize();
    }

    public static void AddPassive(int id, int pid, int level = -1)
    {
        Log.LogInfo($"Adding passive {pid} to {id}");
        foreach (var x in Singleton<StaticDataManager>.Instance.PersonalityPassiveList.list)
        {
            if (x.personalityID == id)
            {
                foreach (var y in x.battlePassiveList)
                {
                    if (level != -1)
                    {
                        if (y.Level == level)
                        {
                            y.passiveIDList.Add(pid);
                        }
                    }
                    else
                    {
                        y.passiveIDList.Add(pid);
                    }
                }
            }
        }
    }

    public static void RemovePassive(int id, int pid)
    {
        Log.LogInfo($"Removing passive {pid} from {id}");
        foreach (var x in Singleton<StaticDataManager>.Instance.PersonalityPassiveList.list)
        {
            if (x.personalityID == id)
            {
                foreach (var y in x.battlePassiveList)
                {
                    if (y.passiveIDList.Contains(pid))
                    {
                        y.passiveIDList.Remove(pid);
                    }
                }
            }
        }
    }

    public static void LoadAbnoUnit(string path)
    {
        Log.LogInfo($"AbnoUnit: Loading {path}");
        var p = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        string contents = File.ReadAllText($@"{DATA_PATH}\json\unit\Abno\{path}");
        var y = JsonUtility.FromJson<AbnormalityStaticData>(contents.ToString());
        Singleton<StaticDataManager>.Instance.AbnormalityUnitList.list.Add(y);
    }
    public static void LoadAbnoPartUnit(string path)
    {
        Log.LogInfo($"AbnoUnitPart: Loading {path}");
        var p = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        string contents = File.ReadAllText($@"{DATA_PATH}\json\unit\Abno\{path}");
        var y = JsonUtility.FromJson<AbnormalityPartStaticData>(contents.ToString());
        Singleton<StaticDataManager>.Instance.AbnormalityPartList.list.Add(y);
    }

    public static void LoadBuffStatic(string path)
    {
        Log.LogInfo($"BuffStatic: Loading {path}");
        var p = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        string contents = File.ReadAllText($@"{DATA_PATH}\json\buff\{path}");
        var y = JsonUtility.FromJson<BuffStaticData>(contents.ToString());
        Singleton<StaticDataManager>.Instance.BuffList.list.Add(y);
    }

    public static void LoadPersonality(string path)
    {
        Log.LogInfo($"Personality: Loading {path}");
        var p = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        {
            string contents = File.ReadAllText($@"{DATA_PATH}\json\unit\Sinners\{path}");
            var y = JsonUtility.FromJson<PersonalityStaticData>(contents.ToString());
            Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.list.Add(y);
        }

        {
            path.Replace(".json", "_text.json");
            string contents = File.ReadAllText($@"{DATA_PATH}\json\unit\Sinners\{path}");
            var y = JsonUtility.FromJson<PersonalityStaticData>(contents.ToString());
            Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.list.Add(y);
        }

    }

    public static void LoadEgoTextAndStatic(string path)
    {
        Log.LogInfo($"EgoLoaderAndTextPlusStatic: Loading {path}");
        var p = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        {
            string contents = File.ReadAllText($@"{DATA_PATH}\json\ego\{path}");
            var y = JsonUtility.FromJson<EgoStaticData>(contents.ToString());
            Singleton<StaticDataManager>.Instance.EgoList.list.Add(y);
        }

        {
            path.Replace(".json", "_text.json");
            string contents = File.ReadAllText($@"{DATA_PATH}\json\ego\{path}");
            var y = JsonUtility.FromJson<TextData_Ego>(contents.ToString());
            Singleton<TextDataSet>.Instance.EgoList._dic.Add(y.ID, y);
        }
    }

    public static void PrepareExpStage(int id, string path)
    {
        Log.LogInfo($"ExpStage: Preparing to load {path}");
        var p = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        string contents = File.ReadAllText($@"{DATA_PATH}\json\stage\{path}");
        eslist.Add(id, JSONNode.Parse(contents));
    }

    public static void InitExpStage()
    {
        Log.LogInfo("Loading ExpStages");
        foreach (var x in eslist)
        {
            var y = JsonUtility.FromJson<StageStaticData>(x.Value.ToString());
            var i = x.Key;
            Log.LogInfo($"ExpStage: Loading id:{i}");

            Singleton<StaticDataManager>.Instance.ExpDungeonBattleList.GetStage(i).hasGoldenBough = y.hasGoldenBough;
            Singleton<StaticDataManager>.Instance.ExpDungeonBattleList.GetStage(i).hasGoldenBoughGray = y.hasGoldenBoughGray;
            Singleton<StaticDataManager>.Instance.ExpDungeonBattleList.GetStage(i).stageLevel = y.stageLevel;
            Singleton<StaticDataManager>.Instance.ExpDungeonBattleList.GetStage(i).stageType = y.stageType;
            Singleton<StaticDataManager>.Instance.ExpDungeonBattleList.GetStage(i).isBatonPassOn = y.isBatonPassOn;
            Singleton<StaticDataManager>.Instance.ExpDungeonBattleList.GetStage(i).stageEnemyType = y.stageEnemyType;
            Singleton<StaticDataManager>.Instance.ExpDungeonBattleList.GetStage(i).battleCameraInfo = y.battleCameraInfo;
            Singleton<StaticDataManager>.Instance.ExpDungeonBattleList.GetStage(i).includeBoss = y.includeBoss;
            Singleton<StaticDataManager>.Instance.ExpDungeonBattleList.GetStage(i).staminaType = y.staminaType;
            Singleton<StaticDataManager>.Instance.ExpDungeonBattleList.GetStage(i).staminaCost = y.staminaCost;
            Singleton<StaticDataManager>.Instance.ExpDungeonBattleList.GetStage(i).recommendedLevel = y.recommendedLevel;
            Singleton<StaticDataManager>.Instance.ExpDungeonBattleList.GetStage(i).story = y.story;
            Singleton<StaticDataManager>.Instance.ExpDungeonBattleList.GetStage(i).questlist = y.questlist;
            Singleton<StaticDataManager>.Instance.ExpDungeonBattleList.GetStage(i).dangerLevel = y.dangerLevel;
            Singleton<StaticDataManager>.Instance.ExpDungeonBattleList.GetStage(i).attributeType = y.attributeType;
            Singleton<StaticDataManager>.Instance.ExpDungeonBattleList.GetStage(i).attackType = y.attackType;
            Singleton<StaticDataManager>.Instance.ExpDungeonBattleList.GetStage(i).abnormalityEventList = y.abnormalityEventList;
            Singleton<StaticDataManager>.Instance.ExpDungeonBattleList.GetStage(i).eventScriptName = y.eventScriptName;
            Singleton<StaticDataManager>.Instance.ExpDungeonBattleList.GetStage(i).participantInfo = y.participantInfo;
            Singleton<StaticDataManager>.Instance.ExpDungeonBattleList.GetStage(i).waveList = y.waveList;
            Singleton<StaticDataManager>.Instance.ExpDungeonBattleList.GetStage(i).turnLimit = y.turnLimit;
            Singleton<StaticDataManager>.Instance.ExpDungeonBattleList.GetStage(i).rewardList = y.rewardList;
            Singleton<StaticDataManager>.Instance.ExpDungeonBattleList.GetStage(i).blockEnemyInfo = y.blockEnemyInfo;
            Singleton<StaticDataManager>.Instance.ExpDungeonBattleList.GetStage(i).forceAllyFormation = y.forceAllyFormation;
            Singleton<StaticDataManager>.Instance.ExpDungeonBattleList.GetStage(i).unlockDanteAbility = y.unlockDanteAbility;
            Singleton<StaticDataManager>.Instance.ExpDungeonBattleList.GetStage(i).abstainSupporterCharacterIds = y.abstainSupporterCharacterIds;
            Singleton<StaticDataManager>.Instance.ExpDungeonBattleList.GetStage(i).lobotomyStageType = y.lobotomyStageType;
            Singleton<StaticDataManager>.Instance.ExpDungeonBattleList.GetStage(i).libraryOfRuinaStageType = y.libraryOfRuinaStageType;
            Singleton<StaticDataManager>.Instance.ExpDungeonBattleList.GetStage(i).sprName = y.sprName;
        }
        Log.LogInfo("Finished loading ExpStages");
        //Log.LogInfo("Clearing [jlist]");
        //jlist.Clear();
    }

    [HarmonyPatch(typeof(ResourceKeyBuilder), "BuildSdResourceKeyInfo")]
    //[HarmonyPatch(new Type[] {typeof(string), typeof(string), typeof(Type), typeof(bool) })]
    [HarmonyPostfix]
    public static void ResourceLogger(ResourceKeyBuilder.SdResourceType type, string id)
    {
        Log.LogWarning(type + ":" + id);
    }


    //static internal System.Collections.Generic.List<object> pss = new System.Collections.Generic.List<object>();
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

        if (id == -1)
        {
            var p = new PassiveAbility_1();
            p.Init(model, null, null);
            pss[sid].Add(p);
        }

        //var ps = Type.GetType($"PassiveAbility_1{Math.Abs(id)}");
        //if (ps != null)
        //{
        //    var p = (Activator.CreateInstance(ps) as PassiveAbility);
        //    p.Init(model, null, null);
        //    pss[sid].Add(p);
        //}
        //else
        //{
        //    Log.LogFatal($"{id} is null");
        //}
    }

    // reset passives + set the passive -1 to the abno part not unit!
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
        } else
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

    //internal static float bsid = 0f;

    //[HarmonyPatch(typeof(BuffDetail), "GetActionSlotAdder")]
    //[HarmonyPrefix]
    //public static bool Scaffold_GetActionSlotAdder_Patch(BuffDetail __instance, ref int __result)
    //{
    //    Log.LogFatal((new System.Diagnostics.StackTrace()).GetFrame(1).GetMethod().Name);
    //    int num = 3;

    //    //foreach (var ps in GetPassives(bsid: bsid))
    //    //{
    //    //    num += ps.GetActionSlotAdder();
    //    //}

    //    //foreach (BuffModel battleUnitBuff in __instance._grantedBuffList)
    //    //{
    //    //    if (battleUnitBuff.IsValid(0) && !battleUnitBuff.IsDestroyed())
    //    //    {
    //    //        num += battleUnitBuff.GetActionSlotAdder();
    //    //    }
    //    //}
    //    //__instance.CheckBuffsDestroyed();
    //    __result = num;
    //    return false;
    //}

    //[HarmonyPatch(typeof(PassiveModel), MethodType.Constructor, new Type[] { typeof(PassiveStaticData) })]
    //[HarmonyPrefix]
    //public static bool CustomPassivePatcher(PassiveModel __instance, PassiveStaticData info)
    //{
    //    foreach (var x in System.Reflection.Assembly.GetExecutingAssembly().GetTypes())
    //    {
    //        Log.LogInfo(x.Name);
    //    }
    //    if (1 == 1)
    //    {
    //        //Type type = Type.GetType("PassiveAbility" + "_" + info.ID.ToString());
    //        Type type = Type.GetType("PassiveAbility_9991001");
    //        Log.LogFatal(type.FullName);
    //        Log.LogFatal(type.GetMethods().Count());
    //        if (type != null)
    //        {
    //            PassiveAbility script = (Activator.CreateInstance(type) as PassiveAbility);
    //            __instance._script = script;
    //            __instance._script._id = info.id;
    //        }
    //        else
    //        {
    //            Log.LogFatal("Passive is null");
    //        }
    //        __instance._classInfo = info;
    //        return false;
    //    }
    //    Log.LogFatal(info.id);
    //    return true;
    //}




    //[HarmonyPatch(typeof(PassiveModel), "Init")]
    //[HarmonyPrefix]
    //public static void CustomPassivePatcher2(PassiveModel __instance, BattleUnitModel owner)
    //{
    //    foreach (var x in System.Reflection.Assembly.GetExecutingAssembly().GetTypes())
    //    {
    //        Log.LogInfo(x.Name);
    //    }
    //    {
    //        //Type type = Type.GetType("PassiveAbility" + "_" + info.ID.ToString());
    //        PassiveAbility type = new PassiveAbility_9991001();
    //        Log.LogFatal(type == null);

    //        Log.LogFatal("Passive 3333");

    //        Log.LogFatal("Passive 4444444");

    //            Log.LogFatal("Passive 55555555");

    //        if (type != null)
    //        {
    //            Log.LogFatal("Passive 1111");

    //            PassiveAbility script = (type as PassiveAbility);
    //            __instance._script = script;
    //            __instance._script._id = 9991001;
    //            Log.LogFatal("Passive 222222");

    //        }
    //        else
    //        {
    //            Log.LogFatal("Passive is null");
    //        }
    //        //__instance._classInfo.id = 9991001;
    //    }
    //}

    //[HarmonyPatch(typeof(HttpRequestCommand<ResPacket_NULL, ResPacket_NULL>), "OnResponse")]
    //[HarmonyPostfix]
    //public static void ResponseLogger(string responseJson)
    //{
    //    Log.LogInfo($"Response: {responseJson}");
    //    //Log.LogFatal($"LoadingAsset: {label}/{resourceId}");
    //}

    //[HarmonyPatch(typeof(AddressableManager), "LoadAssetSync")]
    //[HarmonyPostfix]
    //public static void lol(string label, string resourceId)
    //{
    //    Log.LogFatal($"LoadingAsset: {label}/{resourceId}");
    //}

    [HarmonyPatch(typeof(BattleUnitView), "Init")]
    [HarmonyPostfix]
    public static void UniversalSkinPatcher(BattleUnitView __instance, BattleUnitModel model, int instanceID, int level, int gaksungLevel)
    {

        int id = model.UnitDataModel.ClassInfo.ID;
        if (aplist.ContainsKey(id))
        {
            (string, string) apd = aplist[id];
            {
                CharacterAppearance characterAppearance = null;
                string appearanceID = apd.Item2;
                GameObject item = SingletonBehavior<AddressableManager>.Instance.LoadAssetSync<GameObject>(apd.Item1, apd.Item2, __instance.skinPivot, null).Item1;
                if (item == null)
                {
                    return;
                }
                if (item != null)
                {
                    foreach (var x in __instance._appearances)
                    {
                        x.gameObject.SetActive(false);
                    }
                    __instance._appearances = new Il2CppSystem.Collections.Generic.List<CharacterAppearance>();
                    characterAppearance = item.GetComponent<CharacterAppearance>();
                    if (characterAppearance != null)
                    {
                        characterAppearance.Initialize(__instance);
                        characterAppearance.charInfo.appearanceID = appearanceID;
                    }
                }
                //CharacterAppearance characterAppearance = SDCharacterSkinUtil.CreateSkin(__instance, model, __instance.skinPivot);
                if (characterAppearance == null)
                {
                    Debug.LogError(model.GetAppearanceID() + " is not exist");
                }
                else
                {
                    __instance._appearances.Add(characterAppearance);
                }
                __instance._curAppearance = (__instance._mainAppearance = __instance._appearances[0]);
                foreach (var x in __instance._appearances)
                {
                    if (__instance._curAppearance != null)
                    {
                        __instance._curAppearance.gameObject.SetActive(false);
                    }
                    __instance._curAppearance = characterAppearance;
                    __instance._curAppearance.gameObject.SetActive(true);
                    __instance._curAppearance.ChangeMotion(MOTION_DETAIL.Idle, false, -1, false, null);
                    __instance._curAppearance.ChangeDefaultSpineRenderer(true, false);
                    __instance._mainAppearance = __instance._curAppearance;
                    __instance.UnitCollition.Init(__instance, __instance._curAppearance.charInfo.character_weight);
                    __instance.UIManager.Init_ChangeAppearance(__instance._unitModel, __instance);
                    __instance._viewShadow.SetSize(__instance._curAppearance.charInfo.character_radius);
                    __instance.RefreshEffects();


                    x.Init_Spine(characterAppearance);
                    x.ChangeDefaultSpineRenderer(true, false);
                }
                __instance._curAppearance.SortRenderQueue(3000);
            }
        }
        else
        {
            foreach (var x in __instance._appearances)
            {
                Log.LogInfo(x.name);
            }
        }
        //return true;
    }


    //public static void AddEgoGiftsToBattleUnit<T>(T __instance)
    //{
    //    Log.LogFatal(__instance.GetType());
    //    BattleUnitModel _i = __instance as BattleUnitModel;

    //    if (__instance is BattleUnitModel_Abnormality)
    //    {
    //        foreach ((int eid, int pr, int id) in egolist["Abnormality"])
    //        {
    //            _i.AddEgoGiftAbility(Singleton<StaticDataManager>.Instance.EgoGiftDataMediator.GetEgoGiftAbilityById(eid, pr));
    //        }
    //    }
    //    else if (__instance is BattleUnitModel_Abnormality_Part)
    //    {
    //        foreach ((int eid, int pr) in egolist["Abnormality_Part"])
    //        {
    //            _i.AddEgoGiftAbility(Singleton<StaticDataManager>.Instance.EgoGiftDataMediator.GetEgoGiftAbilityById(eid, pr));
    //        }
    //    }
    //    else if (__instance is BattleUnitModel_Ally)
    //    {
    //        foreach ((int eid, int pr) in egolist["Ally"])
    //        {
    //            _i.AddEgoGiftAbility(Singleton<StaticDataManager>.Instance.EgoGiftDataMediator.GetEgoGiftAbilityById(eid, pr));
    //        }
    //    }
    //    else if (__instance is BattleUnitModel_Assistant)
    //    {
    //        foreach ((int eid, int pr) in egolist["Assistant"])
    //        {
    //            _i.AddEgoGiftAbility(Singleton<StaticDataManager>.Instance.EgoGiftDataMediator.GetEgoGiftAbilityById(eid, pr));
    //        }
    //    }
    //    else if (__instance is BattleUnitModel_Enemy)
    //    {
    //        foreach ((int eid, int pr) in egolist["Enemy"])
    //        {
    //            _i.AddEgoGiftAbility(Singleton<StaticDataManager>.Instance.EgoGiftDataMediator.GetEgoGiftAbilityById(eid, pr));
    //        }
    //    }
    //    else if (__instance is BattleUnitModel_Opponent)
    //    {
    //        foreach ((int eid, int pr) in egolist["Opponent"])
    //        {
    //            _i.AddEgoGiftAbility(Singleton<StaticDataManager>.Instance.EgoGiftDataMediator.GetEgoGiftAbilityById(eid, pr));
    //        }
    //    }
    //    else if (__instance is BattleUnitModel_Player)
    //    {
    //        foreach ((int eid, int pr) in egolist["Player"])
    //        {
    //            _i.AddEgoGiftAbility(Singleton<StaticDataManager>.Instance.EgoGiftDataMediator.GetEgoGiftAbilityById(eid, pr));
    //        }
    //    }
    //}


    //[HarmonyPatch(typeof(PlayerUnitSpriteList), "GetEgoCGData")]
    //[HarmonyPrefix]
    //public static bool EgoAlephFixer(Sprite __result, string cgId, SKILL_TYPE egoSkillType = SKILL_TYPE.EGO_AWAKEN)
    //{
    //    if (cgId == "20499")
    //    {
    //        cgId = "20307";
    //    }

    //    if (egoSkillType == SKILL_TYPE.EGO_EROSION)
    //    {
    //        __result = SingletonBehavior<AddressableManager>.Instance.LoadAssetSync<Sprite>("Unit_EgoCG", cgId + "_e_cg", null, null).Item1;
    //        return false;
    //    }
    //    __result = SingletonBehavior<AddressableManager>.Instance.LoadAssetSync<Sprite>("Unit_EgoCG", cgId + "_cg", null, null).Item1;
    //    return false;

    //    //Log.LogFatal(cgId);
    //    //if (cgId == "20499")
    //    //{
    //    //    cgId.Replace("20499", "20307");
    //    //}
    //    //return true;
    //}

    [HarmonyPatch(typeof(BattleUnitModel), "Init")]
    [HarmonyPostfix]
    public static void egogift(BattleUnitModel __instance)
    {
        if (__instance.GetCharacterID() != -1)
        {
            foreach ((int eid, int pr, int id) in egolist["Player"])
            {
                if (id != 0 && id == __instance.UnitDataModel._classInfo.ID)
                {
                    __instance.AddEgoGiftAbility(Singleton<StaticDataManager>.Instance.EgoGiftDataMediator.GetEgoGiftAbilityById(eid, pr));
                }
                else if (id == 0)
                {
                    __instance.AddEgoGiftAbility(Singleton<StaticDataManager>.Instance.EgoGiftDataMediator.GetEgoGiftAbilityById(eid, pr));
                }
            }
        }
        else
        {
            foreach ((int eid, int pr, int id) in egolist["Enemy"])
            {
                if (id != 0 && id == __instance.UnitDataModel._classInfo.ID)
                {
                    __instance.AddEgoGiftAbility(Singleton<StaticDataManager>.Instance.EgoGiftDataMediator.GetEgoGiftAbilityById(eid, pr));
                }
                else if (id == 0)
                {
                    __instance.AddEgoGiftAbility(Singleton<StaticDataManager>.Instance.EgoGiftDataMediator.GetEgoGiftAbilityById(eid, pr));
                }
            }
        }
    }

    [HarmonyPatch(typeof(StageController), "InitStage")]
    [HarmonyPrefix]
    public static void DungeonFixer(StageController __instance)
    {
        if (__instance.StageModel.ClassInfo.ID == 1 || __instance.StageModel.ClassInfo.ID == 2 || __instance.StageModel.ClassInfo.ID == 3)
        {
            DungeonProgressManager._isOnDungeon = true;
            DungeonProgressManager._progressBridge = new MirrorDungeonProgressBridge
            {
                _egoGiftManager = new EgoGiftManager(),
                _units = new Il2CppSystem.Collections.Generic.List<DungeonUnitModel>(),
            };

            Singleton<UserDataManager>.Instance._mirrorDungeonSaveDataManager = new UserMirrorDungeonSaveDataManager();
            //var lol = new Il2CppSystem.Collections.Generic.List<int>();
            //lol.Add(10109);
            //DungeonProgressManager._progressBridge.EgoGiftManager._egoGiftList.Add(new AcquiredEgoGift(9004, 0, lol));
            var gifts = new Il2CppSystem.Collections.Generic.List<DungeonMapEgoGift>();

            //gifts.Add(new DungeonMapEgoGift(new DungeonMapEgoGiftFormat(1))); // Phlebotomy Pack - only works via old method


            gifts.Add(new DungeonMapEgoGift(new DungeonMapEgoGiftFormat(9004))); // Phlebotomy Pack - only works via old method
            gifts.Add(new DungeonMapEgoGift(new DungeonMapEgoGiftFormat(9014))); // Phlebotomy Pack - only works via old method
            gifts.Add(new DungeonMapEgoGift(new DungeonMapEgoGiftFormat(9017))); // Phlebotomy Pack - only works via old method
            gifts.Add(new DungeonMapEgoGift(new DungeonMapEgoGiftFormat(9021))); // Phlebotomy Pack - only works via old method
            gifts.Add(new DungeonMapEgoGift(new DungeonMapEgoGiftFormat(9058))); // Phlebotomy Pack - only works via old method
            gifts.Add(new DungeonMapEgoGift(new DungeonMapEgoGiftFormat(9068))); // Phlebotomy Pack - only works via old method
            gifts.Add(new DungeonMapEgoGift(new DungeonMapEgoGiftFormat(9118))); // Phlebotomy Pack - only works via old method
            gifts.Add(new DungeonMapEgoGift(new DungeonMapEgoGiftFormat(9153))); // Phlebotomy Pack - only works via old method
            gifts.Add(new DungeonMapEgoGift(new DungeonMapEgoGiftFormat(9419))); // Phlebotomy Pack - only works via old method
            gifts.Add(new DungeonMapEgoGift(new DungeonMapEgoGiftFormat(9423))); // Phlebotomy Pack - only works via old method
            gifts.Add(new DungeonMapEgoGift(new DungeonMapEgoGiftFormat(9710))); // Phlebotomy Pack - only works via old method
            gifts.Add(new DungeonMapEgoGift(new DungeonMapEgoGiftFormat(9739))); // Phlebotomy Pack - only works via old method
            gifts.Add(new DungeonMapEgoGift(new DungeonMapEgoGiftFormat(9154))); // Phlebotomy Pack - only works via old method


            //gifts.Add(new DungeonMapEgoGift(9004)); // Phlebotomy Pack - only works via old method
            //gifts.Add(new DungeonMapEgoGift(9014)); // Rusty Commemorative Coin
            //gifts.Add(new DungeonMapEgoGift(9017)); // Lithograph
            //gifts.Add(new DungeonMapEgoGift(9021)); // Blue Zippo Lighter
            //gifts.Add(new DungeonMapEgoGift(9058)); // Disk Fragment
            //gifts.Add(new DungeonMapEgoGift(9068)); // Grand Welcome
            //gifts.Add(new DungeonMapEgoGift(9118)); // Bone Stake
            //gifts.Add(new DungeonMapEgoGift(9153)); // Oracle
            //gifts.Add(new DungeonMapEgoGift(9419)); // Spicebush Branch
            //gifts.Add(new DungeonMapEgoGift(9423)); // Broken Glasses
            //gifts.Add(new DungeonMapEgoGift(9710)); // Huge Gift Sack
            //gifts.Add(new DungeonMapEgoGift(9739)); // Crystallized Blood

            //gifts.Add(new DungeonMapEgoGift(9154)); // imposed weight

            DungeonProgressManager._progressBridge.EgoGiftManager.SetEgoGiftList(gifts, false);
            //Log.LogFatal(DungeonProgressManager._progressBridge.EgoGiftManager._egoGiftList[0].EgoGiftID);

            Log.LogFatal($"DungeonFixer: {Singleton<StageController>.Instance.IsDungeonOn}");
            Log.LogFatal($"DungeonFixer: {DungeonProgressManager.isOnDungeon}");
            Log.LogFatal($"DungeonFixer: {DungeonProgressManager.IsMirrorDungeon}");
            Log.LogFatal($"DungeonFixer: {DungeonProgressManager.IsRailwayDungeon}");
            _egostage = true;
        }
        //else if (__instance.StageModel.ClassInfo.ID == 3)
        //{
        //    DungeonProgressManager._isOnDungeon = true;
        //    DungeonProgressManager._progressBridge = new RailwayDungeonProgressBridge
        //    {
        //        _egoGiftManager = new EgoGiftManager(),
        //        _units = new Il2CppSystem.Collections.Generic.List<DungeonUnitModel>(),
        //    };

        //    Singleton<UserDataManager>.Instance._mirrorDungeonSaveDataManager = new UserMirrorDungeonSaveDataManager();
        //    //var lol = new Il2CppSystem.Collections.Generic.List<int>();
        //    //lol.Add(10109);
        //    //DungeonProgressManager._progressBridge.EgoGiftManager._egoGiftList.Add(new AcquiredEgoGift(9004, 0, lol));
        //    var gifts = new Il2CppSystem.Collections.Generic.List<DungeonMapEgoGift>();
        //    //gifts.Add(new DungeonMapEgoGift(9004)); // Phlebotomy Pack - only works via old method
        //    gifts.Add(new DungeonMapEgoGift(9014)); // Rusty Commemorative Coin
        //    gifts.Add(new DungeonMapEgoGift(9017)); // Lithograph
        //    gifts.Add(new DungeonMapEgoGift(9021)); // Blue Zippo Lighter
        //    gifts.Add(new DungeonMapEgoGift(9058)); // Disk Fragment
        //    gifts.Add(new DungeonMapEgoGift(9068)); // Grand Welcome
        //    gifts.Add(new DungeonMapEgoGift(9118)); // Bone Stake
        //    gifts.Add(new DungeonMapEgoGift(9153)); // Oracle
        //    gifts.Add(new DungeonMapEgoGift(9419)); // Spicebush Branch
        //    gifts.Add(new DungeonMapEgoGift(9423)); // Broken Glasses
        //    gifts.Add(new DungeonMapEgoGift(9710)); // Huge Gift Sack
        //    gifts.Add(new DungeonMapEgoGift(9739)); // Crystallized Blood

        //    gifts.Add(new DungeonMapEgoGift(9154)); // imposed weight

        //    DungeonProgressManager._progressBridge.EgoGiftManager.SetEgoGiftList(gifts, false);
        //    //Log.LogFatal(DungeonProgressManager._progressBridge.EgoGiftManager._egoGiftList[0].EgoGiftID);

        //    Log.LogFatal($"DungeonFixer: {Singleton<StageController>.Instance.IsDungeonOn}");
        //    Log.LogFatal($"DungeonFixer: {DungeonProgressManager.isOnDungeon}");
        //    Log.LogFatal($"DungeonFixer: {DungeonProgressManager.IsMirrorDungeon}");
        //    Log.LogFatal($"DungeonFixer: {DungeonProgressManager.IsRailwayDungeon}");
        //    _egostage = true;
        //}
    }

    //[HarmonyPatch(typeof(MirrorDungeonProgressBridge), "GetAdditionalEgoGiftAbilityNames")]
    //[HarmonyPrefix]
    //public static bool DungeonFixer77(Il2CppSystem.Collections.Generic.List<EgoGiftAbilityNameData> __result)
    //{
    //    Log.LogInfo("DungeonFixer77");
    //    __result = new Il2CppSystem.Collections.Generic.List<EgoGiftAbilityNameData>();
    //    __result.Add(new EgoGiftAbilityNameData("hi lol", EGO_GIFT_ABILITY_NAME_TYPES.ITEM));
    //    return true;
    //}

    [HarmonyPatch(typeof(BattleUIRoot), "Init")]
    [HarmonyPrefix]
    public static void DungeonFixer1()
    {
        if (_egostage)
        {
            Log.LogInfo("DungeonFixer1");
            DungeonProgressManager._isOnDungeon = false;
        }
        //return false;
    }

    [HarmonyPatch(typeof(StageController), "CreateAllyUnits")]
    [HarmonyPrefix]
    public static void DungeonFixer2()
    {
        if (_egostage)
        {
            Log.LogInfo("DungeonFixer2");
            DungeonProgressManager._isOnDungeon = false;
        }
        //return false;
    }

    [HarmonyPatch(typeof(VoiceGenerator), "Init_Battle")]
    [HarmonyPrefix]
    public static void DungeonFixer3()
    {
        if (_egostage)
        {
            Log.LogInfo("DungeonFixer3");
            DungeonProgressManager._isOnDungeon = true;
        }
        //return false;
    }
    [HarmonyPatch(typeof(GlobalGameManager), "LeaveStage")]
    [HarmonyPrefix]
    public static void DungeonFixer4()
    {
        pss.Clear();
        if (_egostage)
        {
            Log.LogInfo("DungeonFixer4");
            DungeonProgressManager._isOnDungeon = false;
            DungeonProgressManager.ClearData();
            _egostage = false;
        }
        //return false;
    }


    [HarmonyPatch(typeof(MainLobbyUIPanel), "Initialize")]
    [HarmonyPostfix]
    public static void PostMainUIPatch()
    {
        if (!patched)
        {
            Log.LogFatal(JailbreakChecker.IsJailbroken());
            Log.LogFatal(RootJailbreakChecker.IsDeviceRootedOrJailbroken());
            Log.LogFatal(Singleton<ServerSelector>.Instance.GetServerURL());
            Log.LogFatal(Singleton<ServerSelector>.Instance.GetBattleLogServerURL());
            Log.LogFatal(Singleton<ServerSelector>.Instance.IsEnablePacketCrypt());
            Log.LogFatal(Singleton<ServerSelector>.Instance.IsEnableBattleLogPacketCrypt());


            //var nd = new EnemyData { 
            // isHide = false,
            //  unitCount = 1,
            //   unitID = 97712,
            //    unitLevel = 45,
            //};
            //var nd1 = new EnemyData
            //{
            //    isHide = false,
            //    unitCount = 1,
            //    unitID = 71012,
            //    unitLevel = 45,
            //};

            //Singleton<StaticDataManager>.Instance.GetStage(10718).waveList[0].GetEnemyDataList().Add(nd);
            //Singleton<StaticDataManager>.Instance.GetStage(10718).waveList[0].GetEnemyDataList().Add(nd1);


            //{
            //    var total = "[";
            //    foreach (var x in Singleton<TextDataSet>.Instance.EgoGiftData.GetList())
            //    {
            //        total += JsonUtility.ToJson(x);
            //        total += ",";
            //    }
            //    total += "]";
            //    File.WriteAllText(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "Text_EgoGiftData.json", total);
            //}

            //{
            //    var total = "[";
            //    foreach (var x in Singleton<TextDataSet>.Instance.EgoGiftCategory.GetList())
            //    {
            //        total += JsonUtility.ToJson(x);
            //        total += ",";
            //    }
            //    total += "]";
            //    File.WriteAllText(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "Text_EgoGiftCategory.json", total);
            //}

            //{
            //    var total = "[";
            //    foreach (var x in Singleton<TextDataSet>.Instance.MirrorDungeonEgoGiftLockedDescList.GetList())
            //    {
            //        total += JsonUtility.ToJson(x);
            //        total += ",";
            //    }
            //    total += "]";
            //    File.WriteAllText(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "Text_MirrorDungeonEgoGiftLockedDescList.json", total);
            //}

            //{
            //    var total = "[";
            //    foreach (var x in Singleton<StaticDataManager>.Instance.ExpDungeonBattleList.list)
            //    {
            //        total += JsonUtility.ToJson(x);
            //        total += ",";
            //    }
            //    total += "]";
            //    File.WriteAllText(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "Static_ExpDungeonBattleList.json", total);
            //}

            //{
            //    var total = "[";
            //    foreach (var x in Singleton<StaticDataManager>.Instance.ExpDungeonDataList.list)
            //    {
            //        total += JsonUtility.ToJson(x);
            //        total += ",";
            //    }
            //    total += "]";
            //    File.WriteAllText(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "Static_ExpDungeonDataList.json", total);
            //}

            //{
            //    var total = "[";
            //    foreach (var x in Singleton<StaticDataManager>.Instance.ThreadDungeonBattleList.list)
            //    {
            //        total += JsonUtility.ToJson(x);
            //        total += ",";
            //    }
            //    total += "]";
            //    File.WriteAllText(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "Static_ThreadDungeonBattleList.json", total);
            //}

            //{
            //    var total = "[";
            //    foreach (var x in Singleton<StaticDataManager>.Instance.ThreadDungeonDataList.list)
            //    {
            //        total += JsonUtility.ToJson(x);
            //        total += ",";
            //    }
            //    total += "]";
            //    File.WriteAllText(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "Static_ThreadDungeonDataList.json", total);
            //}


            ////var nd3 = new EnemyData
            ////{
            ////    isHide = false,
            ////    unitCount = 1,
            ////    unitID = 8742,
            ////    unitLevel = 45,
            ////};
            ////var nd4 = new EnemyData
            ////{
            ////    isHide = false,
            ////    unitCount = 1,
            ////    unitID = 8012,
            ////    unitLevel = 45,
            ////};

            ////Singleton<StaticDataManager>.Instance.ExpDungeonBattleList.list[0].stageType = "Abnormality";
            ////Singleton<StaticDataManager>.Instance.ExpDungeonBattleList.list[0].waveList[0].GetEnemyDataList().Clear();
            ////Singleton<StaticDataManager>.Instance.ExpDungeonBattleList.list[0].waveList[1].GetEnemyDataList().Clear();
            ////Singleton<StaticDataManager>.Instance.ExpDungeonBattleList.list[0].waveList[0].GetEnemyDataList().Add(nd3);
            ////Singleton<StaticDataManager>.Instance.ExpDungeonBattleList.list[0].waveList[1].GetEnemyDataList().Add(nd4);


            //{
            //    var total = "[";
            //    foreach (var x in Singleton<StaticDataManager>.Instance.EnemyUnitList.list)
            //    {
            //        total += JsonUtility.ToJson(x);
            //        total += ",";
            //    }
            //    total += "]";
            //    File.WriteAllText(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "Static_EnemyUnitList.json", total);
            //}

            //{
            //    var total = "[";
            //    foreach (var x in Singleton<TextDataSet>.Instance.EnemyList.GetList())
            //    {
            //        total += JsonUtility.ToJson(x);
            //        total += ",";
            //    }
            //    total += "]";
            //    File.WriteAllText(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "Text_EnemyList.json", total);
            //}


            //{
            //    var total = "[";
            //    foreach (var x in Singleton<StaticDataManager>.Instance.AbnormalityUnitList.GetList())
            //    {
            //        total += JsonUtility.ToJson(x);
            //        total += ",";
            //    }
            //    total += "]";
            //    File.WriteAllText(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "Static_AbnormalityUnitList.json", total);
            //}


            //{
            //    var total = "[";
            //    foreach (var x in Singleton<TextDataSet>.Instance.AbnormalityContentData.GetList())
            //    {
            //        total += JsonUtility.ToJson(x);
            //        total += ",";
            //    }
            //    total += "]";
            //    File.WriteAllText(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "Text_AbnormalityContentData.json", total);
            //}

            //{
            //    var total = "[";
            //    foreach (var x in Singleton<StaticDataManager>.Instance.SkillList.GetList())
            //    {
            //        total += JsonUtility.ToJson(x);
            //        total += ",";
            //    }
            //    total += "]";
            //    File.WriteAllText(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "Static_SkillList.json", total);
            //}

            //{
            //    var total = "[";
            //    foreach (var x in Singleton<TextDataSet>.Instance.SkillList.GetList())
            //    {
            //        total += JsonUtility.ToJson(x);
            //        total += ",";
            //    }
            //    total += "]";
            //    File.WriteAllText(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "Text_SkillList.json", total);
            //}

            //{
            //    var total = "[";
            //    foreach (var x in Singleton<StaticDataManager>.Instance.EgoList.GetList())
            //    {
            //        total += JsonUtility.ToJson(x);
            //        total += ",";
            //    }
            //    total += "]";
            //    File.WriteAllText(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "Static_EgoList.json", total);
            //}

            //{
            //    var total = "[";
            //    foreach (var x in Singleton<TextDataSet>.Instance.EgoList.GetList())
            //    {
            //        total += JsonUtility.ToJson(x);
            //        total += ",";
            //    }
            //    total += "]";
            //    File.WriteAllText(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "Text_EgoList.json", total);
            //}


            //{
            //    var total = "[";
            //    foreach (var x in Singleton<StaticDataManager>.Instance.storyBattleStageList.GetList())
            //    {
            //        total += JsonUtility.ToJson(x);
            //        total += ",";
            //    }
            //    total += "]";
            //    File.WriteAllText(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "Static_storyBattleStageList.json", total);
            //}

            //{
            //    var total = "[";
            //    foreach (var x in Singleton<TextDataSet>.Instance.StoryTheater.GetList())
            //    {
            //        total += JsonUtility.ToJson(x);
            //        total += ",";
            //    }
            //    total += "]";
            //    File.WriteAllText(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "Text_StoryTheater.json", total);
            //}


            //{
            //    var total = "[";
            //    foreach (var x in Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetList())
            //    {
            //        total += JsonUtility.ToJson(x);
            //        total += ",";
            //    }
            //    total += "]";
            //    File.WriteAllText(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "Static_PersonalityStaticDataList.json", total);
            //}

            //{
            //    var total = "[";
            //    foreach (var x in Singleton<TextDataSet>.Instance.PersonalityList.GetList())
            //    {
            //        total += JsonUtility.ToJson(x);
            //        total += ",";
            //    }
            //    total += "]";
            //    File.WriteAllText(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "Text_PersonalityList.json", total);
            //}

            //{
            //    var total = "[";
            //    foreach (var x in Singleton<StaticDataManager>.Instance.AssistantUnitList.GetList())
            //    {
            //        total += JsonUtility.ToJson(x);
            //        total += ",";
            //    }
            //    total += "]";
            //    File.WriteAllText(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "Static_AssistantUnitList.json", total);
            //}

            //{
            //    var total = "[";
            //    foreach (var x in Singleton<StaticDataManager>.Instance.PassiveList.GetList())
            //    {
            //        total += JsonUtility.ToJson(x);
            //        total += ",";
            //    }
            //    total += "]";
            //    File.WriteAllText(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "Static_PassiveList.json", total);
            //}

            //{
            //    var total = "[";
            //    foreach (var x in Singleton<StaticDataManager>.Instance.PersonalityPassiveList.GetList())
            //    {
            //        total += JsonUtility.ToJson(x);
            //        total += ",";
            //    }
            //    total += "]";
            //    File.WriteAllText(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "Static_PersonalityPassiveList.json", total);
            //}

            //{
            //    var total = "[";
            //    foreach (var x in Singleton<TextDataSet>.Instance.PassiveList.GetList())
            //    {
            //        total += JsonUtility.ToJson(x);
            //        total += ",";
            //    }
            //    total += "]";
            //    File.WriteAllText(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "Text_PassiveList.json", total);
            //}

            //{
            //    var total = "[";
            //    foreach (var x in Singleton<StaticDataManager>.Instance.RailwayDungeonBattleList.GetList())
            //    {
            //        total += JsonUtility.ToJson(x);
            //        total += ",";
            //    }
            //    total += "]";
            //    File.WriteAllText(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "Static_RailwayDungeonBattleList.json", total);
            //}

            //{
            //    var total = "[";
            //    foreach (var x in Singleton<StaticDataManager>.Instance.RailwayDungeonBuffDataList.GetList())
            //    {
            //        total += JsonUtility.ToJson(x);
            //        total += ",";
            //    }
            //    total += "]";
            //    File.WriteAllText(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "Static_RailwayDungeonBuffDataList.json", total);
            //}

            //{
            //    var total = "[";
            //    foreach (var x in Singleton<StaticDataManager>.Instance.RailwayDungeonDataList.GetList())
            //    {
            //        total += JsonUtility.ToJson(x);
            //        total += ",";
            //    }
            //    total += "]";
            //    File.WriteAllText(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "Static_RailwayDungeonDataList.json", total);
            //}

            //{
            //    var total = "[";
            //    foreach (var x in Singleton<StaticDataManager>.Instance.AbnormalityPartList.GetList())
            //    {
            //        total += JsonUtility.ToJson(x);
            //        total += ",";
            //    }
            //    total += "]";
            //    File.WriteAllText(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "Static_AbnormalityPartList.json", total);
            //}

            //{
            //    var total = "[";
            //    foreach (var x in Singleton<StaticDataManager>.Instance.partList.list)
            //    {
            //        total += JsonUtility.ToJson(x);
            //        total += ",";
            //    }
            //    total += "]";
            //    File.WriteAllText(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "Static_partList.json", total);
            //}
            //{
            //    var total = "[";
            //    foreach (var x in Singleton<TextDataSet>.Instance.RailwayDungeonBuffText.GetList())
            //    {
            //        total += JsonUtility.ToJson(x);
            //        total += ",";
            //    }
            //    total += "]";
            //    File.WriteAllText(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "Text_RailwayDungeonBuffText.json", total);
            //}

            //{
            //    var total = "[";
            //    foreach (var x in Singleton<TextDataSet>.Instance.RailwayDungeonNodeText.GetList())
            //    {
            //        total += JsonUtility.ToJson(x);
            //        total += ",";
            //    }
            //    total += "]";
            //    File.WriteAllText(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "Text_RailwayDungeonNodeText.json", total);
            //}

            //{
            //    var total = "[";
            //    foreach (var x in Singleton<TextDataSet>.Instance.RailwayDungeonStationName.GetList())
            //    {
            //        total += JsonUtility.ToJson(x);
            //        total += ",";
            //    }
            //    total += "]";
            //    File.WriteAllText(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "Text_RailwayDungeonStationName.json", total);
            //}

            //{
            //    var total = "[";
            //    foreach (var x in Singleton<TextDataSet>.Instance.RailwayDungeonText.GetList())
            //    {
            //        total += JsonUtility.ToJson(x);
            //        total += ",";
            //    }
            //    total += "]";
            //    File.WriteAllText(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "Text_RailwayDungeonText.json", total);
            //}



            //{
            //    var total = "[";
            //    foreach (var x in Singleton<TextDataSet>.Instance.stageChapter.GetList())
            //    {
            //        total += JsonUtility.ToJson(x);
            //        total += ",";
            //    }
            //    total += "]";
            //    File.WriteAllText(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "Text_stageChapter.json", total);
            //}

            //{
            //    var total = "[";
            //    foreach (var x in Singleton<TextDataSet>.Instance.stagePart.GetList())
            //    {
            //        total += JsonUtility.ToJson(x);
            //        total += ",";
            //    }
            //    total += "]";
            //    File.WriteAllText(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "Text_stagePart.json", total);
            //}

            //Log.LogFatal(1);
            //{
            //    var total = "[";
            //    foreach (var x in Singleton<TextDataSet>.Instance.StageNodeText.GetList())
            //    {
            //        total += JsonUtility.ToJson(x);
            //        total += ",";
            //    }
            //    total += "]";
            //    File.WriteAllText(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "Text_StageNodeText.json", total);
            //}
            //Log.LogFatal(2);
            //Log.LogFatal(1);
            //{
            //    var total = "[";
            //    foreach (var x in Singleton<StaticDataManager>.Instance.StageNodeRewardList.GetList())
            //    {
            //        total += JsonUtility.ToJson(x);
            //        total += ",";
            //    }
            //    total += "]";
            //    File.WriteAllText(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "Static_StageNodeRewardList.json", total);
            //}
            //Log.LogFatal(1);
            //{
            //    var total = "[";
            //    foreach (var x in Singleton<StaticDataManager>.Instance.abBattleStageList.GetList())
            //    {
            //        total += JsonUtility.ToJson(x);
            //        total += ",";
            //    }
            //    total += "]";
            //    File.WriteAllText(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "Static_abBattleStageList.json", total);
            //}
            //Log.LogFatal(1);
            //{
            //    var total = "[";
            //    foreach (var x in Singleton<StaticDataManager>.Instance.dungeonBattleStageList.GetList())
            //    {
            //        total += JsonUtility.ToJson(x);
            //        total += ",";
            //    }
            //    total += "]";
            //    File.WriteAllText(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "Static_dungeonBattleStageList.json", total);
            //}
            //Log.LogFatal(1);
            ////{
            ////    var total = "[";
            ////    foreach (var x in Singleton<StaticDataManager>.Instance.MainStageMapSettingList.GetList())
            ////    {
            ////        total += JsonUtility.ToJson(x);
            ////        total += ",";
            ////    }
            ////    total += "]";
            ////    File.WriteAllText(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "Static_MainStageMapSettingList.json", total);
            ////}
            //Log.LogFatal(1);
            //{
            //    var total = "[";
            //    foreach (var x in Singleton<StaticDataManager>.Instance.mirrorDungeonBattleStageList.GetList())
            //    {
            //        total += JsonUtility.ToJson(x);
            //        total += ",";
            //    }
            //    total += "]";
            //    File.WriteAllText(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "Static_mirrorDungeonBattleStageList.json", total);
            //}
            //{
            //    var total = "[";
            //    foreach (var x in Singleton<StaticDataManager>.Instance.storyBattleStageList.GetList())
            //    {
            //        total += JsonUtility.ToJson(x);
            //        total += ",";
            //    }
            //    total += "]";
            //    File.WriteAllText(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "Static_storyBattleStageList.json", total);
            //}


            //{
            //    var total = "[";
            //    foreach (var x in Singleton<StaticDataManager>.Instance.BuffList.GetList())
            //    {
            //        total += JsonUtility.ToJson(x);
            //        total += ",";
            //    }
            //    total += "]";
            //    File.WriteAllText(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "Static_BuffList.json", total);
            //}



            //{
            //    var total = "[";
            //    foreach (var x in Singleton<StaticDataManager>.Instance.DoubleBuffList.GetList())
            //    {
            //        total += JsonUtility.ToJson(x);
            //        total += ",";
            //    }
            //    total += "]";
            //    File.WriteAllText(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "Static_DoubleBuffList.json", total);
            //}
            //{
            //    var total = "[";
            //    foreach (var x in Singleton<TextDataSet>.Instance.BuffAbilityList.GetList())
            //    {
            //        total += JsonUtility.ToJson(x);
            //        total += ",";
            //    }
            //    total += "]";
            //    File.WriteAllText(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "Text_BuffAbilityList.json", total);
            //}

            Log.LogMessage("Running MainUI Patches");
            PrepareSkillFromLocalJson("redmist/w_spear.json");
            PrepareSkillFromLocalJson("redmist/w_level.json");
            PrepareSkillFromLocalJson("redmist/w_upstanding.json");
            PrepareSkillFromLocalJson("redmist/w_focus.json");
            PrepareSkillFromLocalJson("redmist/w_gsh.json");
            PrepareSkillFromLocalJson("redmist/w_gsv.json");

            PrepareSkillFromLocalJson("xiao/w_aow.json");
            PrepareSkillFromLocalJson("xiao/w_fds.json");
            PrepareSkillFromLocalJson("xiao/w_fe.json");
            PrepareSkillFromLocalJson("xiao/w_coh.json");
            PrepareSkillFromLocalJson("xiao/w_rv.json");

            PrepareSkillFromLocalJson("sancho/S1/w_joy.json");
            PrepareSkillFromLocalJson("sancho/S1/w_sun.json");
            PrepareSkillFromLocalJson("sancho/S2/w_impl.json");
            PrepareSkillFromLocalJson("sancho/S2/w_track.json");
            PrepareSkillFromLocalJson("sancho/S3/w_arma.json");
            PrepareSkillFromLocalJson("sancho/S3/w_term.json");
            PrepareSkillFromLocalJson("sancho/w_evade.json");

            PrepareSkillFromLocalJson("redfraud/w_1.json");
            PrepareSkillFromLocalJson("redfraud/w_2.json");
            PrepareSkillFromLocalJson("redfraud/w_3.json");
            PrepareSkillFromLocalJson("redfraud/w_4.json");
            PrepareSkillFromLocalJson("redfraud/w_5.json");
            PrepareSkillFromLocalJson("redfraud/w_6.json");
            PrepareSkillFromLocalJson("redfraud/w_7.json");
            PrepareSkillFromLocalJson("redfraud/w_8.json");
            PrepareSkillFromLocalJson("redfraud/w_9.json");

            PrepareSkillFromLocalJson("prepatch/w_1030603.json");
            PrepareSkillFromLocalJson("prepatch/w_2020611.json");

            //PrepareSkillFromLocalJson("prepatch/w_1100503.json");
            //PrepareSkillFromLocalJson("prepatch/w_40000101.json");
            //PrepareSkillFromLocalJson("prepatch/w_40000102.json");
            //PrepareSkillFromLocalJson("prepatch/w_40000103.json");

            InitSkills();
            PrepareLocalize("redmist/lc_spear.json");
            PrepareLocalize("redmist/lc_level.json");
            PrepareLocalize("redmist/lc_upstanding.json");
            PrepareLocalize("redmist/lc_focus.json");
            PrepareLocalize("redmist/lc_gsh.json");
            PrepareLocalize("redmist/lc_gsv.json");

            PrepareLocalize("xiao/lc_aow.json");
            PrepareLocalize("xiao/lc_fds.json");
            PrepareLocalize("xiao/lc_fe.json");
            PrepareLocalize("xiao/lc_coh.json");
            PrepareLocalize("xiao/lc_rv.json");

            PrepareLocalize("sancho/S1/lc_joy.json");
            PrepareLocalize("sancho/S1/lc_sun.json");
            PrepareLocalize("sancho/S2/lc_impl.json");
            PrepareLocalize("sancho/S2/lc_track.json");
            PrepareLocalize("sancho/S3/lc_arma.json");
            PrepareLocalize("sancho/S3/lc_term.json");
            PrepareLocalize("sancho/lc_evade.json");

            PrepareLocalize("redfraud/lc_1.json");
            PrepareLocalize("redfraud/lc_2.json");
            PrepareLocalize("redfraud/lc_3.json");
            PrepareLocalize("redfraud/lc_4.json");
            PrepareLocalize("redfraud/lc_5.json");
            PrepareLocalize("redfraud/lc_6.json");
            PrepareLocalize("redfraud/lc_7.json");
            PrepareLocalize("redfraud/lc_8.json");
            PrepareLocalize("redfraud/lc_9.json");

            PrepareLocalize("prepatch/lc_1030603.json");
            PrepareLocalize("prepatch/lc_1110702.json");
            PrepareLocalize("prepatch/lc_1110704.json");
            PrepareLocalize("prepatch/lc_2020611.json");
            PrepareLocalize("prepatch/lc_1050805.json");

            //PrepareLocalize("prepatch/lc_1100503.json");
            InitLocalize();

            Singleton<StaticDataManager>.Instance.ThreadDungeonBattleList.GetStage(4).waveList[0].GetEnemyDataList()[0].unitID = 8154;
            Singleton<StaticDataManager>.Instance.ThreadDungeonBattleList.GetStage(4).waveList[0].GetEnemyDataList()[0].unitLevel = 45;

            PrepareExpStage(1, "br_1.json");
            PrepareExpStage(2, "br_2.json");
            PrepareExpStage(3, "br_3.json");
            InitExpStage();

            //Singleton<StaticDataManager>.Instance.AbnormalityUnitList.GetData(8301).overridePersonalityId = 10301;

            //Singleton<StaticDataManager>.Instance.AbnormalityUnitList.GetData(8301).overridePersonalityId = 10301;


            LoadAbnoUnit("br_3_1.json");
            LoadAbnoUnit("br_3_2.json");
            LoadAbnoUnit("br_3_3.json");

            LoadAbnoPartUnit("br_3_1_1.json");
            LoadEgoTextAndStatic("rm_aleph.json");

            //LoadPersonality("theredmist.json");
            //LoadPersonality("thewavesthatwuther.json");
            //LoadBuffStatic("SlotAdder.json");

            Singleton<StaticDataManager>.Instance.BuffList.GetData("Sinking").maxStack = -1;
            Singleton<StaticDataManager>.Instance.BuffList.GetData("Sinking").maxTurn = -1;

            Singleton<StaticDataManager>.Instance.BuffList.GetData("Combustion").maxStack = -1;
            Singleton<StaticDataManager>.Instance.BuffList.GetData("Combustion").maxTurn = -1;

            Singleton<StaticDataManager>.Instance.BuffList.GetData("Burst").maxStack = -1;
            Singleton<StaticDataManager>.Instance.BuffList.GetData("Burst").maxTurn = -1;

            Singleton<StaticDataManager>.Instance.BuffList.GetData("Laceration").maxStack = -1;
            Singleton<StaticDataManager>.Instance.BuffList.GetData("Laceration").maxTurn = -1;

            Singleton<TextDataSet>.Instance.EnemyList._dic.Add("-3001", new TextData_Enemy { name = "The Red Mist", desc = "Core (teehee~)", id = "-3001" });
            Singleton<TextDataSet>.Instance.EnemyList._dic.Add("-3002", new TextData_Enemy { name = "Every Catherine", desc = "Core (teehee~)", id = "-3002" });
            Singleton<TextDataSet>.Instance.EnemyList._dic.Add("-3003", new TextData_Enemy { name = "Don Quixote (real)", desc = "Core (teehee~)", id = "-3003" });


            //Singleton<StaticDataManager>.Instance.EgoList.GetData(20101).awakeningSkillId = 2010711;
            //Singleton<StaticDataManager>.Instance.EgoList.GetData(20101).requirementList.Clear();
            //Singleton<StaticDataManager>.Instance.EgoList.GetData(20101).requirementList.Add(new EgoSkillRequirement { attributeType = "INDIGO", num = 1 });


            Singleton<StaticDataManager>.Instance.PassiveList.list.Add(new PassiveStaticData { id = -1 });
            Singleton<TextDataSet>.Instance.PassiveList._dic.Add("-1", new TextData_Passive { id = "-1", desc = "Solemn", name = "Lament" });
            AddPassive(10301, -1);
            //AddPassive(10201, -1);

            // custom skins
            {
                aplist.Add(10301, ("SD_Abnormality", "8410_RealDon_2pAppearance"));
                aplist.Add(10307, ("SD_Abnormality", "8380_SanchoAppearance"));
                aplist.Add(10204, ("SD_Abnormality", "8029_CromerAppearance"));
                aplist.Add(10104, ("SD_Abnormality", "8044_Camellia_AwakenAppearance"));
                //aplist.Add(10601, ("SD_Personality", "400001_JiashichunAppearance"));
                aplist.Add(10508, ("SD_Abnormality", "8153_KimSatGat_ErodeAppearance"));
                //aplist.Add(-3001, ("SD_Personality", "10410_Ryoshu_SpiderBudAppearance"));
                aplist.Add(-3001, ("SD_Personality", "10106_Yisang_WCorpAppearance"));
                //aplist.Add(-3002, ("SD_Enemy", "91014_JosephineAppearance"));

                //aplist.Add(10710, ("SD_Abnormality", "8173_MaouHeathclif_RideAppearance"));
                //aplist.Add(10101, ("SD_Abnormality", "90136_JosephineWHAppearance"));

                //aplist.Add(10101, ("SD_EGO", "ErosionAppearance_20501"));
                //aplist.Add(10201, ("SD_EGO", "ErosionAppearance_2050111"));
                //aplist.Add(10101, ("SD_EGO", "20501"));
                //aplist.Add(10201, ("SD_EGO", "2050111"));

                //aplist.Add(10301, ("SD_EGO", "ErosionAppearance_2030711"));
                //aplist.Add(10101, ("SD_Abnormality", "9999_VergiliusAppearance"));
                //aplist.Add(10201, ("SD_Enemy", "9999_VergiliusAppearance"));
                //aplist.Add(10507, ("SD_Enemy", "9999_VergiliusAppearance"));
            }

            // ego gifts
            {
                //egolist["Player"].Add((9025, 0, 0));
                //egolist["Player"].Add((9028, 0, 0));
                //egolist["Player"].Add((9037, 0, 0));
                //egolist["Player"].Add((9075, 0, 0));
                //egolist["Player"].Add((9081, 0, 0));
                //egolist["Player"].Add((9082, 1, 0));
                //egolist["Player"].Add((9084, 0, 0));
                //egolist["Player"].Add((9132, 2, 0));
                egolist["Player"].Add((9014, 0, 0));


                //Singleton<StageController>.Instance.StageModel.ClassInfo.

                //egolist["Player"].Add((2047, 0, 0));

                //9102 - dawn - 2

                //9123 - wild hunt - 2
                // dawn


                // wild hunt
                egolist["Player"].Add((9123, 2, 10710));
                //egolist["Player"].Add((2058, 0, 10710));
                //egolist["Player"].Add((2058, 0, 10710));
                //egolist["Player"].Add((2058, 0, 10710));


                // art sang
                //egolist["Player"].Add((9090, 0, 10109));
                ////egolist["Player"].Add((2058, 0, 10109));
                //egolist["Player"].Add((9014, 0, 10109));
                //egolist["Player"].Add((9419, 0, 10109));
                //egolist["Player"].Add((9042, 0, 10109));
                //egolist["Player"].Add((9416, 0, 10109));
                //egolist["Player"].Add((9026, 0, 10109));
                //egolist["Player"].Add((9004, 0, 10109));

                //egolist["Player"].Add((9021, 0, 0));
                //egolist["Player"].Add((9022, 0, 0));
                //egolist["Player"].Add((9025, 0, 0));
                //egolist["Player"].Add((9061, 0, 0));
                //egolist["Enemy"].Add((9060, 2, 0));
                //egolist["Player"].Add((9154, 0, 0));
                //egolist["Player"].Add((9004, 0, 0));
                //egolist["Player"].Add((9004, 0, 0));
                //egolist["Player"].Add((9066, 2, 0));
                //egolist["Player"].Add((9068, 0, 0));

                //egolist["Abnormality"].Add((9083, 0));
                //egolist["Abnormality_Part"].Add((9083, 0));
                egolist["Enemy"].Add((9083, 0, 8013));
                egolist["Enemy"].Add((9083, 0, 8014));

            }

            //Log.LogFatal($"abno: {Singleton<StaticDataManager>.Instance.EnemyUnitList.GetData(9874).ID}");

            // exp3
            {
                // the red mist
                {
                    int rm_id = 10401;
                    int rm_ds = 750;
                    float rm_il = 32.79f;
                    int rm_aggro = 5;

                    Singleton<TextDataSet>.Instance.PersonalityList.GetData(rm_id).title = "The Red Mist";
                    Singleton<TextDataSet>.Instance.PersonalityList.GetData(rm_id).oneLineTitle = "Lobotomy E.G.O ";

                    Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(rm_id).resistInfo.atkResistList[0].value = 0.75f;
                    Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(rm_id).resistInfo.atkResistList[1].value = 0.75f;
                    Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(rm_id).resistInfo.atkResistList[2].value = 1.5f;

                    Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(rm_id).mentalConditionInfo = Singleton<StaticDataManager>.Instance.AbnormalityUnitList.GetData(8350).mentalConditionInfo;
                    Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(rm_id).attributeList.Clear();

                    Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(rm_id).attributeList.Add(new UnitAttribute { skillId = 9990101, number = 8 });
                    Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(rm_id).attributeList.Add(new UnitAttribute { skillId = 9990102, number = 7 });
                    Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(rm_id).attributeList.Add(new UnitAttribute { skillId = 9990103, number = 6 });
                    Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(rm_id).attributeList.Add(new UnitAttribute { skillId = 9990105, number = 1 });
                    Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(rm_id).attributeList.Add(new UnitAttribute { skillId = 9990106, number = 1 });

                    //AddPassive(rm_id, 809903);
                    AddPassive(rm_id, -1);
                    Singleton<StaticDataManager>.Instance.AbnormalityUnitList.GetData(8042).passiveSet.AddBattlePassive(-1);
                    Singleton<StaticDataManager>.Instance.AbnormalityPartList.GetData(804201).passiveSet.AddBattlePassive(-1);

                    AddPassive(rm_id, 860506);
                    //AddPassive(rm_id, 1040301);
                    AddPassive(rm_id, 2040211);
                    AddPassive(rm_id, 836301);
                    //AddPassive(rm_id, 1060201);
                    AddPassive(rm_id, 1070201);
                    AddPassive(rm_id, 1080101);
                    AddPassive(rm_id, 1080301);
                    //AddPassive(rm_id, 1100201);
                    AddPassive(rm_id, 9999999);

                    Singleton<StaticDataManager>.Instance.PassiveList.GetData(1040101).attributeResonanceCondition[0]._attributeType = ATTRIBUTE_TYPE.VIOLET;

                    Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(rm_id).hp = new StatByLevel
                    {
                        defaultStat = rm_ds,
                        _securedDefaultStat = new CodeStage.AntiCheat.ObscuredTypes.ObscuredInt(rm_ds),
                        _securedIncrementByLevel = new CodeStage.AntiCheat.ObscuredTypes.ObscuredFloat(rm_il),
                        incrementByLevel = rm_il,
                        //_currentLevel = new CodeStage.AntiCheat.ObscuredTypes.ObscuredInt(Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(rm_id).rank),
                        //_currentStat = new CodeStage.AntiCheat.ObscuredTypes.ObscuredInt(rm_ds),
                        //_isInitialized = true,
                    };
                    Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(rm_id).breakSection = new BreakSectionInfo
                    {
                        sectionList = new Il2CppSystem.Collections.Generic.List<int>()
                    };
                    Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(rm_id).breakSection.sectionList.Add(75);
                    Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(rm_id).breakSection.sectionList.Add(50);
                    Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(rm_id).breakSection.sectionList.Add(35);
                    Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(rm_id).breakSection.sectionList.Add(10);

                    //Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(rm_id).initBuffList.Add(new InitBuffPerLevel { level = 4, list = new Il2CppSystem.Collections.Generic.List<InitBuff>() });
                    //Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(rm_id).initBuffList[0].list.Add(new InitBuff { buff = "ParryingResultDown", stack = 3, turn = 0, });


                    var dss = new Il2CppSystem.Collections.Generic.List<CodeStage.AntiCheat.ObscuredTypes.ObscuredInt>();
                    dss.Add(new CodeStage.AntiCheat.ObscuredTypes.ObscuredInt(9990104));
                    Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(rm_id)._securedAggro = new CodeStage.AntiCheat.ObscuredTypes.ObscuredInt(rm_aggro);
                    Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(rm_id)._securedDefenseSkillIDList = dss;
                }
            }

            // sancho
            {
                //aplist.Add(10301, ("SD_Abnormality", "8390_RealDon_1pAppearance"));
                int sh_id = 10307;
                int sh_ds = 197;
                float sh_il = 3f;
                int sh_aggro = 0;

                Singleton<TextDataSet>.Instance.PersonalityList.GetData(sh_id).title = "Sancho";
                Singleton<TextDataSet>.Instance.PersonalityList.GetData(sh_id).oneLineTitle = "Second Kindred ";

                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(sh_id).unitKeywordList.Add("SMALL");
                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(sh_id).unitKeywordList.Add("BLOODFIEND");
                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(sh_id).unitKeywordList.Add("SECOND_DEPENDENT");

                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(sh_id).resistInfo.atkResistList[0].value = 0.50f;
                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(sh_id).resistInfo.atkResistList[1].value = 0.75f;
                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(sh_id).resistInfo.atkResistList[2].value = 1.35f;

                //AddPassive(sh_id, 1111102);
                AddPassive(sh_id, 838005);
                AddPassive(sh_id, 835005);
                AddPassive(sh_id, 40000103);
                AddPassive(sh_id, 837101);
                AddPassive(sh_id, 841202);

                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(sh_id).panicType = 1033;
                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(sh_id).passiveSet._securedPassiveIdList.Clear();
                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(sh_id).passiveSet._securedPassiveIdList.Add(new CodeStage.AntiCheat.ObscuredTypes.ObscuredInt(838001));
                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(sh_id).passiveSet._securedPassiveIdList.Add(new CodeStage.AntiCheat.ObscuredTypes.ObscuredInt(838006));

                //8013
                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(sh_id).attributeList.Clear();
                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(sh_id).attributeList.Add(new UnitAttribute { skillId = 9990301, number = 5 });
                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(sh_id).attributeList.Add(new UnitAttribute { skillId = 9990311, number = 4 });
                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(sh_id).attributeList.Add(new UnitAttribute { skillId = 9990302, number = 5 });
                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(sh_id).attributeList.Add(new UnitAttribute { skillId = 9990312, number = 3 });
                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(sh_id).attributeList.Add(new UnitAttribute { skillId = 9990303, number = 1 });
                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(sh_id).attributeList.Add(new UnitAttribute { skillId = 9990305, number = 1 });

                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(sh_id).hp._securedDefaultStat = new CodeStage.AntiCheat.ObscuredTypes.ObscuredInt(sh_ds);
                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(sh_id).hp._securedIncrementByLevel = new CodeStage.AntiCheat.ObscuredTypes.ObscuredFloat(sh_il);

                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(sh_id).breakSection = new BreakSectionInfo
                {
                    sectionList = new Il2CppSystem.Collections.Generic.List<int>()
                };
                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(sh_id).breakSection.sectionList.Add(35);
                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(sh_id).breakSection.sectionList.Add(25);
                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(sh_id).breakSection.sectionList.Add(10);
                var dss = new Il2CppSystem.Collections.Generic.List<CodeStage.AntiCheat.ObscuredTypes.ObscuredInt>();
                dss.Add(new CodeStage.AntiCheat.ObscuredTypes.ObscuredInt(9990304));

                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(sh_id)._securedAggro = new CodeStage.AntiCheat.ObscuredTypes.ObscuredInt(sh_aggro);
                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(sh_id)._securedDefenseSkillIDList = dss;
            }

            // don quixote
            {
                int dq_id = 10301;
                int dq_ds = 205;
                float dq_il = 5f;
                int dq_aggro = 0;

                Singleton<TextDataSet>.Instance.PersonalityList.GetData(dq_id).title = "Don Quixote";
                Singleton<TextDataSet>.Instance.PersonalityList.GetData(dq_id).oneLineTitle = "First Kindred ";

                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(dq_id).resistInfo.atkResistList[0].value = 0.75f;
                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(dq_id).resistInfo.atkResistList[1].value = 0.75f;
                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(dq_id).resistInfo.atkResistList[2].value = 1f;

                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(dq_id).panicType = 1034;

                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(dq_id).attributeList.Clear();
                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(dq_id).attributeList.Add(new UnitAttribute { skillId = 841001, number = 1 });
                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(dq_id).attributeList.Add(new UnitAttribute { skillId = 841002, number = 1 });
                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(dq_id).attributeList.Add(new UnitAttribute { skillId = 841003, number = 1 });
                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(dq_id).attributeList.Add(new UnitAttribute { skillId = 841004, number = 1 });
                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(dq_id).attributeList.Add(new UnitAttribute { skillId = 841005, number = 1 });
                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(dq_id).attributeList.Add(new UnitAttribute { skillId = 841006, number = 1 });
                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(dq_id).attributeList.Add(new UnitAttribute { skillId = 841007, number = 1 });
                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(dq_id).attributeList.Add(new UnitAttribute { skillId = 841008, number = 1 });
                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(dq_id).attributeList.Add(new UnitAttribute { skillId = 841009, number = 1 });
                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(dq_id).attributeList.Add(new UnitAttribute { skillId = 841010, number = 1 });
                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(dq_id).attributeList.Add(new UnitAttribute { skillId = 841011, number = 1 });
                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(dq_id).attributeList.Add(new UnitAttribute { skillId = 841012, number = 1 });
                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(dq_id).attributeList.Add(new UnitAttribute { skillId = 841013, number = 1 });

                //Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(sh_id).attributeList.Add(new UnitAttribute { skillId = 838001, number = 1 });
                //Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(sh_id).attributeList.Add(new UnitAttribute { skillId = 838002, number = 1 });
                //Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(sh_id).attributeList.Add(new UnitAttribute { skillId = 838003, number = 1 });
                //Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(sh_id).attributeList.Add(new UnitAttribute { skillId = 838004, number = 1 });
                //Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(sh_id).attributeList.Add(new UnitAttribute { skillId = 838005, number = 1 });
                //Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(sh_id).attributeList.Add(new UnitAttribute { skillId = 838006, number = 1 });
                //Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(sh_id).attributeList.Add(new UnitAttribute { skillId = 838007, number = 1 });
                //Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(sh_id).attributeList.Add(new UnitAttribute { skillId = 838008, number = 1 });
                //Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(sh_id).attributeList.Add(new UnitAttribute { skillId = 838009, number = 1 });
                //Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(sh_id).attributeList.Add(new UnitAttribute { skillId = 838010, number = 1 });
                //Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(sh_id).attributeList.Add(new UnitAttribute { skillId = 838011, number = 1 });

                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(dq_id).hp._securedDefaultStat = new CodeStage.AntiCheat.ObscuredTypes.ObscuredInt(dq_ds);
                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(dq_id).hp._securedIncrementByLevel = new CodeStage.AntiCheat.ObscuredTypes.ObscuredFloat(dq_il);

                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(dq_id).breakSection = new BreakSectionInfo
                {
                    sectionList = new Il2CppSystem.Collections.Generic.List<int>()
                };
                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(dq_id).breakSection.sectionList.Add(35);
                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(dq_id).breakSection.sectionList.Add(25);
                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(dq_id).breakSection.sectionList.Add(10);
                var dss = new Il2CppSystem.Collections.Generic.List<CodeStage.AntiCheat.ObscuredTypes.ObscuredInt>();
                dss.Add(new CodeStage.AntiCheat.ObscuredTypes.ObscuredInt(9990304));

                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(dq_id)._securedAggro = new CodeStage.AntiCheat.ObscuredTypes.ObscuredInt(dq_aggro);
                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(dq_id)._securedDefenseSkillIDList = dss;
            }


            Singleton<StaticDataManager>.Instance.PersonalityPassiveList.GetBattlePassiveIDList(10601, 3).Add(1040101);
            Singleton<StaticDataManager>.Instance.PersonalityPassiveList.GetBattlePassiveIDList(10601, 3).Add(1030101);
            Singleton<StaticDataManager>.Instance.PersonalityPassiveList.GetBattlePassiveIDList(10601, 3).Add(1030401);


            //Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(10301).initBuffList.Add(new InitBuffPerLevel { level = 3, list = new Il2CppSystem.Collections.Generic.List<InitBuff>() });
            //Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(10301).initBuffList[0].list.Add(new InitBuff { buff = "HonorableDuel_Don", stack = 1 });
            //Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(10301).initBuffList[0].list.Add(new InitBuff { buff = "RecklessDuel", stack = 1 });
            //Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(10301).initBuffList[0].list.Add(new InitBuff { buff = "RighteousFeeling", stack = 1 });
            //Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(10301).initBuffList[0].list.Add(new InitBuff { buff = "FragmentOfHopeTwo", stack = 4 });
            //Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(10301).initBuffList[0].list.Add(new InitBuff { buff = "UnfinishedDream", stack = 1 });
            //Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(10301).initBuffList[0].list.Add(new InitBuff { buff = "Precarious", stack = 1 });



            //Singleton<StaticDataManager>.Instance.PassiveList.list.Add(new PassiveStaticData { id = 9991001 });
            //Singleton<TextDataSet>.Instance.PassiveList._dic.Add("9991001", new TextData_Passive { id = "9991001", name = "this maybe does something", desc = "probably", summary = "summery I guess"});

            var a = new TextData_Skill_CoinDesc { desc = "[OnSucceedAttackHead] Inflict +1 [Sinking]\r\n" };
            var b = new Il2CppSystem.Collections.Generic.List<TextData_Skill_CoinDesc>();
            b.Add(a);

            var a1 = new TextData_Skill_CoinDesc { desc = "[OnSucceedAttackHead] Inflict +2 [Sinking] Potency\r\n[OnSucceedAttackHead] Inflict +1 [Sinking] Count\r\n" };
            var b1 = new Il2CppSystem.Collections.Generic.List<TextData_Skill_CoinDesc>();
            b1.Add(a1);

            Singleton<TextDataSet>.Instance.SkillList.GetData(1060801).levelList[2].coinlist.Insert(0, new TextData_Skill_Coins { coindescs = b });
            Singleton<TextDataSet>.Instance.SkillList.GetData(1060803).levelList[1].coinlist.Insert(0, new TextData_Skill_Coins { coindescs = b1 });
            Singleton<TextDataSet>.Instance.SkillList.GetData(1060803).levelList[1].coinlist.Insert(0, new TextData_Skill_Coins { coindescs = b1 });

            Singleton<TextDataSet>.Instance.SkillList.GetData(1091003).levelList[0].coinlist[0].coindescs.Add(new TextData_Skill_CoinDesc { desc = "[OnSucceedAttack] Heal by 70% of damage dealt" });
            Singleton<TextDataSet>.Instance.SkillList.GetData(1091003).levelList[1].coinlist[0].coindescs.Add(new TextData_Skill_CoinDesc { desc = "[OnSucceedAttack] Heal by 100% of damage dealt" });
            //Singleton<TextDataSet>.Instance.SkillList.GetData(1091001).levelList[2].coinlist[1].coindescs.Add(new TextData_Skill_CoinDesc { desc = "[OnSucceedAttack] Heal by 50% of damage dealt" });

            Singleton<StaticDataManager>.Instance.SkillList.GetData(1100403).skillData[0].defaultValue = 15;
            Singleton<StaticDataManager>.Instance.SkillList.GetData(1100403).skillData[0].coinList[0].scale = 2;
            Singleton<StaticDataManager>.Instance.SkillList.GetData(1100403).skillData[0].coinList[0].operatorType = "MUL";
            Singleton<StaticDataManager>.Instance.SkillList.GetData(1100403).skillData[0].coinList[0]._operatorType = OPERATOR_TYPE.MUL;

            Singleton<StaticDataManager>.Instance.SkillList.GetData(1100403).skillData[1].defaultValue = 20;
            Singleton<StaticDataManager>.Instance.SkillList.GetData(1100403).skillData[1].coinList[0].scale = 2;
            Singleton<StaticDataManager>.Instance.SkillList.GetData(1100403).skillData[1].coinList[0].operatorType = "MUL";
            Singleton<StaticDataManager>.Instance.SkillList.GetData(1100403).skillData[1].coinList[0]._operatorType = OPERATOR_TYPE.MUL;

            // yi sang solemn
            {
                //Singleton<StaticDataManager>.Instance.SkillList.GetData(1011001).skillData[1].coinList.Add(Singleton<StaticDataManager>.Instance.SkillList.GetData(1011001).skillData[0].coinList[1]);
                //Singleton<StaticDataManager>.Instance.SkillList.GetData(1011001).skillData[2].coinList.Add(Singleton<StaticDataManager>.Instance.SkillList.GetData(1011001).skillData[0].coinList[1]);

                Singleton<StaticDataManager>.Instance.SkillList.GetData(1011003).skillData[1].targetNum = 3;
                //Singleton<StaticDataManager>.Instance.SkillList.GetData(1011003).skillData[1].defaultValue = 2;

                //foreach (var x in Singleton<StaticDataManager>.Instance.SkillList.GetData(1011003).skillData[1].coinList)
                //{
                //    x.operatorType = "MUL";
                //    x._operatorType = OPERATOR_TYPE.MUL;
                //    x.scale = 2;
                //}
            }

            // solemn lament ego
            {
                Singleton<StaticDataManager>.Instance.EgoList.GetData(21207).GetAwakeningSkill().skillData[1].defaultValue = 12;
                Singleton<StaticDataManager>.Instance.EgoList.GetData(21207).GetAwakeningSkill().skillData[2].defaultValue = 16;

                Singleton<StaticDataManager>.Instance.EgoList.GetData(21207).GetAwakeningSkill().skillData[1].mpUsage = 15;
                Singleton<StaticDataManager>.Instance.EgoList.GetData(21207).GetAwakeningSkill().skillData[2].mpUsage = 10;

                Singleton<StaticDataManager>.Instance.EgoList.GetData(21207).RequirementList.RemoveAt(0);
                Singleton<StaticDataManager>.Instance.EgoList.GetData(21207).RequirementList.RemoveAt(0);

                foreach (var x in Singleton<StaticDataManager>.Instance.EgoList.GetData(21207).GetAwakeningSkill().skillData[1].coinList)
                {
                    x.scale = 3;
                    //x.operatorType = "MUL";
                    x.prob = 0.75f;
                }

                foreach (var x in Singleton<StaticDataManager>.Instance.EgoList.GetData(21207).GetAwakeningSkill().skillData[2].coinList)
                {
                    x.scale = 4;
                    //x.operatorType = "MUL";
                    x.prob = 0.8f;
                }
            }

            // w corp stuff
            {
                // don

                //Log.LogInfo(JsonUtility.ToJson(Singleton<StaticDataManager>.Instance.SkillList.GetData(1040504)));
                //Log.LogInfo(JsonUtility.ToJson(Singleton<StaticDataManager>.Instance.SkillList.GetData(1111003)));



                Singleton<StaticDataManager>.Instance.SkillList.GetData(1030203).skillData[0].defaultValue = 3;
                foreach (var x in Singleton<StaticDataManager>.Instance.SkillList.GetData(1030203).skillData[0].coinList)
                {
                    x.scale = 3;
                }

                // ryoshu
                Singleton<StaticDataManager>.Instance.SkillList.GetData(1040501).skillData[2].defaultValue = 5;
                foreach (var x in Singleton<StaticDataManager>.Instance.SkillList.GetData(1040501).skillData[3].coinList)
                {
                    x.scale = 3;
                }
                Singleton<StaticDataManager>.Instance.SkillList.GetData(1040504).skillData[0].defaultValue = 5;
                Singleton<StaticDataManager>.Instance.SkillList.GetData(1040504).skillData[1].defaultValue = 7;

                // outis
                Singleton<StaticDataManager>.Instance.SkillList.GetData(1111003).skillData[1].defaultValue = 5;
            }

            // poise
            {
                // ishamel
                // S3
                Singleton<StaticDataManager>.Instance.SkillList.GetData(1080303).skillData[0].coinList.Add(Singleton<StaticDataManager>.Instance.SkillList.GetData(1080303).skillData[0].coinList[0]);
                Singleton<StaticDataManager>.Instance.SkillList.GetData(1080303).skillData[1].coinList.Add(Singleton<StaticDataManager>.Instance.SkillList.GetData(1080303).skillData[1].coinList[0]);
                // Counter
                Singleton<StaticDataManager>.Instance.SkillList.GetData(1080304).skillData[0].coinList.Add(Singleton<StaticDataManager>.Instance.SkillList.GetData(1080304).skillData[0].coinList[0]);
                Singleton<StaticDataManager>.Instance.SkillList.GetData(1080304).skillData[1].coinList.Add(Singleton<StaticDataManager>.Instance.SkillList.GetData(1080304).skillData[1].coinList[0]);

                AddPassive(10808, 810601);
                AddPassive(10808, 810606);
                AddPassive(10808, 9009801);
                //AddPassive(10808, 810601);



                // tctb sinclair
                Singleton<StaticDataManager>.Instance.SkillList.GetData(1100203).skillData[0].defaultValue = 6;
                Singleton<StaticDataManager>.Instance.SkillList.GetData(1100203).skillData[1].defaultValue = 8;

                Singleton<StaticDataManager>.Instance.SkillList.GetData(1100203).skillData[1].coinList[0].operatorType = "MUL";
                Singleton<StaticDataManager>.Instance.SkillList.GetData(1100203).skillData[1].coinList[0].scale = 3;
                Singleton<StaticDataManager>.Instance.SkillList.GetData(1100203).skillData[1].abilityScriptList.Add(new AbilityData { scriptName = "CoinScaleAdderViaBuffCheckDevideByTurnWithLimit3", buffData = new BuffReferenceData { buffKeyword = "Laceration", buffOwner = "Target", stack = 3, value = 3 } });
                Singleton<StaticDataManager>.Instance.SkillList.GetData(1100203).skillData[1].abilityScriptList.Add(new AbilityData { scriptName = "SkillPowerAdderViaBuffCheckDevideByTurnWithLimit3", buffData = new BuffReferenceData { buffKeyword = "Breath", buffOwner = "Self", stack = 1, value = 3 } });
                Singleton<TextDataSet>.Instance.SkillList.GetData(1100203).levelList[1].desc += "\nCoin Power +3 for every 3 [Laceration] on the target (max 3)\nSkill Power +2 for every [Breath] on self (max 3)";

                // 9 bleed
                // 9 poise



                //Singleton<StaticDataManager>.Instance.SkillList.GetData(1100203).skillData[0].coinList.Add(Singleton<StaticDataManager>.Instance.SkillList.GetData(1100203).skillData[0].coinList[0]);
                //Singleton<StaticDataManager>.Instance.SkillList.GetData(1100203).skillData[1].coinList.Add(Singleton<StaticDataManager>.Instance.SkillList.GetData(1100203).skillData[1].coinList[0]);

                // wuther ryoshu
                Singleton<StaticDataManager>.Instance.SkillList.GetData(1040804).skillData[0].defaultValue = 5;
                Singleton<StaticDataManager>.Instance.SkillList.GetData(1040804).skillData[1].defaultValue = 7;

                Singleton<StaticDataManager>.Instance.SkillList.GetData(1010704).skillData[0].defaultValue = 7;
                Singleton<StaticDataManager>.Instance.SkillList.GetData(1010704).skillData[1].defaultValue = 10;

                // -5 power down meursalt
                //Singleton<TextDataSet>.Instance.SkillList.GetData(1050803).levelList[1].coinlist[0].coindescs[0].desc.Insert(0, "[SuperCoin]\r\n");

                Singleton<StaticDataManager>.Instance.SkillList.GetData(1050805).skillData[0].targetNum = 3;
                Singleton<StaticDataManager>.Instance.SkillList.GetData(1050805).skillData[1].targetNum = 5;

                Singleton<StaticDataManager>.Instance.SkillList.GetData(1050805).skillData[0].defaultValue = 6;
                Singleton<StaticDataManager>.Instance.SkillList.GetData(1050805).skillData[1].defaultValue = 8;

                Singleton<StaticDataManager>.Instance.SkillList.GetData(1050805).skillData[0].coinList[0].abilityScriptList.Add(new AbilityData { scriptName = "HealSelfOnSuccessAttackByRatio5" });
                Singleton<StaticDataManager>.Instance.SkillList.GetData(1050805).skillData[0].coinList[1].abilityScriptList.Add(new AbilityData { scriptName = "HealSelfOnSuccessAttackByRatio5" });
                Singleton<StaticDataManager>.Instance.SkillList.GetData(1050805).skillData[0].coinList[2].abilityScriptList.Add(new AbilityData { scriptName = "HealSelfOnSuccessAttackByRatio5" });

                Singleton<StaticDataManager>.Instance.SkillList.GetData(1050805).skillData[1].coinList[0].abilityScriptList.Add(new AbilityData { scriptName = "HealSelfOnSuccessAttackByRatio10" });
                Singleton<StaticDataManager>.Instance.SkillList.GetData(1050805).skillData[1].coinList[1].abilityScriptList.Add(new AbilityData { scriptName = "HealSelfOnSuccessAttackByRatio10" });
                Singleton<StaticDataManager>.Instance.SkillList.GetData(1050805).skillData[1].coinList[2].abilityScriptList.Add(new AbilityData { scriptName = "HealSelfOnSuccessAttackByRatio10" });


                Singleton<StaticDataManager>.Instance.SkillList.GetData(1050803).skillData[0].defaultValue = 8;
                Singleton<StaticDataManager>.Instance.SkillList.GetData(1050803).skillData[1].defaultValue = 8;

                //Singleton<StaticDataManager>.Instance.SkillList.GetData(1050803).skillData[0].coinList[0].color = "GREY";

                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(10508).hp._securedIncrementByLevel = new CodeStage.AntiCheat.ObscuredTypes.ObscuredFloat(4.3f);
                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(10508).hp.incrementByLevel = 4.3f;

                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(10508).hp._securedDefaultStat = new CodeStage.AntiCheat.ObscuredTypes.ObscuredInt(131);
                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(10508).hp.defaultStat = 131;

                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(10508).resistInfo.atkResistList[0].value = 0.35f;
                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(10508).resistInfo.atkResistList[1].value = 0.65f;
                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(10508).resistInfo.atkResistList[2].value = 1f;

                //Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(10508)._securedDefenseSkillIDList.RemoveAt(0);
                //Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(10508).defenseSkillIDList.RemoveAt(0);

                Singleton<StaticDataManager>.Instance.EgoList.GetData(20501).requirementList[0].attributeType = "CRIMSON";
                Singleton<StaticDataManager>.Instance.EgoList.GetData(20501).requirementList[1].attributeType = "INDIGO";

                foreach (AttributeResistData ard in Singleton<StaticDataManager>.Instance.EgoList.GetData(20501).attributeResistList)
                {
                    ard.value -= 0.5f;
                }

                Singleton<StaticDataManager>.Instance.EgoList.GetData(20501).requirementList[0].num = 2;
                Singleton<StaticDataManager>.Instance.EgoList.GetData(20501).requirementList[1].num = 3;
                Singleton<StaticDataManager>.Instance.EgoList.GetData(20501).requirementList.RemoveAt(2);

                foreach (var x in Singleton<StaticDataManager>.Instance.SkillList.GetData(1050805).skillData[0].coinList)
                {
                    x.color = "GREY";
                    x.operatorType = "MUL";
                    x.scale = 2;
                    x.prob = 0.75f;
                }

                foreach (var x in Singleton<StaticDataManager>.Instance.SkillList.GetData(1050805).skillData[1].coinList)
                {
                    x.color = "GREY";
                    x.operatorType = "MUL";
                    x.scale = 2;
                    x.prob = 0.75f;
                }
            }

            // bleed
            {
                foreach (var x in Singleton<StaticDataManager>.Instance.SkillList.GetData(1030603).skillData[1].coinList)
                {
                    x.scale = 4;
                }

                Singleton<StaticDataManager>.Instance.SkillList.GetData(1030603).skillData[0].defaultValue = 5;
                Singleton<StaticDataManager>.Instance.SkillList.GetData(1030603).skillData[1].defaultValue = 5;

                // middle don

                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(10306).resistInfo.atkResistList[0].value = 0.50f;
                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(10306).resistInfo.atkResistList[1].value = 0.75f;
                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(10306).resistInfo.atkResistList[2].value = 1.25f;

                Singleton<StaticDataManager>.Instance.SkillList.GetData(1030605).skillData[0].defaultValue = 5;
                Singleton<StaticDataManager>.Instance.SkillList.GetData(1030605).skillData[1].defaultValue = 6;

                // ring sang - guard -> counter with S2 coins
                Singleton<TextDataSet>.Instance.SkillList.GetData(1010904).levelList = Singleton<TextDataSet>.Instance.SkillList.GetData(1010902).levelList;
                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(10109).resistInfo.atkResistList[0].value = 0.75f;
                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(10109).resistInfo.atkResistList[1].value = 1f;
                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(10109).resistInfo.atkResistList[2].value = 1.25f;

                Singleton<StaticDataManager>.Instance.SkillList.GetData(1010901).skillData[1].defaultValue = 3;
                Singleton<StaticDataManager>.Instance.SkillList.GetData(1010901).skillData[2].defaultValue = 4;

                AddPassive(10109, 1110901, 2);
                AddPassive(10109, 1110911, 4);
                //RemovePassive(10109, 1010902);

                Singleton<StaticDataManager>.Instance.SkillList.GetData(1010902).skillData[1].coinList[0].operatorType = "MUL";
                Singleton<StaticDataManager>.Instance.SkillList.GetData(1010902).skillData[1].coinList[0].scale = 2;
                //Singleton<StaticDataManager>.Instance.SkillList.GetData(1010902).skillData[2].coinList[0].operatorType = "MUL";
                //Log.LogInfo(9);
                //Singleton<StaticDataManager>.Instance.SkillList.GetData(1010902).skillData[2].coinList[0].scale = 2;

                Singleton<StaticDataManager>.Instance.SkillList.GetData(1010903).skillData[0].coinList[0].scale = 4;
                Singleton<StaticDataManager>.Instance.SkillList.GetData(1010903).skillData[1].coinList[0].scale = 5;
                Singleton<StaticDataManager>.Instance.SkillList.GetData(1010903).skillData[1].targetNum = 3;


                Singleton<StaticDataManager>.Instance.SkillList.GetData(1010904).skillData[0].attributeType = "SCARLET";
                Singleton<StaticDataManager>.Instance.SkillList.GetData(1010904).skillData[0].iconID = "1010902";
                Singleton<StaticDataManager>.Instance.SkillList.GetData(1010904).skillData[0].atkType = "SLASH";
                Singleton<StaticDataManager>.Instance.SkillList.GetData(1010904).skillData[0].defType = "COUNTER";
                Singleton<StaticDataManager>.Instance.SkillList.GetData(1010904).skillData[0].iconID = Singleton<StaticDataManager>.Instance.SkillList.GetData(1010902).skillData[1].iconID;
                Singleton<StaticDataManager>.Instance.SkillList.GetData(1010904).skillData[0].coinList = Singleton<StaticDataManager>.Instance.SkillList.GetData(1010902).skillData[1].coinList;

                Singleton<StaticDataManager>.Instance.SkillList.GetData(1010904).skillData[1].attributeType = "SCARLET";
                Singleton<StaticDataManager>.Instance.SkillList.GetData(1010904).skillData[1].iconID = "1010902";
                Singleton<StaticDataManager>.Instance.SkillList.GetData(1010904).skillData[1].atkType = "SLASH";
                Singleton<StaticDataManager>.Instance.SkillList.GetData(1010904).skillData[1].defType = "COUNTER";

                Singleton<StaticDataManager>.Instance.SkillList.GetData(1010904).skillData[1].iconID = Singleton<StaticDataManager>.Instance.SkillList.GetData(1010902).skillData[1].iconID;
                Singleton<StaticDataManager>.Instance.SkillList.GetData(1010904).skillData[1].coinList = Singleton<StaticDataManager>.Instance.SkillList.GetData(1010902).skillData[2].coinList;


                // nclair
                Singleton<StaticDataManager>.Instance.SkillList.GetData(1100501).skillData[2].defaultValue = 12;
                Singleton<StaticDataManager>.Instance.SkillList.GetData(1100502).skillData[2].defaultValue = 20;
                Singleton<StaticDataManager>.Instance.SkillList.GetData(1100503).skillData[1].defaultValue = 35;


                foreach (var x in Singleton<StaticDataManager>.Instance.SkillList.GetData(1100501).skillData[2].coinList)
                {
                    x.scale = 1;
                }

                foreach (var x in Singleton<StaticDataManager>.Instance.SkillList.GetData(1100502).skillData[2].coinList)
                {
                    x.scale = 3;
                }

                foreach (var x in Singleton<StaticDataManager>.Instance.SkillList.GetData(1100503).skillData[1].coinList)
                {
                    x.scale = 10;
                    //x._coinColorType = COIN_COLOR_TYPE.GREY;
                }
                //Singleton<TextDataSet>.Instance.SkillList.GetData(1100503).levelList[1].coinlist[0].coindescs[0].desc.Insert(0, "[SuperCoin]\n");
                //Singleton<TextDataSet>.Instance.SkillList.GetData(1100503).levelList[1].coinlist[1].coindescs[0].desc.Insert(0, "[SuperCoin]\n");
                //Singleton<TextDataSet>.Instance.SkillList.GetData(1100503).levelList[1].coinlist[2].coindescs[0].desc.Insert(0, "[SuperCoin]\n");

                // rodya
                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(10911).hp._securedIncrementByLevel += 0.7f;
                //Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(10911).hp._securedDefaultStat += 64;

                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(10911).resistInfo.atkResistList[0].value = 1.25f;
                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(10911).resistInfo.atkResistList[1].value = 0.25f;
                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(10911).resistInfo.atkResistList[2].value = 0.75f;

                //Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(10911).breakSection.SectionList[0] -= 30;
                Singleton<StaticDataManager>.Instance.SkillList.GetData(1091103).skillData[1].targetNum = 3;
                Singleton<StaticDataManager>.Instance.SkillList.GetData(1091105).skillData[1].targetNum = 5;
                AddPassive(10911, 9014503, 4);
                AddPassive(10911, 9014803, 4);


                Singleton<StaticDataManager>.Instance.SkillList.GetData(1091101).skillData[2].abilityScriptList.Add(new AbilityData { scriptName = "GiveBuffOnWinDuel", buffData = new BuffReferenceData { buffKeyword = "BloomingThornsRodionFirst", target = "Self", stack = 2, value = 0.0f } });
                Singleton<TextDataSet>.Instance.SkillList.GetData(1091101).levelList[2].desc += "\n[WinDuel] Gain 2 [BloomingThornsRodionFirst]";

                Singleton<StaticDataManager>.Instance.SkillList.GetData(1091102).skillData[2].abilityScriptList.Add(new AbilityData { scriptName = "GiveBuffOnWinDuel", buffData = new BuffReferenceData { buffKeyword = "BloomingThornsRodionFirst", target = "Self", stack = 3, value = 0.0f } });
                Singleton<TextDataSet>.Instance.SkillList.GetData(1091102).levelList[2].desc += "\n[WinDuel] Gain 3 [BloomingThornsRodionFirst]";



                foreach (var x in Singleton<StaticDataManager>.Instance.SkillList.GetData(1091101).skillData[2].coinList)
                {
                    x.scale += 1;
                }

                foreach (var x in Singleton<StaticDataManager>.Instance.SkillList.GetData(1091102).skillData[2].coinList)
                {
                    x.scale += 2;
                }

                foreach (var x in Singleton<StaticDataManager>.Instance.SkillList.GetData(1091103).skillData[0].coinList)
                {
                    x.scale += 2;
                }

                foreach (var x in Singleton<StaticDataManager>.Instance.SkillList.GetData(1091103).skillData[1].coinList)
                {
                    x.scale += 5;
                }

                foreach (var x in Singleton<StaticDataManager>.Instance.SkillList.GetData(1091105).skillData[0].coinList)
                {
                    x.scale += 7;
                    x.color = "GREY";
                }

                foreach (var x in Singleton<StaticDataManager>.Instance.SkillList.GetData(1091105).skillData[1].coinList)
                {
                    x.scale += 13;
                    x.color = "GREY";
                }

                Singleton<StaticDataManager>.Instance.SkillList.GetData(1091103).skillData[0].coinList[0].abilityScriptList.Add(new AbilityData { scriptName = "GiveBuffOnSucceedAttack", buffData = new BuffReferenceData { buffKeyword = "BloomingThornsRodionFirst", target = "Self", turn = 2, value = 0.0f } });
                Singleton<StaticDataManager>.Instance.SkillList.GetData(1091103).skillData[0].coinList[1].abilityScriptList.Add(new AbilityData { scriptName = "GiveBuffOnSucceedAttack", buffData = new BuffReferenceData { buffKeyword = "BloomingThornsRodionFirst", target = "Self", turn = 2, value = 0.0f } });
                Singleton<StaticDataManager>.Instance.SkillList.GetData(1091103).skillData[0].coinList[2].abilityScriptList.Add(new AbilityData { scriptName = "GiveBuffOnSucceedAttack", buffData = new BuffReferenceData { buffKeyword = "BloomingThornsRodionFirst", target = "Self", turn = 3, value = 0.0f } });

                Singleton<TextDataSet>.Instance.SkillList.GetData(1091103).levelList[1].coinlist[0].coindescs.Add(new TextData_Skill_CoinDesc { desc = "<style=\"highlight\">[OnSucceedAttack] Gain 2 blooming[thornyfall_panicRodionFirst]</style>" });
                Singleton<TextDataSet>.Instance.SkillList.GetData(1091103).levelList[1].coinlist[1].coindescs.Add(new TextData_Skill_CoinDesc { desc = "<style=\"highlight\">[OnSucceedAttack] Gain 2 BloomingThornsRodionFirst</style>" });
                Singleton<TextDataSet>.Instance.SkillList.GetData(1091103).levelList[1].coinlist[2].coindescs.Add(new TextData_Skill_CoinDesc { desc = "<style=\"highlight\">[OnSucceedAttack] Gain 3 blooming[thornyfall_panicRodionSecond]</style>" });

                Singleton<StaticDataManager>.Instance.SkillList.GetData(1091105).skillData[1].coinList[0].abilityScriptList.Add(new AbilityData { scriptName = "ForceToActivateBuffOSAOnRC_NONE4", buffData = new BuffReferenceData { buffKeyword = "Laceration", target = "Target", turn = 1, value = 0.0f } });
                Singleton<StaticDataManager>.Instance.SkillList.GetData(1091105).skillData[1].coinList[1].abilityScriptList.Add(new AbilityData { scriptName = "ForceToActivateBuffOSAOnRC_NONE4", buffData = new BuffReferenceData { buffKeyword = "Laceration", target = "Target", turn = 1, value = 0.0f } });
                Singleton<StaticDataManager>.Instance.SkillList.GetData(1091105).skillData[1].coinList[2].abilityScriptList.Add(new AbilityData { scriptName = "ForceToActivateBuffOSAOnRC_NONE4", buffData = new BuffReferenceData { buffKeyword = "Laceration", target = "Target", turn = 1, value = 0.0f } });

                Singleton<TextDataSet>.Instance.SkillList.GetData(1091105).levelList[1].coinlist[0].coindescs.Add(new TextData_Skill_CoinDesc { desc = "<style=\"highlight\">[OnSucceedAttack] Activate [Laceration] on self and the target once (both lose 1 [Laceration] Count)</style>" });
                Singleton<TextDataSet>.Instance.SkillList.GetData(1091105).levelList[1].coinlist[1].coindescs.Add(new TextData_Skill_CoinDesc { desc = "<style=\"highlight\">[OnSucceedAttack] Activate [Laceration] on self and the target once (both lose 1 [Laceration] Count)</style>" });
                Singleton<TextDataSet>.Instance.SkillList.GetData(1091105).levelList[1].coinlist[2].coindescs.Add(new TextData_Skill_CoinDesc { desc = "<style=\"highlight\">[OnSucceedAttack] Activate [Laceration] on self and the target once (both lose 1 [Laceration] Count)</style>" });

                foreach (var x in Singleton<StaticDataManager>.Instance.SkillList.GetData(1091104).skillData[0].coinList)
                {
                    x.scale += 2;
                    x.color = "GREY";
                }

                Singleton<StaticDataManager>.Instance.SkillList.GetData(1091104).skillData[1].coinList = Singleton<StaticDataManager>.Instance.SkillList.GetData(1091104).skillData[0].coinList;
                foreach (var x in Singleton<StaticDataManager>.Instance.SkillList.GetData(1091104).skillData[1].coinList)
                {
                    x.scale += 3;
                }
            }

            // sinking
            {
                // Heatcliff S3 - Normal
                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(10710).hp._securedIncrementByLevel += new CodeStage.AntiCheat.ObscuredTypes.ObscuredFloat(0.75f);
                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(10710).hp._securedDefaultStat += new CodeStage.AntiCheat.ObscuredTypes.ObscuredInt(21);
                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(10710).resistInfo.atkResistList[0].value = 1.5f;
                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(10710).resistInfo.atkResistList[1].value = 0.75f;
                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(10710).resistInfo.atkResistList[2].value = 0.5f;



                //Singleton<StaticDataManager>.Instance.SkillList.GetData(1071005).skillData[1].defaultValue = 62;
                //Singleton<StaticDataManager>.Instance.SkillList.GetData(1071005).skillData[1].coinList[0].scale = 26;
                //Singleton<StaticDataManager>.Instance.SkillList.GetData(1071005).skillData[1].coinList[1].scale = 26;

                //Singleton<StaticDataManager>.Instance.SkillList.GetData(1071005).skillData[1].targetNum = 5;
                Singleton<StaticDataManager>.Instance.SkillList.GetData(1071003).skillData[1].targetNum = 3;

                Singleton<StaticDataManager>.Instance.SkillList.GetData(1071003).skillData[0].defaultValue = 8;
                Singleton<StaticDataManager>.Instance.SkillList.GetData(1071003).skillData[1].defaultValue = 12;

                // heir gregor

                //Log.LogInfo(JsonUtility.ToJson(Singleton<StaticDataManager>.Instance.SkillList.GetData(1120903)));
                //Log.LogInfo(JsonUtility.ToJson(Singleton<StaticDataManager>.Instance.SkillList.GetData(1060801)));
                Singleton<StaticDataManager>.Instance.SkillList.GetData(1120904).skillData[0].defaultValue = 5;
                Singleton<StaticDataManager>.Instance.SkillList.GetData(1120904).skillData[1].defaultValue = 6;

                //"abilityScriptList": [
                //            {
                //    "scriptName": "GiveBuffOnSucceedAttack",
                //                "buffData": {
                //        "buffKeyword": "Sinking",
                //                    "target": "Target",
                //                    "buffOwner": "",
                //                    "stack": 1,
                //                    "turn": 0,
                //                    "activeRound": 0,
                //                    "value": 0,
                //                    "limit": 0
                //                }
                //}
                //        ],

                //                        {
                //    "scriptName": "11209034_50",
                //                    "buffData": {
                //        "buffKeyword": "Sinking",
                //                        "target": "",
                //                        "buffOwner": "",
                //                        "stack": 10,
                //                        "turn": 0,
                //                        "activeRound": 0,
                //                        "value": 0,
                //                        "limit": 0
                //                    }
                //}

                // dieci hong lu
                //
                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(10608).resistInfo.atkResistList[0].value = 0.75f;
                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(10608).resistInfo.atkResistList[1].value = 1.5f;
                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(10608).resistInfo.atkResistList[2].value = 0.5f;


                Singleton<StaticDataManager>.Instance.SkillList.GetData(1060801).skillData[0].coinList[0].abilityScriptList.Add(new AbilityData { scriptName = "GiveBuffOnSucceedAttackHead", buffData = new BuffReferenceData { buffKeyword = "Sinking", target = "", buffOwner = "", stack = 1, turn = 0, activeRound = 0, value = 0, limit = 0 } });

                Singleton<StaticDataManager>.Instance.SkillList.GetData(1060803).skillData[1].coinList[0].abilityScriptList.Add(new AbilityData { scriptName = "GiveBuffOnSucceedAttackHead", buffData = new BuffReferenceData { buffKeyword = "Sinking", target = "", buffOwner = "", stack = 2, turn = 1, activeRound = 0, value = 0, limit = 0 } });
                Singleton<StaticDataManager>.Instance.SkillList.GetData(1060803).skillData[1].coinList[1].abilityScriptList.Add(new AbilityData { scriptName = "GiveBuffOnSucceedAttackHead", buffData = new BuffReferenceData { buffKeyword = "Sinking", target = "", buffOwner = "", stack = 2, turn = 1, activeRound = 0, value = 0, limit = 0 } });
            }

            // rupture
            {

                // 7 outis
                Singleton<StaticDataManager>.Instance.SkillList.GetData(1110401).skillData[0].coinList.Add(Singleton<StaticDataManager>.Instance.SkillList.GetData(1110401).skillData[0].coinList[0]);
                Singleton<StaticDataManager>.Instance.SkillList.GetData(1110401).skillData[1].coinList.Add(Singleton<StaticDataManager>.Instance.SkillList.GetData(1110401).skillData[0].coinList[0]);
                Singleton<StaticDataManager>.Instance.SkillList.GetData(1110401).skillData[2].coinList.Add(Singleton<StaticDataManager>.Instance.SkillList.GetData(1110401).skillData[0].coinList[0]);
                Singleton<StaticDataManager>.Instance.SkillList.GetData(1110401).skillData[3].coinList.Add(Singleton<StaticDataManager>.Instance.SkillList.GetData(1110401).skillData[0].coinList[0]);

                Singleton<StaticDataManager>.Instance.SkillList.GetData(1110401).skillData[2].defaultValue = 8;


                Singleton<StaticDataManager>.Instance.SkillList.GetData(1110402).skillData[1].defaultValue = 6;
                Singleton<StaticDataManager>.Instance.SkillList.GetData(1110402).skillData[2].defaultValue = 6;

                Singleton<StaticDataManager>.Instance.SkillList.GetData(1110403).skillData[0].defaultValue = 8;
                Singleton<StaticDataManager>.Instance.SkillList.GetData(1110403).skillData[1].defaultValue = 10;

                // silly don s3
                Singleton<TextDataSet>.Instance.SkillList.GetData(1030703).levelList[1].coinlist[1].coindescs.Add(new TextData_Skill_CoinDesc { desc = "[OnSucceedAttack] Inflict 3 [Burst]" });

                Singleton<StaticDataManager>.Instance.SkillList.GetData(1030703).skillData[1].coinList[1].abilityScriptList.Add(new AbilityData { scriptName = "GiveBuffOnSucceedAttack", buffData = new BuffReferenceData { activeRound = 0, buffKeyword = "Burst", buffOwner = "", limit = 0, stack = 3, target = "Target", turn = 0, value = 0 }, conditionalData = new ConditionalData() });

                // amazon rodya

                Singleton<StaticDataManager>.Instance.SkillList.GetData(1091003).skillData[0].coinList[0].abilityScriptList.Add(new AbilityData { scriptName = "HealSelfOnSuccessAttackByRatio70", buffData = new BuffReferenceData { activeRound = 0, buffKeyword = "", buffOwner = "", limit = 0, stack = 0, target = "", turn = 0, value = 0 }, conditionalData = new ConditionalData() });
                Singleton<StaticDataManager>.Instance.SkillList.GetData(1091003).skillData[1].coinList[0].abilityScriptList.Add(new AbilityData { scriptName = "HealSelfOnSuccessAttackByRatio100", buffData = new BuffReferenceData { activeRound = 0, buffKeyword = "", buffOwner = "", limit = 0, stack = 0, target = "", turn = 0, value = 0 }, conditionalData = new ConditionalData() });
                //Singleton<StaticDataManager>.Instance.SkillList.GetData(1091001).skillData[2].coinList[1].abilityScriptList.Add(new AbilityData { scriptName = "HealSelfOnSuccessAttackByRatio50", buffData = new BuffReferenceData { activeRound = 0, buffKeyword = "", buffOwner = "", limit = 0, stack = 0, target = "", turn = 0, value = 0 }, conditionalData = new ConditionalData() });

                //10610
                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(10610)._securedDefCorrection = new CodeStage.AntiCheat.ObscuredTypes.ObscuredInt(5);
                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(10610).resistInfo.atkResistList[0].value = 1;

                {
                    var s1 = Singleton<StaticDataManager>.Instance.SkillList.GetData(1061003).skillData[1].coinList[0];
                    var s2 = Singleton<StaticDataManager>.Instance.SkillList.GetData(1061003).skillData[1].coinList[1];
                    var s3 = Singleton<StaticDataManager>.Instance.SkillList.GetData(1061003).skillData[1].coinList[2];

                    Singleton<StaticDataManager>.Instance.SkillList.GetData(1061003).skillData[1].coinList.Clear();
                    Singleton<StaticDataManager>.Instance.SkillList.GetData(1061003).skillData[1].coinList.Add(s1);
                    Singleton<StaticDataManager>.Instance.SkillList.GetData(1061003).skillData[1].coinList.Add(s1);

                    Singleton<StaticDataManager>.Instance.SkillList.GetData(1061003).skillData[1].coinList.Add(s2);
                    Singleton<StaticDataManager>.Instance.SkillList.GetData(1061003).skillData[1].coinList.Add(s2);

                    Singleton<StaticDataManager>.Instance.SkillList.GetData(1061003).skillData[1].coinList.Add(s3);
                }

                {
                    var s1 = Singleton<StaticDataManager>.Instance.SkillList.GetData(1061003).skillData[0].coinList[0];
                    var s2 = Singleton<StaticDataManager>.Instance.SkillList.GetData(1061003).skillData[0].coinList[1];
                    var s3 = Singleton<StaticDataManager>.Instance.SkillList.GetData(1061003).skillData[0].coinList[2];

                    Singleton<StaticDataManager>.Instance.SkillList.GetData(1061003).skillData[0].coinList.Clear();
                    Singleton<StaticDataManager>.Instance.SkillList.GetData(1061003).skillData[0].coinList.Add(s1);
                    Singleton<StaticDataManager>.Instance.SkillList.GetData(1061003).skillData[0].coinList.Add(s1);

                    Singleton<StaticDataManager>.Instance.SkillList.GetData(1061003).skillData[0].coinList.Add(s2);
                    Singleton<StaticDataManager>.Instance.SkillList.GetData(1061003).skillData[0].coinList.Add(s2);

                    Singleton<StaticDataManager>.Instance.SkillList.GetData(1061003).skillData[0].coinList.Add(s3);
                }
                //Singleton<TextDataSet>.Instance.SkillList.GetData(1061003).levelList[1].coinlist[0].coindescs.Add(new TextData_Skill_CoinDesc { desc = "[OnSucceedAttack] Heal by 70% of damage dealt" });

                foreach (var x in Singleton<StaticDataManager>.Instance.SkillList.GetData(1061003).skillData[0].coinList)
                {
                    x.scale = 5;
                }
                Singleton<StaticDataManager>.Instance.SkillList.GetData(1061004).skillData[0].defaultValue = 22;

                foreach (var x in Singleton<StaticDataManager>.Instance.SkillList.GetData(1061003).skillData[1].coinList)
                {
                    x.scale = 5;
                }
                Singleton<StaticDataManager>.Instance.SkillList.GetData(1061004).skillData[1].defaultValue = 22;
            }

            // burn
            {
                // outis burn
                Log.LogWarning(1);
                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(11107).initBuffList.Add(new InitBuffPerLevel { level = 3, list = new Il2CppSystem.Collections.Generic.List<InitBuff>() });
                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(11107).initBuffList[1].list.Add(new InitBuff { buff = "FreischutzShotCount", stack = 2 });

                Log.LogWarning(11);

                Singleton<StaticDataManager>.Instance.SkillList.GetData(1110703).skillData[1].coinList[0].scale = 1;
                Singleton<StaticDataManager>.Instance.SkillList.GetData(1110703).skillData[1].coinList[0].operatorType = "MUL";

                Singleton<StaticDataManager>.Instance.SkillList.GetData(1110704).skillData[1].defaultValue = 15;
                Log.LogWarning(111);

                Singleton<StaticDataManager>.Instance.SkillList.GetData(1110704).skillData[1].abilityScriptList[0].scriptName = "GiveBuffOnBattleStart(limit:1)";
                Singleton<StaticDataManager>.Instance.SkillList.GetData(1110702).skillData[2].coinList[1].abilityScriptList.Insert(0, new AbilityData { scriptName = "GiveBuffOnSucceedAttackHead", buffData = new BuffReferenceData { buffKeyword = "DarkFlame", target = "Target", buffOwner = "", stack = 2, turn = 0, activeRound = 0, value = 0, limit = 0 } });
                Log.LogWarning(1111);

                //Singleton<TextDataSet>.Instance.SkillList.GetData(1110704).levelList[1].desc.Replace("[WhenUse] Gain 1", "[StartBattle] Gain 1");
                //Singleton<TextDataSet>.Instance.SkillList.GetData(1110702).levelList[2].coinlist[1].coindescs.Insert(0, Singleton<TextDataSet>.Instance.SkillList.GetData(1110702).levelList[2].coinlist[1].coindescs[0]);
                //Singleton<TextDataSet>.Instance.SkillList.GetData(1110702).levelList[2].coinlist[1].coindescs[0].desc = "[OnSucceedAttackHead] Inflict 2 [DarkFlame]";

                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(11107).resistInfo.atkResistList[0].value = 1.75f;
                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(11107).resistInfo.atkResistList[1].value = 0.50f;
                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(11107).resistInfo.atkResistList[2].value = 0.75f;

                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(11107).hp._securedIncrementByLevel += new CodeStage.AntiCheat.ObscuredTypes.ObscuredFloat(0.789431f);
                //GiveBuffOnSucceedAttackHead
                // burn sinclair
                //Singleton<StaticDataManager>.Instance.SkillList.GetData(1100901).skillData[1].defaultValue = 7;
                //Singleton<StaticDataManager>.Instance.SkillList.GetData(1100901).skillData[1].coinList[0].scale = 3;

                //Singleton<StaticDataManager>.Instance.SkillList.GetData(1100904).skillData[1].defaultValue = 7;
                //Singleton<StaticDataManager>.Instance.SkillList.GetData(1100904).skillData[1].coinList[0].scale = 3;
                //Singleton<StaticDataManager>.Instance.SkillList.GetData(1100904).skillData[1].coinList[0].operatorType = "MUL";

                Singleton<TextDataSet>.Instance.SkillList.GetData(1100902).levelList[1].coinlist[0].coindescs.Insert(0, new TextData_Skill_CoinDesc { desc = "[OnSucceedAttack] Inflict 2 [Combustion]" });
                Singleton<TextDataSet>.Instance.SkillList.GetData(1100902).levelList[1].coinlist[1].coindescs.Insert(0, new TextData_Skill_CoinDesc { desc = "[OnSucceedAttack] Inflict 2 [Combustion]" });
                Singleton<TextDataSet>.Instance.SkillList.GetData(1100902).levelList[1].coinlist[2].coindescs.Insert(0, new TextData_Skill_CoinDesc { desc = "[OnSucceedAttack] Inflict 2 [DefenseDown] next turn" });

                Singleton<TextDataSet>.Instance.SkillList.GetData(1100902).levelList[2].coinlist[0].coindescs.Insert(0, new TextData_Skill_CoinDesc { desc = "[OnSucceedAttack] Inflict 2 [Combustion]" });
                Singleton<TextDataSet>.Instance.SkillList.GetData(1100902).levelList[2].coinlist[1].coindescs.Insert(0, new TextData_Skill_CoinDesc { desc = "[OnSucceedAttack] Inflict 2 [Combustion]" });
                Singleton<TextDataSet>.Instance.SkillList.GetData(1100902).levelList[2].coinlist[2].coindescs.Insert(0, new TextData_Skill_CoinDesc { desc = "[OnSucceedAttack] Inflict 2 [DefenseDown] next turn" });

                Singleton<StaticDataManager>.Instance.SkillList.GetData(1100902).skillData[1].coinList[0].abilityScriptList.Insert(0, new AbilityData { scriptName = "GiveBuffOnSucceedAttack", buffData = new BuffReferenceData { buffKeyword = "Combustion", target = "Target", buffOwner = "", stack = 2, turn = 0, activeRound = 0, value = 0, limit = 0 } });
                Singleton<StaticDataManager>.Instance.SkillList.GetData(1100902).skillData[1].coinList[1].abilityScriptList.Insert(0, new AbilityData { scriptName = "GiveBuffOnSucceedAttack", buffData = new BuffReferenceData { buffKeyword = "Combustion", target = "Target", buffOwner = "", stack = 2, turn = 0, activeRound = 0, value = 0, limit = 0 } });
                Singleton<StaticDataManager>.Instance.SkillList.GetData(1100902).skillData[1].coinList[2].abilityScriptList.Insert(0, new AbilityData { scriptName = "GiveBuffOnSucceedAttack", buffData = new BuffReferenceData { buffKeyword = "Combustion", target = "Target", buffOwner = "", stack = 2, turn = 0, activeRound = 1, value = 0, limit = 0 } });
            }

            // tremor
            {
                //{
                //    "scriptName": "PileUpVibrationOSA_VibrationNesting",
                //"buffData": {
                //        "buffKeyword": "VibrationEcho",
                //  "target": "Target",
                //  "buffOwner": "Target",
                //  "stack": 0,
                //  "turn": 0,
                //  "activeRound": 0,
                //  "value": 0.0,
                //  "limit": 0
                //}


            }


            // etc
            {

                // lob ryoshu S3 - Final
                Singleton<StaticDataManager>.Instance.SkillList.GetData(1041005).skillData[0].defaultValue = 7;
                Singleton<StaticDataManager>.Instance.SkillList.GetData(1041005).skillData[1].defaultValue = 8;

                // garden of thorns - gregor
                Singleton<StaticDataManager>.Instance.EgoList.GetData(21205).GetAwakeningSkill().skillData[1].defaultValue = 30;
                Singleton<StaticDataManager>.Instance.EgoList.GetData(21205).GetAwakeningSkill().skillData[2].defaultValue = 40;

                Singleton<StaticDataManager>.Instance.EgoList.GetData(21205).GetCorrosionSkill().skillData[1].defaultValue = 35;
                Singleton<StaticDataManager>.Instance.EgoList.GetData(21205).GetCorrosionSkill().skillData[2].defaultValue = 35;

                // bygone days - yi sang - corrosion
                Singleton<StaticDataManager>.Instance.EgoList.GetData(21205).GetCorrosionSkill().skillData[1].coinList[0].abilityScriptList.Add(Singleton<StaticDataManager>.Instance.SkillList.GetData(1010403).skillData[1].coinList[2].abilityScriptList[0]);
                Singleton<StaticDataManager>.Instance.EgoList.GetData(21205).GetCorrosionSkill().skillData[2].coinList[0].abilityScriptList.Add(Singleton<StaticDataManager>.Instance.SkillList.GetData(1010403).skillData[1].coinList[2].abilityScriptList[0]);

                // sunshower yi sang                
                Singleton<TextDataSet>.Instance.SkillList.GetData(Singleton<StaticDataManager>.Instance.EgoList.GetData(20105).awakeningSkillId).levelList[1].coinlist[0].coindescs.Add(new TextData_Skill_CoinDesc { desc = "[OnSucceedAttack] Inflict 3 [Curse]" });
                Singleton<TextDataSet>.Instance.SkillList.GetData(Singleton<StaticDataManager>.Instance.EgoList.GetData(20105).awakeningSkillId).levelList[1].coinlist[0].coindescs.Add(new TextData_Skill_CoinDesc { desc = "[OnSucceedAttack] Heal by 50% of damage dealt" });

                Singleton<StaticDataManager>.Instance.EgoList.GetData(20105).GetAwakeningSkill().skillData[1].coinList[0].abilityScriptList.Add(new AbilityData { scriptName = "GiveBuffOnSucceedAttack", buffData = new BuffReferenceData { buffKeyword = "Curse", target = "Target", buffOwner = "", stack = 3, turn = 0, activeRound = 0, value = 0, limit = 0 } });
                Singleton<StaticDataManager>.Instance.EgoList.GetData(20105).GetAwakeningSkill().skillData[1].coinList[0].abilityScriptList.Add(new AbilityData { scriptName = "HealSelfOnSuccessAttackByRatio50", buffData = new BuffReferenceData { buffKeyword = "", target = "", buffOwner = "", stack = 0, turn = 0, activeRound = 0, value = 0, limit = 0 } });
            }

            // base ego
            {


                // crow's eye view
                Singleton<TextDataSet>.Instance.SkillList.GetData(Singleton<StaticDataManager>.Instance.EgoList.GetData(20101).awakeningSkillId).levelList[1].coinlist[0].coindescs.Add(new TextData_Skill_CoinDesc { desc = "[OnSucceedAttack] Inflict 2 [Curse]" });
                Singleton<TextDataSet>.Instance.SkillList.GetData(Singleton<StaticDataManager>.Instance.EgoList.GetData(20101).awakeningSkillId).levelList[1].coinlist[0].coindescs.Add(new TextData_Skill_CoinDesc { desc = "[OnSucceedAttack] Gain 1 [PlusCoinValueUp] next turn" });

                Singleton<TextDataSet>.Instance.SkillList.GetData(Singleton<StaticDataManager>.Instance.EgoList.GetData(20101).awakeningSkillId).levelList[2].coinlist[0].coindescs.Insert(2, new TextData_Skill_CoinDesc { desc = "[OnSucceedAttack] Inflict 3 [Curse]" });
                Singleton<TextDataSet>.Instance.SkillList.GetData(Singleton<StaticDataManager>.Instance.EgoList.GetData(20101).awakeningSkillId).levelList[2].coinlist[0].coindescs.Insert(3, new TextData_Skill_CoinDesc { desc = "[OnSucceedAttack] Gain 2 [PlusCoinValueUp] next turn" });

                Singleton<StaticDataManager>.Instance.EgoList.GetData(20101).GetAwakeningSkill().skillData[1].coinList[0].abilityScriptList.Add(new AbilityData { scriptName = "GiveBuffOnSucceedAttack", buffData = new BuffReferenceData { buffKeyword = "Curse", target = "Target", buffOwner = "", stack = 2, turn = 0, activeRound = 0, value = 0, limit = 0 } });
                Singleton<StaticDataManager>.Instance.EgoList.GetData(20101).GetAwakeningSkill().skillData[1].coinList[0].abilityScriptList.Add(new AbilityData { scriptName = "GiveBuffOnSucceedAttack", buffData = new BuffReferenceData { buffKeyword = "PlusCoinValueUp", target = "Self", buffOwner = "", stack = 1, turn = 0, activeRound = 1, value = 0, limit = 0 } });
                Singleton<StaticDataManager>.Instance.EgoList.GetData(20101).GetAwakeningSkill().skillData[2].coinList[0].abilityScriptList.Add(new AbilityData { scriptName = "GiveBuffOnSucceedAttack", buffData = new BuffReferenceData { buffKeyword = "Curse", target = "Target", buffOwner = "", stack = 3, turn = 0, activeRound = 0, value = 0, limit = 0 } });
                Singleton<StaticDataManager>.Instance.EgoList.GetData(20101).GetAwakeningSkill().skillData[2].coinList[0].abilityScriptList.Add(new AbilityData { scriptName = "GiveBuffOnSucceedAttack", buffData = new BuffReferenceData { buffKeyword = "PlusCoinValueUp", target = "Self", buffOwner = "", stack = 2, turn = 0, activeRound = 1, value = 0, limit = 0 } });

                // representation emitter
                Singleton<TextDataSet>.Instance.SkillList.GetData(Singleton<StaticDataManager>.Instance.EgoList.GetData(20201).awakeningSkillId).levelList[1].coinlist[0].coindescs.Add(new TextData_Skill_CoinDesc { desc = "[OnSucceedAttack] Inflict 2 [DefenseDown] next turn" });
                Singleton<TextDataSet>.Instance.SkillList.GetData(Singleton<StaticDataManager>.Instance.EgoList.GetData(20201).awakeningSkillId).levelList[2].coinlist[0].coindescs.Add(new TextData_Skill_CoinDesc { desc = "[OnSucceedAttack] Inflict 3 [DefenseDown] next turn" });


                Singleton<StaticDataManager>.Instance.EgoList.GetData(20201).GetAwakeningSkill().skillData[1].coinList[0].abilityScriptList.Add(new AbilityData { scriptName = "GiveBuffOnSucceedAttack", buffData = new BuffReferenceData { buffKeyword = "DefenseDown", target = "Target", buffOwner = "", stack = 2, turn = 0, activeRound = 1, value = 0, limit = 0 } });
                Singleton<StaticDataManager>.Instance.EgoList.GetData(20201).GetAwakeningSkill().skillData[2].coinList[0].abilityScriptList.Add(new AbilityData { scriptName = "GiveBuffOnSucceedAttack", buffData = new BuffReferenceData { buffKeyword = "DefenseDown", target = "Target", buffOwner = "", stack = 3, turn = 0, activeRound = 1, value = 0, limit = 0 } });


                // LA SANGRE DE SANCHO!!!!!
                Singleton<TextDataSet>.Instance.SkillList.GetData(Singleton<StaticDataManager>.Instance.EgoList.GetData(20301).awakeningSkillId).levelList[1].coinlist[0].coindescs.Insert(1, new TextData_Skill_CoinDesc { desc = "[OnSucceedAttack] Inflict 6 [Burst]" });
                Singleton<TextDataSet>.Instance.SkillList.GetData(Singleton<StaticDataManager>.Instance.EgoList.GetData(20301).awakeningSkillId).levelList[1].coinlist[0].coindescs.Insert(1, new TextData_Skill_CoinDesc { desc = "[OnSucceedAttack] Inflict +2 [Burst] Count" });

                Singleton<TextDataSet>.Instance.SkillList.GetData(Singleton<StaticDataManager>.Instance.EgoList.GetData(20301).awakeningSkillId).levelList[2].coinlist[0].coindescs.Insert(1, new TextData_Skill_CoinDesc { desc = "[OnSucceedAttack] Inflict 8 [Burst]" });
                Singleton<TextDataSet>.Instance.SkillList.GetData(Singleton<StaticDataManager>.Instance.EgoList.GetData(20301).awakeningSkillId).levelList[2].coinlist[0].coindescs.Insert(1, new TextData_Skill_CoinDesc { desc = "[OnSucceedAttack] Inflict +3 [Burst] Count" });

                Singleton<StaticDataManager>.Instance.EgoList.GetData(20301).GetAwakeningSkill().skillData[1].coinList[0].abilityScriptList.Insert(1, new AbilityData { scriptName = "GiveBuffOnSucceedAttack", buffData = new BuffReferenceData { buffKeyword = "Burst", target = "Target", buffOwner = "", stack = 6, turn = 0, activeRound = 0, value = 0, limit = 0 } });
                Singleton<StaticDataManager>.Instance.EgoList.GetData(20301).GetAwakeningSkill().skillData[1].coinList[0].abilityScriptList.Insert(1, new AbilityData { scriptName = "GiveBuffOnSucceedAttack", buffData = new BuffReferenceData { buffKeyword = "Burst", target = "Target", buffOwner = "", stack = 0, turn = 2, activeRound = 0, value = 1, limit = 0 } });

                Singleton<StaticDataManager>.Instance.EgoList.GetData(20301).GetAwakeningSkill().skillData[2].coinList[0].abilityScriptList.Insert(1, new AbilityData { scriptName = "GiveBuffOnSucceedAttack", buffData = new BuffReferenceData { buffKeyword = "Burst", target = "Target", buffOwner = "", stack = 8, turn = 0, activeRound = 0, value = 0, limit = 0 } });
                Singleton<StaticDataManager>.Instance.EgoList.GetData(20301).GetAwakeningSkill().skillData[2].coinList[0].abilityScriptList.Insert(1, new AbilityData { scriptName = "GiveBuffOnSucceedAttack", buffData = new BuffReferenceData { buffKeyword = "Burst", target = "Target", buffOwner = "", stack = 0, turn = 3, activeRound = 0, value = 1, limit = 0 } });

                // F.F.T.F
                Singleton<TextDataSet>.Instance.SkillList.GetData(Singleton<StaticDataManager>.Instance.EgoList.GetData(20401).awakeningSkillId).levelList[1].coinlist[0].coindescs.Insert(1, new TextData_Skill_CoinDesc { desc = "[OnSucceedAttack] Inflict 2 [Curse]" });
                Singleton<TextDataSet>.Instance.SkillList.GetData(Singleton<StaticDataManager>.Instance.EgoList.GetData(20401).awakeningSkillId).levelList[2].coinlist[0].coindescs.Insert(1, new TextData_Skill_CoinDesc { desc = "[OnSucceedAttack] Inflict 3 [Curse]" });

                Singleton<StaticDataManager>.Instance.EgoList.GetData(20401).GetAwakeningSkill().skillData[1].coinList[0].abilityScriptList.Insert(0, new AbilityData { scriptName = "GiveBuffOnSucceedAttack", buffData = new BuffReferenceData { buffKeyword = "Curse", target = "Target", buffOwner = "", stack = 2, turn = 0, activeRound = 0, value = 0, limit = 0 } });
                Singleton<StaticDataManager>.Instance.EgoList.GetData(20401).GetAwakeningSkill().skillData[2].coinList[0].abilityScriptList.Insert(0, new AbilityData { scriptName = "GiveBuffOnSucceedAttack", buffData = new BuffReferenceData { buffKeyword = "Curse", target = "Target", buffOwner = "", stack = 3, turn = 0, activeRound = 0, value = 0, limit = 0 } });

                // chains of others
                Singleton<TextDataSet>.Instance.SkillList.GetData(Singleton<StaticDataManager>.Instance.EgoList.GetData(20501).awakeningSkillId).levelList[1].coinlist[0].coindescs.Insert(4, new TextData_Skill_CoinDesc { desc = "[OnSucceedAttack] Inflict 3 [Curse]" });
                Singleton<TextDataSet>.Instance.SkillList.GetData(Singleton<StaticDataManager>.Instance.EgoList.GetData(20501).awakeningSkillId).levelList[2].coinlist[0].coindescs.Insert(4, new TextData_Skill_CoinDesc { desc = "[OnSucceedAttack] Inflict 3 [HitTakeDamageUp]" });
                Singleton<TextDataSet>.Instance.SkillList.GetData(Singleton<StaticDataManager>.Instance.EgoList.GetData(20501).awakeningSkillId).levelList[2].coinlist[0].coindescs.Insert(4, new TextData_Skill_CoinDesc { desc = "[OnSucceedAttack] Inflict 3 [Curse]" });

                Singleton<StaticDataManager>.Instance.EgoList.GetData(20501).GetAwakeningSkill().skillData[1].coinList[0].abilityScriptList.Add(new AbilityData { scriptName = "GiveBuffOnSucceedAttack", buffData = new BuffReferenceData { buffKeyword = "Curse", target = "Target", buffOwner = "", stack = 3, turn = 0, activeRound = 0, value = 0, limit = 0 } });
                Singleton<StaticDataManager>.Instance.EgoList.GetData(20501).GetAwakeningSkill().skillData[2].coinList[0].abilityScriptList.Add(new AbilityData { scriptName = "GiveBuffOnSucceedAttack", buffData = new BuffReferenceData { buffKeyword = "Curse", target = "Target", buffOwner = "", stack = 3, turn = 0, activeRound = 0, value = 0, limit = 0 } });
                Singleton<StaticDataManager>.Instance.EgoList.GetData(20501).GetAwakeningSkill().skillData[2].coinList[0].abilityScriptList.Add(new AbilityData { scriptName = "GiveBuffOnSucceedAttack", buffData = new BuffReferenceData { buffKeyword = "HitTakeDamageUp", target = "Target", buffOwner = "", stack = 3, turn = 0, activeRound = 0, value = 0, limit = 0 } });

                // hong uwu
                Singleton<TextDataSet>.Instance.SkillList.GetData(Singleton<StaticDataManager>.Instance.EgoList.GetData(20601).awakeningSkillId).levelList[1].coinlist[0].coindescs.Insert(1, new TextData_Skill_CoinDesc { desc = "[OnSucceedAttack] Inflict +3 [Sinking] Count" });
                Singleton<TextDataSet>.Instance.SkillList.GetData(Singleton<StaticDataManager>.Instance.EgoList.GetData(20601).awakeningSkillId).levelList[2].coinlist[0].coindescs.Insert(1, new TextData_Skill_CoinDesc { desc = "[OnSucceedAttack] Inflict +5 [Sinking] Count" });

                Singleton<StaticDataManager>.Instance.EgoList.GetData(20601).GetAwakeningSkill().skillData[1].coinList[0].abilityScriptList.Insert(0, new AbilityData { scriptName = "GiveBuffOnSucceedAttack", buffData = new BuffReferenceData { buffKeyword = "Sinking", target = "Target", buffOwner = "", stack = 0, turn = 3, activeRound = 0, value = 1, limit = 0 } });
                Singleton<StaticDataManager>.Instance.EgoList.GetData(20601).GetAwakeningSkill().skillData[2].coinList[0].abilityScriptList.Insert(0, new AbilityData { scriptName = "GiveBuffOnSucceedAttack", buffData = new BuffReferenceData { buffKeyword = "Sinking", target = "Target", buffOwner = "", stack = 0, turn = 5, activeRound = 0, value = 1, limit = 0 } });

                // roland v2
                Singleton<TextDataSet>.Instance.SkillList.GetData(Singleton<StaticDataManager>.Instance.EgoList.GetData(20701).awakeningSkillId).levelList[1].coinlist[0].coindescs.Insert(1, new TextData_Skill_CoinDesc { desc = "[OnSucceedAttack] Inflict 1 [Paralysis] next turn" });
                Singleton<TextDataSet>.Instance.SkillList.GetData(Singleton<StaticDataManager>.Instance.EgoList.GetData(20701).awakeningSkillId).levelList[2].coinlist[0].coindescs.Insert(1, new TextData_Skill_CoinDesc { desc = "[OnSucceedAttack] Inflict 2 [Paralysis] next turn" });


                Singleton<StaticDataManager>.Instance.EgoList.GetData(20701).GetAwakeningSkill().skillData[1].coinList[0].abilityScriptList.Insert(1, new AbilityData { scriptName = "GiveBuffOnSucceedAttack", buffData = new BuffReferenceData { buffKeyword = "Paralysis", target = "Target", buffOwner = "", stack = 1, turn = 0, activeRound = 1, value = 0, limit = 0 } });
                Singleton<StaticDataManager>.Instance.EgoList.GetData(20701).GetAwakeningSkill().skillData[2].coinList[0].abilityScriptList.Insert(1, new AbilityData { scriptName = "GiveBuffOnSucceedAttack", buffData = new BuffReferenceData { buffKeyword = "Paralysis", target = "Target", buffOwner = "", stack = 2, turn = 0, activeRound = 1, value = 0, limit = 0 } });

                // THE FAULT LIES WITHING YOU AHAB
                Singleton<TextDataSet>.Instance.SkillList.GetData(Singleton<StaticDataManager>.Instance.EgoList.GetData(20801).awakeningSkillId).levelList[1].coinlist[0].coindescs.Insert(1, new TextData_Skill_CoinDesc { desc = "[OnSucceedAttack] Inflict 1 [Paralysis] next turn" });
                Singleton<TextDataSet>.Instance.SkillList.GetData(Singleton<StaticDataManager>.Instance.EgoList.GetData(20801).awakeningSkillId).levelList[1].coinlist[0].coindescs.Insert(1, new TextData_Skill_CoinDesc { desc = "[OnSucceedAttack] Inflict 2 [Curse]" });

                Singleton<TextDataSet>.Instance.SkillList.GetData(Singleton<StaticDataManager>.Instance.EgoList.GetData(20801).awakeningSkillId).levelList[2].coinlist[0].coindescs.Insert(1, new TextData_Skill_CoinDesc { desc = "[OnSucceedAttack] Inflict 2 [Paralysis] next turn" });
                Singleton<TextDataSet>.Instance.SkillList.GetData(Singleton<StaticDataManager>.Instance.EgoList.GetData(20801).awakeningSkillId).levelList[2].coinlist[0].coindescs.Insert(1, new TextData_Skill_CoinDesc { desc = "[OnSucceedAttack] Inflict 3 [Curse]" });


                Singleton<StaticDataManager>.Instance.EgoList.GetData(20801).GetAwakeningSkill().skillData[1].coinList[0].abilityScriptList.Insert(1, new AbilityData { scriptName = "GiveBuffOnSucceedAttack", buffData = new BuffReferenceData { buffKeyword = "Paralysis", target = "Target", buffOwner = "", stack = 1, turn = 0, activeRound = 1, value = 0, limit = 0 } });
                Singleton<StaticDataManager>.Instance.EgoList.GetData(20801).GetAwakeningSkill().skillData[1].coinList[0].abilityScriptList.Insert(1, new AbilityData { scriptName = "GiveBuffOnSucceedAttack", buffData = new BuffReferenceData { buffKeyword = "Curse", target = "Target", buffOwner = "", stack = 2, turn = 0, activeRound = 0, value = 0, limit = 0 } });

                Singleton<StaticDataManager>.Instance.EgoList.GetData(20801).GetAwakeningSkill().skillData[2].coinList[0].abilityScriptList.Insert(1, new AbilityData { scriptName = "GiveBuffOnSucceedAttack", buffData = new BuffReferenceData { buffKeyword = "Paralysis", target = "Target", buffOwner = "", stack = 2, turn = 0, activeRound = 1, value = 0, limit = 0 } });
                Singleton<StaticDataManager>.Instance.EgoList.GetData(20801).GetAwakeningSkill().skillData[2].coinList[0].abilityScriptList.Insert(1, new AbilityData { scriptName = "GiveBuffOnSucceedAttack", buffData = new BuffReferenceData { buffKeyword = "Curse", target = "Target", buffOwner = "", stack = 3, turn = 0, activeRound = 0, value = 0, limit = 0 } });


                // What is cast
                Singleton<TextDataSet>.Instance.SkillList.GetData(Singleton<StaticDataManager>.Instance.EgoList.GetData(20901).awakeningSkillId).levelList[1].coinlist[0].coindescs.Insert(1, new TextData_Skill_CoinDesc { desc = "[OnSucceedAttack] Inflict 3 [Curse]" });
                Singleton<TextDataSet>.Instance.SkillList.GetData(Singleton<StaticDataManager>.Instance.EgoList.GetData(20901).awakeningSkillId).levelList[2].coinlist[0].coindescs.Insert(1, new TextData_Skill_CoinDesc { desc = "[OnSucceedAttack] Inflict 5 [Curse]" });


                Singleton<StaticDataManager>.Instance.EgoList.GetData(20901).GetAwakeningSkill().skillData[1].coinList[0].abilityScriptList.Insert(1, new AbilityData { scriptName = "GiveBuffOnSucceedAttack", buffData = new BuffReferenceData { buffKeyword = "Curse", target = "Target", buffOwner = "", stack = 3, turn = 0, activeRound = 0, value = 0, limit = 0 } });
                Singleton<StaticDataManager>.Instance.EgoList.GetData(20901).GetAwakeningSkill().skillData[2].coinList[0].abilityScriptList.Insert(1, new AbilityData { scriptName = "GiveBuffOnSucceedAttack", buffData = new BuffReferenceData { buffKeyword = "Curse", target = "Target", buffOwner = "", stack = 5, turn = 0, activeRound = 0, value = 0, limit = 0 } });

                // clair of sins
                Singleton<TextDataSet>.Instance.SkillList.GetData(Singleton<StaticDataManager>.Instance.EgoList.GetData(21001).awakeningSkillId).levelList[1].coinlist[0].coindescs.Insert(1, new TextData_Skill_CoinDesc { desc = "[OnSucceedAttack] Inflict +5 [Burst] Count" });
                Singleton<TextDataSet>.Instance.SkillList.GetData(Singleton<StaticDataManager>.Instance.EgoList.GetData(21001).awakeningSkillId).levelList[2].coinlist[0].coindescs.Insert(1, new TextData_Skill_CoinDesc { desc = "[OnSucceedAttack] Inflict +5 [Burst] Count" });


                Singleton<StaticDataManager>.Instance.EgoList.GetData(21001).GetAwakeningSkill().skillData[1].coinList[0].abilityScriptList.Insert(1, new AbilityData { scriptName = "GiveBuffOnSucceedAttack", buffData = new BuffReferenceData { buffKeyword = "Burst", target = "Target", buffOwner = "", stack = 0, turn = 0, activeRound = 3, value = 0, limit = 1 } });
                Singleton<StaticDataManager>.Instance.EgoList.GetData(21001).GetAwakeningSkill().skillData[2].coinList[0].abilityScriptList.Insert(1, new AbilityData { scriptName = "GiveBuffOnSucceedAttack", buffData = new BuffReferenceData { buffKeyword = "Burst", target = "Target", buffOwner = "", stack = 0, turn = 0, activeRound = 5, value = 0, limit = 1 } });

                // outis
                Singleton<StaticDataManager>.Instance.EgoList.GetData(21101).GetAwakeningSkill().skillData[1].coinList[0].scale = 6;
                Singleton<StaticDataManager>.Instance.EgoList.GetData(21101).GetAwakeningSkill().skillData[2].coinList[0].scale = 8;

                // ring hag v2
                Singleton<StaticDataManager>.Instance.EgoList.GetData(21201).GetAwakeningSkill().skillData[1].coinList[0].operatorType = "MUL";
                Singleton<StaticDataManager>.Instance.EgoList.GetData(21201).GetAwakeningSkill().skillData[1].coinList[0].scale = 2;

                Singleton<StaticDataManager>.Instance.EgoList.GetData(21201).GetAwakeningSkill().skillData[2].coinList[0].operatorType = "MUL";
                Singleton<StaticDataManager>.Instance.EgoList.GetData(21201).GetAwakeningSkill().skillData[2].coinList[0].scale = 3;
            }


            patched = true;
            Log.LogInfo("Patching sucessful!");
        
        }
    }

    [HarmonyPatch(typeof(AskSettingsPopup), "OpenSettingsPopup")]
    [HarmonyPrefix]
    public static bool AskSettingsPopup_OpenSettingsPopup_Pre()
    {
        Log.LogInfo("Settings patch");

        //var stage = Singleton<StaticDataManager>.Instance.GetStage(10733);
        //Formation formation = Singleton<FormationList>.Instance.GetData(0);
        //PlayerUnitFormation fm = new PlayerUnitFormation(formation, null, false, 0, null, null);
        //var pl = new Il2CppSystem.Collections.Generic.List<PlayerUnitData>();
        ////pl.Add(new PlayerUnitData(new CustomPersonality(10301, 50, 4, 0, true, 0, PERSONALITY_TYPES.USER), null, true, false));
        ////fm.AddPlayerAndSort(pl);
        ////Singleton<StageController>.Instance.InitStageModel(stage, STAGE_TYPE.NORMAL_BATTLE, null, false, fm);
        //_skip1 = true;
        //GlobalGameManager.Instance.EnterStage(stage, -1, -1, -1, 10733, STAGE_TYPE.NORMAL_BATTLE, null, true, fm);

        //GlobalGameManager.Instance.EnterStage(stage, -1, -1, -1, 10701, STAGE_TYPE.STORY_DUNGEON, null, false, fm);

        if (!_asPatched)
        {
            Log.LogInfo("patching...");

            // Debug stick
            {
                int debug_id = 10101;
                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(debug_id).resistInfo.atkResistList[0].value = 0f;
                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(debug_id).resistInfo.atkResistList[1].value = -20f;
                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(debug_id).resistInfo.atkResistList[2].value = -255f;
                Singleton<TextDataSet>.Instance.PersonalityList.GetData(debug_id).title = "Sang Yi";
                Singleton<TextDataSet>.Instance.PersonalityList.GetData(debug_id).oneLineTitle = "Debug stick";
                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(debug_id)._securedDefCorrection = new CodeStage.AntiCheat.ObscuredTypes.ObscuredInt(1000);
                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(debug_id).defCorrection = 2000;
                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(debug_id).hp._securedDefaultStat = new CodeStage.AntiCheat.ObscuredTypes.ObscuredInt(32767);
                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(debug_id)._securedAggro = new CodeStage.AntiCheat.ObscuredTypes.ObscuredInt(100);
            }

            // verg
            {
                int vg_id = 10507;
                int vg_ds = 350;
                float vg_il = 15f;
                int vg_aggro = 1;
                //aplist.Add(vg_id, ("SD_Personality", "9999_VergiliusAppearance"));
                aplist.Add(vg_id, ("SD_Personality", "9999_Vergilius_EgoAppearance"));

                Singleton<TextDataSet>.Instance.PersonalityList.GetData(vg_id).title = "Vergilius";
                Singleton<TextDataSet>.Instance.PersonalityList.GetData(vg_id).oneLineTitle = "The Red Gaze ";

                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(vg_id).resistInfo.atkResistList[0].value = 0.50f;
                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(vg_id).resistInfo.atkResistList[1].value = 0.50f;
                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(vg_id).resistInfo.atkResistList[2].value = 0.50f;

                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(vg_id).panicType = 9997;
                //Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(vg_id)._uniqueAttribute = ATTRIBUTE_TYPE.SCARLET;

                AddPassive(vg_id, 999901);
                AddPassive(vg_id, 999902);
                AddPassive(vg_id, 999903);
                AddPassive(vg_id, 999904);
                AddPassive(vg_id, 999905);
                AddPassive(vg_id, 999906);
                AddPassive(vg_id, 999907);
                AddPassive(vg_id, 999908);
                AddPassive(vg_id, 9999998);

                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(vg_id).attributeList.Clear();
                Singleton<StaticDataManager>.Instance.SkillList.GetData(999901).skillData[0].attributeType = "SCARLET";

                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(vg_id).attributeList.Add(new UnitAttribute { skillId = 9990401, number = 1 });
                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(vg_id).attributeList.Add(new UnitAttribute { skillId = 9990402, number = 1 });
                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(vg_id).attributeList.Add(new UnitAttribute { skillId = 9990403, number = 1 });
                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(vg_id).attributeList.Add(new UnitAttribute { skillId = 9990404, number = 1 });
                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(vg_id).attributeList.Add(new UnitAttribute { skillId = 9990405, number = 1 });
                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(vg_id).attributeList.Add(new UnitAttribute { skillId = 9990406, number = 1 });
                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(vg_id).attributeList.Add(new UnitAttribute { skillId = 9990407, number = 1 });
                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(vg_id).attributeList.Add(new UnitAttribute { skillId = 9990408, number = 1 });
                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(vg_id).attributeList.Add(new UnitAttribute { skillId = 9990409, number = 1 });

                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(vg_id).hp._securedDefaultStat = new CodeStage.AntiCheat.ObscuredTypes.ObscuredInt(vg_ds);
                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(vg_id).hp._securedIncrementByLevel = new CodeStage.AntiCheat.ObscuredTypes.ObscuredFloat(vg_il);

                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(vg_id).breakSection = new BreakSectionInfo
                {
                    sectionList = new Il2CppSystem.Collections.Generic.List<int>()
                };
                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(vg_id).breakSection.sectionList.Add(20);
                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(vg_id).breakSection.sectionList.Add(15);
                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(vg_id).breakSection.sectionList.Add(10);
                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(vg_id)._securedAggro = new CodeStage.AntiCheat.ObscuredTypes.ObscuredInt(vg_aggro);
            }

            // xiao
            {
                int xi_id = 10806;
                int xi_ds = 90;
                float xi_il = 3f;
                int xi_aggro = 2;

                Singleton<TextDataSet>.Instance.PersonalityList.GetData(xi_id).title = "Xiao";
                Singleton<TextDataSet>.Instance.PersonalityList.GetData(xi_id).oneLineTitle = "Iron Lotus ";

                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(xi_id).resistInfo.atkResistList[0].value = 0.50f;
                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(xi_id).resistInfo.atkResistList[1].value = 1f;
                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(xi_id).resistInfo.atkResistList[2].value = 1.25f;


                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(xi_id).attributeList.Clear();
                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(xi_id).attributeList.Add(new UnitAttribute { skillId = 9990201, number = 3 });
                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(xi_id).attributeList.Add(new UnitAttribute { skillId = 9990202, number = 2 });
                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(xi_id).attributeList.Add(new UnitAttribute { skillId = 9990203, number = 1 });
                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(xi_id).attributeList.Add(new UnitAttribute { skillId = 9990204, number = 1 });

                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(xi_id).hp = new StatByLevel
                {
                    defaultStat = xi_ds,
                    _securedDefaultStat = new CodeStage.AntiCheat.ObscuredTypes.ObscuredInt(xi_ds),
                    _securedIncrementByLevel = new CodeStage.AntiCheat.ObscuredTypes.ObscuredFloat(xi_il),
                    incrementByLevel = xi_il,
                    _currentLevel = new CodeStage.AntiCheat.ObscuredTypes.ObscuredInt(Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(xi_id).rank),
                    _currentStat = new CodeStage.AntiCheat.ObscuredTypes.ObscuredInt(xi_ds),
                    _isInitialized = true,
                };
                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(xi_id).breakSection = new BreakSectionInfo
                {
                    sectionList = new Il2CppSystem.Collections.Generic.List<int>()
                };
                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(xi_id).breakSection.sectionList.Add(55);
                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(xi_id).breakSection.sectionList.Add(35);
                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(xi_id).breakSection.sectionList.Add(15);
                var dss = new Il2CppSystem.Collections.Generic.List<CodeStage.AntiCheat.ObscuredTypes.ObscuredInt>();
                dss.Add(new CodeStage.AntiCheat.ObscuredTypes.ObscuredInt(9990205));

                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(xi_id)._securedAggro = new CodeStage.AntiCheat.ObscuredTypes.ObscuredInt(xi_aggro);
                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(xi_id)._securedDefenseSkillIDList = dss;
            }

            Log.LogInfo("Patch done!");
            _asPatched = true;
            return false;
        }
        //}
        //Singleton<StaticDataManager>.Instance.SkillList.GetList().Find((SkillStaticData x) => x.ID == 1030103).skillData[0].defaultValue = 99;
        //Debug.Log(JsonUtility.ToJson(Singleton<StaticDataManager>.Instance.SkillList.GetList().Find((SkillStaticData x) => x.ID == 1030103)));

        //this._stageModel.SetFormation(formationInfo);

        //GlobalGameManager.Instance.EnterStage(-1, -1, -1, 10218, STAGE_TYPE.NORMAL_BATTLE, );
        //GlobalGameManager.Instance.EnterStage(-1, -1, -1, 10218, );
        //StageManager

        //HotPatch();
        return true;
    }
    ////[HarmonyPatch("Execute")]
    //[HarmonyPatch(typeof(LoginInfoManager), "ProviderLogin_Steam")]
    //[HarmonyPrefix]
    //public static bool loginhandler(DelegateEvent callback)
    //{
    //    // Your logic after the original method executes
    //    //Console.WriteLine("After Execute method");
    //    Log.LogInfo("login patch");
    //    SoundGenerator.Init();
    //    Debug.Log("dev login");
    //    //GlobalGameManager.Instance.Login_DEV();
    //    //callback.Invoke();
    //    GlobalGameManager.Instance.LoadUserDataAndSetScene(GlobalGameManager.Instance.sceneState);
    //    Debug.Log("dev work");
    //    return false;
    //}

    //[HarmonyPatch(typeof(SimpleCrypto), "HexToBytes")]
    //[HarmonyPrefix]
    //public static bool sch(System.String hex)
    //{
    //    // Your logic after the original method executes
    //    //Console.WriteLine("After Execute method");
    //    Log.LogInfo("funny patch rn: " + hex);
    //    //Log.LogInfo("Server response: " + responseJson);
    //    return false;
    //}

    //[HarmonyPatch(typeof(SimpleCrypto), "Encrypt")]
    //[HarmonyPrefix]
    //public static bool sce(System.Byte[] bytes, System.Int64 encryptedTime)
    //{
    //    // Your logic after the original method executes
    //    //Console.WriteLine("After Execute method");
    //    Log.LogInfo("funny patch rn 2: " + encryptedTime);
    //    Log.LogInfo("funny patch rn 2: " + bytes);
    //    //Log.LogInfo("Server response: " + responseJson);
    //    return false;
    //}

    //[HarmonyPatch(typeof(SimpleCrypto), "BytesToHex")]
    //[HarmonyPrefix]
    //public static bool scb(System.Byte[] bytes)
    //{
    //    // Your logic after the original method executes
    //    //Console.WriteLine("After Execute method");
    //    Log.LogInfo("funny patch rn 3: " + bytes);
    //    //Log.LogInfo("Server response: " + responseJson);
    //    return false;
    //}

    //[HarmonyPatch(typeof(HttpApiRequester), "PostWebRequest")]
    //[HarmonyPrefix]
    //static bool PostWebRequest(HttpApiRequester __instance, System.Collections.IEnumerator __result, HttpApiSchema httpApiSchema, bool isUrgent)
    //{
    //    using (UnityWebRequest www = UnityWebRequest.Post(httpApiSchema.URL, httpApiSchema.RequestJson))
    //    {
    //        byte[] bytes = new UTF8Encoding().GetBytes(httpApiSchema.RequestJson);
    //        www.uploadHandler.Dispose();
    //        www.uploadHandler = new UploadHandlerRaw(bytes);
    //        www.SetRequestHeader("Content-Type", "application/json");
    //        Debug.Log("PostWebRequest to " + httpApiSchema.URL);
    //        Debug.Log("request : " + httpApiSchema.RequestJson);
    //        __instance.networkingUI.ActiveConnectingText(true);
    //        __result = www.SendWebRequest();
    //        __instance.networkingUI.ActiveConnectingText(false);
    //        if (!isUrgent)
    //        {
    //            if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
    //            {
    //                if (Application.internetReachability == NetworkReachability.NotReachable)
    //                {
    //                    __instance.OnResponseWithErrorCode(httpApiSchema, -4, false, true);
    //                }
    //                else
    //                {
    //                    __instance.OnResponseWithErrorCode(httpApiSchema, 0, false, true);
    //                }
    //            }
    //            else
    //            {
    //                string text = www.downloadHandler.text;
    //                //DelegateEventString a = httpApiSchema._responseEvent;
    //                //httpApiSchema._responseEvent(text);
    //            }
    //        }
    //    }
    //    UnityWebRequest www = null;
    //    return false;
    //}
}
