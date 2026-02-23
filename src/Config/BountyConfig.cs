using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace HaldorBounties
{
    public class BountyEntry
    {
        public string Id = "";
        public string Title = "";
        public string Description = "";
        public string Type = "Kill";
        public string Target = "";
        public int Amount = 1;
        public int Reward = 50;
        public string RequiredBoss = "";
        public int SpawnLevel = 0;
        public string Tier = "Easy";
        public int Gender = 0;
    }

    public static class BountyConfig
    {
        public static List<BountyEntry> Bounties { get; private set; } = new List<BountyEntry>();
        private static string _configPath;

        public static void Initialize(string configPath)
        {
            _configPath = configPath;

            if (File.Exists(_configPath))
            {
                try
                {
                    string json = File.ReadAllText(_configPath);
                    Bounties = JsonConvert.DeserializeObject<List<BountyEntry>>(json);
                    if (Bounties != null && Bounties.Count > 0)
                    {
                        HaldorBounties.Log.LogInfo($"[BountyConfig] Loaded {Bounties.Count} bounties from config.");
                        return;
                    }
                }
                catch (Exception ex)
                {
                    HaldorBounties.Log.LogWarning($"[BountyConfig] Failed to load config: {ex.Message}. Using defaults.");
                }
            }

            Bounties = BuildDefaultBounties();
            SaveToFile();
            HaldorBounties.Log.LogInfo($"[BountyConfig] Generated {Bounties.Count} default bounties and saved to config.");
        }

        private static void SaveToFile()
        {
            try
            {
                string json = JsonConvert.SerializeObject(Bounties, Formatting.Indented);
                string dir = Path.GetDirectoryName(_configPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                File.WriteAllText(_configPath, json);
            }
            catch (Exception ex)
            {
                HaldorBounties.Log.LogWarning($"[BountyConfig] Failed to save config: {ex.Message}");
            }
        }

        // ═══════════════════════════════════════════════════════
        //  DEFAULT BOUNTIES — 180 total (30 per biome x 6 biomes)
        //  10 Kill + 10 Miniboss (5M/5F) + 10 Raid per biome
        // ═══════════════════════════════════════════════════════

        private static List<BountyEntry> BuildDefaultBounties()
        {
            var list = new List<BountyEntry>();

            // ── MEADOWS ──
            AddKillBounties(list, "", "Easy",
                K("m_greyling_1", "Pest Control", "Greylings have infested the roads. Thin their numbers.", "Greyling", 5, 25),
                K("m_greyling_2", "Grey Tide", "A swarm of Greylings overwhelms the meadow paths.", "Greyling", 12, 50),
                K("m_neck_1", "Shoreline Watch", "Necks lurk along the waterline, snapping at travelers.", "Neck", 5, 30),
                K("m_neck_2", "River Fangs", "Every pond teems with aggressive Necks.", "Neck", 12, 60),
                K("m_boar_1", "Wild Tusks", "Feral boars trample camps and gore travelers.", "Boar", 8, 40),
                K("m_boar_2", "Razorback Fury", "Enraged boars charge anything that moves.", "Boar", 15, 75),
                K("m_deer_1", "The Hunt", "Haldor wants fresh venison. Track down some deer.", "Deer", 5, 30),
                K("m_deer_2", "Trophy Season", "A proper hunt — bring back a dozen deer.", "Deer", 12, 60),
                K("m_greyling_3", "Swarm Breaker", "An enormous Greyling colony must be destroyed.", "Greyling", 20, 80),
                K("m_neck_3", "Jaws of the Shallows", "The Neck population has exploded beyond control.", "Neck", 20, 90));

            AddMinibossBounties(list, "", "HB_BountyNpc_T1",
                MB("mb_m_m1", "Meadows Outlaw", "A rogue Viking ambushes travelers at crossroads.", 200, 1),
                MB("mb_m_f1", "The Scavenger", "A scarred deserter raids supply caches by night.", 220, 2),
                MB("mb_m_m2", "Crossroad Thief", "A hulking brute blocks the meadow crossroads.", 210, 1),
                MB("mb_m_f2", "Thornheart", "An exile fights like the creatures she lives among.", 230, 2),
                MB("mb_m_m3", "Meadow Reaver", "A wild-eyed raider prowls the meadow edges.", 200, 1),
                MB("mb_m_f3", "Grassland Viper", "She strikes from the tall grass without warning.", 240, 2),
                MB("mb_m_m4", "The Vagabond", "A wandering fighter challenges anyone he meets.", 210, 1),
                MB("mb_m_f4", "Dawn Stalker", "She hunts at first light when travelers are careless.", 250, 2),
                MB("mb_m_m5", "Flint Fang", "Armed with crude but deadly weapons.", 200, 1),
                MB("mb_m_f5", "The Wayfarer", "A former trader turned bandit. Knows every road.", 230, 2));

            AddRaidBounties(list, "", "HB_BountyNpc_T1",
                RD("rd_m_1", "Rogue Warband", "A warband of rogues ambushes travelers.", 3, 350),
                RD("rd_m_2", "Outlaw Uprising", "Desperate outlaws have banded together.", 4, 500),
                RD("rd_m_3", "Highway Wolves", "Bandits prey on anyone traveling the roads.", 3, 400),
                RD("rd_m_4", "Camp Raiders", "Raiders burn camps and steal supplies.", 4, 550),
                RD("rd_m_5", "Meadow Marauders", "A gang of marauders terrorizes the open fields.", 3, 380),
                RD("rd_m_6", "The Outcasts", "Exiled warriors with nothing left to lose.", 4, 480),
                RD("rd_m_7", "Greyling Herders", "Outlaws who use Greylings as scouts.", 3, 420),
                RD("rd_m_8", "Crossroad Gang", "They control every crossroad in the meadows.", 4, 520),
                RD("rd_m_9", "Dusk Prowlers", "They strike at dusk when visibility drops.", 3, 400),
                RD("rd_m_10", "The Desperate", "Starving raiders fighting for survival.", 4, 500));

            // ── BLACK FOREST ──
            AddKillBounties(list, "defeated_eikthyr", "Easy",
                K("bf_greydwarf_1", "Forest Sentries", "Greydwarf scouts watch the roads.", "Greydwarf", 8, 50),
                K("bf_greydwarf_2", "Root Rot", "A massive Greydwarf horde gathers deep in the forest.", "Greydwarf", 20, 120),
                K("bf_brute_1", "Heavy Hitters", "Greydwarf Brutes block key crossings.", "Greydwarf_Elite", 3, 90),
                K("bf_brute_2", "Timber Crushers", "Packs of Brutes roam the forest.", "Greydwarf_Elite", 6, 160),
                K("bf_shaman_1", "Hedge Witches", "Shamans heal wounded faster than you can fell them.", "Greydwarf_Shaman", 3, 80),
                K("bf_shaman_2", "Dark Menders", "Shamans sustain entire warbands with foul magic.", "Greydwarf_Shaman", 6, 140),
                K("bf_skeleton_1", "Bone Rattlers", "Skeletons wander beyond the burial grounds.", "Skeleton", 8, 60),
                K("bf_skeleton_2", "Crypt Purge", "Burial chambers overflow with restless dead.", "Skeleton", 15, 110),
                K("bf_troll_1", "Troll Trouble", "A Troll has claimed a vital forest path.", "Troll", 1, 90),
                K("bf_ghost_1", "Restless Spirits", "Spirits haunt the old burial sites.", "Ghost", 5, 120));

            AddMinibossBounties(list, "defeated_eikthyr", "HB_BountyNpc_T2",
                MB("mb_bf_m1", "Forest Brigand", "A cunning brigand preys on miners hauling ore.", 500, 1),
                MB("mb_bf_f1", "The Iron Maiden", "A shieldmaiden turned bandit queen.", 550, 2),
                MB("mb_bf_m2", "Deepwood Warden", "He demands tribute from anyone entering his territory.", 580, 1),
                MB("mb_bf_f2", "Root Witch", "She wears root armor and fights with unnatural ferocity.", 600, 2),
                MB("mb_bf_m3", "Copper King", "A miner who kills for ore instead of digging.", 520, 1),
                MB("mb_bf_f3", "Shadow Fern", "She vanishes into the forest after every kill.", 560, 2),
                MB("mb_bf_m4", "The Troll Tamer", "He fights alongside trained Greydwarves.", 620, 1),
                MB("mb_bf_f4", "Darkbark", "Covered in bark and moss, she is one with the forest.", 640, 2),
                MB("mb_bf_m5", "Stone Breaker", "A massive warrior who shatters shields.", 580, 1),
                MB("mb_bf_f5", "Nightshade", "She coats her blades in poison.", 650, 2));

            AddRaidBounties(list, "defeated_eikthyr", "HB_BountyNpc_T2",
                RD("rd_bf_1", "Iron Ambush", "Brigands with iron weapons ambush the forest road.", 3, 750),
                RD("rd_bf_2", "War Party", "A war party in root armor has fortified the forest.", 4, 1000),
                RD("rd_bf_3", "Troll-Hide Gang", "Outlaws in troll leather strike without warning.", 3, 850),
                RD("rd_bf_4", "Crypt Raiders", "They plunder burial chambers and kill witnesses.", 4, 950),
                RD("rd_bf_5", "Forest Reavers", "Reavers who know every path through the trees.", 3, 780),
                RD("rd_bf_6", "Bronze Brotherhood", "A brotherhood of deserters armed with bronze.", 4, 900),
                RD("rd_bf_7", "Timber Wolves", "They ambush loggers and steal their haul.", 3, 800),
                RD("rd_bf_8", "Surtling Cult", "Outlaws who worship fire and carry surtling cores.", 4, 1050),
                RD("rd_bf_9", "The Undergrowth", "They hide in the brush and strike from below.", 3, 820),
                RD("rd_bf_10", "Darkwood Company", "A mercenary company that takes no prisoners.", 4, 1100));

            // ── SWAMP ──
            AddKillBounties(list, "defeated_gdking", "Medium",
                K("sw_draugr_1", "Swamp Watch", "Draugr shuffle near the swamp borders.", "Draugr", 8, 120),
                K("sw_draugr_2", "Dead Tide", "The swamp crawls with Draugr in growing numbers.", "Draugr", 18, 250),
                K("sw_elite_1", "Grave Knights", "Draugr Elites lead raiding parties.", "Draugr_Elite", 3, 180),
                K("sw_elite_2", "Wight Purge", "Draugr Elites have risen in alarming numbers.", "Draugr_Elite", 6, 320),
                K("sw_blob_1", "Toxic Cleanup", "Poisonous Blobs leave trails of toxic slime.", "Blob", 8, 120),
                K("sw_wraith_1", "Night Terrors", "Wraiths materialize from the swamp fog.", "Wraith", 3, 220),
                K("sw_leech_1", "Bloodsuckers", "Giant Leeches infest every body of water.", "Leech", 10, 130),
                K("sw_surtling_1", "Fire Harvest", "Surtlings blaze near the fire geysers.", "Surtling", 8, 130),
                K("sw_abom_1", "Root Horror", "An Abomination has risen from the muck.", "Abomination", 1, 180),
                K("sw_abom_2", "Swamp Cleansing", "Multiple Abominations tear through the landscape.", "Abomination", 3, 420));

            AddMinibossBounties(list, "defeated_gdking", "HB_BountyNpc_T3",
                MB("mb_sw_m1", "Bog Stalker", "Adapted to the mire, he fights with ruthless efficiency.", 1000, 1),
                MB("mb_sw_f1", "The Drowned Queen", "Even the Draugr give her a wide berth.", 1100, 2),
                MB("mb_sw_m2", "Crypt Breaker", "His silver blade drips with undead ichor.", 1200, 1),
                MB("mb_sw_f2", "Mire Witch", "She commands the fog itself as a weapon.", 1150, 2),
                MB("mb_sw_m3", "Rot Walker", "He wades through poison without flinching.", 1050, 1),
                MB("mb_sw_f3", "Iron Widow", "She lost her shield-brothers and seeks vengeance.", 1250, 2),
                MB("mb_sw_m4", "The Undertaker", "He buries his victims in the mud.", 1100, 1),
                MB("mb_sw_f4", "Swamp Siren", "Her voice lures travelers into the deep mire.", 1300, 2),
                MB("mb_sw_m5", "Leech Lord", "The bloodsuckers seem to obey his command.", 1150, 1),
                MB("mb_sw_f5", "Guck Weaver", "She crafts deadly traps from swamp materials.", 1200, 2));

            AddRaidBounties(list, "defeated_gdking", "HB_BountyNpc_T3",
                RD("rd_sw_1", "Silver Marauders", "Silver-clad marauders hide in the mist.", 3, 1500),
                RD("rd_sw_2", "Drowned Company", "Battle-hardened and relentless.", 4, 2000),
                RD("rd_sw_3", "Mire Reavers", "They use the fog to devastating effect.", 3, 1800),
                RD("rd_sw_4", "Crypt Plunderers", "Tomb raiders who kill on sight.", 4, 1900),
                RD("rd_sw_5", "The Rotting Hand", "Warriors who have embraced the swamp's decay.", 3, 1600),
                RD("rd_sw_6", "Bog Runners", "Fast and lethal, they strike from the muck.", 4, 2100),
                RD("rd_sw_7", "Iron Tide", "An iron-clad force pushing through the swamp.", 3, 1700),
                RD("rd_sw_8", "The Sunken", "They emerge from underwater to attack.", 4, 2200),
                RD("rd_sw_9", "Wraith Pact", "Warriors who have made pacts with spirits.", 3, 1650),
                RD("rd_sw_10", "Swamp Wolves", "A wolfpack of fighters hunting in formation.", 4, 2300));

            // ── MOUNTAIN ──
            AddKillBounties(list, "defeated_bonemass", "Medium",
                K("mt_wolf_1", "Frostfang Pack", "A wolf pack attacks anyone climbing.", "Wolf", 5, 150),
                K("mt_wolf_2", "Alpha Cull", "The mountain is overrun with wolves.", "Wolf", 15, 350),
                K("mt_drake_1", "Ice Breakers", "Frost Drakes rain ice on anyone below.", "Hatchling", 5, 190),
                K("mt_drake_2", "Frozen Skies", "The skies are thick with Drakes.", "Hatchling", 10, 340),
                K("mt_golem_1", "Stone Cracker", "A Stone Golem blocks the pass.", "StoneGolem", 1, 200),
                K("mt_golem_2", "Avalanche", "Multiple Golems crush everything in their path.", "StoneGolem", 3, 420),
                K("mt_fenring_1", "Moonlit Hunt", "Fenrings emerge under cover of darkness.", "Fenring", 3, 340),
                K("mt_ulv_1", "Ghost Wolves", "Ulvs lurk in mountain caves.", "Ulv", 5, 280),
                K("mt_bat_1", "Cave Cleaners", "Swarms of bats pour from mountain caves.", "Bat", 10, 150),
                K("mt_wolf_3", "Howling Peaks", "Wolf packs grow dangerously large.", "Wolf", 10, 260));

            AddMinibossBounties(list, "defeated_bonemass", "HB_BountyNpc_T4",
                MB("mb_mt_m1", "Frostbitten", "His silver weapons have claimed many lives.", 2000, 1),
                MB("mb_mt_f1", "Summit Huntress", "She commands the wolves of the peaks.", 2200, 2),
                MB("mb_mt_m2", "Blizzard Born", "He fights through storms without flinching.", 2100, 1),
                MB("mb_mt_f2", "Avalanche", "She has crushed more challengers than any golem.", 2400, 2),
                MB("mb_mt_m3", "Frost Warden", "Guardian of the high passes.", 2050, 1),
                MB("mb_mt_f3", "Ice Whisper", "She moves silently through the snow.", 2300, 2),
                MB("mb_mt_m4", "Peak Breaker", "He breaks through any defense with brute force.", 2500, 1),
                MB("mb_mt_f4", "Crystal Edge", "Her crystal-tipped weapons cut through armor.", 2600, 2),
                MB("mb_mt_m5", "The Cold One", "He feels nothing — not cold, not pain.", 2150, 1),
                MB("mb_mt_f5", "Storm Caller", "She fights hardest during blizzards.", 2350, 2));

            AddRaidBounties(list, "defeated_bonemass", "HB_BountyNpc_T4",
                RD("rd_mt_1", "Frost Reavers", "Black metal glints in the cold air.", 3, 3000),
                RD("rd_mt_2", "Peak Warhost", "They challenge anyone who dares ascend.", 4, 4200),
                RD("rd_mt_3", "Blizzard Fangs", "They strike during mountain storms.", 3, 3500),
                RD("rd_mt_4", "Summit Guard", "Elite warriors who hold the highest peaks.", 4, 4000),
                RD("rd_mt_5", "Silver Brotherhood", "United by silver and blood oaths.", 3, 3200),
                RD("rd_mt_6", "Wolf Pack", "Berserkers who fight like wolves.", 4, 4500),
                RD("rd_mt_7", "The Frozen", "Warriors who endure any cold.", 3, 3300),
                RD("rd_mt_8", "Crystal Guard", "Armed with crystal weapons.", 4, 4100),
                RD("rd_mt_9", "Ice Reavers", "Fast and lethal in the snow.", 3, 3400),
                RD("rd_mt_10", "High King's Guard", "The finest warriors of the peaks.", 4, 4800));

            // ── PLAINS ──
            AddKillBounties(list, "defeated_dragon", "Hard",
                K("pl_fuling_1", "Goblin Outriders", "Fuling scouts probe the edges of the plains.", "Goblin", 8, 250),
                K("pl_fuling_2", "Horde Breaker", "A massive Fuling invasion force marches.", "Goblin", 25, 600),
                K("pl_brute_1", "Berserker's Challenge", "Fuling Berserkers crush armor like parchment.", "GoblinBrute", 2, 380),
                K("pl_brute_2", "Wrecking Crew", "Multiple Berserkers on a rampage.", "GoblinBrute", 4, 650),
                K("pl_shaman_1", "Flame Snuffers", "Fuling Shamans channel dark fire magic.", "GoblinShaman", 3, 300),
                K("pl_squito_1", "Needle Storm", "Deathsquitos punch through armor.", "Deathsquito", 10, 270),
                K("pl_squito_2", "Plague of Needles", "An overwhelming swarm descends.", "Deathsquito", 20, 500),
                K("pl_lox_1", "Beast Slayer", "Lox trample everything beneath their hooves.", "Lox", 2, 320),
                K("pl_lox_2", "Thunder Hooves", "A herd of enraged Lox stampedes.", "Lox", 5, 620),
                K("pl_tar_1", "Tar Blight", "Tar Growths spread corruption from black pits.", "BlobTar", 5, 320));

            AddMinibossBounties(list, "defeated_dragon", "HB_BountyNpc_T5",
                MB("mb_pl_m1", "Golden Executioner", "Carapace armor makes even Fulings flee.", 3200, 1),
                MB("mb_pl_f1", "The Conqueress", "Her blade has carved through villages.", 3600, 2),
                MB("mb_pl_m2", "Dusk Reaper", "He strikes at twilight. None have seen his face.", 3800, 1),
                MB("mb_pl_f2", "Plains Valkyrie", "She fights as if chosen by Odin himself.", 4000, 2),
                MB("mb_pl_m3", "Sand Viper", "He strikes from the tall grass.", 3400, 1),
                MB("mb_pl_f3", "Barley Queen", "She controls the farmlands through fear.", 3700, 2),
                MB("mb_pl_m4", "Lox Rider", "He charges on foot but fights like a beast.", 4200, 1),
                MB("mb_pl_f4", "Needle Dancer", "She dodges like a Deathsquito.", 3900, 2),
                MB("mb_pl_m5", "The Warlord", "A conqueror of terrible reputation.", 4500, 1),
                MB("mb_pl_f5", "Golden Fury", "Fury incarnate on the golden fields.", 4100, 2));

            AddRaidBounties(list, "defeated_dragon", "HB_BountyNpc_T5",
                RD("rd_pl_1", "Carapace Raiders", "Exotic weapons and coordinated tactics.", 4, 6000),
                RD("rd_pl_2", "Conqueror's Guard", "The deadliest warriors in the realm.", 5, 8500),
                RD("rd_pl_3", "Dusk Riders", "They sweep the plains at sunset.", 4, 7000),
                RD("rd_pl_4", "Golden Host", "An army on the golden fields.", 5, 9000),
                RD("rd_pl_5", "Fuling Slayers", "They hunt Fulings for sport.", 4, 6500),
                RD("rd_pl_6", "The Vanguard", "First into battle, last to retreat.", 5, 8000),
                RD("rd_pl_7", "Plains Wolves", "Fast and lethal across open ground.", 4, 7500),
                RD("rd_pl_8", "Iron Harvest", "They reap what others have sown.", 5, 9500),
                RD("rd_pl_9", "The Bloodied", "Veterans of a hundred battles.", 4, 7200),
                RD("rd_pl_10", "Sand Storm", "They move like wind across the plains.", 5, 10000));

            // ── MISTLANDS ──
            AddKillBounties(list, "defeated_goblinking", "Hard",
                K("ml_seeker_1", "Hive Clearance", "Seekers strike from the darkness.", "Seeker", 8, 420),
                K("ml_seeker_2", "Infestation", "Seeker hives spread through every ruin.", "Seeker", 15, 680),
                K("ml_brute_1", "Shell Cracker", "Seeker Brutes guard the deepest hives.", "SeekerBrute", 2, 500),
                K("ml_brute_2", "Chitin Siege", "Seeker Brutes block all exploration.", "SeekerBrute", 4, 880),
                K("ml_tick_1", "Bloodtick Purge", "Ticks drop from the canopy without warning.", "Tick", 10, 360),
                K("ml_tick_2", "Canopy Scourge", "A plague of Ticks overwhelms warriors.", "Tick", 20, 620),
                K("ml_gjall_1", "Sky Terror", "A Gjall rains explosive projectiles.", "Gjall", 1, 460),
                K("ml_gjall_2", "Bombardment", "Multiple Gjall crater the landscape.", "Gjall", 3, 920),
                K("ml_dvergr_1", "Rogue Dvergr", "Some Dvergr have turned hostile.", "Dverger", 5, 540),
                K("ml_seeker_3", "Deep Hive", "The largest hive must be destroyed.", "Seeker", 20, 800));

            AddMinibossBounties(list, "defeated_goblinking", "HB_BountyNpc_T6",
                MB("mb_ml_m1", "Phantom of the Mist", "Even the Seekers fear him.", 5000, 1),
                MB("mb_ml_f1", "The Ashborne", "The most dangerous bounty ever posted.", 5500, 2),
                MB("mb_ml_m2", "Eitr Walker", "His weapons hum with raw eitr.", 5800, 1),
                MB("mb_ml_f2", "Voidwalker", "She wears armor no smith has ever forged.", 6200, 2),
                MB("mb_ml_m3", "Mist Reaver", "He strikes from the fog and vanishes.", 5200, 1),
                MB("mb_ml_f3", "Spider Queen", "She moves like a Seeker but thinks like a Viking.", 6000, 2),
                MB("mb_ml_m4", "The Dverger", "A Dvergr-trained warrior with their weapons.", 5600, 1),
                MB("mb_ml_f4", "Chitin Empress", "Carapace armor fused with eitr magic.", 6500, 2),
                MB("mb_ml_m5", "Shadow Blade", "His blade cuts through darkness itself.", 5400, 1),
                MB("mb_ml_f5", "Mistwalker", "She has walked deeper into the mist than anyone.", 6800, 2));

            AddRaidBounties(list, "defeated_goblinking", "HB_BountyNpc_T6",
                RD("rd_ml_1", "Flametal Brotherhood", "Devastating coordinated assaults.", 4, 8500),
                RD("rd_ml_2", "Ashlands Vanguard", "An army unto themselves.", 5, 12000),
                RD("rd_ml_3", "Void Pact", "They fight in eerie silence.", 4, 9500),
                RD("rd_ml_4", "The Last Legion", "Flametal from head to toe.", 5, 14000),
                RD("rd_ml_5", "Mist Walkers", "They emerge from the fog as one.", 4, 9000),
                RD("rd_ml_6", "Eitr Guard", "Their weapons crackle with eitr energy.", 5, 13000),
                RD("rd_ml_7", "Chitin Cult", "Warriors bound in Seeker chitin.", 4, 10000),
                RD("rd_ml_8", "The Forgotten", "No one remembers their names. Few survive.", 5, 15000),
                RD("rd_ml_9", "Deep Patrol", "They patrol the deepest reaches of the mist.", 4, 9800),
                RD("rd_ml_10", "Ashborn Elite", "The final challenge for any Viking.", 5, 16000));

            return list;
        }

        // ── Builder helpers ──

        private struct KillDef { public string Id, Title, Desc, Target; public int Amount, Reward; }
        private struct MinibossDef { public string Id, Title, Desc; public int Reward, Gender; }
        private struct RaidDef { public string Id, Title, Desc; public int Amount, Reward; }

        private static KillDef K(string id, string title, string desc, string target, int amount, int reward)
            => new KillDef { Id = id, Title = title, Desc = desc, Target = target, Amount = amount, Reward = reward };
        private static MinibossDef MB(string id, string title, string desc, int reward, int gender)
            => new MinibossDef { Id = id, Title = title, Desc = desc, Reward = reward, Gender = gender };
        private static RaidDef RD(string id, string title, string desc, int amount, int reward)
            => new RaidDef { Id = id, Title = title, Desc = desc, Amount = amount, Reward = reward };

        private static void AddKillBounties(List<BountyEntry> list, string boss, string tier, params KillDef[] defs)
        {
            foreach (var d in defs)
                list.Add(new BountyEntry { Id = d.Id, Title = d.Title, Description = d.Desc, Type = "Kill", Target = d.Target, Amount = d.Amount, Reward = d.Reward, RequiredBoss = boss, Tier = tier });
        }

        private static void AddMinibossBounties(List<BountyEntry> list, string boss, string target, params MinibossDef[] defs)
        {
            foreach (var d in defs)
                list.Add(new BountyEntry { Id = d.Id, Title = d.Title, Description = d.Desc, Type = "Kill", Target = target, Amount = 1, Reward = d.Reward, RequiredBoss = boss, SpawnLevel = 1, Tier = "Miniboss", Gender = d.Gender });
        }

        private static void AddRaidBounties(List<BountyEntry> list, string boss, string target, params RaidDef[] defs)
        {
            foreach (var d in defs)
                list.Add(new BountyEntry { Id = d.Id, Title = d.Title, Description = d.Desc, Type = "Kill", Target = target, Amount = d.Amount, Reward = d.Reward, RequiredBoss = boss, SpawnLevel = 1, Tier = "Raid" });
        }
    }
}
