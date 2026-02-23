using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace HaldorBounties
{
    [HarmonyPatch(typeof(Character), "OnDeath")]
    public static class KillTracker
    {
        // Use a stack so re-entrant OnDeath calls remain correctly paired.
        [ThreadStatic] private static Stack<(string Prefab, int Level)?> _killStack;

        // Cached reflection, initialized once on first use.
        private static System.Reflection.FieldInfo _hasHitField;
        private static System.Reflection.FieldInfo _lastHitField;
        private static bool _fieldsCached;

        private static bool IsActiveTaggedBounty(Character character)
        {
            var nview = character.GetComponent<ZNetView>();
            var zdo = nview?.GetZDO();
            if (zdo == null || !zdo.GetBool("HaldorBountyMiniboss")) return false;

            string bountyId = zdo.GetString("HaldorBountyId", "");
            if (string.IsNullOrEmpty(bountyId)) return false;

            var state = BountyManager.Instance?.GetState(bountyId) ?? BountyState.Available;
            return state == BountyState.Active || state == BountyState.Ready;
        }

        [HarmonyPrefix]
        private static void Prefix(Character __instance)
        {
            if (_killStack == null) _killStack = new Stack<(string, int)?>();

            try
            {
                if (__instance == null || __instance.IsPlayer())
                {
                    _killStack.Push(null);
                    return;
                }

                if (!_fieldsCached)
                {
                    _hasHitField = AccessTools.Field(typeof(Character), "m_localPlayerHasHit");
                    _lastHitField = AccessTools.Field(typeof(Character), "m_lastHit");
                    _fieldsCached = true;
                }

                bool hasHit = false;
                bool lastHitByLocal = false;
                bool taggedBounty = IsActiveTaggedBounty(__instance);

                try
                {
                    if (_hasHitField != null)
                        hasHit = (bool)_hasHitField.GetValue(__instance);

                    if (_lastHitField?.GetValue(__instance) is HitData lastHit)
                        lastHitByLocal = lastHit.GetAttacker() == Player.m_localPlayer;
                }
                catch (Exception ex)
                {
                    HaldorBounties.Log.LogWarning($"[KillTracker] Failed to read kill fields: {ex.Message}");
                }

                bool isRelevant = hasHit || lastHitByLocal || taggedBounty;

                // Proximity fallback: if the primary detection failed but the creature
                // died near the local player, still count it. Handles edge cases where
                // reflection fails or certain attack types don't set m_localPlayerHasHit.
                if (!isRelevant && Player.m_localPlayer != null)
                {
                    float dist = Vector3.Distance(__instance.transform.position, Player.m_localPlayer.transform.position);
                    if (dist < 40f)
                        isRelevant = true;
                }

                if (!isRelevant)
                {
                    _killStack.Push(null);
                    return;
                }

                string prefab = Utils.GetPrefabName(__instance.gameObject);
                int level = __instance.GetLevel();
                _killStack.Push((prefab, level));
            }
            catch (Exception ex)
            {
                HaldorBounties.Log.LogWarning($"[KillTracker] Prefix error: {ex.Message}");
                _killStack?.Push(null);
            }
        }

        [HarmonyPostfix]
        private static void Postfix()
        {
            if (_killStack == null || _killStack.Count == 0) return;

            var entry = _killStack.Pop();
            try
            {
                if (!entry.HasValue || string.IsNullOrEmpty(entry.Value.Prefab)) return;
                BountyManager.Instance?.IncrementKill(entry.Value.Prefab, entry.Value.Level);
            }
            catch (Exception ex)
            {
                HaldorBounties.Log.LogWarning($"[KillTracker] Postfix error: {ex.Message}");
            }
        }
    }
}
