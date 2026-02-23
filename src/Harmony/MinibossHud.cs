using System;
using HarmonyLib;
using UnityEngine;

namespace HaldorBounties
{
    public static class MinibossHud
    {
        // ── Restore m_boss and m_name from ZDO when creature loads ──
        // Uses Start instead of Awake to ensure ZNetView.Awake has already run
        // and the ZDO is available. m_boss and m_name are C# fields that reset
        // to prefab defaults when the creature's zone reloads or the game restarts.
        [HarmonyPatch(typeof(Character), "Start")]
        [HarmonyPostfix]
        private static void CharacterStart_Postfix(Character __instance)
        {
            try
            {
                if (__instance == null || __instance.IsPlayer()) return;

                var nview = __instance.GetComponent<ZNetView>();
                if (nview == null) return;
                var zdo = nview.GetZDO();
                if (zdo == null || !zdo.GetBool("HaldorBountyMiniboss")) return;

                // Re-apply boss properties from ZDO
                __instance.m_boss = true;

                string bossName = zdo.GetString("HaldorBountyBossName", "");
                if (!string.IsNullOrEmpty(bossName))
                    __instance.m_name = bossName;

                // Re-register with BountyManager for pin tracking
                string bountyId = zdo.GetString("HaldorBountyId", "");
                if (!string.IsNullOrEmpty(bountyId))
                    BountyManager.Instance?.RegisterBountyCreature(bountyId, __instance);

                HaldorBounties.Log.LogInfo($"[MinibossHud] Restored bounty creature: {bossName} (bounty={bountyId})");
            }
            catch (Exception ex)
            {
                HaldorBounties.Log.LogWarning($"[MinibossHud] CharacterStart error: {ex.Message}");
            }
        }

        // ── Scale down miniboss HUD (75% of normal boss bar) ──
        [HarmonyPatch(typeof(EnemyHud), "ShowHud")]
        [HarmonyPostfix]
        private static void ShowHud_Postfix(EnemyHud __instance, Character c, bool isMount)
        {
            try
            {
                if (c == null || !c.IsBoss() || isMount) return;

                // Check for our ZDO tag
                var nview = c.GetComponent<ZNetView>();
                if (nview == null) return;
                var zdo = nview.GetZDO();
                if (zdo == null || !zdo.GetBool("HaldorBountyMiniboss")) return;

                // Get the HUD data for this character
                var hudsField = AccessTools.Field(typeof(EnemyHud), "m_huds");
                if (hudsField == null) return;

                var huds = hudsField.GetValue(__instance) as System.Collections.IDictionary;
                if (huds == null || !huds.Contains(c)) return;

                // Access the m_gui field from HudData
                var hudData = huds[c];
                var guiField = AccessTools.Field(hudData.GetType(), "m_gui");
                if (guiField == null) return;

                var gui = guiField.GetValue(hudData) as GameObject;
                if (gui == null) return;

                // Scale down the miniboss HUD (75% of normal boss bar)
                gui.transform.localScale = new Vector3(0.75f, 0.75f, 1f);
            }
            catch (Exception ex)
            {
                HaldorBounties.Log.LogWarning($"[MinibossHud] ShowHud error: {ex.Message}");
            }
        }
    }
}
