using System;
using System.Collections.Generic;
using UnityEngine;

namespace HaldorBounties
{
    public static class RewardResolver
    {
        public class ResolvedReward
        {
            public RewardCategory Category;
            public string DisplayText;
            public string PrefabName;
            public int Stack;
            public int Quality;
            public int CoinAmount;
            public Sprite Icon;
        }

        /// <summary>
        /// Resolve the 4 reward options for a bounty. Deterministic based on bounty ID.
        /// Categories: Bank Deposit, Ingots, Resources, Consumables.
        /// </summary>
        public static List<ResolvedReward> ResolveRewards(BountyEntry entry)
        {
            var rewards = new List<ResolvedReward>();
            int seed = StableHash(entry.Id);
            var rng = new System.Random(seed);
            var biome = RewardPool.GetBiomeTier(entry);
            bool isMiniboss = entry.Tier == "Miniboss" || entry.Tier == "Special";
            bool isRaid = entry.Tier == "Raid";

            // Multipliers: miniboss 1.5x, raid 1.25x
            float coinMult = isMiniboss ? 1.5f : isRaid ? 1.25f : 1f;
            float stackMult = isMiniboss ? 1.5f : isRaid ? 1.25f : 1f;

            // 1) Bank Deposit — coins deposited directly into bank
            int coinAmount = (int)(entry.Reward * coinMult);
            rewards.Add(new ResolvedReward
            {
                Category = RewardCategory.Coins,
                CoinAmount = coinAmount,
                PrefabName = "Coins",
                DisplayText = $"{coinAmount}c",
                Stack = coinAmount,
                Quality = 1,
            });

            // 2) Ingots (refined metals/bars)
            rewards.Add(ResolveFromPool(RewardPool.IngotPool, biome, rng, stackMult, RewardCategory.Ingots));

            // 3) Resources (crafting materials)
            rewards.Add(ResolveFromPool(RewardPool.ResourcePool, biome, rng, stackMult, RewardCategory.Resources));

            // 4) Consumables (food/mead)
            rewards.Add(ResolveFromPool(RewardPool.ConsumablePool, biome, rng, stackMult, RewardCategory.Consumables));

            return rewards;
        }

        private static ResolvedReward ResolveFromPool(
            Dictionary<BiomeTier, RewardItem[]> pool,
            BiomeTier biome, System.Random rng, float stackMult,
            RewardCategory category)
        {
            if (!pool.TryGetValue(biome, out var items) || items.Length == 0)
            {
                if (!pool.TryGetValue(BiomeTier.Meadows, out items) || items == null || items.Length == 0)
                    return new ResolvedReward { Category = category, DisplayText = "???", PrefabName = "", Stack = 1, Quality = 1 };
            }

            int index = rng.Next(items.Length);
            var item = items[index];

            int stack = rng.Next(item.MinStack, item.MaxStack + 1);
            if (stackMult > 1f)
                stack = Mathf.Max(1, (int)(stack * stackMult));

            string displayName = GetDisplayName(item.PrefabName);
            string displayText = stack > 1
                ? $"{stack}x {displayName}"
                : displayName;

            return new ResolvedReward
            {
                Category = category,
                PrefabName = item.PrefabName,
                Stack = Mathf.Max(1, stack),
                Quality = item.Quality,
                DisplayText = displayText,
            };
        }

        /// <summary>
        /// Deliver the chosen reward to the player. Returns true on success.
        /// Coins are handled by BountyManager directly — this handles item rewards.
        /// </summary>
        public static bool DeliverReward(ResolvedReward reward, Player player)
        {
            if (player == null || reward == null) return false;
            if (reward.Category == RewardCategory.Coins) return true;

            try
            {
                var prefab = ObjectDB.instance?.GetItemPrefab(reward.PrefabName);
                if (prefab == null)
                {
                    HaldorBounties.Log.LogWarning($"[RewardResolver] Prefab not found: {reward.PrefabName}");
                    return false;
                }

                var itemDrop = prefab.GetComponent<ItemDrop>();
                if (itemDrop == null) return false;

                var itemData = itemDrop.m_itemData.Clone();
                itemData.m_dropPrefab = prefab;
                itemData.m_worldLevel = (byte)Game.m_worldLevel;
                itemData.m_stack = reward.Stack;
                itemData.m_quality = reward.Quality;
                itemData.m_durability = itemData.GetMaxDurability();

                var inventory = ((Humanoid)player).GetInventory();
                if (inventory.CanAddItem(itemData))
                {
                    inventory.AddItem(itemData);
                    HaldorBounties.Log.LogInfo($"[RewardResolver] Added to inventory: {reward.PrefabName} x{reward.Stack}");
                    return true;
                }

                // Inventory full — drop at player feet
                Vector3 dropPos = player.transform.position + player.transform.forward * 1.5f + Vector3.up;
                ItemDrop.DropItem(itemData, reward.Stack, dropPos, player.transform.rotation);
                MessageHud.instance?.ShowMessage(MessageHud.MessageType.TopLeft,
                    "Inventory full — reward dropped nearby");
                HaldorBounties.Log.LogInfo($"[RewardResolver] Dropped at player: {reward.PrefabName} x{reward.Stack}");
                return true;
            }
            catch (Exception ex)
            {
                HaldorBounties.Log.LogWarning($"[RewardResolver] Delivery failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>Get localized display name from prefab name.</summary>
        public static string GetDisplayName(string prefabName)
        {
            try
            {
                var prefab = ObjectDB.instance?.GetItemPrefab(prefabName);
                var drop = prefab?.GetComponent<ItemDrop>();
                if (drop?.m_itemData?.m_shared != null)
                {
                    string localized = Localization.instance?.Localize(drop.m_itemData.m_shared.m_name);
                    if (!string.IsNullOrEmpty(localized))
                        return localized;
                }
            }
            catch { }
            return prefabName;
        }

        /// <summary>Get icon sprite for a reward item.</summary>
        public static Sprite GetRewardIcon(ResolvedReward reward)
        {
            if (reward.Icon != null) return reward.Icon;
            try
            {
                var prefab = ObjectDB.instance?.GetItemPrefab(reward.PrefabName);
                reward.Icon = prefab?.GetComponent<ItemDrop>()?.m_itemData?.GetIcon();
            }
            catch { }
            return reward.Icon;
        }

        /// <summary>Stable hash that doesn't change between sessions (unlike string.GetHashCode).</summary>
        private static int StableHash(string s)
        {
            unchecked
            {
                int hash = 5381;
                foreach (char c in s)
                    hash = ((hash << 5) + hash) + c;
                return hash & 0x7FFFFFFF;
            }
        }
    }
}
