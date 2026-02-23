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

    internal class ConfigWrapper
    {
        public int Version;
        public List<BountyEntry> Bounties;
    }

    public static class BountyConfig
    {
        private const int CurrentConfigVersion = 2;

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

                    // Try versioned wrapper first
                    var wrapper = JsonConvert.DeserializeObject<ConfigWrapper>(json);
                    if (wrapper != null && wrapper.Version >= CurrentConfigVersion && wrapper.Bounties != null && wrapper.Bounties.Count > 0)
                    {
                        Bounties = wrapper.Bounties;
                        ValidateEntries();
                        HaldorBounties.Log.LogInfo($"[BountyConfig] Loaded {Bounties.Count} bounties (v{wrapper.Version}).");
                        return;
                    }

                    // Outdated or legacy format — regenerate
                    int oldVersion = wrapper?.Version ?? 0;
                    HaldorBounties.Log.LogInfo($"[BountyConfig] Config version {oldVersion} < {CurrentConfigVersion}, regenerating.");
                }
                catch (Exception ex)
                {
                    HaldorBounties.Log.LogWarning($"[BountyConfig] Failed to load config: {ex.Message}. Using defaults.");
                }
            }

            Bounties = BuildDefaultBounties();
            ValidateEntries();
            SaveToFile();
            HaldorBounties.Log.LogInfo($"[BountyConfig] Generated {Bounties.Count} default bounties (v{CurrentConfigVersion}).");
        }

        private static void ValidateEntries()
        {
            foreach (var entry in Bounties)
            {
                if ((entry.Tier == "Miniboss" || entry.Tier == "Raid") && entry.SpawnLevel < 1)
                {
                    HaldorBounties.Log.LogWarning($"[BountyConfig] Entry '{entry.Id}' is {entry.Tier} but SpawnLevel={entry.SpawnLevel}, fixing to 1.");
                    entry.SpawnLevel = 1;
                }
            }
        }

        private static void SaveToFile()
        {
            try
            {
                var wrapper = new ConfigWrapper
                {
                    Version = CurrentConfigVersion,
                    Bounties = Bounties
                };
                string json = JsonConvert.SerializeObject(wrapper, Formatting.Indented);
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
                K("m_greyling_1", "Skogvættr's Brood", "The grey ones crawl from the earth like maggots from a wound. Haldor's caravans cannot pass.", "Greyling", 5, 25),
                K("m_greyling_2", "Scourge of the Glade", "Greylings swarm the meadow in numbers unseen. The roads must be cleared by steel.", "Greyling", 12, 50),
                K("m_neck_1", "Serpents of the Shallows", "Necks snap at wading travelers along every shore. Make the riverbanks safe again.", "Neck", 5, 30),
                K("m_neck_2", "Fang-Water", "Every stream and pond teems with Necks grown bold on easy prey.", "Neck", 12, 60),
                K("m_boar_1", "Tusks of the Wildwood", "Feral boars gore anyone who strays from the paths. Cull them before they breed further.", "Boar", 8, 40),
                K("m_boar_2", "Razorback Rampage", "The boars have gone berserk — tusks bloody, eyes wild. Something in the meadow drives them mad.", "Boar", 15, 75),
                K("m_deer_1", "Eikthyr's Offering", "Haldor craves fresh venison for his stores. Track the swiftest deer and bring them down.", "Deer", 5, 30),
                K("m_deer_2", "The Great Hunt", "A proper hunt worthy of song — enough deer to feast a warband.", "Deer", 12, 60),
                K("m_greyling_3", "Grey Tide Rising", "An enormous colony festers beneath the meadow roots. Burn them out.", "Greyling", 20, 80),
                K("m_neck_3", "Jaws of the Runoff", "The Neck swarms have grown beyond all reckoning. The waterways belong to them now — take them back.", "Neck", 20, 90));

            AddMinibossBounties(list, "", "HB_BountyNpc_T1",
                MB("mb_m_m1", "The Oath-Breaker", "A disgraced warrior haunts the crossroads, challenging travelers to holmgang they cannot refuse.", 200, 1),
                MB("mb_m_f1", "Grelka the Scavenger", "A scarred deserter raids supply caches by moonlight, leaving only bones and ash.", 220, 2),
                MB("mb_m_m2", "Bjolf Stone-Fist", "A hulking brute blocks the meadow crossing, demanding tribute in blood.", 210, 1),
                MB("mb_m_f2", "Runa Thorn-Heart", "An exile who fights like the wild creatures she lives among — without mercy.", 230, 2),
                MB("mb_m_m3", "The Nithing", "An outlaw branded with the mark of shame. He has nothing left to lose.", 200, 1),
                MB("mb_m_f3", "Ylva Grass-Viper", "She strikes from the tall grass and vanishes before the blood dries.", 240, 2),
                MB("mb_m_m4", "Vagn the Wanderer", "A drifting sellsword who cuts down anyone foolish enough to draw steel.", 210, 1),
                MB("mb_m_f4", "Sigrid Dawn-Stalker", "She hunts at first light when travelers are bleary-eyed and careless.", 250, 2),
                MB("mb_m_m5", "Keld Flint-Fang", "Armed with crude but wicked weapons shaped from the meadow's stones.", 200, 1),
                MB("mb_m_f5", "Inga Way-Finder", "A former merchant who knows every road — and the best places to ambush them.", 230, 2));

            AddRaidBounties(list, "", "HB_BountyNpc_T1",
                RD("rd_m_1", "Oath-Breakers' Warband", "A pack of disgraced warriors roam the meadow seeking plunder and redemption through blood.", 3, 350),
                RD("rd_m_2", "Outlaws of the Glade", "Desperate exiles have sworn a blood-oath and banded together. They answer to no jarl.", 4, 500),
                RD("rd_m_3", "Road-Wolves", "Bandits who prey on lone travelers, picking the roads clean like carrion birds.", 3, 400),
                RD("rd_m_4", "Camp-Burners", "Firebrands who torch camps and steal every scrap. Smoke marks their passing.", 4, 550),
                RD("rd_m_5", "Meadow Reavers", "A gang of marauders terrorizes the open fields, emboldened by their easy prey.", 3, 380),
                RD("rd_m_6", "The Landless", "Exiled warriors stripped of name and land, fighting with the fury of those who have nothing.", 4, 480),
                RD("rd_m_7", "Grey-Herders", "Outlaws who drive Greylings before them as scouts and distractions.", 3, 420),
                RD("rd_m_8", "Crossroad Kinsmen", "They control every crossroad in the meadows, demanding toll in steel.", 4, 520),
                RD("rd_m_9", "Dusk-Prowlers", "They strike at twilight when the shadows grow long and eyes grow dim.", 3, 400),
                RD("rd_m_10", "The Hunger-Sworn", "Starving raiders who have sworn to take what they need or die fighting.", 4, 500));

            // ── BLACK FOREST ──
            AddKillBounties(list, "defeated_eikthyr", "Easy",
                K("bf_greydwarf_1", "Eyes in the Undergrowth", "Greydwarf scouts watch every road through the forest with hollow, hateful eyes.", "Greydwarf", 8, 50),
                K("bf_greydwarf_2", "Root and Ruin", "A Greydwarf horde gathers beneath the ancient roots. If left unchecked, the forest will be lost.", "Greydwarf", 20, 120),
                K("bf_brute_1", "The Trunk-Breakers", "Greydwarf Brutes have blockaded the crossings, smashing carts and travelers alike.", "Greydwarf_Elite", 3, 90),
                K("bf_brute_2", "Timber Crushers", "Packs of Brutes lumber through the forest, toppling trees and crushing all who flee.", "Greydwarf_Elite", 6, 160),
                K("bf_shaman_1", "Menders of the Dark", "Shamans knit wounds with foul sap-magic, making the hordes near-unkillable.", "Greydwarf_Shaman", 3, 80),
                K("bf_shaman_2", "Curse-Weavers", "Their poison magic sustains entire warbands. Cut down the shamans and the rest will follow.", "Greydwarf_Shaman", 6, 140),
                K("bf_skeleton_1", "The Restless Dead", "Skeletons wander far beyond their burial mounds, rattling through the darkened woods.", "Skeleton", 8, 60),
                K("bf_skeleton_2", "Crypt-Purge", "The burial chambers overflow with the risen dead. Someone must seal them back in.", "Skeleton", 15, 110),
                K("bf_troll_1", "Bridge-Breaker", "A Troll has claimed a vital forest bridge. Travelers detour for half a day to avoid it.", "Troll", 1, 90),
                K("bf_ghost_1", "The Barrow-Haunts", "Spirits of the ancient dead drift through the old burial sites, hungering for warmth.", "Ghost", 5, 120));

            AddMinibossBounties(list, "defeated_eikthyr", "HB_BountyNpc_T2",
                MB("mb_bf_m1", "Grimjaw the Brigand", "A cunning forest brigand who preys on miners hauling ore from the deep woods.", 500, 1),
                MB("mb_bf_f1", "Hild Iron-Maiden", "A shieldmaiden turned bandit queen. She takes what she wants and burns the rest.", 550, 2),
                MB("mb_bf_m2", "Torolf Deepwood-Warden", "He demands blood-tribute from anyone who sets foot in his claimed territory.", 580, 1),
                MB("mb_bf_f2", "Bodil Root-Witch", "Clad in living root-armor, she fights with a ferocity that is not entirely human.", 600, 2),
                MB("mb_bf_m3", "Ketill Copper-King", "A miner who found it easier to kill for ore than to dig for it.", 520, 1),
                MB("mb_bf_f3", "Alva Shadow-Fern", "She vanishes into the undergrowth after every kill. No tracker has found her camp.", 560, 2),
                MB("mb_bf_m4", "Ulf Troll-Tamer", "He fights alongside trained Greydwarves and knows every hollow in the forest.", 620, 1),
                MB("mb_bf_f4", "Svala Dark-Bark", "Covered in bark and moss, she has become one with the black forest.", 640, 2),
                MB("mb_bf_m5", "Hreidar Stone-Breaker", "A massive warrior who shatters shields with a single blow of his bronze mace.", 580, 1),
                MB("mb_bf_f5", "Jorunn Night-Shade", "She tips her blades in poison brewed from forest fungi. Death comes slowly.", 650, 2));

            AddRaidBounties(list, "defeated_eikthyr", "HB_BountyNpc_T2",
                RD("rd_bf_1", "The Iron Ambush", "Brigands with iron weapons lie in wait along the forest road, hungry for blood and bronze.", 3, 750),
                RD("rd_bf_2", "Root-Clad War Party", "A war party in root armor has fortified a ruin in the heart of the forest.", 4, 1000),
                RD("rd_bf_3", "Troll-Hide Gang", "Outlaws clad in troll leather who strike without warning and vanish into the trees.", 3, 850),
                RD("rd_bf_4", "Crypt-Plunderers", "Grave-robbers who kill any witness. They've grown rich — and dangerous.", 4, 950),
                RD("rd_bf_5", "Forest Kinslayers", "Reavers who know every path through the black wood and use them to deadly effect.", 3, 780),
                RD("rd_bf_6", "The Bronze Brotherhood", "Deserters bound by oath and bronze, they answer challenges with axe and shield.", 4, 900),
                RD("rd_bf_7", "Timber-Wolves", "They ambush loggers and steal their haul. The forest roads are theirs.", 3, 800),
                RD("rd_bf_8", "Flame-Sworn Cult", "Outlaws who worship the Surtling flame and carry cores as holy relics.", 4, 1050),
                RD("rd_bf_9", "The Undergrowth", "They hide in the brush and strike from below. You'll never see the blade that kills you.", 3, 820),
                RD("rd_bf_10", "Darkwood Huskarls", "A warband of hardened killers who take no prisoners and leave no survivors.", 4, 1100));

            // ── SWAMP ──
            AddKillBounties(list, "defeated_gdking", "Medium",
                K("sw_draugr_1", "The Walking Dead", "Draugr shuffle near the borders, testing the living with dead hands and rusty steel.", "Draugr", 8, 120),
                K("sw_draugr_2", "Dead Tide", "The swamp crawls with Draugr rising in numbers not seen since the old wars.", "Draugr", 18, 250),
                K("sw_elite_1", "Grave-Knights", "Draugr Elites clad in ancient iron lead raiding parties out of the sunken crypts.", "Draugr_Elite", 3, 180),
                K("sw_elite_2", "Wight Purge", "Draugr Elites have risen in alarming numbers. The dead outnumber the living.", "Draugr_Elite", 6, 320),
                K("sw_blob_1", "Bile-Cleansing", "Poisonous Blobs ooze through the bog, leaving trails of rot that kill the very earth.", "Blob", 8, 120),
                K("sw_wraith_1", "Fog-Wraiths", "Wraiths materialize from the mire-fog, their wailing a death-knell for the unwary.", "Wraith", 3, 220),
                K("sw_leech_1", "Blood-Drinkers", "Giant Leeches infest every body of stagnant water. Wading is a death sentence.", "Leech", 10, 130),
                K("sw_surtling_1", "Geyser-Flames", "Surtlings blaze near the fire geysers, scorching anyone who seeks their cores.", "Surtling", 8, 130),
                K("sw_abom_1", "Root-Horror", "An Abomination has torn itself free of the muck. The ground shakes with its fury.", "Abomination", 1, 180),
                K("sw_abom_2", "The Great Uprooting", "Multiple Abominations tear through the swamp. The land itself has turned hostile.", "Abomination", 3, 420));

            AddMinibossBounties(list, "defeated_gdking", "HB_BountyNpc_T3",
                MB("mb_sw_m1", "Halvard Bog-Stalker", "Born in the mire, he fights with ruthless cunning. The Draugr fear his iron.", 1000, 1),
                MB("mb_sw_f1", "Groa the Drowned Queen", "Even the walking dead give this one a wide berth. She rules the sunken places.", 1100, 2),
                MB("mb_sw_m2", "Vidar Crypt-Breaker", "His silver blade drips with undead ichor — he has slain many, and will slay you next.", 1200, 1),
                MB("mb_sw_f2", "Thora Mire-Witch", "She commands the swamp-fog itself as a weapon, blinding her prey before the kill.", 1150, 2),
                MB("mb_sw_m3", "Eindride Rot-Walker", "He wades through poison without flinching, armored in leather cured with grave-wax.", 1050, 1),
                MB("mb_sw_f3", "Hervor Iron-Widow", "She lost every shield-brother to the swamp. Now she seeks vengeance on all who live.", 1250, 2),
                MB("mb_sw_m4", "Gorm the Undertaker", "He buries his victims in the mud where they rise again to serve him.", 1100, 1),
                MB("mb_sw_f4", "Solveig Mire-Singer", "Her voice carries through the fog, luring travelers into the drowning-deeps.", 1300, 2),
                MB("mb_sw_m5", "Bjarke Leech-Lord", "The bloodsuckers seem to obey his whispered commands. He is more swamp than man.", 1150, 1),
                MB("mb_sw_f5", "Vigdis Guck-Weaver", "She crafts traps from swamp-filth that would make a Draugr recoil.", 1200, 2));

            AddRaidBounties(list, "defeated_gdking", "HB_BountyNpc_T3",
                RD("rd_sw_1", "Silver Marauders", "Silver-clad raiders stalk the mist-shrouded bogs, hunting for worthy prey.", 3, 1500),
                RD("rd_sw_2", "The Drowned Company", "Battle-hardened and half-mad from the swamp. They fight as if already dead.", 4, 2000),
                RD("rd_sw_3", "Mire-Reavers", "They use the fog like a war-cloak, striking from the murk and melting away.", 3, 1800),
                RD("rd_sw_4", "Crypt-Raiders", "Tomb raiders armed with grave-plunder. They kill any witness to their sacrilege.", 4, 1900),
                RD("rd_sw_5", "The Rotting Hand", "Warriors who have embraced the swamp's decay and wear it like battle-paint.", 3, 1600),
                RD("rd_sw_6", "Bog-Runners", "Fast and lethal, they vault across the muck while their prey sinks and drowns.", 4, 2100),
                RD("rd_sw_7", "The Iron Tide", "An iron-clad warband pushing through the swamp, crushing all in their path.", 3, 1700),
                RD("rd_sw_8", "The Sunken Ones", "They emerge from black water where no man should survive. No one knows how.", 4, 2200),
                RD("rd_sw_9", "Wraith-Pact Warriors", "Warriors who have sworn oaths to the swamp spirits. They do not die easily.", 3, 1650),
                RD("rd_sw_10", "Mire-Wolves", "A wolfpack of fighters who hunt in formation through the sucking bog.", 4, 2300));

            // ── MOUNTAIN ──
            AddKillBounties(list, "defeated_bonemass", "Medium",
                K("mt_wolf_1", "Fenrir's Get", "A wolf pack descends on climbers with fangs bared and frost on their breath.", "Wolf", 5, 150),
                K("mt_wolf_2", "Blood on the Snow", "The mountain is overrun — wolves howl from every ridge and ravine.", "Wolf", 15, 350),
                K("mt_drake_1", "Ice-Wyrm Hunters", "Frost Drakes rain shards of ice on anyone below. The skies are not safe.", "Hatchling", 5, 190),
                K("mt_drake_2", "Storm of Wings", "The skies are thick with Drakes. They must be thinned before passage is possible.", "Hatchling", 10, 340),
                K("mt_golem_1", "Stone-Breaker", "A Stone Golem blocks the mountain pass, grinding boulders in its fists.", "StoneGolem", 1, 200),
                K("mt_golem_2", "Walking Avalanche", "Multiple Golems descend the slopes, crushing everything in their grinding path.", "StoneGolem", 3, 420),
                K("mt_fenring_1", "Moon-Hunters", "Fenrings emerge under the cold stars, their howls echoing off the peaks.", "Fenring", 3, 340),
                K("mt_ulv_1", "Ghost-Wolves", "Ulvs haunt the mountain caves, spectral and savage in equal measure.", "Ulv", 5, 280),
                K("mt_bat_1", "The Shrieking Dark", "Swarms of bats pour from mountain caves, blotting out the light.", "Bat", 10, 150),
                K("mt_wolf_3", "Howling Peaks", "Wolf packs grow dangerously large on the heights. The mountain belongs to them.", "Wolf", 10, 260));

            AddMinibossBounties(list, "defeated_bonemass", "HB_BountyNpc_T4",
                MB("mb_mt_m1", "Skallagrim Frost-Bitten", "His silver weapons have claimed lives beyond counting. The cold has made him merciless.", 2000, 1),
                MB("mb_mt_f1", "Astrid Summit-Huntress", "She commands the wolves of the peaks and hunts alongside them.", 2200, 2),
                MB("mb_mt_m2", "Thorvald Blizzard-Born", "He walks through storms that would bury other men, blade in hand.", 2100, 1),
                MB("mb_mt_f2", "Brynhild the Avalanche", "She has crushed more challengers than any golem on the mountain.", 2400, 2),
                MB("mb_mt_m3", "Egil Frost-Warden", "Self-proclaimed guardian of the high passes. He lets no one through alive.", 2050, 1),
                MB("mb_mt_f3", "Eira Ice-Whisper", "She moves silently through the snow. Her victims never hear the killing blow.", 2300, 2),
                MB("mb_mt_m4", "Hallbjorn Peak-Breaker", "He breaks through shield-walls with the force of a mountain gale.", 2500, 1),
                MB("mb_mt_f4", "Dagny Crystal-Edge", "Her crystal-tipped weapons cut through armor as if it were cloth.", 2600, 2),
                MB("mb_mt_m5", "Arne the Cold One", "He feels nothing — not frost, not pain, not mercy.", 2150, 1),
                MB("mb_mt_f5", "Sif Storm-Caller", "She fights hardest when the blizzard howls, as if the storm answers her will.", 2350, 2));

            AddRaidBounties(list, "defeated_bonemass", "HB_BountyNpc_T4",
                RD("rd_mt_1", "Frost-Reavers", "Black metal glints in the cold air. These warriors have slain dragons for their steel.", 3, 3000),
                RD("rd_mt_2", "Summit Warhost", "They hold the highest peaks and challenge any fool who dares ascend.", 4, 4200),
                RD("rd_mt_3", "Blizzard Fangs", "They attack only during storms, invisible until the killing blow.", 3, 3500),
                RD("rd_mt_4", "The Summit-Guard", "Elite warriors sworn to hold the peaks against all comers. They have never broken.", 4, 4000),
                RD("rd_mt_5", "Silver Brotherhood", "Bound by silver and blood-oaths, they fight as one terrible weapon.", 3, 3200),
                RD("rd_mt_6", "Ulfhednar", "Berserkers who don wolf-skins and fight with the frenzy of Fenrir's children.", 4, 4500),
                RD("rd_mt_7", "The Frozen Ones", "Warriors who endure any cold, any wound. They do not stop.", 3, 3300),
                RD("rd_mt_8", "Crystal-Guard", "Armed with obsidian-edged weapons that sing in the frozen air.", 4, 4100),
                RD("rd_mt_9", "Hrimthursar's Kin", "Frost-touched warriors as unyielding as the mountain itself.", 3, 3400),
                RD("rd_mt_10", "The High King's Hird", "The finest warriors of the peaks, forged in ice and sworn to death.", 4, 4800));

            // ── PLAINS ──
            AddKillBounties(list, "defeated_dragon", "Hard",
                K("pl_fuling_1", "Goblin Outriders", "Fuling scouts probe the plains' edges, their crude spears gleaming with malice.", "Goblin", 8, 250),
                K("pl_fuling_2", "The Great Horde", "A Fuling invasion force marches across the golden fields. The earth trembles.", "Goblin", 25, 600),
                K("pl_brute_1", "Skull-Crushers", "Fuling Berserkers crush armor like parchment and bone like kindling.", "GoblinBrute", 2, 380),
                K("pl_brute_2", "The Wrecking Tide", "Multiple Berserkers on a rampage — nothing stands before them.", "GoblinBrute", 4, 650),
                K("pl_shaman_1", "Flame-Snuffers", "Fuling Shamans channel dark fire-magic that chars flesh and warps iron.", "GoblinShaman", 3, 300),
                K("pl_squito_1", "Needle-Storm", "Deathsquitos punch through armor with barbed stingers. Death by a thousand needles.", "Deathsquito", 10, 270),
                K("pl_squito_2", "Plague of Needles", "An overwhelming swarm descends. The buzzing alone drives warriors to madness.", "Deathsquito", 20, 500),
                K("pl_lox_1", "Beast-Feller", "Lox trample everything beneath their thundering hooves. Even the Fulings flee.", "Lox", 2, 320),
                K("pl_lox_2", "Thunder Hooves", "A herd of enraged Lox stampedes across the plains, crushing all in their wake.", "Lox", 5, 620),
                K("pl_tar_1", "Tar-Blight", "Tar Growths spread black corruption from bubbling pits. The land itself sickens.", "BlobTar", 5, 320));

            AddMinibossBounties(list, "defeated_dragon", "HB_BountyNpc_T5",
                MB("mb_pl_m1", "Ragnar Golden-Bane", "Carapace armor gleams on his shoulders. Even Fuling war-chiefs flee at his approach.", 3200, 1),
                MB("mb_pl_f1", "Freydis the Conqueress", "Her blade has carved a path through villages and war-camps alike.", 3600, 2),
                MB("mb_pl_m2", "Styrbjorn Dusk-Reaper", "He strikes at twilight when the golden light blinds. None have seen his face and lived.", 3800, 1),
                MB("mb_pl_f2", "Gudrun Shield-Maiden", "She fights as if Odin himself watches — because she believes he does.", 4000, 2),
                MB("mb_pl_m3", "Hakon Sand-Viper", "He strikes from the tall barley like a serpent, silent and lethal.", 3400, 1),
                MB("mb_pl_f3", "Alfhild Barley-Queen", "She controls the farmlands through fear and fire. The harvest is hers.", 3700, 2),
                MB("mb_pl_m4", "Bjorn Lox-Breaker", "He charges on foot but hits like a rampaging beast.", 4200, 1),
                MB("mb_pl_f4", "Ranveig Needle-Dancer", "She weaves through combat like a Deathsquito — impossible to pin down.", 3900, 2),
                MB("mb_pl_m5", "Jarl Skuli the Bloody", "A self-proclaimed jarl of terrible reputation. His saga is written in blood.", 4500, 1),
                MB("mb_pl_f5", "Sigrun Golden-Fury", "Fury incarnate on the golden fields. She fights until the plains run red.", 4100, 2));

            AddRaidBounties(list, "defeated_dragon", "HB_BountyNpc_T5",
                RD("rd_pl_1", "Carapace Raiders", "Clad in chitin-plate with exotic weapons and deadly coordination.", 4, 6000),
                RD("rd_pl_2", "The Conqueror's Hird", "The deadliest warband in the realm. Songs are sung of those brave enough to face them.", 5, 8500),
                RD("rd_pl_3", "Dusk-Riders", "They sweep the plains at sunset, silhouettes against the dying light.", 4, 7000),
                RD("rd_pl_4", "The Golden Host", "An army ablaze on the golden fields. Odin watches this battle.", 5, 9000),
                RD("rd_pl_5", "Fuling-Slayers", "They hunt Fulings for sport and wear their trophies as war-paint.", 4, 6500),
                RD("rd_pl_6", "Vanguard of the Fallen", "First into the shield-wall, last to retreat. They know no fear.", 5, 8000),
                RD("rd_pl_7", "Plains-Wolves", "Fast and lethal across open ground, they run down prey like a wolf pack.", 4, 7500),
                RD("rd_pl_8", "The Iron Harvest", "They reap what others have sown, and their scythes are sharpened steel.", 5, 9500),
                RD("rd_pl_9", "Blood-Drenched Veterans", "Survivors of a hundred battles. They fight without hesitation or mercy.", 4, 7200),
                RD("rd_pl_10", "Sand-Storm Berserkers", "They charge like wind across the plains, howling war-cries to Tyr.", 5, 10000));

            // ── MISTLANDS ──
            AddKillBounties(list, "defeated_goblinking", "Hard",
                K("ml_seeker_1", "Hive-Purge", "Seekers strike from the eternal twilight, their mandibles slick with venom.", "Seeker", 8, 420),
                K("ml_seeker_2", "The Infestation", "Seeker hives spread through every ruin and hollow. The mist breeds them endlessly.", "Seeker", 15, 680),
                K("ml_brute_1", "Chitin-Cracker", "Seeker Brutes guard the deepest hives, armored in plates that turn steel.", "SeekerBrute", 2, 500),
                K("ml_brute_2", "Chitin Siege", "Seeker Brutes blockade all passage through the mist. Exploration has ceased.", "SeekerBrute", 4, 880),
                K("ml_tick_1", "Bloodtick Purge", "Ticks drop from the canopy without warning, draining the life from armored warriors.", "Tick", 10, 360),
                K("ml_tick_2", "Canopy Scourge", "A plague of Ticks overwhelms even the strongest. The treetops writhe with them.", "Tick", 20, 620),
                K("ml_gjall_1", "Sky-Terror", "A Gjall rains explosive bile from above, cratering the earth below.", "Gjall", 1, 460),
                K("ml_gjall_2", "Bombardment", "Multiple Gjall turn the landscape into a cratered wasteland of fire and chitin.", "Gjall", 3, 920),
                K("ml_dvergr_1", "Rogue Dvergr", "Some Dvergr have turned hostile, wielding arcane weapons of terrible power.", "Dverger", 5, 540),
                K("ml_seeker_3", "The Deep Hive", "The largest hive pulses beneath the mist. It must be destroyed before it hatches.", "Seeker", 20, 800));

            AddMinibossBounties(list, "defeated_goblinking", "HB_BountyNpc_T6",
                MB("mb_ml_m1", "Arnbjorn Mist-Phantom", "Even the Seekers recoil from his approach. He is more wraith than warrior.", 5000, 1),
                MB("mb_ml_f1", "Aslaug the Ash-Born", "The most dangerous bounty ever nailed to Haldor's board. Approach with caution.", 5500, 2),
                MB("mb_ml_m2", "Fenrir Eitr-Walker", "His weapons hum with raw eitr. One cut festers into something worse than death.", 5800, 1),
                MB("mb_ml_f2", "Gunnhild Void-Walker", "She wears armor no living smith has ever forged. It moves with her like skin.", 6200, 2),
                MB("mb_ml_m3", "Snorri Mist-Reaver", "He strikes from the fog and vanishes, leaving only corpses and silence.", 5200, 1),
                MB("mb_ml_f3", "Torhild Seeker-Queen", "She moves like a Seeker but thinks like a war-chief. The worst of both worlds.", 6000, 2),
                MB("mb_ml_m4", "Floki the Dvergr-Touched", "Trained by the Dvergr in arts no human should know. His weapons defy nature.", 5600, 1),
                MB("mb_ml_f4", "Hallveig Chitin-Empress", "Carapace armor fused with eitr-magic. She is the mist made flesh.", 6500, 2),
                MB("mb_ml_m5", "Hrolf Shadow-Blade", "His blade drinks the light itself. You will not see the edge that ends you.", 5400, 1),
                MB("mb_ml_f5", "Sigrid Mist-Walker", "She has walked deeper into the mist than any living soul — and returned changed.", 6800, 2));

            AddRaidBounties(list, "defeated_goblinking", "HB_BountyNpc_T6",
                RD("rd_ml_1", "Flametal Brotherhood", "Their weapons blaze with flametal fire. Their coordinated assaults are devastating.", 4, 8500),
                RD("rd_ml_2", "Ashlands Vanguard", "An army unto themselves, forged in realms beyond the mist.", 5, 12000),
                RD("rd_ml_3", "The Void-Pact", "They fight in eerie silence, communicating without words. Unnerving and lethal.", 4, 9500),
                RD("rd_ml_4", "The Last Legion", "Flametal from helm to heel. The final obstacle between you and legend.", 5, 14000),
                RD("rd_ml_5", "Mist-Walkers", "They emerge from the fog as one, a wall of steel and eitr-glow.", 4, 9000),
                RD("rd_ml_6", "Eitr-Guard", "Their weapons crackle with eitr energy that burns through any defense.", 5, 13000),
                RD("rd_ml_7", "Chitin-Sworn", "Warriors bound in Seeker chitin who have surrendered their humanity.", 4, 10000),
                RD("rd_ml_8", "The Forgotten", "No saga remembers their names. Few who face them survive to sing one.", 5, 15000),
                RD("rd_ml_9", "Deep Patrol", "They patrol the deepest, darkest reaches of the mist where no light penetrates.", 4, 9800),
                RD("rd_ml_10", "Ragnarok's Herald", "The final challenge for any Viking who would earn a seat in Valhalla.", 5, 16000));

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
