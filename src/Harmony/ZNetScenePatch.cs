using HarmonyLib;

namespace HaldorBounties
{
    [HarmonyPatch(typeof(ZNetScene), "Awake")]
    public static class ZNetScenePatch
    {
        [HarmonyPostfix]
        static void Postfix(ZNetScene __instance)
        {
            NpcPrefabs.Init(__instance);
        }
    }
}
