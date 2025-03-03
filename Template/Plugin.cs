using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System.IO;
using System.Reflection;
using UnityEngine;
using LethalLib.Modules;
using System;
using DunGen;
using System.Collections.Generic;
using YourThunderstoreTeam;
using Unity.Netcode;
using BepInEx.Configuration;
using System.Threading.Tasks;
using System.Linq;

namespace yandereMod;

[BepInPlugin("YandereMod", "YandereMod", "1.0.0")]
[BepInDependency(LethalLib.Plugin.ModGUID)]
public class Plugin : BaseUnityPlugin
{
    public static Plugin Instance { get; set; }

    public static ManualLogSource Log => Instance.Logger;

    private readonly Harmony _harmony = new("YandereMod");

    public static string assembly_path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

    public static Transform yandereRoomToTarget;

    public static GameObject yandereChair;
    public static bool chairSpawned = false;
    public static Transform chairLocation;

    public static GameObject NetworkClassObj;
    private static GameObject SpawnedNetworkClassObj;
    private static GameObject NoAIPrefab;

    private static ConfigEntry<string> Chat_Service;

    private static ConfigEntry<string> ChatGPT_api_key;
    private static ConfigEntry<string> ChatGPT_model;

    private static ConfigEntry<string> Gemini_api_key;
    private static ConfigEntry<string> Gemini_model;

    private static ConfigEntry<string> Azure_api_key;
    private static ConfigEntry<string> Azure_region;
    private static ConfigEntry<string> Azure_language;

    public static void WriteToConsole(string output)
    {
        Console.WriteLine("YandereMod: " + output);
    }

    public static class Assets
    {
        public static string mainAssetBundleName = "yandereassets";

        public static AssetBundle MainAssetBundle = null;

        private static string GetAssemblyName()
        {
            return Assembly.GetExecutingAssembly().FullName.Split(',')[0];
        }

        public static void PopulateAssets()
        {
            string resourceName = Assembly.GetExecutingAssembly().GetManifestResourceNames().Single<string>(str => str.EndsWith("yandereassets"));
            var fileStream2 = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);
            var path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "yandereassets");
            if (!File.Exists(path))
            {
                var fileStream = File.Create(path);
                fileStream2.Seek(0, SeekOrigin.Begin);
                fileStream2.CopyTo(fileStream);
                fileStream.Close();
            }

            if ((UnityEngine.Object)(object)MainAssetBundle == (UnityEngine.Object)null)
            {
                string sAssemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

                MainAssetBundle = AssetBundle.LoadFromFile(Path.Combine(sAssemblyLocation, "yandereassets"));
            }
        }
    }

    public Plugin()
    {
        Instance = this;
    }

    private void Awake()
    {
        var types = Assembly.GetExecutingAssembly().GetTypes();
        foreach (var type in types)
        {
            var methods = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            foreach (var method in methods)
            {
                var attributes = method.GetCustomAttributes(typeof(RuntimeInitializeOnLoadMethodAttribute), false);
                if (attributes.Length > 0)
                {
                    method.Invoke(null, null);
                }
            }
        }

        Chat_Service = Config.Bind<string>(
                "Chat",
                "Chat Service",
                "ChatGPT",
                new ConfigDescription(
                "Which chat service to use (ChatGPT or Gemini)",
                new AcceptableValueList<string>("ChatGPT", "Gemini")
                )
                );

        ChatGPT_api_key = Config.Bind<string>(
                "Chat",
                "ChatGPT API key",
                "",
                "Your ChatGPT API key. Do NOT add extra characters like \""
                );

        ChatGPT_model = Config.Bind<string>(
            "Chat",
            "ChatGPT Model",
            "gpt-4o",
            new ConfigDescription(
            "Which gpt model to use. Use gpt-4o-mini if you want to save on cost and don't mind less convincing results",
            new AcceptableValueList<string>("gpt-4o", "gpt-4o-mini")
            )
            );

        Gemini_api_key = Config.Bind<string>(
                "Chat",
                "Gemini API key",
                "",
                "Your Gemini API key. Do NOT add extra characters like \""
                );

        Gemini_model = Config.Bind<string>(
            "Chat",
            "Gemini Model",
            "gemini-2.0-flash",
            new ConfigDescription(
            "Which Gemini model to use. Use gemini-2.0-flash if you want faster responses",
            new AcceptableValueList<string>("gemini-1.5-pro", "gemini-2.0-flash")
            )
            );

        Azure_api_key = Config.Bind<string>(
            "Azure",
            "API key",
            "",
            "Your Azure API key. Do NOT add extra characters like \""
            );

        Azure_region = Config.Bind<string>(
            "Azure",
            "Region",
            "canadacentral",
            "Your Azure region"
            );

        Azure_language = Config.Bind<string>(
            "Azure",
            "Language",
            "en-US",
            "Your desired speech recognition language, list of supported languages can be found here: https://learn.microsoft.com/en-us/azure/ai-services/speech-service/language-support?tabs=stt"
            );

        
        
        AzureSTT.Init(Azure_api_key.Value, Azure_region.Value, Azure_language.Value);
        if (Chat_Service.Value == "ChatGPT")
            ChatManager.Init(ChatGPT_api_key.Value, ChatGPT_model.Value);
        else
            ChatManager.Init(Gemini_api_key.Value, Gemini_model.Value, true);
        
        

        Assets.PopulateAssets();
        WriteToConsole("Populated Assets: " + Assets.MainAssetBundle.name);
        foreach (var asset in Assets.MainAssetBundle.GetAllAssetNames())
        {
            WriteToConsole(asset);
        }
        EnemyType val = Assets.MainAssetBundle.LoadAsset<EnemyType>("assets/yandereenemy.asset");
        TerminalNode val2 = Assets.MainAssetBundle.LoadAsset<TerminalNode>("assets/yandereterminalnode.asset");
        TerminalKeyword val3 = Assets.MainAssetBundle.LoadAsset<TerminalKeyword>("assets/yandereterminalkeyword.asset");
        yandereChair = Assets.MainAssetBundle.LoadAsset<GameObject>("assets/yanderechair.prefab");
        NetworkClassObj = Assets.MainAssetBundle.LoadAsset<GameObject>("assets/yanderenetworkmanager.prefab");
        NoAIPrefab = Assets.MainAssetBundle.LoadAsset<GameObject>("assets/yandererigupdatednoai.prefab");
        LethalLib.Modules.NetworkPrefabs.RegisterNetworkPrefab(val.enemyPrefab);
        Enemies.RegisterEnemy(val, 22, (Levels.LevelTypes)(-1), (Enemies.SpawnType)0, val2, val3);
        Log.LogInfo($"Applying patches...");
        ApplyPluginPatch();
        Log.LogInfo($"Patches applied");
    }

    [HarmonyPatch(typeof(RoundManager), "PlotOutEnemiesForNextHour")]
    class EnemySpawnPatch()
    {
        static void Postfix(RoundManager __instance)
        {
            WriteToConsole("Plotting...");
            if (__instance.IsServer)
            {
                NetworkingClass.Instance.SpawnChairClientRpc();
            }
        }
    }

    [HarmonyPatch(typeof(yandereAI), "Start")]
    class YandereStartPatch()
    {
        static void Postfix(yandereAI __instance)
        {
            __instance.roomToTarget = yandereRoomToTarget;
            __instance.chairInRoom = chairLocation;
            //WriteToConsole("Set room to target as: " + yandereRoomToTarget.gameObject.name);
        }
    }

    [HarmonyPatch(typeof(GameNetworkManager), "Start")]
    class GameNetworkManagerStartPatch
    {
        static void Postfix()
        {
            WriteToConsole("ADDED PREFAB");
            NetworkManager.Singleton.AddNetworkPrefab(NetworkClassObj);
            NetworkManager.Singleton.AddNetworkPrefab(NoAIPrefab);
        }
    }

    [HarmonyPatch(typeof(StartOfRound), "Start")]
    class StartOfRoundStartPatch
    {
        static void Postfix()
        {
            if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)
            {
                chairSpawned = false;
                WriteToConsole("Spawning");
                var networkHandlerHost = Instantiate(NetworkClassObj, Vector3.zero, Quaternion.identity);
                networkHandlerHost.GetComponent<NetworkObject>().Spawn();
            }
        }
    }

    /// <summary>
    /// Applies the patch to the game.
    /// </summary>
    private void ApplyPluginPatch()
    {
        _harmony.PatchAll();
    }
}
