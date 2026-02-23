using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace HaldorBounties
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency("com.haldor.overhaul")]
    public class HaldorBounties : BaseUnityPlugin
    {
        public const string PluginGUID = "com.haldor.bounties";
        public const string PluginName = "Haldor Bounties";
        public const string PluginVersion = "1.0.0";

        private static Harmony _harmony;
        internal static ManualLogSource Log;

        private void Awake()
        {
            Log = Logger;
            Log.LogInfo($"{PluginName} v{PluginVersion} loading...");

            string configPath = Path.Combine(Paths.ConfigPath, "HaldorBounties.bounties.json");
            BountyConfig.Initialize(configPath);
            BountyManager.Initialize();

            _harmony = new Harmony(PluginGUID);
            _harmony.PatchAll(Assembly.GetExecutingAssembly());

            // L-6: Log total patch count only (individual method names are too verbose for production)
            int count = 0;
            foreach (var _ in _harmony.GetPatchedMethods()) count++;
            Log.LogInfo($"{PluginName} loaded successfully! ({count} methods patched)");
        }

        private void Update()
        {
            BountyManager.Instance?.UpdateBountyPins();
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
            Log.LogInfo($"{PluginName} unloaded.");
        }
    }
}
