using System;
using HarmonyLib;
using UnityEngine;

namespace HaldorBounties
{
    public static class GatherTracker
    {
        // Capture item info in prefix (GO may be destroyed after pickup)
        [ThreadStatic] private static string _pendingPrefab;
        [ThreadStatic] private static int _pendingStack;

        [HarmonyPatch(typeof(Humanoid), "Pickup")]
        [HarmonyPrefix]
        private static void Pickup_Prefix(GameObject go)
        {
            _pendingPrefab = null;
            _pendingStack = 0;

            if (go == null) return;
            var itemDrop = go.GetComponent<ItemDrop>();
            if (itemDrop == null || itemDrop.m_itemData == null) return;

            // Get prefab name
            if (itemDrop.m_itemData.m_dropPrefab != null)
                _pendingPrefab = itemDrop.m_itemData.m_dropPrefab.name;
            else
            {
                // Fallback: use Utils.GetPrefabName (strips "(Clone)")
                _pendingPrefab = Utils.GetPrefabName(go);
            }

            _pendingStack = Mathf.Max(itemDrop.m_itemData.m_stack, 1);
        }

        [HarmonyPatch(typeof(Humanoid), "Pickup")]
        [HarmonyPostfix]
        private static void Pickup_Postfix(bool __result, Humanoid __instance)
        {
            try
            {
                if (!__result) return;
                if (string.IsNullOrEmpty(_pendingPrefab)) return;
                if (__instance != Player.m_localPlayer) return;

                HaldorBounties.Log.LogInfo($"[GatherTracker] Picked up: {_pendingPrefab} x{_pendingStack}");
                BountyManager.Instance?.IncrementGather(_pendingPrefab, _pendingStack);
            }
            catch (Exception ex)
            {
                HaldorBounties.Log.LogWarning($"[GatherTracker] Error: {ex.Message}");
            }
            finally
            {
                _pendingPrefab = null;
                _pendingStack = 0;
            }
        }
    }
}
