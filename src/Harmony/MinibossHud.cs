using System;
using HarmonyLib;
using UnityEngine;

namespace HaldorBounties
{
    public static class MinibossHud
    {
        private static bool TryApplyMinibossFlags(Character character, out string bountyId)
        {
            bountyId = "";
            if (character == null || character.IsPlayer()) return false;

            var nview = character.GetComponent<ZNetView>();
            if (nview == null) return false;
            var zdo = nview.GetZDO();
            if (zdo == null || !zdo.GetBool("HaldorBountyMiniboss")) return false;

            bountyId = zdo.GetString("HaldorBountyId", "");
            string bossName = zdo.GetString("HaldorBountyBossName", "");

            // Check if this is a raid bounty — raids use normal enemy HUD, not boss bar
            bool isRaid = false;
            if (BountyManager.Instance != null && !string.IsNullOrEmpty(bountyId))
            {
                if (BountyManager.Instance.TryGetEntry(bountyId, out var entry) && entry.Tier == "Raid")
                    isRaid = true;
            }

            if (isRaid)
            {
                character.m_name = "Valheim Raider";
                character.m_boss = false;
                character.m_dontHideBossHud = false;
            }
            else
            {
                character.m_boss = true;
                character.m_dontHideBossHud = true;
                if (!string.IsNullOrEmpty(bossName))
                    character.m_name = bossName;
            }

            return true;
        }

        // Restore miniboss flags/name after reloads or world streaming.
        [HarmonyPatch(typeof(Character), "Start")]
        public static class CharacterStartPatch
        {
            [HarmonyPostfix]
            private static void Postfix(Character __instance)
            {
                try
                {
                    if (!TryApplyMinibossFlags(__instance, out string bountyId)) return;

                    if (!string.IsNullOrEmpty(bountyId))
                        BountyManager.Instance?.RegisterBountyCreature(bountyId, __instance);

                    HaldorBounties.Log.LogInfo($"[MinibossHud] Restored bounty creature: {__instance.m_name} (bounty={bountyId})");
                }
                catch (Exception ex)
                {
                    HaldorBounties.Log.LogWarning($"[MinibossHud] CharacterStart error: {ex.Message}");
                }
            }
        }

        [HarmonyPatch(typeof(EnemyHud), "ShowHud")]
        public static class ShowHudPatch
        {
            // Ensure miniboss flags are applied before EnemyHud chooses boss vs normal HUD.
            [HarmonyPrefix]
            private static void Prefix(EnemyHud __instance, Character c, bool isMount)
            {
                try
                {
                    if (isMount || c == null) return;

                    bool wasBoss = c.IsBoss();
                    if (!TryApplyMinibossFlags(c, out string bountyId)) return;

                    if (!string.IsNullOrEmpty(bountyId))
                        BountyManager.Instance?.RegisterBountyCreature(bountyId, c);

                    // If this character just became boss-tagged, rebuild stale non-boss HUD data.
                    if (wasBoss) return;

                    var hudsField = AccessTools.Field(typeof(EnemyHud), "m_huds");
                    if (hudsField == null) return;
                    var huds = hudsField.GetValue(__instance) as System.Collections.IDictionary;
                    if (huds == null || !huds.Contains(c)) return;

                    var hudData = huds[c];
                    var guiField = AccessTools.Field(hudData.GetType(), "m_gui");
                    if (guiField?.GetValue(hudData) is GameObject gui)
                        UnityEngine.Object.Destroy(gui);

                    huds.Remove(c);
                }
                catch (Exception ex)
                {
                    HaldorBounties.Log.LogWarning($"[MinibossHud] ShowHud prefix error: {ex.Message}");
                }
            }

            // Scale down miniboss HUD to 75% of normal boss bar size.
            [HarmonyPostfix]
            private static void Postfix(EnemyHud __instance, Character c, bool isMount)
            {
                try
                {
                    if (isMount || !TryApplyMinibossFlags(c, out _)) return;

                    var hudsField = AccessTools.Field(typeof(EnemyHud), "m_huds");
                    if (hudsField == null) return;

                    var huds = hudsField.GetValue(__instance) as System.Collections.IDictionary;
                    if (huds == null || !huds.Contains(c)) return;

                    var hudData = huds[c];
                    var guiField = AccessTools.Field(hudData.GetType(), "m_gui");
                    if (guiField == null) return;

                    var gui = guiField.GetValue(hudData) as GameObject;
                    if (gui == null) return;

                    gui.transform.localScale = new Vector3(0.75f, 0.75f, 1f);
                }
                catch (Exception ex)
                {
                    HaldorBounties.Log.LogWarning($"[MinibossHud] ShowHud error: {ex.Message}");
                }
            }
        }
    }
}
