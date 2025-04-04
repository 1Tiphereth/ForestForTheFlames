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
using Dungeon;
using System.Reflection;
using static Addressable.ResourceKeyBuilder;

namespace ForestForTheFlames;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, "YumYum Enterprises", MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BasePlugin
{
    internal static new ManualLogSource Log;
    //internal static new Logger Logger = new(Logger.BACKEND.BEPINEX, Log);
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
        Harmony.CreateAndPatchAll(typeof(Scaffold));

        Log = base.Log;
        Logger.Log($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded! + " + "C:\\Program Files (x86)\\Steam\\steamapps\\common\\Limbus Company\\BepInEx\\plugins\\");

        egolist.Add("Enemy", new System.Collections.Generic.List<(int, int, int)>());
        egolist.Add("Player", new System.Collections.Generic.List<(int, int, int)>());
    }


    internal static (bool, string, string) requestHeaders = (false, "-", "-");
    [HarmonyPatch(typeof(HttpApiRequester), "AddRequest")]
    [HarmonyPrefix]
    public static bool AddRequest(HttpApiRequester __instance, JIMKEFOFIME NEKAOPOFEOI, int KGHBFJENODO = 0)
    {
        //var a = new HttpApiRequester();
        //a.AddRequest()
        //httpApiSchema._url.Replace("https://www.limbuscompanyapi.com", SERVER_URL);
        var httpApiSchema = NEKAOPOFEOI;
        var priority = KGHBFJENODO;
        if (!requestHeaders.Item1)
        {
            foreach (var fi in httpApiSchema.GetType().GetFields(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (fi.Name.Contains("Public_String_"))
                {
                    var fin = fi.Name.Substring(20).Replace("_Public_get_String_0", "").Replace("_Public_String_0", "");
                    MethodInfo obj = null;
                    foreach (var fm in httpApiSchema.GetType().GetRuntimeMethods())
                    {

                        if (fm.Name.Contains(fin))
                        {
                            obj = fm;
                        }
                    }

                    var r = (string)obj.Invoke(httpApiSchema, null);
                    if (r.Contains("http")) // https://www.limbuscompanyapi.com
                    {
                        requestHeaders.Item2 = obj.Name;
                    }
                    else if (r.Contains("userAuth"))
                    {
                        requestHeaders.Item3 = obj.Name;
                    }
                }
            }
            requestHeaders.Item1 = true;
            Logger.Look(requestHeaders);
        }
        Logger.Log(httpApiSchema.GetType().GetMethod(requestHeaders.Item2).Invoke(httpApiSchema, null) + " : " + httpApiSchema.GetType().GetMethod(requestHeaders.Item3).Invoke(httpApiSchema, null));
        __instance.CIIDJFEEBCC.Enqueue(httpApiSchema, priority);
        __instance.ProceedRequest();
        return false;
    }

    public static void Callback(object any)
    {
        Logger.Log((string)any);
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
        Logger.Log($"Skill: Preparing to load {path}");
        string contents = File.ReadAllText($@"{DATA_PATH}\json\{path}");
        jlist.Add(JSONNode.Parse(contents));
    }

    public static void InitSkills()
    {
        Logger.Log("Loading skills");
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
        Logger.Log("Finished loading skills");
        //Logger.Log("Clearing [jlist]");
        //jlist.Clear();
    }

    public static void PrepareLocalize(string path)
    {
        Logger.Log($"Localize: Preparing to load {path}");
        var p = Path.GetDirectoryName("C:\\Program Files (x86)\\Steam\\steamapps\\common\\Limbus Company\\BepInEx\\plugins\\");
        string contents = File.ReadAllText($@"{DATA_PATH}\json\{path}");
        jlist.Add(JSONNode.Parse(contents));
    }

    public static void InitLocalize()
    {
        Logger.Log("Loading localization");
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
        Logger.Log("Finished loading localization");
        //Logger.Log("Clearing [lclist]");
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
        Logger.Log($"Adding passive {pid} to {id}");
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
        Logger.Log($"Removing passive {pid} from {id}");
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
        Logger.Log($"AbnoUnit: Loading {path}");
        var p = Path.GetDirectoryName("C:\\Program Files (x86)\\Steam\\steamapps\\common\\Limbus Company\\BepInEx\\plugins\\");
        string contents = File.ReadAllText($@"{DATA_PATH}\json\unit\Abno\{path}");
        var y = JsonUtility.FromJson<AbnormalityStaticData>(contents.ToString());
        Singleton<StaticDataManager>.Instance.AbnormalityUnitList.list.Add(y);
    }
    public static void LoadAbnoPartUnit(string path)
    {
        Logger.Log($"AbnoUnitPart: Loading {path}");
        var p = Path.GetDirectoryName("C:\\Program Files (x86)\\Steam\\steamapps\\common\\Limbus Company\\BepInEx\\plugins\\");
        string contents = File.ReadAllText($@"{DATA_PATH}\json\unit\Abno\{path}");
        var y = JsonUtility.FromJson<AbnormalityPartStaticData>(contents.ToString());
        Singleton<StaticDataManager>.Instance.AbnormalityPartList.list.Add(y);
    }

    public static void LoadBuffStatic(string path)
    {
        Logger.Log($"BuffStatic: Loading {path}");
        var p = Path.GetDirectoryName("C:\\Program Files (x86)\\Steam\\steamapps\\common\\Limbus Company\\BepInEx\\plugins\\");
        string contents = File.ReadAllText($@"{DATA_PATH}\json\buff\{path}");
        var y = JsonUtility.FromJson<BuffStaticData>(contents.ToString());
        Singleton<StaticDataManager>.Instance.BuffList.list.Add(y);
    }

    public static void LoadPersonality(string path)
    {
        Logger.Log($"Personality: Loading {path}");
        var p = Path.GetDirectoryName("C:\\Program Files (x86)\\Steam\\steamapps\\common\\Limbus Company\\BepInEx\\plugins\\");
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
        Logger.Log($"EgoLoaderAndTextPlusStatic: Loading {path}");
        var p = Path.GetDirectoryName("C:\\Program Files (x86)\\Steam\\steamapps\\common\\Limbus Company\\BepInEx\\plugins\\");
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
        Logger.Log($"ExpStage: Preparing to load {path}");
        var p = Path.GetDirectoryName("C:\\Program Files (x86)\\Steam\\steamapps\\common\\Limbus Company\\BepInEx\\plugins\\");
        string contents = File.ReadAllText($@"{DATA_PATH}\json\stage\{path}");
        eslist.Add(id, JSONNode.Parse(contents));
    }

    public static void InitExpStage()
    {
        Logger.Log("Loading ExpStages");
        foreach (var x in eslist)
        {
            var y = JsonUtility.FromJson<StageStaticData>(x.Value.ToString());
            var i = x.Key;
            Logger.Log($"ExpStage: Loading id:{i}");

            Singleton<StaticDataManager>.Instance.ExpDungeonBattleList.GetStage(i).hasGoldenBough = y.hasGoldenBough;
            Singleton<StaticDataManager>.Instance.ExpDungeonBattleList.GetStage(i).stageScriptList = y.stageScriptList;
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
        Logger.Log("Finished loading ExpStages");
        //Logger.Log("Clearing [jlist]");
        //jlist.Clear();
    }

    [HarmonyPatch(typeof(ResourceKeyBuilder), "BuildSdResourceKeyInfo")]
    [HarmonyPostfix]
    public static void ResourceLogger(ResourceKeyBuilder.SdResourceType type, string id)
    {
       Logger.Log(type + ":" + id);
    }

    [HarmonyPatch(typeof(ResourceKeyBuilder), "BuildUnitResourceKeyInfo", [typeof(UnitResourceType), typeof(string), typeof(bool), typeof(string)])]
    [HarmonyPostfix]
    public static void ResourceLogger2(ResourceKeyBuilder.UnitResourceType type, string id)
    {
       Logger.Log(type + ":" + id);
    }
    //static internal System.Collections.Generic.List<object> pss = new System.Collections.Generic.List<object>();


    //[HarmonyPatch(typeof(PassiveModel), MethodType.Constructor, new Type[] { typeof(PassiveStaticData) })]
    //[HarmonyPrefix]
    //public static bool CustomPassivePatcher(PassiveModel __instance, PassiveStaticData info)
    //{
    //    foreach (var x in System.Reflection.Assembly.GetExecutingAssembly().GetTypes())
    //    {
    //        Logger.Log(x.FullName);
    //    }
    //    if (1 == 1)
    //    {
    //        //Type type = Type.GetType("PassiveAbility" + "_" + info.ID.ToString());
    //        Type type = Assembly.GetExecutingAssembly().GetType("ForestForTheFlames.PassiveAbility_1");
    //        Log.LogFatal(type == null);
    //        //Type type = Type.GetType("PassiveAbility_9991001");
    //        Log.LogFatal(type.FullName);
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
    //        Logger.Log(x.FullName);
    //    }

    //    {
    //        Type type = Type.GetType("PassiveAbility" + "_" + info.ID.ToString());
    //        //PassiveAbility type = new PassiveAbility_1();
    //        Log.LogFatal(type == null);
    //        Log.LogFatal("Passive 55555555");
    //        if (type != null)
    //        {
    //            Log.LogFatal("Passive 1111");

    //            PassiveAbility script = (type as PassiveAbility);
    //            __instance._script = script;
    //            __instance._script._id = -1;
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
    //    Logger.Log($"Response: {responseJson}");
    //    //Log.LogFatal($"LoadingAsset: {label}/{resourceId}");
    //}


    //[HarmonyPatch(typeof(BattleUnitView), "CreateBloomAppearance")]
    //[HarmonyPostfix]
    //public static void eelog(UnitScript_EGOTransformation unitScript)
    //{
    //    Logger.Log($"{unitScript.appearanceid} {unitScript.effect_egoStart_name} {unitScript.effect_egoEnd_name}");
    //}
    //[HarmonyPatch(typeof(BattleUnitView), "AddBloomAppearance")]
    //[HarmonyPostfix]
    //public static void eelog2(UnitScript_EGOTransformation unitScript)
    //{
    //    Logger.Log($"{unitScript.appearanceid} {unitScript.effect_egoStart_name} {unitScript.effect_egoEnd_name}");
    //}
    //[HarmonyPatch(typeof(BattleUnitView), "ChangeBloomAppearance")]
    //[HarmonyPostfix]
    //public static void eelog3(UnitScript_EGOTransformation unitScript)
    //{
    //    Logger.Log($"{unitScript.appearanceid} {unitScript.effect_egoStart_name} {unitScript.effect_egoEnd_name}");
    //}

    //[HarmonyPatch(typeof(SDCharacterSkinUtil), "CreateSkin", [typeof(BattleUnitView), typeof(string), typeof(Transform), typeof(DelegateEvent)])]
    //[HarmonyPostfix]
    //public static void egologger(BattleUnitView view, string appearanceID)
    //{
    //    Logger.Log(appearanceID);
    //    //SDCharacterSkinUtil.CreateSkin
    //    // [DefaultParameterValue(null)] BattleUnitView view, [DefaultParameterValue(null)] string appearanceID, [DefaultParameterValue(null)] Transform parent, [DefaultParameterValue(null)] out DelegateEvent handle
    //}

    [HarmonyPatch(typeof(BattleUnitView), "Init")]
    [HarmonyPostfix]
    public static void UniversalSkinPatcher(BattleUnitView __instance, BattleUnitModel model, int instanceID, int level, int gaksungLevel)
    {
        int id = model.UnitDataModel.ClassInfo.ID;
        foreach (var x in __instance._appearances)
        {
            Logger.Log($"{model.IsAbnormalityOrPart}:{model.GetCharacterID()}:{id}: {x.name}");
            //Logger.Log($"{id}: {x.GetScriptClassName()}");
        }
        foreach (var x in __instance._changedAppearanceList)
        {
            Logger.Log($"Changed: {model.GetCharacterID()}:{id}: {x.appearance.name}");
            //Logger.Log($"Changed: {id}: {x.appearance.GetScriptClassName()}");
        }
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
                        //Logger.Log($"{id}: {x.name}");
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
                    UnityEngine.Debug.LogError(model.GetAppearanceID() + " is not exist");
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
            //foreach (var x in __instance._appearances)
            //{
            //    Logger.Log(x.name);
            //}
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

    //[HarmonyPatch(typeof(BattleUnitModel), "Init")]
    //[HarmonyPostfix]
    //public static void egogift(BattleUnitModel __instance)
    //{
    //    if (__instance.GetCharacterID() != -1)
    //    {
    //        foreach ((int eid, int pr, int id) in egolist["Player"])
    //        {
    //            if (id != 0 && id == __instance.UnitDataModel._classInfo.ID)
    //            {
    //                __instance.AddEgoGiftAbility(Singleton<StaticDataManager>.Instance.EgoGiftDataMediator.GetEgoGiftAbilityById(eid, pr));
    //            }
    //            else if (id == 0)
    //            {
    //                __instance.AddEgoGiftAbility(Singleton<StaticDataManager>.Instance.EgoGiftDataMediator.GetEgoGiftAbilityById(eid, pr));
    //            }
    //        }
    //    }
    //    else
    //    {
    //        foreach ((int eid, int pr, int id) in egolist["Enemy"])
    //        {
    //            if (id != 0 && id == __instance.UnitDataModel._classInfo.ID)
    //            {
    //                __instance.AddEgoGiftAbility(Singleton<StaticDataManager>.Instance.EgoGiftDataMediator.GetEgoGiftAbilityById(eid, pr));
    //            }
    //            else if (id == 0)
    //            {
    //                __instance.AddEgoGiftAbility(Singleton<StaticDataManager>.Instance.EgoGiftDataMediator.GetEgoGiftAbilityById(eid, pr));
    //            }
    //        }
    //    }
    //}

    [HarmonyPatch(typeof(StageController), "InitStage")]
    [HarmonyPrefix]
    public static void DungeonFixer(StageController __instance)
    {
        return;
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


            //gifts.Add(new DungeonMapEgoGift(new DungeonMapEgoGiftFormat(9004))); // Phlebotomy Pack - only works via old method
            //gifts.Add(new DungeonMapEgoGift(new MHCFNLCHAEN(9014))); // Phlebotomy Pack - only works via old method
            //gifts.Add(new DungeonMapEgoGift(new DungeonMapEgoGiftFormat(9017))); // Phlebotomy Pack - only works via old method
            //gifts.Add(new DungeonMapEgoGift(new DungeonMapEgoGiftFormat(9021))); // Phlebotomy Pack - only works via old method
            //gifts.Add(new DungeonMapEgoGift(new DungeonMapEgoGiftFormat(9058))); // Phlebotomy Pack - only works via old method
            //gifts.Add(new DungeonMapEgoGift(new DungeonMapEgoGiftFormat(9068))); // Phlebotomy Pack - only works via old method
            //gifts.Add(new DungeonMapEgoGift(new DungeonMapEgoGiftFormat(9118))); // Phlebotomy Pack - only works via old method
            //gifts.Add(new DungeonMapEgoGift(new DungeonMapEgoGiftFormat(9153))); // Phlebotomy Pack - only works via old method
            //gifts.Add(new DungeonMapEgoGift(new DungeonMapEgoGiftFormat(9419))); // Phlebotomy Pack - only works via old method
            //gifts.Add(new DungeonMapEgoGift(new DungeonMapEgoGiftFormat(9423))); // Phlebotomy Pack - only works via old method
            //gifts.Add(new DungeonMapEgoGift(new DungeonMapEgoGiftFormat(9710))); // Phlebotomy Pack - only works via old method
            //gifts.Add(new DungeonMapEgoGift(new DungeonMapEgoGiftFormat(9739))); // Phlebotomy Pack - only works via old method
            //gifts.Add(new DungeonMapEgoGift(new DungeonMapEgoGiftFormat(9154))); // Phlebotomy Pack - only works via old method


            //gifts.Add(new DungeonMapEgoGift(9004)); // Phlebotomy Pack - only works via old method

            int[] gids = new int[]
            {
                9014,9017,9021,9058,9068,9118,9153,9419,9423,9710,9739,9154
            };
            foreach (var gid in gids)
            {
                gifts.Add(new DungeonMapEgoGift(new FBHBFFIGHHK(gid)));
            }
            //gifts.Add(new DungeonMapEgoGift(new MHCFNLCHAEN(9014))); // Rusty Commemorative Coin
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
        //}

        //[HarmonyPatch(typeof(MirrorDungeonProgressBridge), "GetAdditionalEgoGiftAbilityNames")]
        //[HarmonyPrefix]
        //public static bool DungeonFixer77(Il2CppSystem.Collections.Generic.List<EgoGiftAbilityNameData> __result)
        //{
        //    Logger.Log("DungeonFixer77");
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
                Logger.Log("DungeonFixer1");
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
                Logger.Log("DungeonFixer2");
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
                Logger.Log("DungeonFixer3");
                DungeonProgressManager._isOnDungeon = true;
            }
            //return false;
        }
        [HarmonyPatch(typeof(GlobalGameManager), "LeaveStage")]
        [HarmonyPrefix]
        public static void DungeonFixer4()
        {
            Scaffold.pss.Clear();
            if (_egostage)
            {
                Logger.Log("DungeonFixer4");
                DungeonProgressManager._isOnDungeon = false;
                DungeonProgressManager.ClearData();
                _egostage = false;
            }
            //return false;
        }



    //[HarmonyPatch(typeof(PassiveModel), new[] { typeof(PassiveStaticData) })]
    //[HarmonyPatch(MethodType.Constructor)]
    //[HarmonyPostfix]
    //public static void PassiveModel_Constructor(PassiveModel __instance, PassiveStaticData info)
    //{
    //    Logger.Log("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA");
    //    try
    //    {
    //        if (info.ID < 0)
    //        {
    //            Logger.Log("found <0 passive");
    //            __instance._script = (PassiveAbility)Activator.CreateInstance(System.Type.GetType($"ForestForTheFlames.PassiveAbility_{System.Math.Abs(info.id)}"));
    //            __instance._classInfo = info;
    //            __instance._classInfo.id = info.id;
    //            Logger.Log("done lolll +" + __instance.Script == null);
    //        }
    //    } catch
    //    {
    //        Logger.Log("passive broke lmao");
    //    }
    //}

    //[HarmonyPatch(typeof(BuffAbilityManager), "GetType", [typeof(string)])]
    //[HarmonyPrefix]
    //public static void BuffAbilityManager_GetType(string abilityName)
    //{
    //    Logger.Log(abilityName);
    //    //try
    //    //{
    //    //    Logger.Log(abilityName);
    //    //    foreach (var x in BuffAbilityManager._abilityMap)
    //    //    {
    //    //        Logger.Log($"{x.key}:{x.value.FullName}");
    //    //    }
    //    //    if (abilityName == "CustomBuff1" || abilityName == "Sinking")
    //    //    {
    //    //        abilityName = "CustomBuff1";
    //    //        Il2CppSystem.Type cached;
    //    //        if (BuffAbilityManager._abilityMap.TryGetValue(abilityName, out cached))
    //    //        {
    //    //            Logger.Log("cached " + abilityName);
    //    //            __result = cached;
    //    //            return false;
    //    //        }
    //    //        else
    //    //        {
    //    //            var buf = Il2CppSystem.Type.GetType($"ForestForTheFlames.{BuffAbilityManager.GenerateClassName(abilityName)}");
    //    //            Logger.Log(abilityName + " is null? " + buf == null);
    //    //            if (buf != null)
    //    //            {
    //    //                BuffAbilityManager._abilityMap.Add(abilityName, buf);
    //    //                __result = buf;
    //    //                return false;
    //    //            }
    //    //        }
    //    //    }
    //    //    return true;
    //    //} catch
    //    //{
    //    //    Logger.Log("gt broke lol");
    //    //    return true;
    //    //}
    //}

    internal static void DumpFiles()
    {
        {
            var total = "[";
            foreach (var x in Singleton<TextDataSet>.Instance.EgoGiftData.GetList())
            {
                total += JsonUtility.ToJson(x);
                total += ",";
            }
            total += "]";
            File.WriteAllText(Path.GetDirectoryName("C:\\Program Files (x86)\\Steam\\steamapps\\common\\Limbus Company\\BepInEx\\plugins\\") + "Text_EgoGiftData.json", total);
        }

        {
            var total = "[";
            foreach (var x in Singleton<TextDataSet>.Instance.EgoGiftCategory.GetList())
            {
                total += JsonUtility.ToJson(x);
                total += ",";
            }
            total += "]";
            File.WriteAllText(Path.GetDirectoryName("C:\\Program Files (x86)\\Steam\\steamapps\\common\\Limbus Company\\BepInEx\\plugins\\") + "Text_EgoGiftCategory.json", total);
        }

        {
            var total = "[";
            foreach (var x in Singleton<TextDataSet>.Instance.MirrorDungeonEgoGiftLockedDescList.GetList())
            {
                total += JsonUtility.ToJson(x);
                total += ",";
            }
            total += "]";
            File.WriteAllText(Path.GetDirectoryName("C:\\Program Files (x86)\\Steam\\steamapps\\common\\Limbus Company\\BepInEx\\plugins\\") + "Text_MirrorDungeonEgoGiftLockedDescList.json", total);
        }

        {
            var total = "[";
            foreach (var x in Singleton<StaticDataManager>.Instance.ExpDungeonBattleList.list)
            {
                total += JsonUtility.ToJson(x);
                total += ",";
            }
            total += "]";
            File.WriteAllText(Path.GetDirectoryName("C:\\Program Files (x86)\\Steam\\steamapps\\common\\Limbus Company\\BepInEx\\plugins\\") + "Static_ExpDungeonBattleList.json", total);
        }

        {
            var total = "[";
            foreach (var x in Singleton<StaticDataManager>.Instance.ExpDungeonDataList.list)
            {
                total += JsonUtility.ToJson(x);
                total += ",";
            }
            total += "]";
            File.WriteAllText(Path.GetDirectoryName("C:\\Program Files (x86)\\Steam\\steamapps\\common\\Limbus Company\\BepInEx\\plugins\\") + "Static_ExpDungeonDataList.json", total);
        }

        {
            var total = "[";
            foreach (var x in Singleton<StaticDataManager>.Instance.ThreadDungeonBattleList.list)
            {
                total += JsonUtility.ToJson(x);
                total += ",";
            }
            total += "]";
            File.WriteAllText(Path.GetDirectoryName("C:\\Program Files (x86)\\Steam\\steamapps\\common\\Limbus Company\\BepInEx\\plugins\\") + "Static_ThreadDungeonBattleList.json", total);
        }

        {
            var total = "[";
            foreach (var x in Singleton<StaticDataManager>.Instance.ThreadDungeonDataList.list)
            {
                total += JsonUtility.ToJson(x);
                total += ",";
            }
            total += "]";
            File.WriteAllText(Path.GetDirectoryName("C:\\Program Files (x86)\\Steam\\steamapps\\common\\Limbus Company\\BepInEx\\plugins\\") + "Static_ThreadDungeonDataList.json", total);
        }

        {
            var total = "[";
            foreach (var x in Singleton<StaticDataManager>.Instance.EnemyUnitList.list)
            {
                total += JsonUtility.ToJson(x);
                total += ",";
            }
            total += "]";
            File.WriteAllText(Path.GetDirectoryName("C:\\Program Files (x86)\\Steam\\steamapps\\common\\Limbus Company\\BepInEx\\plugins\\") + "Static_EnemyUnitList.json", total);
        }

        {
            var total = "[";
            foreach (var x in Singleton<TextDataSet>.Instance.EnemyList.GetList())
            {
                total += JsonUtility.ToJson(x);
                total += ",";
            }
            total += "]";
            File.WriteAllText(Path.GetDirectoryName("C:\\Program Files (x86)\\Steam\\steamapps\\common\\Limbus Company\\BepInEx\\plugins\\") + "Text_EnemyList.json", total);
        }


        {
            var total = "[";
            foreach (var x in Singleton<StaticDataManager>.Instance.AbnormalityUnitList.GetList())
            {
                total += JsonUtility.ToJson(x);
                total += ",";
            }
            total += "]";
            File.WriteAllText(Path.GetDirectoryName("C:\\Program Files (x86)\\Steam\\steamapps\\common\\Limbus Company\\BepInEx\\plugins\\") + "Static_AbnormalityUnitList.json", total);
        }


        {
            var total = "[";
            foreach (var x in Singleton<TextDataSet>.Instance.AbnormalityContentData.GetList())
            {
                total += JsonUtility.ToJson(x);
                total += ",";
            }
            total += "]";
            File.WriteAllText(Path.GetDirectoryName("C:\\Program Files (x86)\\Steam\\steamapps\\common\\Limbus Company\\BepInEx\\plugins\\") + "Text_AbnormalityContentData.json", total);
        }

        {
            var total = "[";
            foreach (var x in Singleton<StaticDataManager>.Instance.SkillList.GetList())
            {
                total += JsonUtility.ToJson(x);
                total += ",";
            }
            total += "]";
            File.WriteAllText(Path.GetDirectoryName("C:\\Program Files (x86)\\Steam\\steamapps\\common\\Limbus Company\\BepInEx\\plugins\\") + "Static_SkillList.json", total);
        }

        {
            var total = "[";
            foreach (var x in Singleton<TextDataSet>.Instance.SkillList.GetList())
            {
                total += JsonUtility.ToJson(x);
                total += ",";
            }
            total += "]";
            File.WriteAllText(Path.GetDirectoryName("C:\\Program Files (x86)\\Steam\\steamapps\\common\\Limbus Company\\BepInEx\\plugins\\") + "Text_SkillList.json", total);
        }

        {
            var total = "[";
            foreach (var x in Singleton<StaticDataManager>.Instance.EgoList.GetList())
            {
                total += JsonUtility.ToJson(x);
                total += ",";
            }
            total += "]";
            File.WriteAllText(Path.GetDirectoryName("C:\\Program Files (x86)\\Steam\\steamapps\\common\\Limbus Company\\BepInEx\\plugins\\") + "Static_EgoList.json", total);
        }

        {
            var total = "[";
            foreach (var x in Singleton<TextDataSet>.Instance.EgoList.GetList())
            {
                total += JsonUtility.ToJson(x);
                total += ",";
            }
            total += "]";
            File.WriteAllText(Path.GetDirectoryName("C:\\Program Files (x86)\\Steam\\steamapps\\common\\Limbus Company\\BepInEx\\plugins\\") + "Text_EgoList.json", total);
        }


        {
            var total = "[";
            foreach (var x in Singleton<StaticDataManager>.Instance.storyBattleStageList.GetList())
            {
                total += JsonUtility.ToJson(x);
                total += ",";
            }
            total += "]";
            File.WriteAllText(Path.GetDirectoryName("C:\\Program Files (x86)\\Steam\\steamapps\\common\\Limbus Company\\BepInEx\\plugins\\") + "Static_storyBattleStageList.json", total);
        }

        {
            var total = "[";
            foreach (var x in Singleton<TextDataSet>.Instance.StoryTheater.GetList())
            {
                total += JsonUtility.ToJson(x);
                total += ",";
            }
            total += "]";
            File.WriteAllText(Path.GetDirectoryName("C:\\Program Files (x86)\\Steam\\steamapps\\common\\Limbus Company\\BepInEx\\plugins\\") + "Text_StoryTheater.json", total);
        }


        {
            var total = "[";
            foreach (var x in Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetList())
            {
                total += JsonUtility.ToJson(x);
                total += ",";
            }
            total += "]";
            File.WriteAllText(Path.GetDirectoryName("C:\\Program Files (x86)\\Steam\\steamapps\\common\\Limbus Company\\BepInEx\\plugins\\") + "Static_PersonalityStaticDataList.json", total);
        }

        {
            var total = "[";
            foreach (var x in Singleton<TextDataSet>.Instance.PersonalityList.GetList())
            {
                total += JsonUtility.ToJson(x);
                total += ",";
            }
            total += "]";
            File.WriteAllText(Path.GetDirectoryName("C:\\Program Files (x86)\\Steam\\steamapps\\common\\Limbus Company\\BepInEx\\plugins\\") + "Text_PersonalityList.json", total);
        }

        {
            var total = "[";
            foreach (var x in Singleton<StaticDataManager>.Instance.AssistantUnitList.GetList())
            {
                total += JsonUtility.ToJson(x);
                total += ",";
            }
            total += "]";
            File.WriteAllText(Path.GetDirectoryName("C:\\Program Files (x86)\\Steam\\steamapps\\common\\Limbus Company\\BepInEx\\plugins\\") + "Static_AssistantUnitList.json", total);
        }

        {
            var total = "[";
            foreach (var x in Singleton<StaticDataManager>.Instance.PassiveList.GetList())
            {
                total += JsonUtility.ToJson(x);
                total += ",";
            }
            total += "]";
            File.WriteAllText(Path.GetDirectoryName("C:\\Program Files (x86)\\Steam\\steamapps\\common\\Limbus Company\\BepInEx\\plugins\\") + "Static_PassiveList.json", total);
        }

        {
            var total = "[";
            foreach (var x in Singleton<StaticDataManager>.Instance.PersonalityPassiveList.GetList())
            {
                total += JsonUtility.ToJson(x);
                total += ",";
            }
            total += "]";
            File.WriteAllText(Path.GetDirectoryName("C:\\Program Files (x86)\\Steam\\steamapps\\common\\Limbus Company\\BepInEx\\plugins\\") + "Static_PersonalityPassiveList.json", total);
        }

        {
            var total = "[";
            foreach (var x in Singleton<TextDataSet>.Instance.PassiveList.GetList())
            {
                total += JsonUtility.ToJson(x);
                total += ",";
            }
            total += "]";
            File.WriteAllText(Path.GetDirectoryName("C:\\Program Files (x86)\\Steam\\steamapps\\common\\Limbus Company\\BepInEx\\plugins\\") + "Text_PassiveList.json", total);
        }

        {
            var total = "[";
            foreach (var x in Singleton<StaticDataManager>.Instance.RailwayDungeonBattleList.GetList())
            {
                total += JsonUtility.ToJson(x);
                total += ",";
            }
            total += "]";
            File.WriteAllText(Path.GetDirectoryName("C:\\Program Files (x86)\\Steam\\steamapps\\common\\Limbus Company\\BepInEx\\plugins\\") + "Static_RailwayDungeonBattleList.json", total);
        }

        {
            var total = "[";
            foreach (var x in Singleton<StaticDataManager>.Instance.RailwayDungeonBuffDataList.GetList())
            {
                total += JsonUtility.ToJson(x);
                total += ",";
            }
            total += "]";
            File.WriteAllText(Path.GetDirectoryName("C:\\Program Files (x86)\\Steam\\steamapps\\common\\Limbus Company\\BepInEx\\plugins\\") + "Static_RailwayDungeonBuffDataList.json", total);
        }

        {
            var total = "[";
            foreach (var x in Singleton<StaticDataManager>.Instance.RailwayDungeonDataList.GetList())
            {
                total += JsonUtility.ToJson(x);
                total += ",";
            }
            total += "]";
            File.WriteAllText(Path.GetDirectoryName("C:\\Program Files (x86)\\Steam\\steamapps\\common\\Limbus Company\\BepInEx\\plugins\\") + "Static_RailwayDungeonDataList.json", total);
        }

        {
            var total = "[";
            foreach (var x in Singleton<StaticDataManager>.Instance.AbnormalityPartList.GetList())
            {
                total += JsonUtility.ToJson(x);
                total += ",";
            }
            total += "]";
            File.WriteAllText(Path.GetDirectoryName("C:\\Program Files (x86)\\Steam\\steamapps\\common\\Limbus Company\\BepInEx\\plugins\\") + "Static_AbnormalityPartList.json", total);
        }

        {
            var total = "[";
            foreach (var x in Singleton<StaticDataManager>.Instance.partList.list)
            {
                total += JsonUtility.ToJson(x);
                total += ",";
            }
            total += "]";
            File.WriteAllText(Path.GetDirectoryName("C:\\Program Files (x86)\\Steam\\steamapps\\common\\Limbus Company\\BepInEx\\plugins\\") + "Static_partList.json", total);
        }
        {
            var total = "[";
            foreach (var x in Singleton<TextDataSet>.Instance.RailwayDungeonBuffText.GetList())
            {
                total += JsonUtility.ToJson(x);
                total += ",";
            }
            total += "]";
            File.WriteAllText(Path.GetDirectoryName("C:\\Program Files (x86)\\Steam\\steamapps\\common\\Limbus Company\\BepInEx\\plugins\\") + "Text_RailwayDungeonBuffText.json", total);
        }

        {
            var total = "[";
            foreach (var x in Singleton<TextDataSet>.Instance.RailwayDungeonNodeText.GetList())
            {
                total += JsonUtility.ToJson(x);
                total += ",";
            }
            total += "]";
            File.WriteAllText(Path.GetDirectoryName("C:\\Program Files (x86)\\Steam\\steamapps\\common\\Limbus Company\\BepInEx\\plugins\\") + "Text_RailwayDungeonNodeText.json", total);
        }

        {
            var total = "[";
            foreach (var x in Singleton<TextDataSet>.Instance.RailwayDungeonStationName.GetList())
            {
                total += JsonUtility.ToJson(x);
                total += ",";
            }
            total += "]";
            File.WriteAllText(Path.GetDirectoryName("C:\\Program Files (x86)\\Steam\\steamapps\\common\\Limbus Company\\BepInEx\\plugins\\") + "Text_RailwayDungeonStationName.json", total);
        }

        {
            var total = "[";
            foreach (var x in Singleton<TextDataSet>.Instance.RailwayDungeonText.GetList())
            {
                total += JsonUtility.ToJson(x);
                total += ",";
            }
            total += "]";
            File.WriteAllText(Path.GetDirectoryName("C:\\Program Files (x86)\\Steam\\steamapps\\common\\Limbus Company\\BepInEx\\plugins\\") + "Text_RailwayDungeonText.json", total);
        }



        {
            var total = "[";
            foreach (var x in Singleton<TextDataSet>.Instance.stageChapter.GetList())
            {
                total += JsonUtility.ToJson(x);
                total += ",";
            }
            total += "]";
            File.WriteAllText(Path.GetDirectoryName("C:\\Program Files (x86)\\Steam\\steamapps\\common\\Limbus Company\\BepInEx\\plugins\\") + "Text_stageChapter.json", total);
        }

        {
            var total = "[";
            foreach (var x in Singleton<TextDataSet>.Instance.stagePart.GetList())
            {
                total += JsonUtility.ToJson(x);
                total += ",";
            }
            total += "]";
            File.WriteAllText(Path.GetDirectoryName("C:\\Program Files (x86)\\Steam\\steamapps\\common\\Limbus Company\\BepInEx\\plugins\\") + "Text_stagePart.json", total);
        }

        Log.LogFatal(1);
        {
            var total = "[";
            foreach (var x in Singleton<TextDataSet>.Instance.StageNodeText.GetList())
            {
                total += JsonUtility.ToJson(x);
                total += ",";
            }
            total += "]";
            File.WriteAllText(Path.GetDirectoryName("C:\\Program Files (x86)\\Steam\\steamapps\\common\\Limbus Company\\BepInEx\\plugins\\") + "Text_StageNodeText.json", total);
        }
        Log.LogFatal(2);
        Log.LogFatal(1);
        {
            var total = "[";
            foreach (var x in Singleton<StaticDataManager>.Instance.StageNodeRewardList.GetList())
            {
                total += JsonUtility.ToJson(x);
                total += ",";
            }
            total += "]";
            File.WriteAllText(Path.GetDirectoryName("C:\\Program Files (x86)\\Steam\\steamapps\\common\\Limbus Company\\BepInEx\\plugins\\") + "Static_StageNodeRewardList.json", total);
        }
        Log.LogFatal(1);
        {
            var total = "[";
            foreach (var x in Singleton<StaticDataManager>.Instance.abBattleStageList.GetList())
            {
                total += JsonUtility.ToJson(x);
                total += ",";
            }
            total += "]";
            File.WriteAllText(Path.GetDirectoryName("C:\\Program Files (x86)\\Steam\\steamapps\\common\\Limbus Company\\BepInEx\\plugins\\") + "Static_abBattleStageList.json", total);
        }
        Log.LogFatal(1);
        {
            var total = "[";
            foreach (var x in Singleton<StaticDataManager>.Instance.dungeonBattleStageList.GetList())
            {
                total += JsonUtility.ToJson(x);
                total += ",";
            }
            total += "]";
            File.WriteAllText(Path.GetDirectoryName("C:\\Program Files (x86)\\Steam\\steamapps\\common\\Limbus Company\\BepInEx\\plugins\\") + "Static_dungeonBattleStageList.json", total);
        }
        Log.LogFatal(1);
        //{
        //    var total = "[";
        //    foreach (var x in Singleton<StaticDataManager>.Instance.MainStageMapSettingList.GetList())
        //    {
        //        total += JsonUtility.ToJson(x);
        //        total += ",";
        //    }
        //    total += "]";
        //    File.WriteAllText(Path.GetDirectoryName("C:\\Program Files (x86)\\Steam\\steamapps\\common\\Limbus Company\\BepInEx\\plugins\\") + "Static_MainStageMapSettingList.json", total);
        //}
        Log.LogFatal(1);
        {
            var total = "[";
            foreach (var x in Singleton<StaticDataManager>.Instance.mirrorDungeonBattleStageList.GetList())
            {
                total += JsonUtility.ToJson(x);
                total += ",";
            }
            total += "]";
            File.WriteAllText(Path.GetDirectoryName("C:\\Program Files (x86)\\Steam\\steamapps\\common\\Limbus Company\\BepInEx\\plugins\\") + "Static_mirrorDungeonBattleStageList.json", total);
        }
        {
            var total = "[";
            foreach (var x in Singleton<StaticDataManager>.Instance.storyBattleStageList.GetList())
            {
                total += JsonUtility.ToJson(x);
                total += ",";
            }
            total += "]";
            File.WriteAllText(Path.GetDirectoryName("C:\\Program Files (x86)\\Steam\\steamapps\\common\\Limbus Company\\BepInEx\\plugins\\") + "Static_storyBattleStageList.json", total);
        }


        {
            var total = "[";
            foreach (var x in Singleton<StaticDataManager>.Instance.BuffList.GetList())
            {
                total += JsonUtility.ToJson(x);
                total += ",";
            }
            total += "]";
            File.WriteAllText(Path.GetDirectoryName("C:\\Program Files (x86)\\Steam\\steamapps\\common\\Limbus Company\\BepInEx\\plugins\\") + "Static_BuffList.json", total);
        }



        {
            var total = "[";
            foreach (var x in Singleton<StaticDataManager>.Instance.DoubleBuffList.GetList())
            {
                total += JsonUtility.ToJson(x);
                total += ",";
            }
            total += "]";
            File.WriteAllText(Path.GetDirectoryName("C:\\Program Files (x86)\\Steam\\steamapps\\common\\Limbus Company\\BepInEx\\plugins\\") + "Static_DoubleBuffList.json", total);
        }
        {
            var total = "[";
            foreach (var x in Singleton<TextDataSet>.Instance.BuffAbilityList.GetList())
            {
                total += JsonUtility.ToJson(x);
                total += ",";
            }
            total += "]";
            File.WriteAllText(Path.GetDirectoryName("C:\\Program Files (x86)\\Steam\\steamapps\\common\\Limbus Company\\BepInEx\\plugins\\") + "Text_BuffAbilityList.json", total);
        }
    }

    [HarmonyPatch(typeof(MainLobbyUIPanel), "Initialize")]
    [HarmonyPostfix]
    public static void PostMainUIPatch()
    {
        if (!patched)
        {
            //Logger.Look(SingletonBehavior<AddressableManager>.Instance);
            //Logger.Look(Singleton<ServerSelector>.Instance.GEAKHAIBAKM);

            Logger.Look(typeof(ResourceKeyBuilder));
            Logger.Look(typeof(BattleUnitView));
            Logger.Look(typeof(SDCharacterSkinUtil));
            try
            {
                //DumpFiles();
            } catch
            {
                Logger.Log("dump failed");
            }
            //{
            //    var total = "[";
            //    foreach (var x in Singleton<TextDataSet>.Instance.BufList.GetList())
            //    {
            //        total += JsonUtility.ToJson(x);
            //        total += ",";
            //    }
            //    total += "]";
            //    File.WriteAllText(Path.GetDirectoryName("C:\\Program Files (x86)\\Steam\\steamapps\\common\\Limbus Company\\BepInEx\\plugins\\") + "Text_BufList.json", total);
            //}

            //{
            //    var total = "[";
            //    foreach (var x in Singleton<StaticDataManager>.Instance.BuffList.GetList())
            //    {
            //        total += JsonUtility.ToJson(x);
            //        total += ",";
            //    }
            //    total += "]";
            //    File.WriteAllText(Path.GetDirectoryName("C:\\Program Files (x86)\\Steam\\steamapps\\common\\Limbus Company\\BepInEx\\plugins\\") + "Static_BuffList.json", total);
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

            LoadAbnoUnit("br_3_1.json");
            LoadAbnoUnit("br_3_2.json");
            LoadAbnoUnit("br_3_3.json");

            LoadAbnoPartUnit("br_3_1_1.json");
            LoadEgoTextAndStatic("rm_aleph.json");

            //LoadBuffStatic("CustomBuff1.json");
            //Singleton<TextDataSet>.Instance.BuffAbilityList._dic.Add("CustomBuff1", new TextData_BuffAbility { id = "CustomBuff1", desc = "custom buff 1 lol" });
            //Singleton<TextDataSet>.Instance.BufList._dic.Add("CustomBuff1", new TextData_Buf { id = "CustomBuff1", name = "Custom buff from code (1)", desc = "why are you reading this?? {0}:{1}"});

            //LoadPersonality("theredmist.json");
            //LoadPersonality("thewavesthatwuther.json");
            //LoadBuffStatic("SlotAdder.#

            Singleton<StaticDataManager>.Instance.BuffList.GetData("Sinking").maxStack = 9999;
            Singleton<StaticDataManager>.Instance.BuffList.GetData("Sinking").maxTurn = 9999;
            Singleton<StaticDataManager>.Instance.BuffList.GetData("Combustion").maxStack = 9999;
            Singleton<StaticDataManager>.Instance.BuffList.GetData("Combustion").maxTurn = 9999;
            Singleton<StaticDataManager>.Instance.BuffList.GetData("Burst").maxStack = 9999;
            Singleton<StaticDataManager>.Instance.BuffList.GetData("Burst").maxTurn = 9999;
            Singleton<StaticDataManager>.Instance.BuffList.GetData("Laceration").maxStack = 9999;
            Singleton<StaticDataManager>.Instance.BuffList.GetData("Laceration").maxTurn = 9999;

            Singleton<TextDataSet>.Instance.EnemyList._dic.Add("-3001", new TextData_Enemy { name = "The Red Mist", desc = "Core (teehee~)", id = "-3001" });
            Singleton<TextDataSet>.Instance.EnemyList._dic.Add("-3002", new TextData_Enemy { name = "Every Catherine", desc = "Core (teehee~)", id = "-3002" });
            Singleton<TextDataSet>.Instance.EnemyList._dic.Add("-3003", new TextData_Enemy { name = "Don Quixote (real)", desc = "Core (teehee~)", id = "-3003" });

            Singleton<StaticDataManager>.Instance.PassiveList.list.Add(new PassiveStaticData { id = -1 });
            Singleton<StaticDataManager>.Instance.PassiveList.list.Add(new PassiveStaticData { id = -100 });
            Singleton<TextDataSet>.Instance.PassiveList._dic.Add("-1", new TextData_Passive { id = "-1", desc = "Probably does something fun", name = "Custom Passive (-1)" });
            Singleton<TextDataSet>.Instance.PassiveList._dic.Add("-100", new TextData_Passive { id = "-100", desc = "Take 5 less HP damage from attacks", name = "Nuovo Fabric" });

            AddPassive(10301, -1);
            AddPassive(10201, -1);
            AddPassive(10101, -1);
            AddPassive(10112, -1);
            AddPassive(10101, -1);

            //Singleton<StaticDataManager>.Instance.AbnormalityUnitList.GetData(8127).PassiveSetInfo.AddBattlePassive(-1);

            // Nuovo Fabric
            AddPassive(10608, -100);
            AddPassive(10211, -100);
            AddPassive(11009, -100);
            AddPassive(10112, -100);
            AddPassive(10110, -100);
            AddPassive(10404, -100);
            AddPassive(10410, -100);
            AddPassive(10605, -100);
            AddPassive(11108, -100);
            AddPassive(11107, -100);
            AddPassive(11005, -100);
            AddPassive(11010, -100);
            AddPassive(11002, -100);

            // custom skins
            {
                //aplist.Add(10301, ("SD_Abnormality", "8410_RealDon_2pAppearance"));
                //aplist.Add(10307, ("SD_Abnormality", "8380_SanchoAppearance"));

                aplist.Add(10301, ("SD_Enemy", "1079_Sancho_BerserkAppearance"));

                aplist.Add(10204, ("SD_Abnormality", "8029_CromerAppearance"));
                aplist.Add(10104, ("SD_Abnormality", "8044_Camellia_AwakenAppearance"));
                //aplist.Add(10601, ("SD_Personality", "400001_JiashichunAppearance"));
                aplist.Add(10508, ("SD_Abnormality", "8153_KimSatGat_ErodeAppearance"));
                aplist.Add(-3001, ("SD_Personality", "10410_Ryoshu_SpiderBudAppearance"));
                aplist.Add(10601, ("SD_Enemy", "Fools_9999_VergiliusAppearance"));

                // 1079

                //aplist.Add(-3001, ("SD_Personality", "10106_Yisang_WCorpAppearance"));
                //aplist.Add(-3002, ("SD_Enemy", "91014_JosephineAppearance"));

                //aplist.Add(10710, ("SD_Abnormality", "8173_MaouHeathclif_RideAppearance"));
                //aplist.Add(10101, ("SD_Abnormality", "90136_JosephineWHAppearance"));

                //aplist.Add(10101, ("SD_EGO", $"ErosionAppearance_{Singleton<StaticDataManager>.Instance.EgoList.GetData(20106).corrosionSkillId}"));
                //aplist.Add(10101, ("SD_EGO", $"ErosionAppearance_{Singleton<StaticDataManager>.Instance.EgoList.GetData(21207).corrosionSkillId}"));
                //aplist.Add(10201, ("SD_EGO", $"{Singleton<StaticDataManager>.Instance.EgoList.GetData(20106).AwakeningSkillId}"));
                //aplist.Add(10201, ("SD_EGO", $"2120711_Gregor_DeadButterfly"));
                //aplist.Add(10301, ("SD_EGO", $"CorrosionAppearance_{Singleton<StaticDataManager>.Instance.EgoList.GetData(21207).corrosionSkillId}"));
                //aplist.Add(10401, ("SD_EGO", $"CorrosionAppearance_{Singleton<StaticDataManager>.Instance.EgoList.GetData(21207).awakeningSkillId}"));
                //aplist.Add(10301, ("SD_EGO", $"Appearance_{Singleton<StaticDataManager>.Instance.EgoList.GetData(20106).AwakeningSkillId}"));

                // SD_DeadScene/CharacterAppearanceDeadScene_8263.prefab
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


            {
                int vv_id = 10601;
                int real_vv_id = 8999;
                Logger.Log(JsonUtility.ToJson(Singleton<StaticDataManager>.Instance.AbnormalityUnitList.GetData(real_vv_id)));
                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(vv_id).attributeList.Clear();
                foreach (var x in Singleton<StaticDataManager>.Instance.AbnormalityUnitList.GetData(real_vv_id).attributeList)
                {
                    Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(vv_id).attributeList.Add(x);
                }
                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(vv_id).resistInfo = Singleton<StaticDataManager>.Instance.AbnormalityUnitList.GetData(real_vv_id).resistInfo;
                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(vv_id).hp = Singleton<StaticDataManager>.Instance.AbnormalityUnitList.GetData(real_vv_id).hp;
                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(vv_id).initBuffList = Singleton<StaticDataManager>.Instance.AbnormalityUnitList.GetData(real_vv_id).initBuffList;
                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(vv_id).panicType = Singleton<StaticDataManager>.Instance.AbnormalityUnitList.GetData(real_vv_id).panicType;
                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(vv_id)._securedDefenseSkillIDList = Singleton<StaticDataManager>.Instance.AbnormalityUnitList.GetData(real_vv_id)._securedDefenseSkillIDList;

                foreach (var ps in Singleton<StaticDataManager>.Instance.AbnormalityUnitList.GetData(real_vv_id).PassiveSetInfo.PassiveIdList)
                {
                    AddPassive(vv_id, ps);
                }
            }

            // exp3
            {
                // the red mist
                {
                    int rm_id = 10401;
                    int rm_ds = 571;
                    float rm_il = 13.71f;
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
                    //AddPassive(rm_id, -1);
                    //Singleton<StaticDataManager>.Instance.AbnormalityUnitList.GetData(8042).passiveSet.AddBattlePassive(-1);
                    //Singleton<StaticDataManager>.Instance.AbnormalityPartList.GetData(804201).passiveSet.AddBattlePassive(-1);

                    AddPassive(rm_id, 860506);
                    //AddPassive(rm_id, 1040301);
                    AddPassive(rm_id, 2040211);
                    AddPassive(rm_id, 836301);
                    //AddPassive(rm_id, 1060201);
                    //AddPassive(rm_id, 1070201);
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
                int sh_id = 10301;
                int sh_ds = 132;
                float sh_il = 1.1f;
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
                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(sh_id).breakSection.sectionList.Add(55);
                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(sh_id).breakSection.sectionList.Add(25);
                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(sh_id).breakSection.sectionList.Add(10);
                var dss = new Il2CppSystem.Collections.Generic.List<CodeStage.AntiCheat.ObscuredTypes.ObscuredInt>();
                dss.Add(new CodeStage.AntiCheat.ObscuredTypes.ObscuredInt(9990304));

                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(sh_id)._securedAggro = new CodeStage.AntiCheat.ObscuredTypes.ObscuredInt(sh_aggro);
                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(sh_id)._securedDefenseSkillIDList = dss;
            }

            // don quixote
            //{
            //    int dq_id = 10301;
            //    int dq_ds = 205;
            //    float dq_il = 5f;
            //    int dq_aggro = 0;

            //    Singleton<TextDataSet>.Instance.PersonalityList.GetData(dq_id).title = "Don Quixote";
            //    Singleton<TextDataSet>.Instance.PersonalityList.GetData(dq_id).oneLineTitle = "First Kindred ";

            //    Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(dq_id).resistInfo.atkResistList[0].value = 0.75f;
            //    Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(dq_id).resistInfo.atkResistList[1].value = 0.75f;
            //    Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(dq_id).resistInfo.atkResistList[2].value = 1f;

            //    Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(dq_id).panicType = 1034;

            //    Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(dq_id).attributeList.Clear();
            //    Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(dq_id).attributeList.Add(new UnitAttribute { skillId = 841001, number = 1 });
            //    Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(dq_id).attributeList.Add(new UnitAttribute { skillId = 841002, number = 1 });
            //    Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(dq_id).attributeList.Add(new UnitAttribute { skillId = 841003, number = 1 });
            //    Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(dq_id).attributeList.Add(new UnitAttribute { skillId = 841004, number = 1 });
            //    Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(dq_id).attributeList.Add(new UnitAttribute { skillId = 841005, number = 1 });
            //    Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(dq_id).attributeList.Add(new UnitAttribute { skillId = 841006, number = 1 });
            //    Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(dq_id).attributeList.Add(new UnitAttribute { skillId = 841007, number = 1 });
            //    Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(dq_id).attributeList.Add(new UnitAttribute { skillId = 841008, number = 1 });
            //    Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(dq_id).attributeList.Add(new UnitAttribute { skillId = 841009, number = 1 });
            //    Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(dq_id).attributeList.Add(new UnitAttribute { skillId = 841010, number = 1 });
            //    Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(dq_id).attributeList.Add(new UnitAttribute { skillId = 841011, number = 1 });
            //    Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(dq_id).attributeList.Add(new UnitAttribute { skillId = 841012, number = 1 });
            //    Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(dq_id).attributeList.Add(new UnitAttribute { skillId = 841013, number = 1 });

            //    //Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(sh_id).attributeList.Add(new UnitAttribute { skillId = 838001, number = 1 });
            //    //Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(sh_id).attributeList.Add(new UnitAttribute { skillId = 838002, number = 1 });
            //    //Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(sh_id).attributeList.Add(new UnitAttribute { skillId = 838003, number = 1 });
            //    //Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(sh_id).attributeList.Add(new UnitAttribute { skillId = 838004, number = 1 });
            //    //Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(sh_id).attributeList.Add(new UnitAttribute { skillId = 838005, number = 1 });
            //    //Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(sh_id).attributeList.Add(new UnitAttribute { skillId = 838006, number = 1 });
            //    //Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(sh_id).attributeList.Add(new UnitAttribute { skillId = 838007, number = 1 });
            //    //Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(sh_id).attributeList.Add(new UnitAttribute { skillId = 838008, number = 1 });
            //    //Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(sh_id).attributeList.Add(new UnitAttribute { skillId = 838009, number = 1 });
            //    //Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(sh_id).attributeList.Add(new UnitAttribute { skillId = 838010, number = 1 });
            //    //Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(sh_id).attributeList.Add(new UnitAttribute { skillId = 838011, number = 1 });

            //    Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(dq_id).hp._securedDefaultStat = new CodeStage.AntiCheat.ObscuredTypes.ObscuredInt(dq_ds);
            //    Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(dq_id).hp._securedIncrementByLevel = new CodeStage.AntiCheat.ObscuredTypes.ObscuredFloat(dq_il);

            //    Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(dq_id).breakSection = new BreakSectionInfo
            //    {
            //        sectionList = new Il2CppSystem.Collections.Generic.List<int>()
            //    };
            //    Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(dq_id).breakSection.sectionList.Add(35);
            //    Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(dq_id).breakSection.sectionList.Add(25);
            //    Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(dq_id).breakSection.sectionList.Add(10);
            //    var dss = new Il2CppSystem.Collections.Generic.List<CodeStage.AntiCheat.ObscuredTypes.ObscuredInt>();
            //    dss.Add(new CodeStage.AntiCheat.ObscuredTypes.ObscuredInt(9990304));

            //    Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(dq_id)._securedAggro = new CodeStage.AntiCheat.ObscuredTypes.ObscuredInt(dq_aggro);
            //    Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(dq_id)._securedDefenseSkillIDList = dss;
            //}


            //Singleton<StaticDataManager>.Instance.PersonalityPassiveList.GetBattlePassiveIDList(10601, 3).Add(1040101);
            //Singleton<StaticDataManager>.Instance.PersonalityPassiveList.GetBattlePassiveIDList(10601, 3).Add(1030101);
            //Singleton<StaticDataManager>.Instance.PersonalityPassiveList.GetBattlePassiveIDList(10601, 3).Add(1030401);


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
                //Singleton<StaticDataManager>.Instance.SkillList.GetData(1011001).skillData[2].coinList.Add(Singleton<StaticDataManager>.Instance.SkillList.GetData(1011001).skillData[2].coinList[0]);
                //Singleton<StaticDataManager>.Instance.SkillList.GetData(1011002).skillData[2].coinList.Add(Singleton<StaticDataManager>.Instance.SkillList.GetData(1011002).skillData[2].coinList[1]);
                //Singleton<StaticDataManager>.Instance.SkillList.GetData(1011004).skillData[1].coinList.Add(Singleton<StaticDataManager>.Instance.SkillList.GetData(1011004).skillData[1].coinList[0]);

                Singleton<StaticDataManager>.Instance.SkillList.GetData(1011003).skillData[1].targetNum = 3;
                Singleton<StaticDataManager>.Instance.SkillList.GetData(1011002).skillData[2].targetNum = 2;
                //Singleton<StaticDataManager>.Instance.SkillList.GetData(1011003).skillData[1].defaultValue = 2;

                foreach (var x in Singleton<StaticDataManager>.Instance.SkillList.GetData(1011001).skillData[2].coinList)
                {
                    x.scale += 1;
                }

                foreach (var x in Singleton<StaticDataManager>.Instance.SkillList.GetData(1011002).skillData[2].coinList)
                {
                    x.scale += 2;
                }

                foreach (var x in Singleton<StaticDataManager>.Instance.SkillList.GetData(1011003).skillData[1].coinList)
                {
                    x.scale += 4;
                }
                //Singleton<StaticDataManager>.Instance.SkillList.GetData(1011003).skillData[1].coinList[3].abilityScriptList.Add(new )
                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(10110).hp._securedDefaultStat = new CodeStage.AntiCheat.ObscuredTypes.ObscuredInt(103);
                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(10110).hp._securedIncrementByLevel = new CodeStage.AntiCheat.ObscuredTypes.ObscuredFloat(2.3f);

            }

            // solemn lament ego
            {
                Singleton<StaticDataManager>.Instance.EgoList.GetData(21207).GetAwakeningSkill().skillData[1].defaultValue = 12;
                Singleton<StaticDataManager>.Instance.EgoList.GetData(21207).GetAwakeningSkill().skillData[2].defaultValue = 12;

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
            Logger.Log("W Corp");
            // w corp stuff
            {
                // don

                //Logger.Log(JsonUtility.ToJson(Singleton<StaticDataManager>.Instance.SkillList.GetData(1040504)));
                //Logger.Log(JsonUtility.ToJson(Singleton<StaticDataManager>.Instance.SkillList.GetData(1111003)));



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

            Logger.Log("Poise");
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
                //AddPassive(10808, 810606);
                //AddPassive(10808, 9009801);
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

                Singleton<StaticDataManager>.Instance.SkillList.GetData(1050805).skillData[0].defaultValue = 5;
                Singleton<StaticDataManager>.Instance.SkillList.GetData(1050805).skillData[1].defaultValue = 5;

                Singleton<StaticDataManager>.Instance.SkillList.GetData(1050805).skillData[0].coinList[0].abilityScriptList.Add(new AbilityData { scriptName = "HealSelfOnSuccessAttackByRatio5" });
                Singleton<StaticDataManager>.Instance.SkillList.GetData(1050805).skillData[0].coinList[1].abilityScriptList.Add(new AbilityData { scriptName = "HealSelfOnSuccessAttackByRatio5" });
                Singleton<StaticDataManager>.Instance.SkillList.GetData(1050805).skillData[0].coinList[2].abilityScriptList.Add(new AbilityData { scriptName = "HealSelfOnSuccessAttackByRatio5" });

                Singleton<StaticDataManager>.Instance.SkillList.GetData(1050805).skillData[1].coinList[0].abilityScriptList.Add(new AbilityData { scriptName = "HealSelfOnSuccessAttackByRatio10" });
                Singleton<StaticDataManager>.Instance.SkillList.GetData(1050805).skillData[1].coinList[1].abilityScriptList.Add(new AbilityData { scriptName = "HealSelfOnSuccessAttackByRatio10" });
                Singleton<StaticDataManager>.Instance.SkillList.GetData(1050805).skillData[1].coinList[2].abilityScriptList.Add(new AbilityData { scriptName = "HealSelfOnSuccessAttackByRatio10" });

                Singleton<StaticDataManager>.Instance.SkillList.GetData(1050803).skillData[0].defaultValue = 8;
                Singleton<StaticDataManager>.Instance.SkillList.GetData(1050803).skillData[1].defaultValue = 8;

                //Singleton<StaticDataManager>.Instance.SkillList.GetData(1050803).skillData[0].coinList[0].color = "GREY";

                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(10508).hp._securedIncrementByLevel = new CodeStage.AntiCheat.ObscuredTypes.ObscuredFloat(2.2f);
                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(10508).hp.incrementByLevel = 2.2f;

                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(10508).hp._securedDefaultStat = new CodeStage.AntiCheat.ObscuredTypes.ObscuredInt(99);
                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(10508).hp.defaultStat = 99;

                //Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(10508).resistInfo.atkResistList[0].value = 0.35f;
                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(10508).resistInfo.atkResistList[1].value = 0.75f;
                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(10508).resistInfo.atkResistList[2].value = 0.25f;

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

            Logger.Log("Bleed");
            // bleed
            {
                // ryoshu chef

                Singleton<StaticDataManager>.Instance.SkillList.GetData(1040403).skillData[0].coinList.Add(Singleton<StaticDataManager>.Instance.SkillList.GetData(1040403).skillData[0].coinList[0]);

                foreach (var x in Singleton<StaticDataManager>.Instance.SkillList.GetData(1040403).skillData[0].coinList)
                {
                    x.scale = 5;
                }

                foreach (var x in Singleton<StaticDataManager>.Instance.SkillList.GetData(1040402).skillData[2].coinList)
                {
                    x.scale = 7;
                }

                foreach (var x in Singleton<StaticDataManager>.Instance.SkillList.GetData(1040401).skillData[2].coinList)
                {
                    x.scale = 5;
                }

                foreach (var x in Singleton<StaticDataManager>.Instance.SkillList.GetData(1040402).skillData[1].coinList)
                {
                    x.scale = 6;
                }

                foreach (var x in Singleton<StaticDataManager>.Instance.SkillList.GetData(1040401).skillData[1].coinList)
                {
                    x.scale = 4;
                }

                Singleton<StaticDataManager>.Instance.SkillList.GetData(1040403).skillData[0].coinList[4].scale = 7;
                Singleton<StaticDataManager>.Instance.SkillList.GetData(1040403).skillData[0].targetNum = 3;

                //Singleton<StaticDataManager>.Instance.SkillList.GetData(1040402).skillData[1].coinList[2].abilityScriptList.Add(new AbilityData { scriptName = "HealSelfOnSuccessAttackByRatio25", buffData = new BuffReferenceData { buffKeyword = "", target = "", buffOwner = "", stack = 0, turn = 0, activeRound = 0, value = 0, limit = 0 } });
                //Singleton<StaticDataManager>.Instance.SkillList.GetData(1040402).skillData[2].coinList[2].abilityScriptList.Add(new AbilityData { scriptName = "HealSelfOnSuccessAttackByRatio25", buffData = new BuffReferenceData { buffKeyword = "", target = "", buffOwner = "", stack = 0, turn = 0, activeRound = 0, value = 0, limit = 0 } });

                //Singleton<TextDataSet>.Instance.SkillList.GetData(1040402).levelList[1].coinlist[2].coindescs.Add(new TextData_Skill_CoinDesc { desc = "[OnSucceedAttack] Heal by 25% of damage dealt" });
                //Singleton<TextDataSet>.Instance.SkillList.GetData(1040402).levelList[2].coinlist[2].coindescs.Add(new TextData_Skill_CoinDesc { desc = "[OnSucceedAttack] Heal by 25% of damage dealt" });

                //Singleton<TextDataSet>.Instance.SkillList.GetData(1040403).levelList[0].coinlist.Add(Singleton<TextDataSet>.Instance.SkillList.GetData(1040403).levelList[0].coinlist[1]);
                //Singleton<TextDataSet>.Instance.SkillList.GetData(1040403).levelList[0].coinlist.Add(Singleton<TextDataSet>.Instance.SkillList.GetData(1040403).levelList[0].coinlist[1]);

                //Singleton<TextDataSet>.Instance.SkillList.GetData(1040403).levelList[0].coinlist[4].coindescs.Add(new TextData_Skill_CoinDesc { desc = "[OnSucceedAttack] Heal by 75% of damage dealt" });

                Singleton<StaticDataManager>.Instance.SkillList.GetData(1040403).skillData[0].coinList[4].abilityScriptList.Add(new AbilityData { scriptName = "HealSelfOnSuccessAttackByRatio75", buffData = new BuffReferenceData { buffKeyword = "", target = "", buffOwner = "", stack = 0, turn = 0, activeRound = 0, value = 0, limit = 0 } });
                Singleton<StaticDataManager>.Instance.SkillList.GetData(1040403).skillData[0].coinList[3].abilityScriptList = Singleton<StaticDataManager>.Instance.SkillList.GetData(1040403).skillData[0].coinList[2].abilityScriptList;

                // 
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
                //Singleton<TextDataSet>.Instance.SkillList.GetData(1010904).levelList = Singleton<TextDataSet>.Instance.SkillList.GetData(1010902).levelList;
                //Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(10109).resistInfo.atkResistList[0].value = 0.75f;
                //Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(10109).resistInfo.atkResistList[1].value = 1f;
                //Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(10109).resistInfo.atkResistList[2].value = 1.25f;

                //Singleton<StaticDataManager>.Instance.SkillList.GetData(1010901).skillData[1].defaultValue = 3;
                //Singleton<StaticDataManager>.Instance.SkillList.GetData(1010901).skillData[2].defaultValue = 4;

                //AddPassive(10109, 1110901, 2);
                //AddPassive(10109, 1110911, 4);
                ////RemovePassive(10109, 1010902);

                //Singleton<StaticDataManager>.Instance.SkillList.GetData(1010902).skillData[1].coinList[0].operatorType = "MUL";
                //Singleton<StaticDataManager>.Instance.SkillList.GetData(1010902).skillData[1].coinList[0].scale = 2;
                //Singleton<StaticDataManager>.Instance.SkillList.GetData(1010902).skillData[2].coinList[0].operatorType = "MUL";
                //Logger.Log(9);
                //Singleton<StaticDataManager>.Instance.SkillList.GetData(1010902).skillData[2].coinList[0].scale = 2;

                //Singleton<StaticDataManager>.Instance.SkillList.GetData(1010903).skillData[0].coinList[0].scale = 4;
                //Singleton<StaticDataManager>.Instance.SkillList.GetData(1010903).skillData[1].coinList[0].scale = 5;
                //Singleton<StaticDataManager>.Instance.SkillList.GetData(1010903).skillData[1].targetNum = 3;


                //Singleton<StaticDataManager>.Instance.SkillList.GetData(1010904).skillData[0].attributeType = "SCARLET";
                //Singleton<StaticDataManager>.Instance.SkillList.GetData(1010904).skillData[0].iconID = "1010902";
                //Singleton<StaticDataManager>.Instance.SkillList.GetData(1010904).skillData[0].atkType = "SLASH";
                //Singleton<StaticDataManager>.Instance.SkillList.GetData(1010904).skillData[0].defType = "COUNTER";
                //Singleton<StaticDataManager>.Instance.SkillList.GetData(1010904).skillData[0].iconID = Singleton<StaticDataManager>.Instance.SkillList.GetData(1010902).skillData[1].iconID;
                //Singleton<StaticDataManager>.Instance.SkillList.GetData(1010904).skillData[0].coinList = Singleton<StaticDataManager>.Instance.SkillList.GetData(1010902).skillData[1].coinList;

                //Singleton<StaticDataManager>.Instance.SkillList.GetData(1010904).skillData[1].attributeType = "SCARLET";
                //Singleton<StaticDataManager>.Instance.SkillList.GetData(1010904).skillData[1].iconID = "1010902";
                //Singleton<StaticDataManager>.Instance.SkillList.GetData(1010904).skillData[1].atkType = "SLASH";
                //Singleton<StaticDataManager>.Instance.SkillList.GetData(1010904).skillData[1].defType = "COUNTER";

                //Singleton<StaticDataManager>.Instance.SkillList.GetData(1010904).skillData[1].iconID = Singleton<StaticDataManager>.Instance.SkillList.GetData(1010902).skillData[1].iconID;
                //Singleton<StaticDataManager>.Instance.SkillList.GetData(1010904).skillData[1].coinList = Singleton<StaticDataManager>.Instance.SkillList.GetData(1010902).skillData[2].coinList;


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

                Singleton<TextDataSet>.Instance.SkillList.GetData(1091103).levelList[1].coinlist[0].coindescs.Add(new TextData_Skill_CoinDesc { desc = "<style=\"highlight\">[OnSucceedAttack] Gain 2 BloomingThornsRodionFirst</style>" });
                Singleton<TextDataSet>.Instance.SkillList.GetData(1091103).levelList[1].coinlist[1].coindescs.Add(new TextData_Skill_CoinDesc { desc = "<style=\"highlight\">[OnSucceedAttack] Gain 2 BloomingThornsRodionFirst</style>" });
                Singleton<TextDataSet>.Instance.SkillList.GetData(1091103).levelList[1].coinlist[2].coindescs.Add(new TextData_Skill_CoinDesc { desc = "<style=\"highlight\">[OnSucceedAttack] Gain 3 BloomingThornsRodionFirst</style>" });

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

                // ishamel ink over
                Singleton<StaticDataManager>.Instance.SkillList.GetData(1081102).skillData[1].targetNum = 2;
                Singleton<StaticDataManager>.Instance.SkillList.GetData(1081103).skillData[1].targetNum = 3;
                Singleton<StaticDataManager>.Instance.SkillList.GetData(1081103).skillData[1].defaultValue = 11;

                foreach (var x in Singleton<StaticDataManager>.Instance.SkillList.GetData(1081101).skillData[1].coinList)
                {
                    x.scale += 1;
                }

                foreach (var x in Singleton<StaticDataManager>.Instance.SkillList.GetData(1081102).skillData[1].coinList)
                {
                    x.scale += 2;
                }

                foreach (var x in Singleton<StaticDataManager>.Instance.SkillList.GetData(1081103).skillData[1].coinList)
                {
                    x.scale += 6;
                }
            }

            Logger.Log("Sinking");
            // sinking
            {
                // 10104 - yi sang flower
                Singleton<StaticDataManager>.Instance.SkillList.GetData(1010401).skillData[2].targetNum = 3;
                Singleton<StaticDataManager>.Instance.SkillList.GetData(1010403).skillData[1].targetNum = 3;

                // ishamel
                Singleton<StaticDataManager>.Instance.SkillList.GetData(1080901).skillData[2].targetNum = 3;
                Singleton<StaticDataManager>.Instance.SkillList.GetData(1080902).skillData[2].targetNum = 3;
                Singleton<StaticDataManager>.Instance.SkillList.GetData(1080903).skillData[0].targetNum = 3;
                Singleton<StaticDataManager>.Instance.SkillList.GetData(1080903).skillData[1].targetNum = 5;

                // Heatcliff S3 - Normal
                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(10710).hp._securedIncrementByLevel += new CodeStage.AntiCheat.ObscuredTypes.ObscuredFloat(0.75f);
                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(10710).hp._securedDefaultStat += new CodeStage.AntiCheat.ObscuredTypes.ObscuredInt(21);
                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(10710).resistInfo.atkResistList[0].value = 1.5f;
                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(10710).resistInfo.atkResistList[1].value = 0.75f;
                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(10710).resistInfo.atkResistList[2].value = 0.5f;

                Singleton<StaticDataManager>.Instance.SkillList.GetData(1071005).skillData[1].targetNum = 3;
                Singleton<StaticDataManager>.Instance.SkillList.GetData(1071005).skillData[1].defaultValue = 67;
                Singleton<StaticDataManager>.Instance.SkillList.GetData(1071005).skillData[1].coinList[0].scale = 33;
                Singleton<StaticDataManager>.Instance.SkillList.GetData(1071005).skillData[1].coinList[1].scale = 33;

                //Singleton<StaticDataManager>.Instance.SkillList.GetData(1071005).skillData[1].defaultValue = 62;
                //Singleton<StaticDataManager>.Instance.SkillList.GetData(1071005).skillData[1].coinList[0].scale = 26;
                //Singleton<StaticDataManager>.Instance.SkillList.GetData(1071005).skillData[1].coinList[1].scale = 26;

                //Singleton<StaticDataManager>.Instance.SkillList.GetData(1071005).skillData[1].targetNum = 5;
                Singleton<StaticDataManager>.Instance.SkillList.GetData(1071003).skillData[1].targetNum = 5;
                Singleton<StaticDataManager>.Instance.SkillList.GetData(1071003).skillData[0].defaultValue = 12;
                Singleton<StaticDataManager>.Instance.SkillList.GetData(1071003).skillData[1].defaultValue = 24;

                // heir gregor

                //Logger.Log(JsonUtility.ToJson(Singleton<StaticDataManager>.Instance.SkillList.GetData(1120903)));
                //Logger.Log(JsonUtility.ToJson(Singleton<StaticDataManager>.Instance.SkillList.GetData(1060801)));
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

                // outis
                AddPassive(11108, 1020901);

                foreach (var coin in Singleton<StaticDataManager>.Instance.SkillList.GetData(1110803).skillData[1].coinList) {
                    coin.scale += 2;
                }
            }

            Logger.Log("Rupture");
            // rupture
            {
                // dao outis 112604
                try
                {
                    Singleton<StaticDataManager>.Instance.SkillList.GetData(1111203).skillData[0].targetNum = 3;
                    Singleton<StaticDataManager>.Instance.SkillList.GetData(1111203).skillData[1].targetNum = 3;

                    Singleton<TextDataSet>.Instance.SkillList.GetData(1111202).levelList[1].coinlist[0].coindescs.Add(new TextData_Skill_CoinDesc { desc = "[OnSucceedAttack] Heal by 50% of damage dealt" });
                    Singleton<StaticDataManager>.Instance.SkillList.GetData(1111202).skillData[1].coinList[1].abilityScriptList.Add(new AbilityData { scriptName = "HealSelfOnSuccessAttackByRatio50", buffData = new BuffReferenceData { activeRound = 0, buffKeyword = "", buffOwner = "", limit = 0, stack = 0, target = "", turn = 0, value = 0 }, conditionalData = new ConditionalData() });

                    Singleton<TextDataSet>.Instance.SkillList.GetData(1111202).levelList[1].coinlist[1].coindescs.Add(new TextData_Skill_CoinDesc { desc = "[OnSucceedAttack] Heal by 70% of damage dealt" });
                    Singleton<StaticDataManager>.Instance.SkillList.GetData(1111202).skillData[1].coinList[1].abilityScriptList.Add(new AbilityData { scriptName = "HealSelfOnSuccessAttackByRatio70", buffData = new BuffReferenceData { activeRound = 0, buffKeyword = "", buffOwner = "", limit = 0, stack = 0, target = "", turn = 0, value = 0 }, conditionalData = new ConditionalData() });

                    Singleton<TextDataSet>.Instance.SkillList.GetData(1111202).levelList[2].coinlist[0].coindescs.Add(new TextData_Skill_CoinDesc { desc = "[OnSucceedAttack] Heal by 50% of damage dealt" });
                    Singleton<StaticDataManager>.Instance.SkillList.GetData(1111202).skillData[2].coinList[1].abilityScriptList.Add(new AbilityData { scriptName = "HealSelfOnSuccessAttackByRatio50", buffData = new BuffReferenceData { activeRound = 0, buffKeyword = "", buffOwner = "", limit = 0, stack = 0, target = "", turn = 0, value = 0 }, conditionalData = new ConditionalData() });

                    Singleton<TextDataSet>.Instance.SkillList.GetData(1111202).levelList[2].coinlist[1].coindescs.Add(new TextData_Skill_CoinDesc { desc = "[OnSucceedAttack] Heal by 70% of damage dealt" });
                    Singleton<StaticDataManager>.Instance.SkillList.GetData(1111202).skillData[2].coinList[1].abilityScriptList.Add(new AbilityData { scriptName = "HealSelfOnSuccessAttackByRatio70", buffData = new BuffReferenceData { activeRound = 0, buffKeyword = "", buffOwner = "", limit = 0, stack = 0, target = "", turn = 0, value = 0 }, conditionalData = new ConditionalData() });

                    foreach (var script in Singleton<StaticDataManager>.Instance.SkillList.GetData(1110403).skillData[0].coinList[2].abilityScriptList)
                    {
                        Singleton<StaticDataManager>.Instance.SkillList.GetData(1111203).skillData[0].coinList[2].abilityScriptList.Insert(1, script);
                        Singleton<StaticDataManager>.Instance.SkillList.GetData(1111203).skillData[1].coinList[2].abilityScriptList.Insert(1, script);
                    }

                    foreach (var coin in Singleton<StaticDataManager>.Instance.SkillList.GetData(1111201).skillData[1].coinList)
                    {
                        coin.scale += 1;
                    }

                    foreach (var coin in Singleton<StaticDataManager>.Instance.SkillList.GetData(1111201).skillData[2].coinList)
                    {
                        coin.scale += 1;
                    }

                    // {"scriptName":"RerollOnSAIfSelfSpeedNotLessThan10Upto1","value":0.0,"turnLimit":0,"buffData":{"buffKeyword":"","target":"","buffOwner":"","stack":0,"turn":0,"activeRound":0,"value":0.0,"limit":0},"conditionalData":{"category":"","value":"","conditionBuffData":{"buffKeyword":"","target":"","buffOwner":"","stack":0,"turn":0,"activeRound":0,"value":0.0,"limit":0},"type":"","resultValue":"","resultBuffData":{"buffKeyword":"","target":"","buffOwner":"","stack":0,"turn":0,"activeRound":0,"value":0.0,"limit":0}}}

                    //foreach (var script in Singleton<StaticDataManager>.Instance.SkillList.GetData(1111203).skillData[1].coinList[2].abilityScriptList)
                    //{
                    //    Logger.Log(script.ScriptName);
                    //    Logger.Log(script.value);

                    //    Logger.Log(script.buffData.Limit);
                    //    Logger.Log(script.buffData.value);

                    //    Logger.Log(JsonUtility.ToJson(script));
                    //    Logger.Log(JsonUtility.ToJson(script.buffData));
                    //    Logger.Log(JsonUtility.ToJson(script.conditionalData));


                    //}
                    foreach (var coin in Singleton<StaticDataManager>.Instance.SkillList.GetData(1111203).skillData[0].coinList)
                    {
                        coin.scale += 2;
                    }

                    foreach (var coin in Singleton<StaticDataManager>.Instance.SkillList.GetData(1111203).skillData[1].coinList)
                    {
                        coin.scale += 3;
                    }

                    Singleton<TextDataSet>.Instance.SkillList.GetData(1111203).levelList[0].coinlist[^1].GetDescs().Insert(1, new TextData_Skill_CoinDesc { desc = "[OnSucceedAttack] Deal bonus Slash damage by 15% of damage dealt" });
                    Singleton<TextDataSet>.Instance.SkillList.GetData(1111203).levelList[0].coinlist[^1].GetDescs().Insert(1, new TextData_Skill_CoinDesc { desc = "[OnSucceedAttack] Inflict [WeaknessAnalysis] next turn" });

                    Singleton<TextDataSet>.Instance.SkillList.GetData(1111203).levelList[1].coinlist[^1].GetDescs().Insert(1, new TextData_Skill_CoinDesc { desc = "[OnSucceedAttack] Deal bonus Slash damage by 15% of damage dealt" });
                    Singleton<TextDataSet>.Instance.SkillList.GetData(1111203).levelList[1].coinlist[^1].GetDescs().Insert(1, new TextData_Skill_CoinDesc { desc = "[OnSucceedAttack] Inflict [WeaknessAnalysis] next turn" });
                    AddPassive(112604, 10102);
                }
                catch
                {
                    Logger.Log(Logger.LEVEL.FATAL,"patching function","dao outis failed!!");
                }

                // ryoshu 10411
                try
                {
                    Singleton<StaticDataManager>.Instance.SkillList.GetData(1041103).skillData[0].targetNum = 3;
                    Singleton<StaticDataManager>.Instance.SkillList.GetData(1041103).skillData[1].targetNum = 3;
                    
                    foreach (var script in Singleton<StaticDataManager>.Instance.SkillList.GetData(1110403).skillData[0].coinList[2].abilityScriptList)
                    {
                        Singleton<StaticDataManager>.Instance.SkillList.GetData(1041103).skillData[0].coinList[2].abilityScriptList.Insert(1, script);
                        Singleton<StaticDataManager>.Instance.SkillList.GetData(1041103).skillData[1].coinList[2].abilityScriptList.Insert(1, script);
                    }

                    foreach (var coin in Singleton<StaticDataManager>.Instance.SkillList.GetData(1041101).skillData[1].coinList)
                    {
                        coin.scale += 1;
                    }

                    foreach (var coin in Singleton<StaticDataManager>.Instance.SkillList.GetData(1041101).skillData[2].coinList)
                    {
                        coin.scale += 1;
                    }

                    foreach (var coin in Singleton<StaticDataManager>.Instance.SkillList.GetData(1041103).skillData[0].coinList)
                    {
                        coin.scale += 4;
                    }

                    foreach (var coin in Singleton<StaticDataManager>.Instance.SkillList.GetData(1041103).skillData[1].coinList)
                    {
                        coin.scale += 7;
                    }

                    //Singleton<TextDataSet>.Instance.SkillList.GetData(1041101).levelList[1].coinlist[1].coindescs.Add(new TextData_Skill_CoinDesc { desc = "[OnSucceedAttack] Inflict +1 [Burst] Count" });
                    //Singleton<StaticDataManager>.Instance.SkillList.GetData(1041101).skillData[1].coinList[1].abilityScriptList.Add(new AbilityData { scriptName = "GiveBuffOnSucceedAttack", buffData = new BuffReferenceData { activeRound = 0, buffKeyword = "Burst", buffOwner = "", limit = 0, stack = 0, target = "Target", turn = 1, value = 0 }, conditionalData = new ConditionalData() });

                    Singleton<TextDataSet>.Instance.SkillList.GetData(1041103).levelList[0].coinlist[^1].GetDescs().Insert(1, new TextData_Skill_CoinDesc { desc = "[OnSucceedAttack] Deal bonus Slash damage by 15% of damage dealt" });
                    Singleton<TextDataSet>.Instance.SkillList.GetData(1041103).levelList[0].coinlist[^1].GetDescs().Insert(1, new TextData_Skill_CoinDesc { desc = "[OnSucceedAttack] Inflict [WeaknessAnalysis] next turn" });

                    Singleton<TextDataSet>.Instance.SkillList.GetData(1041103).levelList[1].coinlist[^1].GetDescs().Insert(1, new TextData_Skill_CoinDesc { desc = "[OnSucceedAttack] Deal bonus Slash damage by 15% of damage dealt" });
                    Singleton<TextDataSet>.Instance.SkillList.GetData(1041103).levelList[1].coinlist[^1].GetDescs().Insert(1, new TextData_Skill_CoinDesc { desc = "[OnSucceedAttack] Inflict [WeaknessAnalysis] next turn" });
                    AddPassive(10411, 10102);
                }
                catch
                {
                    Logger.Log(Logger.LEVEL.FATAL, "patching function", "dao ryoshu failed!!");
                }

                // lce yi sang
                AddPassive(10111, 2050111);

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

            Logger.Log("Burn");
            // burn
            {
                // faust burn ego!!!
                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(10211).hp._securedDefaultStat = new CodeStage.AntiCheat.ObscuredTypes.ObscuredInt(89);
                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(10211).hp._securedIncrementByLevel = new CodeStage.AntiCheat.ObscuredTypes.ObscuredFloat(2.9f);

                Singleton<StaticDataManager>.Instance.SkillList.GetData(1021103).skillData[0].targetNum = 3;
                Singleton<StaticDataManager>.Instance.SkillList.GetData(1021103).skillData[1].targetNum = 3;
                foreach (var x in Singleton<StaticDataManager>.Instance.SkillList.GetData(1021103).skillData[0].coinList)
                {
                    x.scale += 1;
                }

                foreach (var x in Singleton<StaticDataManager>.Instance.SkillList.GetData(1021103).skillData[1].coinList)
                {
                    x.scale += 2;
                }

                Singleton<StaticDataManager>.Instance.SkillList.GetData(1021105).skillData[0].targetNum = 5;
                Singleton<StaticDataManager>.Instance.SkillList.GetData(1021105).skillData[0].targetNum = 7;
                foreach (var x in Singleton<StaticDataManager>.Instance.SkillList.GetData(1021105).skillData[0].coinList)
                {
                    x.scale += 7;
                }

                foreach (var x in Singleton<StaticDataManager>.Instance.SkillList.GetData(1021105).skillData[1].coinList)
                {
                    x.scale += 11;
                }

                foreach (var x in Singleton<StaticDataManager>.Instance.SkillList.GetData(1021102).skillData[1].coinList)
                {
                    x.scale += 1;
                }

                foreach (var x in Singleton<StaticDataManager>.Instance.SkillList.GetData(1021102).skillData[2].coinList)
                {
                    x.scale += 2;
                }

                foreach (var x in Singleton<StaticDataManager>.Instance.SkillList.GetData(1021101).skillData[1].coinList)
                {
                    x.scale += 1;
                }

                foreach (var x in Singleton<StaticDataManager>.Instance.SkillList.GetData(1021101).skillData[2].coinList)
                {
                    x.scale += 2;
                }
                // yi sang

                // outis burn
                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(11107).initBuffList.Add(new InitBuffPerLevel { level = 3, list = new Il2CppSystem.Collections.Generic.List<InitBuff>() });
                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(11107).initBuffList[1].list.Add(new InitBuff { buff = "FreischutzShotCount", stack = 2 });
                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(11107).initBuffList[1].list.Add(new InitBuff { buff = "FreishutzOutisEgoBullet_1st", stack = 1 });
                Singleton<StaticDataManager>.Instance.SkillList.GetData(1110703).skillData[1].coinList[0].scale = 15;
                //Singleton<StaticDataManager>.Instance.SkillList.GetData(1110703).skillData[1].coinList[0].operatorType = "MUL";

                Singleton<StaticDataManager>.Instance.SkillList.GetData(1110704).skillData[1].defaultValue = 15;

                Singleton<StaticDataManager>.Instance.EgoList.GetData(21108).GetAwakeningSkill().skillData[2].mpUsage = 17;
                    Singleton<StaticDataManager>.Instance.EgoList.GetData(21108).GetAwakeningSkill().skillData[1].targetNum = 7;
                    Singleton<StaticDataManager>.Instance.EgoList.GetData(21108).GetAwakeningSkill().skillData[1].defaultValue = 37;

                Singleton<StaticDataManager>.Instance.SkillList.GetData(1110704).skillData[1].abilityScriptList[0].scriptName = "GiveBuffOnBattleStart(limit:1)";
                Singleton<StaticDataManager>.Instance.SkillList.GetData(1110702).skillData[2].coinList[1].abilityScriptList.Insert(0, new AbilityData { scriptName = "GiveBuffOnSucceedAttackHead", buffData = new BuffReferenceData { buffKeyword = "DarkFlame", target = "Target", buffOwner = "", stack = 2, turn = 0, activeRound = 0, value = 0, limit = 0 } });

                //Singleton<StaticDataManager>.Instance.SkillList.GetData(1110702).skillData[2].coinList[1].abilityScriptList.Add(new AbilityData { scriptName = "GiveBuffOnSucceedAttackHead", buffData = new BuffReferenceData { buffKeyword = "Smoke", target = "Target", buffOwner = "", stack = 3, turn = 0, activeRound = 0, value = 0, limit = 0 } });
                //Singleton<StaticDataManager>.Instance.SkillList.GetData(1110703).skillData[1].coinList[0].abilityScriptList.Insert(0, new AbilityData { scriptName = "GiveBuffOnSucceedAttackHead", buffData = new BuffReferenceData { buffKeyword = "Smoke", target = "Target", buffOwner = "", stack = 0, turn = 3, activeRound = 0, value = 0, limit = 0 } });

                Singleton<TextDataSet>.Instance.SkillList.GetData(1110704).levelList[1].desc.Replace("[WhenUse] Gain 1", "[StartBattle] Gain 1");
                //Singleton<TextDataSet>.Instance.SkillList.GetData(1110702).levelList[2].coinlist[1].coindescs.Insert(0, Singleton<TextDataSet>.Instance.SkillList.GetData(1110702).levelList[2].coinlist[1].coindescs[0]);
                //Singleton<TextDataSet>.Instance.SkillList.GetData(1110702).levelList[2].coinlist[1].coindescs[0].desc = "[OnSucceedAttackHead] Inflict 2 [DarkFlame]";

                //Singleton<TextDataSet>.Instance.SkillList.GetData(1110702).levelList[2].coinlist[1].coindescs.Insert(0, Singleton<TextDataSet>.Instance.SkillList.GetData(1110702).levelList[2].coinlist[1].coindescs[0]);
                //Singleton<TextDataSet>.Instance.SkillList.GetData(1110702).levelList[2].coinlist[1].coindescs[0].desc = "[OnSucceedAttackHead] Inflict 3 [Smoke]";

                //Singleton<TextDataSet>.Instance.SkillList.GetData(1110703).levelList[1].coinlist[0].coindescs.Insert(0, Singleton<TextDataSet>.Instance.SkillList.GetData(1110702).levelList[2].coinlist[1].coindescs[0]);
                //Singleton<TextDataSet>.Instance.SkillList.GetData(1110703).levelList[1].coinlist[0].coindescs[0].desc = "[OnSucceedAttackHead] Inflict 5 [Smoke] Count";


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

            Logger.Log("Tremor");
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


            Logger.Log("Etc");
            // etc
            {
                // ardor blossom star
                Singleton<StaticDataManager>.Instance.EgoList.GetData(20803).GetAwakeningSkill().skillData[1].coinList[0].scale = 27;
                Singleton<StaticDataManager>.Instance.EgoList.GetData(20803).GetCorrosionSkill().skillData[1].defaultValue = 57;

                Singleton<StaticDataManager>.Instance.EgoList.GetData(20803).GetAwakeningSkill().skillData[2].coinList[0].scale = 32;
                Singleton<StaticDataManager>.Instance.EgoList.GetData(20803).GetCorrosionSkill().skillData[2].defaultValue = 63;

                // awe
                foreach (var x in Singleton<StaticDataManager>.Instance.EgoList.GetData(20407).GetAwakeningSkill().skillData[2].coinList)
                {
                    x.scale = 24;
                }

                foreach (var x in Singleton<StaticDataManager>.Instance.EgoList.GetData(20407).GetAwakeningSkill().skillData[1].coinList)
                {
                    x.scale = 17;
                }

                // soda hong lu
                Singleton<StaticDataManager>.Instance.EgoList.GetData(20604).GetAwakeningSkill().skillData[1].coinList[0]._coinColorType = COIN_COLOR_TYPE.GREY;
                Singleton<StaticDataManager>.Instance.EgoList.GetData(20604).GetAwakeningSkill().skillData[2].coinList[0]._coinColorType = COIN_COLOR_TYPE.GREY;

                Logger.Log(1);
                Singleton<StaticDataManager>.Instance.EgoList.GetData(20604).GetAwakeningSkill().skillData[1].coinList[0].abilityScriptList.Add(new AbilityData { scriptName = "GiveBuffOnSucceedAttackHead", buffData = new BuffReferenceData { buffKeyword = "Smoke", target = "Target", buffOwner = "", stack = 6, turn = 2, activeRound = 0, value = 0, limit = 0 } });
                Singleton<TextDataSet>.Instance.SkillList.GetData(Singleton<StaticDataManager>.Instance.EgoList.GetData(20604).awakeningSkillId).levelList[1].coinlist[0].coindescs.Add(new TextData_Skill_CoinDesc { desc = "[OnSucceedAttackHead] Inflict 6 [Smoke] and +2 [Smoke] Count" });
                
                Singleton<StaticDataManager>.Instance.EgoList.GetData(20604).GetAwakeningSkill().skillData[2].coinList[0].abilityScriptList.Add(new AbilityData { scriptName = "GiveBuffOnSucceedAttackHead", buffData = new BuffReferenceData { buffKeyword = "Smoke", target = "Target", buffOwner = "", stack = 7, turn = 3, activeRound = 0, value = 0, limit = 0 } });
                Singleton<TextDataSet>.Instance.SkillList.GetData(Singleton<StaticDataManager>.Instance.EgoList.GetData(20604).awakeningSkillId).levelList[2].coinlist[0].coindescs.Add(new TextData_Skill_CoinDesc { desc = "[OnSucceedAttackHead] Inflict 7 [Smoke] and +3 [Smoke] Count" });
                Logger.Log(1111111);

                Singleton<StaticDataManager>.Instance.EgoList.GetData(20604).GetAwakeningSkill().skillData[1].coinList[0].abilityScriptList.Add(new AbilityData { scriptName = "GiveBuffOnSucceedAttackHead", buffData = new BuffReferenceData { buffKeyword = "BurstAgility", target = "Target", buffOwner = "", stack = 0, turn = 0, activeRound = 0, value = 0, limit = 0 } });
                Singleton<TextDataSet>.Instance.SkillList.GetData(Singleton<StaticDataManager>.Instance.EgoList.GetData(20604).awakeningSkillId).levelList[1].coinlist[0].coindescs.Add(new TextData_Skill_CoinDesc { desc = "[OnSucceedAttackHead] Inflict [BurstAgility]" });

                Singleton<StaticDataManager>.Instance.EgoList.GetData(20604).GetAwakeningSkill().skillData[2].coinList[0].abilityScriptList.Add(new AbilityData { scriptName = "GiveBuffOnSucceedAttackHead", buffData = new BuffReferenceData { buffKeyword = "BurstAgility", target = "Target", buffOwner = "", stack = 0, turn = 0, activeRound = 0, value = 0, limit = 0 } });
                Singleton<TextDataSet>.Instance.SkillList.GetData(Singleton<StaticDataManager>.Instance.EgoList.GetData(20604).awakeningSkillId).levelList[2].coinlist[0].coindescs.Add(new TextData_Skill_CoinDesc { desc = "[OnSucceedAttackHead] Inflict [BurstAgility]" });

                Singleton<StaticDataManager>.Instance.EgoList.GetData(20604).GetAwakeningSkill().skillData[1].abilityScriptList.Add(new AbilityData { scriptName = "GiveBuffOnUse", buffData = new BuffReferenceData { buffKeyword = "Persistent", target = "Self", buffOwner = "", stack = 4, turn = 0, activeRound = 0, value = 0, limit = 0 } });
                Singleton<TextDataSet>.Instance.SkillList.GetData(Singleton<StaticDataManager>.Instance.EgoList.GetData(20604).awakeningSkillId).levelList[1].desc += "\n[WhenUse] Gain 4 [Persistent]";

                Singleton<StaticDataManager>.Instance.EgoList.GetData(20604).GetAwakeningSkill().skillData[2].abilityScriptList.Add(new AbilityData { scriptName = "GiveBuffOnUse", buffData = new BuffReferenceData { buffKeyword = "Persistent", target = "Self", buffOwner = "", stack = 4, turn = 0, activeRound = 0, value = 0, limit = 0 } });
                Singleton<TextDataSet>.Instance.SkillList.GetData(Singleton<StaticDataManager>.Instance.EgoList.GetData(20604).awakeningSkillId).levelList[2].desc += "\n[WhenUse] Gain 4 [Persistent]";

                Singleton<StaticDataManager>.Instance.EgoList.GetData(20604).awakeningPassiveList.Add(2050311);
                Singleton<StaticDataManager>.Instance.EgoList.GetData(20604).awakeningPassiveList.Add(2050111);

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

            Logger.Log("Base ego");
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
            Logger.Log("Patching sucessful!");
        
        }
    }

    [HarmonyPatch(typeof(AskSettingsPopup), "OpenSettingsPopup")]
    [HarmonyPrefix]
    public static bool AskSettingsPopup_OpenSettingsPopup_Pre()
    {
        Logger.Log("Settings patch");
        //Singleton<FileDownloadManager>.Instance;
        foreach (var x in Singleton<SynchronousDataManager>.Instance.NoticeSynchronousDataList.notices)
        {
            x.title = "Hong Lu bought this ad spot";
            x.contentFormatList.list.Clear();
            x.contentFormatList.list.Add(new IDPHPJPKLJA { formatKey = "Text", formatValue = "Hi there, this post was brought to you by the International Hong K. Lu Corporation LLC>" });
            x.contentFormatList.list.Add(new IDPHPJPKLJA { formatKey = "SubTitle", formatValue = "don't listen to ryoshu" });
            x.contentFormatList.list.Add(new IDPHPJPKLJA { formatKey = "HyperLink", formatValue = "https://lcbcountdown.carrd.co/" });
            x.contentFormatList.list.Add(new IDPHPJPKLJA { formatKey = "HyperLink", formatValue = "file:///C:/Windows/assembly/Desktop.ini" });
            x.contentFormatList.list.Add(new IDPHPJPKLJA { formatKey = "HyperLink", formatValue = "hi no, go fuck yourself btw" });
            x.contentFormatList.list.Add(new IDPHPJPKLJA { formatKey = "Text", formatValue = "<script>alert(1)</script>" });
            x.contentFormatList.list.Add(new IDPHPJPKLJA { formatKey = "SubTittle", formatValue = "<script>alert(1)</script>" });
            x.contentFormatList.list.Add(new IDPHPJPKLJA { formatKey = "HyperLink", formatValue = "<script>alert(1)</script>" });



            //x.contentFormatList.list.Add(new IDPHPJPKLJA { formatKey = "Text", formatValue = "Hi there, this post was brought to you by the International Hong K. Lu Corporation LLC>" });
            //x.contentFormatList.list.Add(new IDPHPJPKLJA { formatKey = "Text", formatValue = "Hi there, this post was brought to you by the International Hong K. Lu Corporation LLC>" });


            //Logger.Log($"{JsonUtility.ToJson(x.contentFormatList)}");
            //Logger.Log($"{JsonUtility.ToJson(x.contentFormatList.list)}");
            //foreach (var y in x.contentFormatList.list._items)
            //{
            //    Logger.Log($"{JsonUtility.ToJson(y)}");
            //}
            //x.content = "Hi there, this post was bought by Hong K. Lu, {\"list\":[{\"formatKey\":\"Text\",\"formatValue\":\"Ryoshu was also here fyi\"},{\"formatKey\":\"SubTitle\",\"formatValue\":\"<Official Sex>\"},{\"formatKey\":\"Text\",\"formatValue\":\"��� Sex company\"},{\"formatKey\":\"HyperLink\",\"formatValue\":\"https://lcbcountdown.carrd.co/\"},{\"formatKey\":\"Text\",\"formatValue\":\"��� Project Noon\"},{\"formatKey\":\"HyperLink\",\"formatValue\":\"file:///C:/Windows/assembly/Desktop.ini\"},{\"formatKey\":\"Text\",\"formatValue\":\"Fuck you.\"}]}";
        }

        //foreach (var x in Singleton<SynchronousDataManager>.Instance.NoticeSynchronousDataList.noticeFormats)
        //{
        //    x.title_EN = "Hong Lu bought this post (2)";
        //    x.content_EN = "Hi there, this post was bought by Hong K. Lu, {\"list\":[{\"formatKey\":\"Text\",\"formatValue\":\"Ryoshu was also here fyi\"},{\"formatKey\":\"SubTitle\",\"formatValue\":\"<Official Sex>\"},{\"formatKey\":\"Text\",\"formatValue\":\"��� Sex company\"},{\"formatKey\":\"HyperLink\",\"formatValue\":\"https://lcbcountdown.carrd.co/\"},{\"formatKey\":\"Text\",\"formatValue\":\"��� Project Noon\"},{\"formatKey\":\"HyperLink\",\"formatValue\":\"file:///C:/Windows/assembly/Desktop.ini\"},{\"formatKey\":\"Text\",\"formatValue\":\"Fuck you.\"}]}";
        //}
        //foreach (var x in Singleton<SynchronousDataManager>.Instance.NoticeSynchronousDataList.noticeFormats)
        //{
        //    Logger.Log($"noticeFormats: {JsonUtility.ToJson(x)}");
        //}

        ////foreach (var x in Singleton<SynchronousDataManager>.Instance.NoticeSynchronousDataList.notices)
        ////{
        ////    Logger.Log($"notices: {JsonUtility.ToJson(x)}");
        ////}

        //foreach (var x in Singleton<StaticDataManager>.Instance.NewlyUpdatedContentStaticDataList.conentIdList)
        //{
        //    Logger.Log($"1:{x}");
        //}

        //foreach (var x in Singleton<StaticDataManager>.Instance.NewlyUpdatedContentStaticDataList._contentIdMap)
        //{
        //    Logger.Log($"2:{x.key}:{JsonUtility.ToJson(x.Value)}");
        //}
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
            Logger.Log("patching...");

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
                int vg_id = 10501;
                int vg_ds = 350;
                float vg_il = 15f;
                int vg_aggro = 100;
                //aplist.Add(vg_id, ("SD_Personality", "9999_VergiliusAppearance"));
                aplist.Add(vg_id, ("SD_Personality", "9999_Vergilius_EgoAppearance"));

                AddPassive(vg_id, -1);
                Singleton<TextDataSet>.Instance.PersonalityList.GetData(vg_id).title = "Vergilius";
                Singleton<TextDataSet>.Instance.PersonalityList.GetData(vg_id).oneLineTitle = "The Red Gaze ";

                //Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(vg_id).initBuffList.Add(new InitBuffPerLevel { level = 3, list = new Il2CppSystem.Collections.Generic.List<InitBuff>() });
                //Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(vg_id).initBuffList[1].list.Add(new InitBuff { buff = "Agility", stack = 200 });

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

                foreach (var skill in Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(vg_id).attributeList)
                {
                    Singleton<StaticDataManager>.Instance.SkillList.GetData(skill.skillId).skillData[0].targetNum = 77;
                    Singleton<StaticDataManager>.Instance.SkillList.GetData(skill.skillId).skillData[0].defaultValue += 1000000;
                    foreach (var coin in Singleton<StaticDataManager>.Instance.SkillList.GetData(skill.skillId).skillData[0].coinList) {
                        coin.scale = 9999999;
                    }
                }

                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(vg_id).hp._securedDefaultStat = new CodeStage.AntiCheat.ObscuredTypes.ObscuredInt(vg_ds);
                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(vg_id).hp._securedIncrementByLevel = new CodeStage.AntiCheat.ObscuredTypes.ObscuredFloat(vg_il);

                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(vg_id).breakSection = new BreakSectionInfo
                {
                    sectionList = new Il2CppSystem.Collections.Generic.List<int>()
                };
                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(vg_id).breakSection.sectionList.Add(20);
                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(vg_id).breakSection.sectionList.Add(15);
                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(vg_id).breakSection.sectionList.Add(10);
                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(vg_id).breakSection.sectionList.Add(9);
                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(vg_id).breakSection.sectionList.Add(8);
                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(vg_id).breakSection.sectionList.Add(7);
                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(vg_id).breakSection.sectionList.Add(6);
                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(vg_id).breakSection.sectionList.Add(5);
                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(vg_id).breakSection.sectionList.Add(4);
                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(vg_id).breakSection.sectionList.Add(3);
                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(vg_id).breakSection.sectionList.Add(2);
                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(vg_id).breakSection.sectionList.Add(1);
                //Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(vg_id).breakSection.sectionList.Add(0);

                var dss = new Il2CppSystem.Collections.Generic.List<CodeStage.AntiCheat.ObscuredTypes.ObscuredInt>();
                dss.Add(new CodeStage.AntiCheat.ObscuredTypes.ObscuredInt(1021105));

                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(vg_id)._securedDefenseSkillIDList = dss;


                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(vg_id)._securedAggro = new CodeStage.AntiCheat.ObscuredTypes.ObscuredInt(vg_aggro);

                //Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(vg_id)._securedMinSpeedList.Clear();
                //Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(vg_id)._securedMaxSpeedList.Clear();
                //Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(vg_id).minSpeedList.Clear();
                //Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(vg_id).maxSpeedList.Clear();

                //Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(vg_id)._securedMinSpeedList.Add(new CodeStage.AntiCheat.ObscuredTypes.ObscuredInt(100));
                //Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(vg_id)._securedMaxSpeedList.Add(new CodeStage.AntiCheat.ObscuredTypes.ObscuredInt(200));
                //Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(vg_id).minSpeedList.Add(new CodeStage.AntiCheat.ObscuredTypes.ObscuredInt(100));
                //Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(vg_id).maxSpeedList.Add(new CodeStage.AntiCheat.ObscuredTypes.ObscuredInt(200));
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

                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(xi_id)._securedDefenseSkillIDList = dss;
                Singleton<StaticDataManager>.Instance.PersonalityStaticDataList.GetData(xi_id)._securedAggro = new CodeStage.AntiCheat.ObscuredTypes.ObscuredInt(xi_aggro);
            }

            Logger.Log("Patch done!");
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
    //    Logger.Log("login patch");
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
    //    Logger.Log("funny patch rn: " + hex);
    //    //Logger.Log("Server response: " + responseJson);
    //    return false;
    //}

    //[HarmonyPatch(typeof(SimpleCrypto), "Encrypt")]
    //[HarmonyPrefix]
    //public static bool sce(System.Byte[] bytes, System.Int64 encryptedTime)
    //{
    //    // Your logic after the original method executes
    //    //Console.WriteLine("After Execute method");
    //    Logger.Log("funny patch rn 2: " + encryptedTime);
    //    Logger.Log("funny patch rn 2: " + bytes);
    //    //Logger.Log("Server response: " + responseJson);
    //    return false;
    //}

    //[HarmonyPatch(typeof(SimpleCrypto), "BytesToHex")]
    //[HarmonyPrefix]
    //public static bool scb(System.Byte[] bytes)
    //{
    //    // Your logic after the original method executes
    //    //Console.WriteLine("After Execute method");
    //    Logger.Log("funny patch rn 3: " + bytes);
    //    //Logger.Log("Server response: " + responseJson);
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
