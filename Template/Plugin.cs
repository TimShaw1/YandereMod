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

namespace yandereMod;

[BepInPlugin("YandereMod", "YandereMod", "1.0.0")]
[BepInDependency(LethalLib.Plugin.ModGUID)]
public class Plugin : BaseUnityPlugin
{
    public static Plugin Instance { get; set; }

    public static ManualLogSource Log => Instance.Logger;

    private readonly Harmony _harmony = new("YandereMod");

    public static string assembly_path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

    static TileSet MyTileSet;

    public Transform yandereRoomToTarget;

    static GameObject yandereChair;
    static bool chairSpawned = false;

    static void WriteToConsole(string output)
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

        Assets.PopulateAssets();
        WriteToConsole("Populated Assets: " + Assets.MainAssetBundle.name);
        foreach (var asset in Assets.MainAssetBundle.GetAllAssetNames())
        {
            WriteToConsole(asset);
        }
        EnemyType val = Assets.MainAssetBundle.LoadAsset<EnemyType>("assets/yandereenemy.asset");
        TerminalNode val2 = Assets.MainAssetBundle.LoadAsset<TerminalNode>("assets/yandereterminalnode.asset");
        TerminalKeyword val3 = Assets.MainAssetBundle.LoadAsset<TerminalKeyword>("assets/yandereterminalkeyword.asset");
        MyTileSet = Assets.MainAssetBundle.LoadAsset<TileSet>("assets/yanderetileset.asset");
        yandereChair = Assets.MainAssetBundle.LoadAsset<GameObject>("assets/yanderechair.prefab");
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
            if (__instance.IsServer)
            {
                var tiles = FindObjectsOfType<Tile>();
                foreach (var tile in tiles)
                {
                    if (tile.gameObject.name.Contains("SmallRoom2"))
                    {
                        foreach (Transform child in tile.gameObject.transform)
                        {
                            if (child.gameObject.name.Contains("AINode"))
                            {
                                Instance.yandereRoomToTarget = child;
                                if (!chairSpawned)
                                {
                                    var chair = Instantiate(yandereChair, tile.gameObject.transform.position + new Vector3(0, 2, 0), tile.gameObject.transform.rotation, tile.gameObject.transform);
                                    // Move chair "forwards" (from where you would look when sitting) 4 units
                                    chair.transform.position += chair.transform.forward * -4;
                                    chairSpawned = true;
                                }
                                return;
                            }
                        }
                    }
                }
                WriteToConsole("No suitable target room found for yandere.");
                // Consider doing this for each round of enemies?
            }
        }
    }

    [HarmonyPatch(typeof(yandereAI), "Start")]
    class YandereStartPatch()
    {
        static void Postfix(yandereAI __instance)
        {
            __instance.roomToTarget = Instance.yandereRoomToTarget;
            WriteToConsole("Set room to target as: " + Instance.yandereRoomToTarget.gameObject.name);
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
