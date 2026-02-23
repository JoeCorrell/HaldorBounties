using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace HaldorBounties
{
    public enum BountyState { Available, Active, Ready, Claimed }

    public class BountyManager
    {
        private const string StateKeyPrefix    = "HaldorBounty_State_";
        private const string ProgressKeyPrefix = "HaldorBounty_Progress_";
        private const string BossNameKeyPrefix = "HaldorBounty_BossName_";
        private const string BankDataKey       = "HaldorBank_Balance";
        private const string LastDayKey        = "HaldorBounty_LastDay";

        public static BountyManager Instance { get; private set; }

        // Cached reflection for TraderUI.ReloadBankBalance()
        private MethodInfo _reloadBankBalance;
        private MethodInfo _getTraderUI;

        // Day tracking
        private int _lastDay = -1;

        // Minimap pins for spawned bounty creatures
        private readonly Dictionary<string, Minimap.PinData> _bountyPins     = new Dictionary<string, Minimap.PinData>();
        private readonly Dictionary<string, Character>       _bountyCreatures = new Dictionary<string, Character>();

        // H-1: Active bounty ID set for fast filtering in IncrementKill/IncrementGather
        private readonly HashSet<string> _activeBountyIds = new HashSet<string>();
        private bool _activeBountyIdsLoaded;

        // O(1) entry lookup by ID (built once from BountyConfig at Initialize time)
        private readonly Dictionary<string, BountyEntry> _bountyLookup;

        private static readonly string[] BossNames =
        {
            "Grendel", "Thundermaw", "Ironhide", "Dreadfang", "Shadowbane",
            "Bonecrusher", "Frostbite", "Ashwalker", "Bloodreaver", "Stormcaller",
            "Nightterror", "Grimjaw", "Doombringer", "Soulrender", "Hellfire",
            "Deathgrip", "Voidfang", "Skullsplitter", "Ravenmaw", "Warbringer",
            "Emberclaw", "Steelhorn", "Rotfang", "Icevein", "Plaguebringer",
            "Titan", "Colossus", "Ravager", "Overlord", "Behemoth",
            "Gorefist", "Wraithbane", "Cinderborn", "Venomstrike", "Darkhorn",
            "Thunderfoot", "Ironjaw", "Blazeclaw", "Frostfang", "Doomhowl"
        };

        // Build O(1) lookup table from BountyConfig (must be called after BountyConfig.Initialize)
        private BountyManager()
        {
            _bountyLookup = new Dictionary<string, BountyEntry>(BountyConfig.Bounties.Count);
            foreach (var entry in BountyConfig.Bounties)
                _bountyLookup[entry.Id] = entry;
        }

        public static void Initialize()
        {
            Instance = new BountyManager();
            Instance.CacheReflection();
        }

        private void CacheReflection()
        {
            try
            {
                var traderUIType = Type.GetType("HaldorOverhaul.TraderUI, HaldorOverhaul");
                if (traderUIType != null)
                    _reloadBankBalance = traderUIType.GetMethod("ReloadBankBalance", BindingFlags.Public | BindingFlags.Instance);

                var controlPatchesType = Type.GetType("HaldorOverhaul.ControlPatches, HaldorOverhaul");
                if (controlPatchesType != null)
                    _getTraderUI = controlPatchesType.GetMethod("GetTraderUI", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);

                // L-4: Warn so players/devs know why bank UI won't refresh
                if (_getTraderUI == null)
                    HaldorBounties.Log.LogWarning("[BountyManager] GetTraderUI not found — bank balance will not refresh on coin reward.");
                if (_reloadBankBalance == null)
                    HaldorBounties.Log.LogWarning("[BountyManager] ReloadBankBalance not found — bank balance will not refresh on coin reward.");
            }
            catch (Exception ex)
            {
                HaldorBounties.Log.LogWarning($"[BountyManager] Reflection cache failed: {ex.Message}");
            }
        }

        // ── Active bounty set ──

        /// <summary>
        /// Lazy-loads active bounty IDs from player customData on first use.
        /// Subsequent calls are no-ops once loaded.
        /// </summary>
        private void EnsureActiveBountyIdsLoaded()
        {
            if (_activeBountyIdsLoaded) return;
            var player = Player.m_localPlayer;
            if (player == null) return;

            _activeBountyIds.Clear();
            foreach (var kvp in player.m_customData)
            {
                if (kvp.Key.StartsWith(StateKeyPrefix) && kvp.Value == "active")
                    _activeBountyIds.Add(kvp.Key.Substring(StateKeyPrefix.Length));
            }
            _activeBountyIdsLoaded = true;
        }

        // ── Day tracking ──

        public int GetCurrentDay()
        {
            if (EnvMan.instance == null || ZNet.instance == null) return 0;
            return EnvMan.instance.GetDay(ZNet.instance.GetTimeSeconds());
        }

        public void CheckDayReset()
        {
            var player = Player.m_localPlayer;
            if (player == null) return;

            int currentDay = GetCurrentDay();
            if (currentDay <= 0) return;

            if (_lastDay < 0)
            {
                if (player.m_customData.TryGetValue(LastDayKey, out string dayStr) && int.TryParse(dayStr, out int savedDay))
                    _lastDay = savedDay;
                else
                    _lastDay = currentDay;
            }

            if (currentDay == _lastDay) return;

            // Clear claimed states so bounties refresh for the new day
            var keysToRemove = new List<string>();
            foreach (var kvp in player.m_customData)
            {
                if (kvp.Key.StartsWith(StateKeyPrefix) && kvp.Value == "claimed")
                    keysToRemove.Add(kvp.Key);
            }
            foreach (var key in keysToRemove)
                player.m_customData.Remove(key);

            foreach (var key in keysToRemove)
            {
                string bountyId = key.Substring(StateKeyPrefix.Length);
                player.m_customData.Remove(ProgressKeyPrefix + bountyId);
                player.m_customData.Remove(BossNameKeyPrefix + bountyId);
            }

            _lastDay = currentDay;
            player.m_customData[LastDayKey] = currentDay.ToString();

            // Rebuild active bounty set — claimed entries were just removed
            _activeBountyIdsLoaded = false;
            EnsureActiveBountyIdsLoaded();

            HaldorBounties.Log.LogInfo($"[BountyManager] New day {currentDay} — reset {keysToRemove.Count} claimed bounties.");
        }

        // ── Daily bounty selection ──

        public List<BountyEntry> GetDailyBounties()
        {
            int day = GetCurrentDay();
            // M-1: Cast to long before multiplying to avoid int overflow for large day values
            var rng = new System.Random((int)((long)day * 31337 % int.MaxValue));

            var pool    = BountyConfig.Bounties.Where(b => IsBossGateUnlocked(b)).ToList();
            var gather  = pool.Where(b => b.Type == "Gather").ToList();
            var kill    = pool.Where(b => b.Type == "Kill" && !IsMiniboss(b)).ToList();
            var miniboss = pool.Where(b => IsMiniboss(b)).ToList();

            var daily = new List<BountyEntry>();
            daily.AddRange(PickRandom(gather,   2, rng));
            daily.AddRange(PickRandom(kill,     2, rng));
            daily.AddRange(PickRandom(miniboss, 2, rng));
            return daily;
        }

        private static List<BountyEntry> PickRandom(List<BountyEntry> pool, int count, System.Random rng)
        {
            if (pool.Count <= count) return new List<BountyEntry>(pool);
            return pool.OrderBy(_ => rng.Next()).Take(count).ToList();
        }

        private static bool IsMiniboss(BountyEntry entry) => entry.Tier == "Miniboss" || entry.Tier == "Special";

        private static bool IsBossGateUnlocked(BountyEntry entry)
        {
            if (string.IsNullOrEmpty(entry.RequiredBoss)) return true;
            if (ZoneSystem.instance == null) return false;
            return ZoneSystem.instance.GetGlobalKey(entry.RequiredBoss);
        }

        // ── State queries ──

        public BountyState GetState(string bountyId)
        {
            var player = Player.m_localPlayer;
            if (player == null) return BountyState.Available;

            if (!player.m_customData.TryGetValue(StateKeyPrefix + bountyId, out string stateStr))
                return BountyState.Available;

            if (stateStr == "claimed") return BountyState.Claimed;
            if (stateStr == "active")
            {
                // Use O(1) lookup instead of LINQ
                if (_bountyLookup.TryGetValue(bountyId, out var entry) && GetProgress(bountyId) >= entry.Amount)
                    return BountyState.Ready;
                return BountyState.Active;
            }
            return BountyState.Available;
        }

        public int GetProgress(string bountyId)
        {
            var player = Player.m_localPlayer;
            if (player == null) return 0;
            if (player.m_customData.TryGetValue(ProgressKeyPrefix + bountyId, out string val) && int.TryParse(val, out int progress))
                return progress;
            return 0;
        }

        public bool IsAvailable(string bountyId)
        {
            if (!_bountyLookup.TryGetValue(bountyId, out var entry)) return false;
            if (GetState(bountyId) != BountyState.Available) return false;
            return IsBossGateUnlocked(entry);
        }

        public List<BountyEntry> GetVisibleBounties()
        {
            CheckDayReset();

            var result   = new List<BountyEntry>();
            var addedIds = new HashSet<string>();

            foreach (var b in GetDailyBounties())
            {
                var state = GetState(b.Id);
                if (state == BountyState.Available && !IsAvailable(b.Id)) continue;
                result.Add(b);
                addedIds.Add(b.Id);
            }

            // Overflow: active/ready bounties from previous days
            foreach (var b in BountyConfig.Bounties)
            {
                if (addedIds.Contains(b.Id)) continue;
                var state = GetState(b.Id);
                if (state == BountyState.Active || state == BountyState.Ready)
                {
                    result.Add(b);
                    addedIds.Add(b.Id);
                }
            }
            return result;
        }

        // ── Boss name management ──

        public static string GetBossName(string bountyId)
        {
            var player = Player.m_localPlayer;
            if (player != null)
            {
                if (player.m_customData.TryGetValue(BossNameKeyPrefix + bountyId, out string savedName) && !string.IsNullOrEmpty(savedName))
                    return savedName;
            }
            return GenerateBossName(bountyId);
        }

        private static string GenerateBossName(string bountyId)
        {
            int hash = StableHash(bountyId);
            int day  = 0;
            if (EnvMan.instance != null && ZNet.instance != null)
                day = EnvMan.instance.GetDay(ZNet.instance.GetTimeSeconds());
            int index = ((hash + day * 7) & 0x7FFFFFFF) % BossNames.Length;
            return BossNames[index];
        }

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

        // ── Actions ──

        public bool AcceptBounty(string bountyId)
        {
            var player = Player.m_localPlayer;
            if (player == null) return false;
            if (!IsAvailable(bountyId)) return false;

            player.m_customData[StateKeyPrefix + bountyId]   = "active";
            player.m_customData[ProgressKeyPrefix + bountyId] = "0";

            // H-1: Track in active set
            _activeBountyIds.Add(bountyId);
            _activeBountyIdsLoaded = true;

            // C-1: Generate boss name exactly ONCE here — pass same value to
            // both customData and SpawnBountyCreature to avoid any day-boundary mismatch.
            string bossName = null;
            _bountyLookup.TryGetValue(bountyId, out var entry);
            if (entry != null && entry.SpawnLevel > 0)
            {
                bossName = GenerateBossName(bountyId);
                player.m_customData[BossNameKeyPrefix + bountyId] = bossName;
            }

            try { player.m_skillLevelupEffects.Create(player.transform.position, player.transform.rotation, player.transform); }
            catch { }

            if (entry != null && entry.SpawnLevel > 0)
                SpawnBountyCreature(player, entry, bossName);

            HaldorBounties.Log.LogInfo($"[BountyManager] Accepted bounty: {bountyId}");
            return true;
        }

        public bool AbandonBounty(string bountyId)
        {
            var player = Player.m_localPlayer;
            if (player == null) return false;

            var state = GetState(bountyId);
            if (state != BountyState.Active) return false;

            player.m_customData.Remove(StateKeyPrefix + bountyId);
            player.m_customData.Remove(ProgressKeyPrefix + bountyId);
            player.m_customData.Remove(BossNameKeyPrefix + bountyId);

            // H-1: Remove from active set
            _activeBountyIds.Remove(bountyId);

            RemoveBountyPin(bountyId);
            HaldorBounties.Log.LogInfo($"[BountyManager] Abandoned bounty: {bountyId}");
            return true;
        }

        /// <summary>
        /// C-1: bossName generated once in AcceptBounty and passed here,
        /// so the ZDO tag and customData always hold the identical string.
        /// </summary>
        private void SpawnBountyCreature(Player player, BountyEntry entry, string bossName)
        {
            try
            {
                var prefab = ZNetScene.instance.GetPrefab(entry.Target);
                if (prefab == null)
                {
                    HaldorBounties.Log.LogWarning($"[BountyManager] Prefab not found: {entry.Target}");
                    return;
                }

                int spawnCount    = entry.Amount;
                Vector3 playerPos = player.transform.position;
                // C-2: Default to playerPos so pin is never placed at world origin (0,0,0)
                Vector3 firstSpawnPos = playerPos;

                for (int i = 0; i < spawnCount; i++)
                {
                    Vector3   pos = FindSpawnPosition(playerPos, 700f);
                    if (i == 0) firstSpawnPos = pos;
                    Quaternion rot = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);

                    var go        = UnityEngine.Object.Instantiate(prefab, pos, rot);
                    var character = go.GetComponent<Character>();
                    if (character != null)
                    {
                        character.SetLevel(entry.SpawnLevel);
                        character.m_name = bossName;
                        character.m_boss = true;
                    }

                    var nview = go.GetComponent<ZNetView>();
                    if (nview?.GetZDO() != null)
                    {
                        var zdo = nview.GetZDO();
                        zdo.Set("HaldorBountyMiniboss", true);
                        zdo.Set("HaldorBountyBossName", bossName);
                        zdo.Set("HaldorBountyId", entry.Id);
                        zdo.Persistent = true;
                    }

                    if (i == 0 && character != null)
                        _bountyCreatures[entry.Id] = character;
                }

                // M-7: Minimap pin with fallback for older Valheim versions that lack EventArea
                if (Minimap.instance != null)
                {
                    RemoveBountyPin(entry.Id);
                    try
                    {
                        var pin = Minimap.instance.AddPin(firstSpawnPos, Minimap.PinType.EventArea, bossName, false, false);
                        pin.m_worldSize = 80f;
                        pin.m_animate   = true;
                        _bountyPins[entry.Id] = pin;
                    }
                    catch
                    {
                        try
                        {
                            // Fallback: Boss pin type is universally available
                            var pin = Minimap.instance.AddPin(firstSpawnPos, Minimap.PinType.Boss, bossName, true, false);
                            _bountyPins[entry.Id] = pin;
                        }
                        catch { }
                    }
                }

                MessageHud.instance?.ShowMessage(MessageHud.MessageType.Center,
                    $"{bossName} the {entry.Title} has appeared in the wild!");

                HaldorBounties.Log.LogInfo($"[BountyManager] Spawned {spawnCount}x {entry.Target} \"{bossName}\" level {entry.SpawnLevel} ~700m from player");
            }
            catch (Exception ex)
            {
                HaldorBounties.Log.LogWarning($"[BountyManager] Failed to spawn creature: {ex.Message}");
            }
        }

        /// <summary>Called from MinibossHud when a tagged creature's zone reloads.</summary>
        public void RegisterBountyCreature(string bountyId, Character creature)
        {
            if (string.IsNullOrEmpty(bountyId) || creature == null) return;
            var state = GetState(bountyId);
            if (state != BountyState.Active && state != BountyState.Ready) return;

            _bountyCreatures[bountyId] = creature;

            if (!_bountyPins.ContainsKey(bountyId) && Minimap.instance != null)
            {
                string bossName = creature.m_name;
                try
                {
                    var pin = Minimap.instance.AddPin(creature.transform.position, Minimap.PinType.EventArea, bossName, false, false);
                    pin.m_worldSize = 80f;
                    pin.m_animate   = true;
                    _bountyPins[bountyId] = pin;
                }
                catch
                {
                    try
                    {
                        var pin = Minimap.instance.AddPin(creature.transform.position, Minimap.PinType.Boss, bossName, true, false);
                        _bountyPins[bountyId] = pin;
                    }
                    catch { }
                }
                HaldorBounties.Log.LogInfo($"[BountyManager] Re-registered bounty creature: {bossName} (bounty={bountyId})");
            }
        }

        // M-3: Return double to avoid float precision loss for large world ages
        public double GetSecondsUntilReset()
        {
            if (EnvMan.instance == null || ZNet.instance == null) return 0.0;
            double now       = ZNet.instance.GetTimeSeconds();
            long   dayLen    = EnvMan.instance.m_dayLengthSec;
            int    currentDay = (int)(now / dayLen);
            double nextDayStart = (currentDay + 1) * (double)dayLen;
            return Math.Max(0.0, nextDayStart - now);
        }

        public void UpdateBountyPins()
        {
            if (Minimap.instance == null) return;

            var toRemove = new List<string>();
            foreach (var kvp in _bountyCreatures)
            {
                if (kvp.Value == null || kvp.Value.IsDead()) { toRemove.Add(kvp.Key); continue; }
                if (_bountyPins.TryGetValue(kvp.Key, out var pin) && pin != null)
                    pin.m_pos = kvp.Value.transform.position;
            }

            foreach (var id in toRemove)
            {
                _bountyCreatures.Remove(id);
                if (_bountyPins.TryGetValue(id, out var pin))
                {
                    if (pin != null) Minimap.instance.RemovePin(pin);
                    _bountyPins.Remove(id);
                }
            }
        }

        private void RemoveBountyPin(string bountyId)
        {
            if (_bountyPins.TryGetValue(bountyId, out var pin))
            {
                if (Minimap.instance != null && pin != null) Minimap.instance.RemovePin(pin);
                _bountyPins.Remove(bountyId);
            }
            _bountyCreatures.Remove(bountyId);
        }

        private static Vector3 FindSpawnPosition(Vector3 center, float distance)
        {
            const float waterLevel = 30f;
            for (int attempt = 0; attempt < 30; attempt++)
            {
                float   angle = UnityEngine.Random.Range(0f, 360f);
                float   dist  = distance + UnityEngine.Random.Range(-50f, 50f);
                Vector3 pos   = center + Quaternion.Euler(0f, angle, 0f) * Vector3.forward * dist;
                if (ZoneSystem.instance.GetGroundHeight(pos, out float height) && height > waterLevel)
                {
                    pos.y = height + 0.5f;
                    return pos;
                }
            }
            float   fbAngle   = UnityEngine.Random.Range(0f, 360f);
            Vector3 fallback  = center + Quaternion.Euler(0f, fbAngle, 0f) * Vector3.forward * distance;
            if (ZoneSystem.instance.GetGroundHeight(fallback, out float fbH))
                fallback.y = Mathf.Max(fbH, waterLevel) + 0.5f;
            else
                fallback.y = Mathf.Max(center.y, waterLevel + 1f);
            return fallback;
        }

        // ── Kill / Gather tracking ──

        // H-1: Filter to active bounties first — avoids iterating all 100+ entries on every event
        public void IncrementGather(string prefabName, int count)
        {
            var player = Player.m_localPlayer;
            if (player == null) return;

            EnsureActiveBountyIdsLoaded();
            if (_activeBountyIds.Count == 0) return;

            foreach (var bountyId in _activeBountyIds)
            {
                if (!_bountyLookup.TryGetValue(bountyId, out var bounty)) continue;
                if (bounty.Type != "Gather") continue;
                if (!string.Equals(bounty.Target, prefabName, StringComparison.OrdinalIgnoreCase)) continue;

                int current = GetProgress(bountyId);
                if (current >= bounty.Amount) continue; // Already at goal, don't over-count

                int progress = current + count;
                player.m_customData[ProgressKeyPrefix + bountyId] = progress.ToString();
                HaldorBounties.Log.LogInfo($"[BountyManager] Gather progress: {bountyId} ({progress}/{bounty.Amount})");

                if (progress >= bounty.Amount)
                {
                    HaldorBounties.Log.LogInfo($"[BountyManager] Gather bounty ready: {bountyId}");
                    MessageHud.instance?.ShowMessage(MessageHud.MessageType.Center, $"Bounty Complete: {bounty.Title}!");
                }
            }
        }

        public void IncrementKill(string prefabName, int level)
        {
            var player = Player.m_localPlayer;
            if (player == null) return;

            EnsureActiveBountyIdsLoaded();
            if (_activeBountyIds.Count == 0) return;

            HaldorBounties.Log.LogInfo($"[BountyManager] Kill detected: {prefabName} (level {level})");

            foreach (var bountyId in _activeBountyIds)
            {
                if (!_bountyLookup.TryGetValue(bountyId, out var bounty)) continue;
                if (bounty.Type != "Kill") continue;
                if (!string.Equals(bounty.Target, prefabName, StringComparison.OrdinalIgnoreCase)) continue;

                if (bounty.SpawnLevel > 0 && level != bounty.SpawnLevel)
                {
                    HaldorBounties.Log.LogInfo($"[BountyManager] Level mismatch for {bountyId}: need {bounty.SpawnLevel}, got {level}");
                    continue;
                }

                int current = GetProgress(bountyId);
                if (current >= bounty.Amount) continue; // Already at goal, don't over-count

                int progress = current + 1;
                player.m_customData[ProgressKeyPrefix + bountyId] = progress.ToString();
                HaldorBounties.Log.LogInfo($"[BountyManager] Bounty progress: {bountyId} ({progress}/{bounty.Amount})");

                if (progress >= bounty.Amount)
                {
                    HaldorBounties.Log.LogInfo($"[BountyManager] Bounty ready: {bountyId}");
                    MessageHud.instance?.ShowMessage(MessageHud.MessageType.Center, $"Bounty Complete: {bounty.Title}!");
                    RemoveBountyPin(bountyId);
                }
            }
        }

        public bool ClaimReward(string bountyId)
        {
            return ClaimRewardChoice(bountyId, RewardCategory.Coins);
        }

        public bool ClaimRewardChoice(string bountyId, RewardCategory category)
        {
            var player = Player.m_localPlayer;
            if (player == null) return false;

            if (!_bountyLookup.TryGetValue(bountyId, out var entry)) return false;

            var state = GetState(bountyId);

            if (entry.Type == "Gather")
            {
                if (state != BountyState.Active && state != BountyState.Ready) return false;
                var inv   = ((Humanoid)player).GetInventory();
                int count = CountItems(inv, entry.Target);
                if (count < entry.Amount) return false;
                RemoveItems(inv, entry.Target, entry.Amount);
            }
            else
            {
                if (state != BountyState.Ready) return false;
            }

            var rewards = RewardResolver.ResolveRewards(entry);
            var chosen  = rewards.Find(r => r.Category == category);
            if (chosen == null) return false;

            if (category == RewardCategory.Coins)
            {
                int currentBalance = 0;
                if (player.m_customData.TryGetValue(BankDataKey, out string balStr))
                    int.TryParse(balStr, out currentBalance);
                player.m_customData[BankDataKey] = (currentBalance + chosen.CoinAmount).ToString();
                RefreshBankUI();
            }
            else
            {
                if (!RewardResolver.DeliverReward(chosen, player))
                {
                    HaldorBounties.Log.LogWarning($"[BountyManager] Failed to deliver {category} reward for {bountyId}");
                    return false;
                }
            }

            player.m_customData[StateKeyPrefix + bountyId] = "claimed";
            player.m_customData.Remove(BossNameKeyPrefix + bountyId);
            RemoveBountyPin(bountyId);

            // H-1: Remove from active set
            _activeBountyIds.Remove(bountyId);

            try { player.m_skillLevelupEffects.Create(player.transform.position, player.transform.rotation, player.transform); }
            catch { }

            HaldorBounties.Log.LogInfo($"[BountyManager] Claimed bounty: {bountyId}, reward: {category} ({chosen.DisplayText})");
            return true;
        }

        // ── Gather helpers ──

        public int CountGatherProgress(string bountyId) => GetProgress(bountyId);

        public int CountItemsInInventory(string bountyId)
        {
            if (!_bountyLookup.TryGetValue(bountyId, out var entry) || entry.Type != "Gather") return 0;
            var player = Player.m_localPlayer;
            if (player == null) return 0;
            return CountItems(((Humanoid)player).GetInventory(), entry.Target);
        }

        private static int CountItems(Inventory inv, string prefabName)
        {
            int count = 0;
            foreach (var item in inv.GetAllItems())
            {
                string itemPrefab = item.m_dropPrefab?.name ?? item.m_shared.m_name;
                if (string.Equals(itemPrefab, prefabName, StringComparison.OrdinalIgnoreCase))
                    count += item.m_stack;
            }
            return count;
        }

        private static void RemoveItems(Inventory inv, string prefabName, int amount)
        {
            int remaining = amount;
            foreach (var item in inv.GetAllItems().ToList())
            {
                if (remaining <= 0) break;
                string itemPrefab = item.m_dropPrefab?.name ?? item.m_shared.m_name;
                if (!string.Equals(itemPrefab, prefabName, StringComparison.OrdinalIgnoreCase)) continue;
                int take = Mathf.Min(item.m_stack, remaining);
                remaining -= take;
                inv.RemoveItem(item, take);
            }
        }

        private void RefreshBankUI()
        {
            try
            {
                if (_getTraderUI == null || _reloadBankBalance == null) return;
                var traderUI = _getTraderUI.Invoke(null, null);
                if (traderUI != null) _reloadBankBalance.Invoke(traderUI, null);
            }
            catch (Exception ex)
            {
                HaldorBounties.Log.LogWarning($"[BountyManager] Failed to refresh bank UI: {ex.Message}");
            }
        }
    }
}
