using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System.IO;
using System.Reflection;
using UnityEngine;
using GameNetcodeStuff;

namespace YourThunderstoreTeam;

[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    public static Plugin Instance { get; set; }

    public static ManualLogSource Log => Instance.Logger;

    private readonly Harmony _harmony = new(PluginInfo.PLUGIN_GUID);

    public static string assembly_path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

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
            if ((Object)(object)MainAssetBundle == (Object)null)
            {
                using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(GetAssemblyName() + "." + mainAssetBundleName))
                {
                    MainAssetBundle = AssetBundle.LoadFromStream(stream);
                }
            }
        }
    }

    public Plugin()
    {
        Instance = this;
    }

    private void Awake()
    {
        Assets.PopulateAssets();
        EnemyType val = Assets.MainAssetBundle.LoadAsset<EnemyType>("yandereEnemy");
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
