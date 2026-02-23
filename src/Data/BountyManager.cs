using System;
using System.Collections.Generic;
using System.Globalization;
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
        private const string SpawnPosKeyPrefix = "HaldorBounty_SpawnPos_";
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

        // H-1: Active bounty ID set for fast filtering in IncrementKill
        private readonly HashSet<string> _activeBountyIds = new HashSet<string>();
        private bool _activeBountyIdsLoaded;

        // O(1) entry lookup by ID (built once from BountyConfig at Initialize time)
        private readonly Dictionary<string, BountyEntry> _bountyLookup;

        private static readonly string[] MaleNames =
        {
            "Ragnar", "Sigurd", "Gunnar", "Halvard", "Agnar",
            "Torstein", "Ottar", "Ivar", "Bjarke", "Torbjorn",
            "Thorvald", "Styrbjorn", "Ketil", "Hakon", "Erling",
            "Folkvar", "Geir", "Vidar", "Grimolf", "Thorgrim",
            "Rolf", "Orm", "Asmund", "Einar", "Hjalmar",
            "Dag", "Bodvar", "Ulf", "Bjorn", "Erik",
            "Leif", "Skallagrim", "Hrothgar", "Bragi", "Kolbein",
            "Trygve", "Arnbjorn", "Gudmund", "Fenrir", "Alfgeir",
            "Skuli", "Hrolf", "Guthorm", "Snorri", "Floki"
        };

        private static readonly string[] FemaleNames =
        {
            "Valdis", "Freya", "Brynhild", "Solveig", "Ingrid",
            "Ragnhild", "Hervor", "Svanhild", "Jorunn", "Alva",
            "Astrid", "Sigrid", "Thyra", "Helga", "Sif",
            "Gudrun", "Ylva", "Thora", "Hilde", "Embla",
            "Vigdis", "Eira", "Gunnhild", "Dagny", "Aslaug",
            "Alfhild", "Ranveig", "Sigrun", "Torhild", "Hallveig"
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

        /// <summary>O(1) lookup by bounty ID. Use instead of BountyConfig.Bounties.Find().</summary>
        public bool TryGetEntry(string bountyId, out BountyEntry entry)
        {
            return _bountyLookup.TryGetValue(bountyId, out entry);
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
                player.m_customData.Remove(SpawnPosKeyPrefix + bountyId);
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

            var pool     = BountyConfig.Bounties.Where(b => IsBossGateUnlocked(b)).ToList();
            var kill     = pool.Where(b => b.Type == "Kill" && !IsMiniboss(b) && !IsRaid(b)).ToList();
            var miniboss = pool.Where(b => IsMiniboss(b)).ToList();
            var raid     = pool.Where(b => IsRaid(b)).ToList();

            var daily = new List<BountyEntry>();
            daily.AddRange(PickRandom(kill,     2, rng));
            daily.AddRange(PickRandom(miniboss, 1, rng));
            daily.AddRange(PickRandom(raid,     1, rng));
            return daily;
        }

        private static List<BountyEntry> PickRandom(List<BountyEntry> pool, int count, System.Random rng)
        {
            if (pool.Count <= count) return new List<BountyEntry>(pool);
            return pool.OrderBy(_ => rng.Next()).Take(count).ToList();
        }

        private static bool IsMiniboss(BountyEntry entry) => entry.Tier == "Miniboss" || entry.Tier == "Special";
        private static bool IsRaid(BountyEntry entry) => entry.Tier == "Raid";

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

            int progress = 0;
            if (player.m_customData.TryGetValue(ProgressKeyPrefix + bountyId, out string val))
                int.TryParse(val, out progress);

            return progress;
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
            // Look up gender from config
            int gender = 0;
            if (Instance != null && Instance._bountyLookup.TryGetValue(bountyId, out var entry))
                gender = entry.Gender;
            return GenerateBossName(bountyId, gender);
        }

        private static string GenerateBossName(string bountyId, int gender = 0)
        {
            int hash = StableHash(bountyId);
            int day  = 0;
            if (EnvMan.instance != null && ZNet.instance != null)
                day = EnvMan.instance.GetDay(ZNet.instance.GetTimeSeconds());

            string[] pool = gender == 2 ? FemaleNames : gender == 1 ? MaleNames : MaleNames;
            if (gender == 0) // random gender — pick from combined pool
            {
                int combined = ((hash + day * 7) & 0x7FFFFFFF) % (MaleNames.Length + FemaleNames.Length);
                pool = combined < MaleNames.Length ? MaleNames : FemaleNames;
                int index = combined < MaleNames.Length ? combined : combined - MaleNames.Length;
                return pool[index];
            }

            int idx = ((hash + day * 7) & 0x7FFFFFFF) % pool.Length;
            return pool[idx];
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
                bossName = GenerateBossName(bountyId, entry.Gender);
                player.m_customData[BossNameKeyPrefix + bountyId] = bossName;
            }

            try { player.m_skillLevelupEffects.Create(player.transform.position, player.transform.rotation, player.transform); }
            catch { }

            if (entry != null && entry.SpawnLevel > 0)
            {
                try
                {
                    SpawnBountyCreature(player, entry, bossName);
                }
                catch (Exception ex)
                {
                    HaldorBounties.Log.LogError($"[BountyManager] SpawnBountyCreature failed for {bountyId}: {ex}");
                }
            }

            if (entry != null) AddBountyStatusEffect(player, entry);

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
            player.m_customData.Remove(SpawnPosKeyPrefix + bountyId);

            // H-1: Remove from active set
            _activeBountyIds.Remove(bountyId);

            RemoveBountyStatusEffect(player, bountyId);
            DespawnBountyCreature(bountyId);
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
            var prefab = ZNetScene.instance.GetPrefab(entry.Target);
            if (prefab == null)
            {
                HaldorBounties.Log.LogWarning($"[BountyManager] Prefab not found: {entry.Target}");
                return;
            }

            if (string.IsNullOrEmpty(bossName))
                bossName = GetBossName(entry.Id);

            bool isRaid       = IsRaid(entry);
            int spawnCount    = isRaid ? entry.Amount : 1;
            Vector3 playerPos = player.transform.position;
            Vector3 firstSpawnPos = playerPos;
            Character firstCharacter = null;
            int spawned = 0;

            RemoveBountyPin(entry.Id);
            CleanupStaleCreatures(entry.Id);

            for (int i = 0; i < spawnCount; i++)
            {
                try
                {
                    Vector3 pos;
                    if (isRaid && i > 0)
                        pos = FindSpawnPosition(firstSpawnPos, 10f);
                    else
                        pos = FindSpawnPosition(playerPos, 50f);
                    if (i == 0) firstSpawnPos = pos;
                    Quaternion rot = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);

                    var go        = UnityEngine.Object.Instantiate(prefab, pos, rot);
                    var character = go.GetComponent<Character>();
                    var nview = go.GetComponent<ZNetView>();

                    // SetLevel requires a valid ZDO — must check nview first
                    if (character != null && nview?.GetZDO() != null)
                    {
                        var zdo = nview.GetZDO();

                        // Write gender to ZDO so NpcSetup reads it reliably (no static field race)
                        zdo.Set(StringExtensionMethods.GetStableHashCode("HB_NpcGender"), entry.Gender);

                        character.SetLevel(entry.SpawnLevel);
                        character.SetHealth(character.GetMaxHealth());
                        if (isRaid)
                        {
                            character.m_name = "Valheim Raider";
                        }
                        else
                        {
                            character.m_name = bossName;
                            character.m_boss = true;
                            character.m_dontHideBossHud = true;
                        }

                        zdo.Set("HaldorBountyMiniboss", true);
                        zdo.Set("HaldorBountyBossName", bossName);
                        zdo.Set("HaldorBountyId", entry.Id);
                        zdo.Persistent = true;
                    }

                    if (i == 0 && character != null)
                        firstCharacter = character;

                    spawned++;
                }
                catch (Exception ex)
                {
                    HaldorBounties.Log.LogWarning($"[BountyManager] Failed to spawn creature {i + 1}/{spawnCount}: {ex.Message}");
                }
            }

            if (firstCharacter != null)
                _bountyCreatures[entry.Id] = firstCharacter;

            player.m_customData[SpawnPosKeyPrefix + entry.Id] = string.Format(CultureInfo.InvariantCulture, "{0:F1},{1:F1},{2:F1}", firstSpawnPos.x, firstSpawnPos.y, firstSpawnPos.z);

            if (Minimap.instance != null)
            {
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
                        var pin = Minimap.instance.AddPin(firstSpawnPos, Minimap.PinType.Boss, bossName, true, false);
                        _bountyPins[entry.Id] = pin;
                    }
                    catch { }
                }
            }

            MessageHud.instance?.ShowMessage(MessageHud.MessageType.Center,
                $"{bossName} the {entry.Title} has appeared in the wild!");

            HaldorBounties.Log.LogInfo($"[BountyManager] Spawned {spawned}/{spawnCount}x {entry.Target} \"{bossName}\" level {entry.SpawnLevel}");
        }

        /// <summary>Called from MinibossHud when a tagged creature's zone reloads.</summary>
        public void RegisterBountyCreature(string bountyId, Character creature)
        {
            if (string.IsNullOrEmpty(bountyId) || creature == null) return;
            var state = GetState(bountyId);
            if (state != BountyState.Active && state != BountyState.Ready) return;

            // Check if this is a raid bounty — raids show normal enemy names, no boss HUD
            bool isRaid = false;
            if (_bountyLookup.TryGetValue(bountyId, out var entry))
                isRaid = IsRaid(entry);

            string bossName = GetBossName(bountyId);
            if (isRaid)
            {
                creature.m_name = "Valheim Raider";
            }
            else
            {
                if (!string.IsNullOrEmpty(bossName))
                    creature.m_name = bossName;
                else
                    bossName = creature.m_name;
                creature.m_boss = true;
                creature.m_dontHideBossHud = true;
            }

            _bountyCreatures[bountyId] = creature;

            // Update stored spawn position so pin tracks creature after zone reload
            var player = Player.m_localPlayer;
            if (player != null)
            {
                Vector3 cPos = creature.transform.position;
                player.m_customData[SpawnPosKeyPrefix + bountyId] = string.Format(CultureInfo.InvariantCulture, "{0:F1},{1:F1},{2:F1}", cPos.x, cPos.y, cPos.z);
            }

            if (!_bountyPins.ContainsKey(bountyId) && Minimap.instance != null)
            {
                string pinName = isRaid ? "Valheim Raiders" : bossName;
                try
                {
                    var pin = Minimap.instance.AddPin(creature.transform.position, Minimap.PinType.EventArea, pinName, false, false);
                    pin.m_worldSize = 80f;
                    pin.m_animate   = true;
                    _bountyPins[bountyId] = pin;
                }
                catch
                {
                    try
                    {
                        var pin = Minimap.instance.AddPin(creature.transform.position, Minimap.PinType.Boss, pinName, true, false);
                        _bountyPins[bountyId] = pin;
                    }
                    catch { }
                }
                HaldorBounties.Log.LogInfo($"[BountyManager] Re-registered bounty creature: {pinName} (bounty={bountyId})");
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

        /// <summary>Clears all bounty state/progress/boss-name keys from player customData.</summary>
        public int ResetAll(Player player)
        {
            if (player == null) return 0;

            var keysToRemove = new List<string>();
            foreach (var key in player.m_customData.Keys)
            {
                if (key.StartsWith(StateKeyPrefix) ||
                    key.StartsWith(ProgressKeyPrefix) ||
                    key.StartsWith(BossNameKeyPrefix) ||
                    key.StartsWith(SpawnPosKeyPrefix) ||
                    key == LastDayKey)
                    keysToRemove.Add(key);
            }

            foreach (var key in keysToRemove)
                player.m_customData.Remove(key);

            // Despawn all tracked creatures
            foreach (var kvp in _bountyCreatures)
            {
                if (kvp.Value != null && !kvp.Value.IsDead())
                {
                    var nview = kvp.Value.GetComponent<ZNetView>();
                    if (nview != null && nview.IsValid())
                        nview.Destroy();
                    else
                        UnityEngine.Object.Destroy(kvp.Value.gameObject);
                }
            }
            _bountyCreatures.Clear();

            // Clear all minimap pins
            foreach (var kvp in _bountyPins)
            {
                if (Minimap.instance != null && kvp.Value != null)
                    Minimap.instance.RemovePin(kvp.Value);
            }
            _bountyPins.Clear();

            // Remove all bounty status effects (copy to avoid potential modification during iteration)
            var bountyIdsCopy = new List<string>(_activeBountyIds);
            foreach (var bountyId in bountyIdsCopy)
                RemoveBountyStatusEffect(player, bountyId);

            // Reset active set and day tracking
            _activeBountyIds.Clear();
            _activeBountyIdsLoaded = false;
            _lastDay = -1;

            HaldorBounties.Log.LogInfo($"[BountyManager] Reset all bounties. Removed {keysToRemove.Count} keys.");
            return keysToRemove.Count;
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

        /// <summary>Finds and destroys any lingering creatures with the given bountyId ZDO tag.
        /// Handles persistent NPCs from previous sessions that aren't tracked in _bountyCreatures.</summary>
        private static void CleanupStaleCreatures(string bountyId)
        {
            try
            {
                int cleaned = 0;
                var characters = Character.GetAllCharacters();
                for (int i = characters.Count - 1; i >= 0; i--)
                {
                    var c = characters[i];
                    if (c == null || c.IsPlayer()) continue;
                    var nview = c.GetComponent<ZNetView>();
                    var zdo = nview?.GetZDO();
                    if (zdo == null) continue;
                    if (zdo.GetString("HaldorBountyId", "") != bountyId) continue;

                    if (nview.IsValid() && nview.IsOwner())
                        nview.Destroy();
                    else if (nview.IsOwner())
                        UnityEngine.Object.Destroy(c.gameObject);
                    else
                        continue; // not the ZDO owner — can't destroy in multiplayer
                    cleaned++;
                }
                if (cleaned > 0)
                    HaldorBounties.Log.LogInfo($"[BountyManager] Cleaned up {cleaned} stale creatures for bounty: {bountyId}");
            }
            catch (Exception ex)
            {
                HaldorBounties.Log.LogWarning($"[BountyManager] Stale creature cleanup failed: {ex.Message}");
            }
        }

        /// <summary>Destroys spawned creatures for a bounty (used on abandon/reset).</summary>
        private void DespawnBountyCreature(string bountyId)
        {
            if (_bountyCreatures.TryGetValue(bountyId, out var creature))
            {
                if (creature != null && !creature.IsDead())
                {
                    var nview = creature.GetComponent<ZNetView>();
                    if (nview != null && nview.IsValid())
                        nview.Destroy();
                    else
                        UnityEngine.Object.Destroy(creature.gameObject);
                    HaldorBounties.Log.LogInfo($"[BountyManager] Despawned creature for bounty: {bountyId}");
                }
                _bountyCreatures.Remove(bountyId);
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
                float   dist  = distance + UnityEngine.Random.Range(-10f, 10f);
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

        // ── Kill tracking ──

        // H-1: Filter to active bounties first — avoids iterating all 100+ entries on every event
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
            if (state != BountyState.Ready) return false;

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
            player.m_customData.Remove(SpawnPosKeyPrefix + bountyId);
            RemoveBountyStatusEffect(player, bountyId);
            RemoveBountyPin(bountyId);

            // H-1: Remove from active set
            _activeBountyIds.Remove(bountyId);

            try { player.m_skillLevelupEffects.Create(player.transform.position, player.transform.rotation, player.transform); }
            catch { }

            HaldorBounties.Log.LogInfo($"[BountyManager] Claimed bounty: {bountyId}, reward: {category} ({chosen.DisplayText})");
            return true;
        }

        // ── Bounty status effects ──

        private static Sprite _bountyIcon;

        private static Sprite GetOrLoadBountyIcon()
        {
            if (_bountyIcon != null) return _bountyIcon;
            try
            {
                // Use the in-game Coins item icon as the bounty tracker icon
                var coinPrefab = ObjectDB.instance?.GetItemPrefab("Coins");
                var icons = coinPrefab?.GetComponent<ItemDrop>()?.m_itemData?.m_shared?.m_icons;
                if (icons != null && icons.Length > 0)
                    _bountyIcon = icons[0];
            }
            catch { }
            return _bountyIcon;
        }

        private static void AddBountyStatusEffect(Player player, BountyEntry entry)
        {
            if (player == null || entry == null) return;
            try
            {
                var seMan = ((Humanoid)player).GetSEMan();
                if (seMan == null) return;

                // Don't duplicate
                int hash = ("HaldorBounty_" + entry.Id).GetStableHashCode();
                if (seMan.GetStatusEffect(hash) != null) return;

                var se = ScriptableObject.CreateInstance<BountyStatusEffect>();
                se.Setup(entry.Id, entry.Title, entry.Amount, GetOrLoadBountyIcon());
                seMan.AddStatusEffect(se);
            }
            catch (Exception ex)
            {
                HaldorBounties.Log.LogWarning($"[BountyManager] Failed to add status effect for {entry.Id}: {ex.Message}");
            }
        }

        private static void RemoveBountyStatusEffect(Player player, string bountyId)
        {
            if (player == null) return;
            try
            {
                int hash = ("HaldorBounty_" + bountyId).GetStableHashCode();
                ((Humanoid)player).GetSEMan()?.RemoveStatusEffect(hash);
            }
            catch (Exception ex)
            {
                HaldorBounties.Log.LogWarning($"[BountyManager] Failed to remove status effect for {bountyId}: {ex.Message}");
            }
        }

        /// <summary>Re-applies status effects for all active bounties. Called on player spawn/load.</summary>
        public void RefreshStatusEffects(Player player)
        {
            if (player == null) return;
            EnsureActiveBountyIdsLoaded();
            foreach (var bountyId in _activeBountyIds)
            {
                if (_bountyLookup.TryGetValue(bountyId, out var entry))
                    AddBountyStatusEffect(player, entry);
            }
        }

        /// <summary>Restores minimap pins from stored spawn positions for active bounties after relog.</summary>
        public void RestoreBountyPins(Player player)
        {
            if (player == null || Minimap.instance == null) return;
            EnsureActiveBountyIdsLoaded();

            foreach (var bountyId in _activeBountyIds)
            {
                if (_bountyPins.ContainsKey(bountyId)) continue;
                if (!_bountyLookup.TryGetValue(bountyId, out var entry)) continue;
                if (entry.SpawnLevel <= 0) continue;

                if (!player.m_customData.TryGetValue(SpawnPosKeyPrefix + bountyId, out string posStr)) continue;

                string[] parts = posStr.Split(',');
                if (parts.Length != 3) continue;
                if (!float.TryParse(parts[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float x)) continue;
                if (!float.TryParse(parts[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float y)) continue;
                if (!float.TryParse(parts[2], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float z)) continue;

                Vector3 pos = new Vector3(x, y, z);
                string pinName = IsRaid(entry) ? entry.Title : GetBossName(bountyId);

                try
                {
                    var pin = Minimap.instance.AddPin(pos, Minimap.PinType.EventArea, pinName, false, false);
                    pin.m_worldSize = 80f;
                    pin.m_animate   = true;
                    _bountyPins[bountyId] = pin;
                }
                catch
                {
                    try
                    {
                        var pin = Minimap.instance.AddPin(pos, Minimap.PinType.Boss, pinName, true, false);
                        _bountyPins[bountyId] = pin;
                    }
                    catch { }
                }

                HaldorBounties.Log.LogInfo($"[BountyManager] Restored pin for bounty: {bountyId} at {pos}");
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
