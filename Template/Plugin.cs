using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System.IO;
using System.Reflection;
using UnityEngine;
using LethalLib.Modules;
using System;

namespace yandereMod;

[BepInPlugin("YandereMod", "YandereMod", "1.0.0")]
[BepInDependency(LethalLib.Plugin.ModGUID)]
public class Plugin : BaseUnityPlugin
{
    public static Plugin Instance { get; set; }

    public static ManualLogSource Log => Instance.Logger;

    private readonly Harmony _harmony = new("YandereMod");

    public static string assembly_path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

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
        WriteToConsole("2: " + (val == null));
        TerminalNode val2 = Assets.MainAssetBundle.LoadAsset<TerminalNode>("assets/yandereterminalnode.asset");
        WriteToConsole("3: " + (val2 == null));
        TerminalKeyword val3 = Assets.MainAssetBundle.LoadAsset<TerminalKeyword>("yandere");
        WriteToConsole("4: " + (val2 == null));
        // breaks here
        LethalLib.Modules.NetworkPrefabs.RegisterNetworkPrefab(val.enemyPrefab);
        WriteToConsole("5");
        Enemies.RegisterEnemy(val, 22, (Levels.LevelTypes)(-1), (Enemies.SpawnType)0, val2, val3);
        WriteToConsole("6");
        Log.LogInfo($"Applying patches...");
        ApplyPluginPatch();
        Log.LogInfo($"Patches applied");
    }

    /// <summary>
    /// Applies the patch to the game.
    /// </summary>
    private void ApplyPluginPatch()
    {
        _harmony.PatchAll();
    }
}
