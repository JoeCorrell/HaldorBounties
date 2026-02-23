using HarmonyLib;

namespace HaldorBounties
{
    /// <summary>Re-applies bounty status effects whenever the local player spawns into the world.</summary>
    [HarmonyPatch(typeof(Player), "OnSpawned")]
    public static class PlayerSpawnedPatch
    {
        [HarmonyPostfix]
        private static void Postfix(Player __instance)
        {
            if (__instance != Player.m_localPlayer) return;
            BountyManager.Instance?.RefreshStatusEffects(__instance);
            BountyManager.Instance?.RestoreBountyPins(__instance);
        }
    }
}
