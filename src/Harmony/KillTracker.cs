using System;
using System.Collections.Generic;
using HarmonyLib;

namespace HaldorBounties
{
    public static class KillTracker
    {
        // H-4: Use a stack instead of flat ThreadStatic fields so that re-entrant
        // OnDeath calls (e.g. a death triggers a spawn that immediately dies) don't
        // overwrite the outer prefix's captured data before its postfix runs.
        [ThreadStatic] private static Stack<(string Prefab, int Level)?> _killStack;

        // Cached reflection — initialized once on first use
        private static System.Reflection.FieldInfo _hasHitField;
        private static bool _hasHitFieldCached;

        [HarmonyPatch(typeof(Character), "OnDeath")]
        [HarmonyPrefix]
        private static void OnDeath_Prefix(Character __instance)
        {
            // Always push — keeps stack balanced with postfix pops regardless of early returns
            if (_killStack == null) _killStack = new Stack<(string, int)?>();

            try
            {
                if (__instance == null || __instance.IsPlayer()) { _killStack.Push(null); return; }

                if (!_hasHitFieldCached)
                {
                    _hasHitField      = AccessTools.Field(typeof(Character), "m_localPlayerHasHit");
                    _hasHitFieldCached = true;
                }

                bool hasHit = false;
                try
                {
                    if (_hasHitField != null)
                        hasHit = (bool)_hasHitField.GetValue(__instance);
                }
                catch (Exception ex)
                {
                    HaldorBounties.Log.LogWarning($"[KillTracker] Failed to read m_localPlayerHasHit: {ex.Message}");
                    _killStack.Push(null);
                    return;
                }

                if (!hasHit) { _killStack.Push(null); return; }

                string prefab = Utils.GetPrefabName(__instance.gameObject);
                int    level  = __instance.GetLevel();
                _killStack.Push((prefab, level));

                HaldorBounties.Log.LogInfo($"[KillTracker] Prefix captured: {prefab} level={level}");
            }
            catch (Exception ex)
            {
                HaldorBounties.Log.LogWarning($"[KillTracker] Prefix error: {ex.Message}");
                // Push null to keep the stack balanced with the postfix pop
                _killStack?.Push(null);
            }
        }

        [HarmonyPatch(typeof(Character), "OnDeath")]
        [HarmonyPostfix]
        private static void OnDeath_Postfix()
        {
            if (_killStack == null || _killStack.Count == 0) return;

            var entry = _killStack.Pop();
            try
            {
                if (!entry.HasValue || string.IsNullOrEmpty(entry.Value.Prefab)) return;
                HaldorBounties.Log.LogInfo($"[KillTracker] Kill confirmed: {entry.Value.Prefab} (level {entry.Value.Level})");
                BountyManager.Instance?.IncrementKill(entry.Value.Prefab, entry.Value.Level);
            }
            catch (Exception ex)
            {
                HaldorBounties.Log.LogWarning($"[KillTracker] Postfix error: {ex.Message}");
            }
        }
    }
}
