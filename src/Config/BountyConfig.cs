using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using Newtonsoft.Json;

namespace HaldorBounties
{
    public class BountyEntry
    {
        [JsonProperty("id")] public string Id = "";
        [JsonProperty("title")] public string Title = "";
        [JsonProperty("description")] public string Description = "";
        [JsonProperty("type")] public string Type = "Kill"; // Kill, Gather
        [JsonProperty("target")] public string Target = "";  // prefab name
        [JsonProperty("amount")] public int Amount = 1;
        [JsonProperty("reward")] public int Reward = 50;
        [JsonProperty("required_boss")] public string RequiredBoss = "";
        [JsonProperty("spawn_level")] public int SpawnLevel = 0;
        [JsonProperty("tier")] public string Tier = "Easy"; // Easy, Medium, Hard, Miniboss
    }

    public static class BountyConfig
    {
        private static string ConfigFile => Path.Combine(Paths.ConfigPath, "HaldorBounties.bounties.json");

        public static List<BountyEntry> Bounties { get; private set; } = new List<BountyEntry>();

        public static void Initialize()
        {
            if (!File.Exists(ConfigFile))
            {
                File.WriteAllText(ConfigFile, DefaultJson());
                HaldorBounties.Log.LogInfo("[BountyConfig] Created default bounties config.");
            }

            try
            {
                Bounties = JsonConvert.DeserializeObject<List<BountyEntry>>(File.ReadAllText(ConfigFile))
                           ?? new List<BountyEntry>();
            }
            catch (Exception ex)
            {
                HaldorBounties.Log.LogError($"[BountyConfig] Error loading config: {ex}");
                Bounties = new List<BountyEntry>();
            }

            // Validate
            int removed = 0;
            foreach (var b in Bounties.ToList())
            {
                if (b == null || string.IsNullOrWhiteSpace(b.Id) || string.IsNullOrWhiteSpace(b.Target)
                    || (b.Type != "Kill" && b.Type != "Gather"))
                {
                    Bounties.Remove(b);
                    removed++;
                    continue;
                }
                if (b.Amount <= 0) b.Amount = 1;
                if (b.Reward < 0) b.Reward = 0;
                if (string.IsNullOrEmpty(b.Tier)) b.Tier = "Easy";
            }

            if (removed > 0)
                HaldorBounties.Log.LogWarning($"[BountyConfig] Removed {removed} invalid bounty entries.");

            HaldorBounties.Log.LogInfo($"[BountyConfig] Loaded {Bounties.Count} bounties.");
        }

        private static string DefaultJson() => JsonConvert.SerializeObject(BuildDefaultBounties(), Formatting.Indented);

        private static List<BountyEntry> BuildDefaultBounties()
        {
            var list = new List<BountyEntry>();

            // ═══════════════════════════════════════════
            //  EASY TIER — Meadows & Black Forest
            // ═══════════════════════════════════════════

            // Meadows Kill
            list.Add(B("greyling_patrol", "Greyling Patrol", "Greylings have been scurrying through the meadows near Haldor's camp, raiding supply caches under cover of darkness. Track them down and put an end to their thieving ways before they grow bolder.", "Kill", "Greyling", 5, 20, "", "Easy"));
            list.Add(B("greyling_cleanup", "Greyling Cleanup", "The Greyling population has surged this season, and travelers report being harassed along the main paths. Haldor wants them cleared out so his trade routes remain safe for caravans.", "Kill", "Greyling", 10, 35, "", "Easy"));
            list.Add(B("greyling_infestation", "Greyling Infestation", "An enormous swarm of Greylings has taken root in the meadows, multiplying far beyond their normal numbers. Their nests are spreading and something must be done before they overwhelm the entire region.", "Kill", "Greyling", 20, 65, "", "Easy"));
            list.Add(B("neck_patrol", "Neck Patrol", "Several Necks have been spotted lurking along the shoreline, snapping at anyone who ventures too close to the water. Clear them out before some poor traveler loses a limb.", "Kill", "Neck", 5, 25, "", "Easy"));
            list.Add(B("neck_hunt", "Neck Hunter", "The coastline has become dangerous with Necks infesting every pond and stream. Fishermen can no longer work the shores safely, and Haldor needs the waterways cleared for trade.", "Kill", "Neck", 10, 50, "", "Easy"));
            list.Add(B("neck_massacre", "Neck Massacre", "The Neck population has exploded beyond all reason, choking the waterways and making coastal travel impossible. A massive cull is the only solution to reclaim the shorelines.", "Kill", "Neck", 20, 90, "", "Easy"));
            list.Add(B("boar_cull", "Boar Cull", "Wild boars have been trampling through camps and destroying food stores. Their numbers have grown too large for the meadows to sustain, and Haldor is offering coin to thin the herd.", "Kill", "Boar", 8, 40, "", "Easy"));
            list.Add(B("boar_rampage", "Boar Rampage", "Enraged boars are charging at travelers on sight, goring anyone unfortunate enough to cross their path. Something has them riled up, and they need to be put down before more Vikings are injured.", "Kill", "Boar", 15, 70, "", "Easy"));
            list.Add(B("deer_hunt", "Deer Hunt", "Haldor has a craving for fresh venison and is willing to pay handsomely for it. Head into the meadows, track down some deer, and bring proof of your kills.", "Kill", "Deer", 5, 30, "", "Easy"));
            list.Add(B("deer_trophy", "Trophy Deer", "Haldor is hosting a feast and needs a substantial supply of game. He wants proof of a proper hunt, so head out and bring back trophies from a dozen deer to earn your reward.", "Kill", "Deer", 12, 55, "", "Easy"));

            // Meadows Gather
            list.Add(B("deer_hides_sm", "Hide Delivery", "Haldor's leather stock is running low and he needs deer hides to repair his tent and pack goods for transport. Head out and hunt some deer, then bring back their hides.", "Gather", "DeerHide", 10, 35, "", "Easy"));
            list.Add(B("deer_hides", "Hide Procurement", "A large shipment of leather goods has been ordered and Haldor is short on raw materials. He needs a substantial supply of deer hides to fulfill the contract before the next trade caravan arrives.", "Gather", "DeerHide", 20, 65, "", "Easy"));
            list.Add(B("resin_supply", "Resin Supply", "Resin is essential for torches and waterproofing, and Haldor's reserves are nearly depleted. Gather resin from the Greydwarves of the forest and bring it back to replenish his stocks.", "Gather", "Resin", 30, 45, "", "Easy"));
            list.Add(B("resin_bulk", "Resin Bulk Order", "Haldor has a massive order for resin from a distant settlement that uses it for shipbuilding. He needs fifty units gathered and delivered as quickly as possible to meet the deadline.", "Gather", "Resin", 50, 80, "", "Easy"));
            list.Add(B("wood_delivery", "Wood Delivery", "Haldor needs quality timber to build new shipping crates for his expanding trade operation. Chop down some trees and deliver a load of wood to his camp.", "Gather", "Wood", 50, 30, "", "Easy"));
            list.Add(B("wood_bulk", "Lumber Order", "Construction is booming across the settlements and lumber is in high demand. Haldor has promised a large wood shipment, and he's counting on you to fill the order.", "Gather", "Wood", 100, 55, "", "Easy"));
            list.Add(B("stone_delivery", "Stone Delivery", "Haldor is reinforcing the foundations of his trading post against the increasingly violent raids. He needs a good supply of stone to shore up the walls and defenses.", "Gather", "Stone", 40, 30, "", "Easy"));
            list.Add(B("flint_gather", "Flint Collection", "Good flint is becoming scarce near the settlements, but it remains essential for crafting tools and arrowheads. Scour the riverbeds and shorelines to gather what Haldor needs.", "Gather", "Flint", 20, 35, "", "Easy"));
            list.Add(B("leather_scraps", "Leather Scraps", "Haldor's workshop burns through leather scraps faster than he can stockpile them. Hunt boars and other creatures for their hides and bring back the scraps he needs for his crafting.", "Gather", "LeatherScraps", 20, 40, "", "Easy"));
            list.Add(B("mushroom_forage", "Mushroom Forage", "Haldor fancies himself quite the cook and wants fresh mushrooms for his famous stew. Forage through the meadows and forest floor to gather a healthy supply for his kitchen.", "Gather", "Mushroom", 20, 30, "", "Easy"));
            list.Add(B("raspberry_pick", "Berry Picking", "Fresh raspberries are a luxury in these harsh lands, and Haldor has developed quite a taste for them. Gather a generous supply from the meadow bushes and bring them back before they spoil.", "Gather", "Raspberries", 25, 25, "", "Easy"));
            list.Add(B("honey_supply", "Honey Supply", "Sweet honey is worth its weight in gold for brewing mead and treating wounds. Haldor needs you to raid some beehives and bring back this precious golden nectar.", "Gather", "Honey", 5, 50, "", "Easy"));

            // Black Forest Kill
            list.Add(B("greydwarf_scout", "Greydwarf Scouts", "Greydwarf scouts have been spotted creeping along the forest edge, watching the roads and marking travelers. Haldor suspects they're planning a coordinated raid on nearby camps.", "Kill", "Greydwarf", 8, 45, "defeated_eikthyr", "Easy"));
            list.Add(B("greydwarf_menace", "Greydwarf Menace", "The Black Forest writhes with Greydwarves in numbers not seen for generations. Their constant ambushes have made the forest paths treacherous, and Haldor needs their numbers thinned.", "Kill", "Greydwarf", 15, 75, "defeated_eikthyr", "Easy"));
            list.Add(B("greydwarf_horde", "Greydwarf Horde", "A massive Greydwarf horde has gathered in the depths of the Black Forest, and their raiding parties grow larger by the day. Haldor is paying top coin for anyone brave enough to break their ranks.", "Kill", "Greydwarf", 25, 110, "defeated_eikthyr", "Easy"));
            list.Add(B("greydwarf_brute_hunt", "Brute Force", "Greydwarf Brutes have been spotted guarding key passages through the deep forest. These hulking creatures crush anyone who tries to pass, and Haldor needs the routes cleared.", "Kill", "Greydwarf_Elite", 3, 80, "defeated_eikthyr", "Easy"));
            list.Add(B("greydwarf_brute_purge", "Brute Purge", "Multiple Greydwarf Brutes now roam the Black Forest in packs, smashing camps and destroying bridges. Their reign of destruction must end before the forest becomes completely impassable.", "Kill", "Greydwarf_Elite", 6, 140, "defeated_eikthyr", "Easy"));
            list.Add(B("greydwarf_shaman_hunt", "Shaman Slayer", "Greydwarf Shamans have been healing wounded warriors faster than they can be cut down. Haldor knows that silencing the Shamans will break the morale of the lesser Greydwarves.", "Kill", "Greydwarf_Shaman", 3, 70, "defeated_eikthyr", "Easy"));
            list.Add(B("greydwarf_shaman_purge", "Shaman Purge", "A disturbing number of Greydwarf Shamans have emerged from the forest depths, keeping entire warbands alive with their dark magic. Their healing must be stopped at the source.", "Kill", "Greydwarf_Shaman", 6, 120, "defeated_eikthyr", "Easy"));
            list.Add(B("skeleton_patrol", "Skeleton Patrol", "Skeletons have begun wandering beyond the burial grounds, their bones rattling through the forest at night. Clear them before they stray closer to the settlements.", "Kill", "Skeleton", 8, 55, "defeated_eikthyr", "Easy"));
            list.Add(B("skeleton_purge", "Skeleton Purge", "The ancient burial chambers are overflowing with restless dead, and skeletons now pour from every crypt and ruin. Descend into the darkness and send them back to their graves.", "Kill", "Skeleton", 15, 95, "defeated_eikthyr", "Easy"));
            list.Add(B("troll_slayer", "Troll Slayer", "A massive Troll has taken up residence near a vital forest path, smashing everything in sight. Haldor needs someone brave or foolish enough to bring the brute down.", "Kill", "Troll", 1, 80, "defeated_eikthyr", "Easy"));
            list.Add(B("troll_hunter", "Troll Hunter", "Multiple Trolls have been spotted in the Black Forest, uprooting trees and terrorizing anyone who ventures too deep. Hunt them down and make the forest safe for travelers once more.", "Kill", "Troll", 3, 200, "defeated_eikthyr", "Easy"));
            list.Add(B("ghost_hunt", "Ghost Hunter", "Restless spirits haunt the burial grounds, their wails echoing through the night and driving away anyone who camps nearby. Put these tormented souls to rest once and for all.", "Kill", "Ghost", 5, 110, "defeated_eikthyr", "Easy"));

            // Black Forest Gather
            list.Add(B("surtling_cores", "Core Collection", "Surtling Cores are essential for powering smelters and portals, and Haldor's supply has run dry. Venture into the burial chambers of the Black Forest and retrieve the cores he needs.", "Gather", "SurtlingCore", 5, 150, "defeated_eikthyr", "Easy"));
            list.Add(B("thistle_gather", "Thistle Harvest", "Thistle is a rare and valuable ingredient used in potent meads and resistance potions. Haldor has buyers willing to pay well, so gather as much as you can find in the shadowy undergrowth.", "Gather", "Thistle", 15, 90, "defeated_eikthyr", "Easy"));
            list.Add(B("blueberry_gather", "Blueberry Harvest", "Haldor is experimenting with new potion recipes that call for blueberries as a base ingredient. Search the Black Forest floor for ripe blueberry bushes and bring back a generous harvest.", "Gather", "Blueberries", 25, 40, "defeated_eikthyr", "Easy"));
            list.Add(B("coal_delivery", "Coal Delivery", "The forges burn day and night, and coal is consumed faster than it can be produced. Haldor needs a fresh supply to keep the smelters running and orders fulfilled.", "Gather", "Coal", 30, 55, "defeated_eikthyr", "Easy"));
            list.Add(B("copper_ore_gather", "Copper Ore", "Copper is the backbone of bronze crafting, and Haldor's metalworkers are desperate for raw ore. Mine the copper deposits in the Black Forest and haul the ore back to his camp.", "Gather", "CopperOre", 15, 70, "defeated_eikthyr", "Easy"));
            list.Add(B("tin_ore_gather", "Tin Ore", "Without tin, there can be no bronze, and without bronze, the settlements fall. Haldor needs tin ore gathered from the riverbeds and shorelines of the Black Forest.", "Gather", "TinOre", 15, 65, "defeated_eikthyr", "Easy"));
            list.Add(B("bone_fragments", "Bone Fragments", "Bone fragments are surprisingly useful for crafting arrowheads and reinforcing tools. Haldor will pay for a good supply, so raid some skeleton nests or hunt creatures for their bones.", "Gather", "BoneFragments", 20, 45, "defeated_eikthyr", "Easy"));
            list.Add(B("carrot_gather", "Carrot Delivery", "Haldor has taken up cooking as a hobby and insists that fresh carrots are essential for his latest recipe. Harvest some from wild seeds or your own garden and deliver them to his camp.", "Gather", "Carrot", 15, 50, "defeated_eikthyr", "Easy"));

            // ═══════════════════════════════════════════
            //  MEDIUM TIER — Swamp & Mountain
            // ═══════════════════════════════════════════

            // Swamp Kill
            list.Add(B("draugr_patrol", "Draugr Patrol", "The rotting dead shuffle through the swamp mists, their rusted weapons still sharp enough to kill. A small patrol of Draugr has been spotted near the edges, and Haldor wants them cut down.", "Kill", "Draugr", 8, 100, "defeated_gdking", "Medium"));
            list.Add(B("draugr_purge", "Draugr Purge", "Draugr shamble endlessly through the swamp in growing numbers, their cursed existence sustained by dark forces. Venture deep into the mire and thin their ranks before they spill into neighboring biomes.", "Kill", "Draugr", 15, 160, "defeated_gdking", "Medium"));
            list.Add(B("draugr_siege", "Draugr Siege", "An army of Draugr has massed near the swamp borders, threatening to overrun nearby settlements. Haldor is paying premium coin for warriors willing to break the siege and scatter the undead host.", "Kill", "Draugr", 25, 240, "defeated_gdking", "Medium"));
            list.Add(B("draugr_elite_hunt", "Elite Draugr Hunt", "Draugr Elites are the strongest of the undead warriors, wielding ancient weapons with deadly skill. Several have been spotted leading raiding parties, and they must be eliminated.", "Kill", "Draugr_Elite", 3, 150, "defeated_gdking", "Medium"));
            list.Add(B("draugr_elite_purge", "Elite Draugr Purge", "An alarming number of Draugr Elites have risen from the crypts, each one commanding lesser undead with ruthless efficiency. Destroy their leadership and the rest will crumble.", "Kill", "Draugr_Elite", 6, 270, "defeated_gdking", "Medium"));
            list.Add(B("blob_cleanup", "Blob Cleanup", "Poisonous Blobs ooze through the swamp, leaving trails of toxic slime that contaminates the water and kills the vegetation. Destroy them before their poison spreads further.", "Kill", "Blob", 8, 100, "defeated_gdking", "Medium"));
            list.Add(B("blob_infestation", "Blob Infestation", "A massive Blob outbreak has turned entire sections of the swamp into a toxic wasteland. The creatures multiply rapidly and must be exterminated before the contamination becomes permanent.", "Kill", "Blob", 15, 170, "defeated_gdking", "Medium"));
            list.Add(B("wraith_hunt", "Wraith Hunter", "Wraiths materialize from the swamp fog at nightfall, their chilling touch draining the life from the living. Track these spectral terrors through the darkness and banish them.", "Kill", "Wraith", 3, 180, "defeated_gdking", "Medium"));
            list.Add(B("wraith_purge", "Wraith Purge", "The swamp nights have become truly deadly as multiple Wraiths now haunt every corner of the marshland. No one dares travel after dark. Banish these spirits and reclaim the night.", "Kill", "Wraith", 5, 280, "defeated_gdking", "Medium"));
            list.Add(B("leech_extermination", "Leech Extermination", "Giant Leeches infest every body of water in the swamp, making it impossible to wade through safely. Drain the swamp of these bloodsucking parasites before more travelers are dragged under.", "Kill", "Leech", 10, 110, "defeated_gdking", "Medium"));
            list.Add(B("surtling_hunt", "Surtling Harvest", "Surtlings blaze through the swamp near fire geysers, their flames setting the dead trees alight. Cut them down and their valuable cores can be salvaged from the ashes.", "Kill", "Surtling", 8, 110, "defeated_gdking", "Medium"));
            list.Add(B("surtling_inferno", "Surtling Inferno", "An abnormal number of Surtlings have gathered near the swamp's fire geysers, creating an inferno that threatens to consume the entire wetland. Extinguish them before the fire spreads.", "Kill", "Surtling", 15, 190, "defeated_gdking", "Medium"));
            list.Add(B("abomination_slayer", "Abomination Slayer", "A massive Abomination has risen from the muck, a walking horror of twisted roots and rotting wood. This ancient swamp creature must be felled before it destroys everything in its path.", "Kill", "Abomination", 1, 150, "defeated_gdking", "Medium"));
            list.Add(B("abomination_purge", "Abomination Purge", "Multiple Abominations have awakened from the swamp floor, their enormous forms tearing through the landscape. These walking nightmares must be destroyed before the swamp becomes completely impassable.", "Kill", "Abomination", 3, 350, "defeated_gdking", "Medium"));

            // Swamp Gather
            list.Add(B("iron_scrap_sm", "Iron Scraps", "Iron is the metal of war, and Haldor always needs more. Descend into the sunken crypts of the swamp, brave the Draugr within, and bring back the muddy iron scraps you find.", "Gather", "IronScrap", 10, 120, "defeated_gdking", "Medium"));
            list.Add(B("iron_scrap_gather", "Iron Procurement", "Haldor has a massive ironworking order that requires far more raw material than he has on hand. He needs iron scrap from the swamp crypts, and he's willing to pay handsomely for a large haul.", "Gather", "IronScrap", 20, 220, "defeated_gdking", "Medium"));
            list.Add(B("bloodbag_gather", "Bloodbag Collection", "The bloated bloodbags carried by Leeches contain a potent alchemical reagent that fetches a high price. Haldor has a buyer lined up, so collect what you can from slain Leeches.", "Gather", "Bloodbag", 10, 110, "defeated_gdking", "Medium"));
            list.Add(B("entrails_gather", "Entrails Supply", "Haldor claims he needs entrails for making sausages, though he refuses to discuss the recipe in detail. Don't ask questions, just bring him what he needs from the Draugr corpses.", "Gather", "Entrails", 15, 100, "defeated_gdking", "Medium"));
            list.Add(B("guck_gather", "Guck Collection", "The strange glowing guck that grows on swamp trees has proven valuable for crafting and alchemy. Haldor wants a supply collected, though harvesting it from the towering trees is no easy task.", "Gather", "Guck", 10, 130, "defeated_gdking", "Medium"));
            list.Add(B("turnip_gather", "Turnip Delivery", "Haldor has developed a taste for turnip stew and insists that swamp-grown turnips have the best flavor. Harvest some from the wild patches in the marshland and bring them back.", "Gather", "Turnip", 15, 90, "defeated_gdking", "Medium"));
            list.Add(B("ancient_bark", "Ancient Bark", "The bark of ancient trees in the swamp is remarkably durable and sought after for shipbuilding and fine woodwork. Haldor has a commission that requires a good supply of this rare material.", "Gather", "ElderBark", 20, 100, "defeated_gdking", "Medium"));

            // Mountain Kill
            list.Add(B("wolf_patrol", "Wolf Patrol", "A small wolf pack has established territory on the lower mountain slopes, attacking anyone who attempts the ascent. Clear them from the trails so travelers can pass safely.", "Kill", "Wolf", 5, 130, "defeated_bonemass", "Medium"));
            list.Add(B("wolf_hunt", "Wolf Cull", "Mountain wolf packs have grown dangerously large, emboldened by the harsh winter. Their howls echo through the passes at night, and their attacks grow more frequent with each passing day.", "Kill", "Wolf", 10, 220, "defeated_bonemass", "Medium"));
            list.Add(B("wolf_massacre", "Wolf Massacre", "The mountain is completely overrun with wolves, their packs numbering in the dozens. Every trail and clearing swarms with snarling predators. A massive cull is the only way to restore order.", "Kill", "Wolf", 18, 340, "defeated_bonemass", "Medium"));
            list.Add(B("drake_hunt", "Drake Slayer", "Frost Drakes circle the mountain peaks, swooping down to rain shards of ice on anyone below. Their frozen breath can kill in seconds. Bring them down from the skies.", "Kill", "Hatchling", 5, 160, "defeated_bonemass", "Medium"));
            list.Add(B("drake_purge", "Drake Purge", "The mountain skies are thick with Frost Drakes, their icy breath making the peaks even more treacherous than usual. Ground every last one of them before the mountain becomes unreachable.", "Kill", "Hatchling", 10, 280, "defeated_bonemass", "Medium"));
            list.Add(B("golem_breaker", "Golem Breaker", "A Stone Golem stands guard at a mountain pass, its massive form blocking all passage. These ancient constructs are nearly indestructible, but Haldor believes a skilled warrior can shatter it.", "Kill", "StoneGolem", 1, 170, "defeated_bonemass", "Medium"));
            list.Add(B("golem_crusher", "Golem Crusher", "Multiple Stone Golems now patrol the mountain paths, crushing boulders and anything else in their way. Their presence has completely cut off trade routes through the peaks.", "Kill", "StoneGolem", 3, 350, "defeated_bonemass", "Medium"));
            list.Add(B("fenring_hunt", "Fenring Hunt", "Fenrings emerge under the cover of darkness, their savage howls splitting the mountain silence. These werewolf-like creatures are deadlier than any normal wolf. Hunt them at night when they appear.", "Kill", "Fenring", 3, 280, "defeated_bonemass", "Medium"));
            list.Add(B("ulv_hunt", "Ulv Extermination", "Ulvs lurk in mountain caves, their pale forms nearly invisible against the snow. These ghostly predators ambush from the shadows. Root them out of their dens and destroy them.", "Kill", "Ulv", 5, 240, "defeated_bonemass", "Medium"));
            list.Add(B("bat_swat", "Bat Swat", "Swarms of bats pour from the mountain caves at dusk, their sheer numbers darkening the sky. They're a menace to anyone mining or exploring the caverns, and they must be dealt with.", "Kill", "Bat", 10, 130, "defeated_bonemass", "Medium"));

            // Mountain Gather
            list.Add(B("silver_ore_sm", "Silver Nuggets", "Silver ore lies buried deep beneath the mountain snow, detectable only with the Wishbone's resonance. Haldor needs a small supply for a custom jewelry order from a wealthy client.", "Gather", "SilverOre", 8, 150, "defeated_bonemass", "Medium"));
            list.Add(B("silver_ore_gather", "Silver Procurement", "Haldor has secured a lucrative deal with a distant buyer who wants a large quantity of raw silver. Mine the mountain veins and haul the ore back to claim a generous reward.", "Gather", "SilverOre", 15, 270, "defeated_bonemass", "Medium"));
            list.Add(B("freeze_gland_gather", "Freeze Gland Supply", "The freeze glands harvested from slain Frost Drakes contain a potent cryogenic fluid prized by alchemists. Haldor's potion makers need a fresh supply for their frost resistance brews.", "Gather", "FreezeGland", 10, 180, "defeated_bonemass", "Medium"));
            list.Add(B("obsidian_gather", "Obsidian Collection", "Obsidian is prized for crafting razor-sharp arrowheads and decorative items. Haldor needs a supply gathered from the exposed mountain rock faces where the volcanic glass gleams in the cold light.", "Gather", "Obsidian", 15, 140, "defeated_bonemass", "Medium"));
            list.Add(B("wolf_pelt_gather", "Wolf Pelt Supply", "Wolf pelts are in high demand for warm cloaks and bedding as winter approaches. Haldor's fur traders need quality pelts, so hunt the mountain wolves and bring back their hides.", "Gather", "WolfPelt", 8, 160, "defeated_bonemass", "Medium"));
            list.Add(B("onion_gather", "Onion Delivery", "Haldor insists that mountain-grown onions are superior for his cooking, claiming the cold altitude gives them a sharper flavor. Harvest some from the mountain gardens and deliver them.", "Gather", "Onion", 15, 100, "defeated_bonemass", "Medium"));

            // ═══════════════════════════════════════════
            //  HARD TIER — Plains, Mistlands, Ashlands
            // ═══════════════════════════════════════════

            // Plains Kill
            list.Add(B("fuling_scout", "Fuling Scouts", "Fuling scouts probe the edges of the plains, their beady eyes watching for weakness. They report back to their villages, and raids follow shortly after. Silence the scouts before they can report.", "Kill", "Goblin", 8, 200, "defeated_dragon", "Hard"));
            list.Add(B("fuling_hunt", "Fuling Raid", "A Fuling raiding party has been terrorizing the plains, burning camps and stealing supplies. Haldor wants them intercepted and destroyed before they can strike again.", "Kill", "Goblin", 15, 320, "defeated_dragon", "Hard"));
            list.Add(B("fuling_invasion", "Fuling Invasion", "A massive Fuling invasion force marches across the plains, their war drums echoing for miles. This is no mere raid. Rally your strength and shatter their army before they reach the settlements.", "Kill", "Goblin", 25, 480, "defeated_dragon", "Hard"));
            list.Add(B("fuling_berserker_hunt", "Berserker Challenge", "Fuling Berserkers are among the deadliest warriors in all the realms, their massive clubs crushing armor like parchment. Haldor dares you to face these monsters and prove your worth.", "Kill", "GoblinBrute", 2, 300, "defeated_dragon", "Hard"));
            list.Add(B("fuling_berserker_purge", "Berserker Rampage", "Multiple Fuling Berserkers have gone on a rampage across the plains, leaving a trail of destruction that stretches for miles. They must be stopped before nothing remains standing.", "Kill", "GoblinBrute", 4, 520, "defeated_dragon", "Hard"));
            list.Add(B("fuling_shaman_hunt", "Shaman Purge", "Fuling Shamans channel dark fire magic, immolating anyone foolish enough to approach their totems. Destroy these spellcasters before they can summon more devastating rituals.", "Kill", "GoblinShaman", 3, 250, "defeated_dragon", "Hard"));
            list.Add(B("fuling_shaman_coven", "Shaman Coven", "A coven of Fuling Shamans has gathered at a sacred site, performing rituals that darken the sky with flame. Their combined power grows with each passing day. Disrupt the coven now.", "Kill", "GoblinShaman", 6, 430, "defeated_dragon", "Hard"));
            list.Add(B("deathsquito_swat", "Deathsquito Swat", "Deathsquitos dart across the plains with terrifying speed, their needle-like proboscis punching through armor with a single strike. Swat these lethal insects before more Vikings fall to them.", "Kill", "Deathsquito", 10, 220, "defeated_dragon", "Hard"));
            list.Add(B("deathsquito_plague", "Deathsquito Plague", "An overwhelming swarm of Deathsquitos has descended upon the plains, their buzzing filling the air like a plague of razors. No one can travel the open ground safely until the swarm is broken.", "Kill", "Deathsquito", 20, 400, "defeated_dragon", "Hard"));
            list.Add(B("lox_hunt", "Lox Hunt", "Lox are enormous beasts that trample everything beneath their massive hooves. Dangerous to approach, but their meat and pelts are valuable. Haldor is paying well for those brave enough.", "Kill", "Lox", 2, 260, "defeated_dragon", "Hard"));
            list.Add(B("lox_stampede", "Lox Stampede", "A herd of enraged Lox stampedes across the plains, flattening camps and crushing anything in their path. The ground shakes with their fury, and someone must put them down.", "Kill", "Lox", 5, 500, "defeated_dragon", "Hard"));
            list.Add(B("growth_slayer", "Growth Slayer", "Tar Growths ooze from the black pits, spreading corruption wherever they crawl. Their toxic tar contaminates the soil and kills all plant life. Destroy them before the blight spreads further.", "Kill", "BlobTar", 5, 260, "defeated_dragon", "Hard"));

            // Plains Gather
            list.Add(B("barley_gather", "Barley Stockpile", "Haldor is planning to brew his finest mead yet, and for that he needs quality barley from the plains. Harvest the golden fields and bring back enough for a proper brewing operation.", "Gather", "Barley", 20, 220, "defeated_dragon", "Hard"));
            list.Add(B("barley_bulk", "Barley Bulk Order", "A massive barley shipment has been ordered by settlements across the realm for bread and mead production. The plains fields must be harvested thoroughly to fill this enormous order.", "Gather", "Barley", 40, 400, "defeated_dragon", "Hard"));
            list.Add(B("needle_gather", "Needle Collection", "Deathsquito needles are prized for their incredible sharpness, making them ideal for crafting the finest arrows. Slay the insects and carefully harvest their needles for Haldor's weaponsmiths.", "Gather", "Needle", 10, 200, "defeated_dragon", "Hard"));
            list.Add(B("lox_meat_gather", "Lox Meat Supply", "Lox meat is considered a delicacy among the Vikings, its rich flavor unmatched by any other game. Haldor's kitchen demands a fresh supply, so hunt the great beasts and butcher their meat.", "Gather", "LoxMeat", 8, 180, "defeated_dragon", "Hard"));
            list.Add(B("black_metal_gather", "Black Metal Scrap", "Black metal is forged in Fuling villages and possesses extraordinary strength. Raid their settlements, scavenge their forges, and bring back the dark metal scraps that Haldor's smiths covet.", "Gather", "BlackMetalScrap", 15, 350, "defeated_dragon", "Hard"));
            list.Add(B("flax_gather", "Flax Delivery", "Flax from the plains is spun into linen thread, essential for padded armor and fine clothing. Haldor needs a large supply harvested from Fuling farms and delivered to his workshop.", "Gather", "Flax", 20, 250, "defeated_dragon", "Hard"));

            // Mistlands Kill
            list.Add(B("seeker_hunt", "Seeker Hunt", "Seekers skitter through the eternal mists, their insectoid forms blending with the darkness until they strike. These creatures infest every ruin and clearing. Hunt them and reclaim the Mistlands.", "Kill", "Seeker", 8, 350, "defeated_goblinking", "Hard"));
            list.Add(B("seeker_purge", "Seeker Extermination", "The Seeker population has grown to epidemic proportions, their hives spreading through every structure in the Mistlands. A full-scale extermination is needed to make any headway into this cursed realm.", "Kill", "Seeker", 15, 550, "defeated_goblinking", "Hard"));
            list.Add(B("seeker_brute_hunt", "Seeker Brute Slayer", "Seeker Brutes are the armored guardians of the deepest hives, their chitinous shells nearly impervious to normal weapons. Face these monstrosities and prove that nothing in the Mistlands is unkillable.", "Kill", "SeekerBrute", 2, 400, "defeated_goblinking", "Hard"));
            list.Add(B("seeker_brute_purge", "Seeker Brute Purge", "Multiple Seeker Brutes have fortified the largest hives, creating an impenetrable defense that blocks all exploration of the Mistlands. Their fortress of chitin must be broken.", "Kill", "SeekerBrute", 4, 700, "defeated_goblinking", "Hard"));
            list.Add(B("tick_extermination", "Tick Extermination", "Ticks drop from the misty canopy without warning, latching onto their victims and draining them dry. The Mistlands crawl with these parasites, and they must be purged.", "Kill", "Tick", 10, 300, "defeated_goblinking", "Hard"));
            list.Add(B("tick_plague", "Tick Plague", "A plague of Ticks infests every corner of the Mistlands, dropping in clusters from above and overwhelming even well-armored warriors. Their numbers must be decimated.", "Kill", "Tick", 20, 500, "defeated_goblinking", "Hard"));
            list.Add(B("gjall_hunt", "Gjall Hunter", "A Gjall drifts through the mists like a living nightmare, raining explosive projectiles from above. These floating horrors must be brought down before they destroy everything beneath them.", "Kill", "Gjall", 1, 380, "defeated_goblinking", "Hard"));
            list.Add(B("gjall_purge", "Gjall Purge", "Multiple Gjall now patrol the Mistlands skies, their bombardments turning the landscape into a cratered wasteland. No one can explore safely until these aerial terrors are eliminated.", "Kill", "Gjall", 3, 750, "defeated_goblinking", "Hard"));
            list.Add(B("dvergr_rogue_hunt", "Rogue Dvergr", "Some Dvergr have abandoned their neutral stance and turned hostile, attacking anyone who approaches their outposts. Haldor once traded with them, and he takes their betrayal personally.", "Kill", "Dverger", 5, 450, "defeated_goblinking", "Hard"));

            // Mistlands Gather
            list.Add(B("sap_gather", "Yggdrasil Sap", "The sap of Yggdrasil's roots is a mystical substance of immense value, glowing with the energy of the World Tree itself. Haldor has alchemists willing to pay a fortune for even a small supply.", "Gather", "Sap", 10, 400, "defeated_goblinking", "Hard"));
            list.Add(B("sap_bulk", "Sap Bulk Order", "A massive order for Yggdrasil sap has come in from Haldor's most exclusive clients. The ancient roots must be tapped carefully, and the precious golden sap collected in large quantities.", "Gather", "Sap", 20, 700, "defeated_goblinking", "Hard"));
            list.Add(B("softtissue_gather", "Soft Tissue Collection", "The soft tissue harvested from slain Seekers contains unique biological compounds that alchemists prize for their transformative properties. Gather what you can from the insectoid corpses.", "Gather", "Softtissue", 10, 350, "defeated_goblinking", "Hard"));

            // Ashlands Kill
            list.Add(B("charred_patrol", "Charred Patrol", "Charred warriors stand eternal vigil at the borders of the Ashlands, their blackened forms wreathed in smoldering embers. They attack anything living that enters their scorched domain.", "Kill", "Charred_Melee", 6, 380, "defeated_queen", "Hard"));
            list.Add(B("charred_purge", "Charred Purge", "Legions of Charred warriors guard the Ashlands in disciplined formations, their burned flesh immune to pain. Break through their ranks to establish a foothold in this hellish landscape.", "Kill", "Charred_Melee", 12, 560, "defeated_queen", "Hard"));
            list.Add(B("charred_siege", "Charred Siege", "Massive Charred forces have assembled at the Ashlands border, blocking all passage into the volcanic wastes. This army of the burned must be shattered to open the way forward.", "Kill", "Charred_Melee", 20, 780, "defeated_queen", "Hard"));
            list.Add(B("morgen_hunt", "Morgen Slayer", "Morgen lurk in the volcanic wastes of the Ashlands, their shadowy forms striking from clouds of ash and smoke. These terrifying creatures are among the deadliest in all the realms.", "Kill", "Morgen", 3, 450, "defeated_queen", "Hard"));
            list.Add(B("morgen_purge", "Morgen Purge", "Multiple Morgen stalk the Ashlands, their presence turning the already dangerous landscape into a death trap. Every shadow could hide one of these nightmarish predators.", "Kill", "Morgen", 6, 750, "defeated_queen", "Hard"));
            list.Add(B("fallen_valkyrie_hunt", "Fallen Valkyrie", "Fallen Valkyries have been corrupted by the Ashlands' dark power, their once-noble wings now trailing ash and flame. They are a disgrace to Valhalla and must be put to rest.", "Kill", "Fallen_Valkyrie", 2, 550, "defeated_queen", "Hard"));
            list.Add(B("fallen_valkyrie_purge", "Valkyrie Purge", "Multiple Fallen Valkyries patrol the skies above the Ashlands, their corrupted battle cries echoing across the volcanic wastes. Free their tortured souls from this unholy existence.", "Kill", "Fallen_Valkyrie", 4, 800, "defeated_queen", "Hard"));
            list.Add(B("asksvinn_hunt", "Asksvinn Tamer", "Wild Asksvinn charge through the ashfields with reckless abandon, their fiery hooves scorching the ground beneath them. These flame-touched beasts are as dangerous as they are beautiful.", "Kill", "Asksvinn", 3, 400, "defeated_queen", "Hard"));
            list.Add(B("asksvinn_stampede", "Asksvinn Stampede", "A massive herd of Asksvinn rampages through the volcanic wastes, their thundering hooves and fiery manes creating a spectacle of destruction. Stop the stampede before it reaches the settlements.", "Kill", "Asksvinn", 6, 650, "defeated_queen", "Hard"));
            list.Add(B("volture_hunt", "Volture Hunt", "Voltures circle the Ashlands skies on leathery wings, diving to snatch up anything they can carry. Their razor talons and scorching breath make them formidable aerial predators.", "Kill", "Volture", 3, 380, "defeated_queen", "Hard"));
            list.Add(B("volture_flock", "Volture Flock", "A massive flock of Voltures has claimed the Ashlands airspace, darkening the sky with their numbers. Their constant aerial attacks make ground travel nearly impossible.", "Kill", "Volture", 8, 620, "defeated_queen", "Hard"));

            // Ashlands Gather
            list.Add(B("flametal_gather", "Flametal Ore", "Flametal is the rarest and most powerful metal known to exist, forged in the heart of volcanic vents. Mining it is extraordinarily dangerous, but Haldor's smiths need it for their finest work.", "Gather", "FlametalOreNew", 5, 500, "defeated_queen", "Hard"));
            list.Add(B("flametal_bulk", "Flametal Bulk", "A massive flametal order has come in from Haldor's most powerful clients. The volcanic forges must be worked around the clock, and every scrap of this precious ore must be collected.", "Gather", "FlametalOreNew", 10, 800, "defeated_queen", "Hard"));

            // ═══════════════════════════════════════════
            //  MINIBOSS TIER — Starred spawn bounties
            // ═══════════════════════════════════════════

            // Meadows Miniboss (no boss gate)
            list.Add(BS("raging_boar", "Raging Boar", "A two-starred Boar of unusual size has been terrorizing the meadows, goring livestock and charging at anyone who strays too close. Its tusks are as long as daggers and its temper is legendary among the settlers.", "Boar", 1, 100, "", 3));
            list.Add(BS("ancient_neck", "Ancient Neck", "A two-starred Neck of monstrous proportions lurks in a pond near the meadow settlements. This ancient creature has dragged several fishermen beneath the surface, and Haldor wants it dealt with immediately.", "Neck", 1, 100, "", 3));
            list.Add(BS("greyling_alpha", "Greyling Alpha", "A three-starred Greyling has emerged from the forest edge, rallying its lesser kin into organized raids on nearby camps. For a Greyling, it shows disturbing cunning and must be eliminated before it causes more damage.", "Greyling", 1, 80, "", 4));
            list.Add(BS("great_deer", "Great Deer", "A two-starred Deer of extraordinary size roams the meadows, easily outrunning hunters and trampling anyone foolish enough to corner it. Haldor has offered a bounty to anyone who can bring this magnificent beast down.", "Deer", 1, 120, "", 3));

            // Black Forest Miniboss
            list.Add(BS("champion_troll", "Champion Troll", "A monstrous three-starred Troll has been sighted deep in the Black Forest, tearing through trees and boulders alike. This beast is far larger and more dangerous than any normal Troll. Haldor wants it dead and is paying accordingly.", "Troll", 1, 500, "defeated_eikthyr", 4));
            list.Add(BS("champion_brute", "Champion Brute", "A two-starred Greydwarf Brute has claimed a section of the Black Forest as its own territory, crushing any who dare enter. The creature's bark-like hide is thick as iron, and its fists hit like falling trees.", "Greydwarf_Elite", 1, 300, "defeated_eikthyr", 3));
            list.Add(BS("infernal_surtling", "Infernal Surtling", "An enormous three-starred Surtling blazes through the swamp with unnatural intensity, its flames burning blue-white and igniting everything within a wide radius. Even the waterlogged swamp cannot quench its fury.", "Surtling", 1, 400, "defeated_gdking", 4));
            list.Add(BS("draugr_overlord", "Draugr Overlord", "A three-starred Draugr Elite has risen from the deepest crypt in the swamp, commanding an army of lesser undead with terrifying intelligence. This ancient warrior-king must be destroyed before his forces grow.", "Draugr_Elite", 1, 700, "defeated_gdking", 4));
            list.Add(BS("alpha_wolf", "Alpha Wolf", "A three-starred Alpha Wolf leads the mountain packs with savage cunning, its howl sending even experienced warriors into retreat. This enormous predator is the reason entire hunting parties have vanished.", "Wolf", 1, 600, "defeated_bonemass", 4));
            list.Add(BS("frost_drake_alpha", "Frost Drake Alpha", "A three-starred Frost Drake circles the highest peaks, its icy breath capable of flash-freezing a Viking solid in seconds. This ancient drake has terrorized the mountains for years, and Haldor finally has coin to end it.", "Hatchling", 1, 800, "defeated_bonemass", 4));
            list.Add(BS("stone_colossus", "Stone Colossus", "A two-starred Stone Golem of immense size guards the mountain summit, its footsteps causing avalanches and its fists shattering cliff faces. This is no ordinary golem. It is an ancient guardian awakened from millennia of slumber.", "StoneGolem", 1, 900, "defeated_bonemass", 3));
            list.Add(BS("fuling_warlord", "Fuling Warlord", "A three-starred Fuling Berserker has united several plains villages under its brutal rule, leading devastating raids. This colossal warlord wields a club the size of a tree trunk.", "GoblinBrute", 1, 1000, "defeated_dragon", 4));
            list.Add(BS("deathsquito_swarm", "Deathsquito Swarm", "A swarm of two-starred Deathsquitos has appeared, each one larger and more aggressive than any seen before. Their synchronized attacks are nearly impossible to dodge. Survive their onslaught and destroy them.", "Deathsquito", 3, 600, "defeated_dragon", 3));
            list.Add(BS("ancient_lox", "Ancient Lox", "A two-starred Ancient Lox wanders the plains, its hide scarred from a hundred battles. This beast is so large it shakes the ground with every step, and its charge can demolish stone walls.", "Lox", 1, 1500, "defeated_dragon", 3));
            list.Add(BS("seeker_matriarch", "Seeker Matriarch", "A three-starred Seeker Brute guards the deepest hive in the Mistlands, its chitinous armor nearly impenetrable and its mandibles capable of shearing through iron. This is the queen's chosen protector.", "SeekerBrute", 1, 2000, "defeated_goblinking", 4));
            list.Add(BS("gjall_leviathan", "Gjall Leviathan", "A two-starred Gjall of terrible size drifts through the Mistlands like a living fortress, its explosive projectiles leveling everything below. This aerial monstrosity is the most dangerous creature in the mists.", "Gjall", 1, 2500, "defeated_goblinking", 3));

            return list;
        }

        // Helper for standard bounties
        private static BountyEntry B(string id, string title, string desc, string type, string target, int amount, int reward, string boss, string tier)
        {
            return new BountyEntry { Id = id, Title = title, Description = desc, Type = type, Target = target, Amount = amount, Reward = reward, RequiredBoss = boss, Tier = tier };
        }

        // Helper for miniboss starred bounties
        private static BountyEntry BS(string id, string title, string desc, string target, int amount, int reward, string boss, int spawnLevel)
        {
            return new BountyEntry { Id = id, Title = title, Description = desc, Type = "Kill", Target = target, Amount = amount, Reward = reward, RequiredBoss = boss, SpawnLevel = spawnLevel, Tier = "Miniboss" };
        }
    }
}
