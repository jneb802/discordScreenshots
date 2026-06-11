using System;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Jotunn.Managers;
using UnityEngine;
using Jotunn.Utils;
using discordScreenshots.Patches;

namespace discordScreenshots
{
    [BepInPlugin(ModGUID, ModName, ModVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    public class discordScreenshotsPlugin : BaseUnityPlugin
    {
        private const string ModName = "discordScreenshots";
        private const string ModVersion = "1.6.4";
        private const string Author = "warpalicious";
        private const string ModGUID = Author + "." + ModName;

        private readonly Harmony HarmonyInstance = new(ModGUID);

        public static readonly ManualLogSource TemplateLogger = BepInEx.Logging.Logger.CreateLogSource(ModName);

        public static AssetBundle assetBundle;

        public void Awake()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            HarmonyInstance.PatchAll(assembly);

            // LoadAssetBundle();

            BepinexConfiguration.GenerateConfigs(Config);
            SimpleDiscordWebhook.ConfigureScreenshotEncodingForStartupResolution();

            // // Initialize our custom pieces with asset bundle
            // PrefabUtils.Initialize();
        }

        public static void LoadAssetBundle()
        {
            assetBundle = AssetUtils.LoadAssetBundleFromResources(
                "discordscreenshots",
                Assembly.GetExecutingAssembly()
            );
        }

        private void OnDestroy()
        {
            Config.Save();
        }

        private void OnApplicationQuit()
        {
            PlayerDeathScreenshotPatch.UploadStoredDeathScreenshot("application quit", waitForUpload: true);
        }
    }
}
