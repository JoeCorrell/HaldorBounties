using System.Collections.Generic;

namespace HaldorBounties
{
    public enum RewardCategory { Coins, Items, Resources, Consumables }

    public enum BiomeTier { Meadows, BlackForest, Swamp, Mountain, Plains, Mistlands, Ashlands }

    public class RewardItem
    {
        public string PrefabName;
        public int MinStack;
        public int MaxStack;
        public int Quality;

        public RewardItem(string prefab, int min, int max, int quality = 1)
        {
            PrefabName = prefab;
            MinStack = min;
            MaxStack = max;
            Quality = quality;
        }
    }

    public static class RewardPool
    {
        // ── ITEMS (weapons, armor, tools) ──
        public static readonly Dictionary<BiomeTier, RewardItem[]> ItemPool = new Dictionary<BiomeTier, RewardItem[]>
        {
            { BiomeTier.Meadows, new[] {
                new RewardItem("AxeFlint", 1, 1),
                new RewardItem("KnifeFlint", 1, 1),
                new RewardItem("SpearFlint", 1, 1),
                new RewardItem("ShieldWood", 1, 1),
                new RewardItem("Bow", 1, 1),
                new RewardItem("ArmorLeatherChest", 1, 1),
                new RewardItem("ArmorLeatherLegs", 1, 1),
                new RewardItem("HelmetLeather", 1, 1),
            }},
            { BiomeTier.BlackForest, new[] {
                new RewardItem("AxeBronze", 1, 1),
                new RewardItem("SwordBronze", 1, 1),
                new RewardItem("MaceBronze", 1, 1),
                new RewardItem("SpearBronze", 1, 1),
                new RewardItem("ShieldBronzeBuckler", 1, 1),
                new RewardItem("ArmorBronzeChest", 1, 1),
                new RewardItem("ArmorBronzeLegs", 1, 1),
                new RewardItem("HelmetBronze", 1, 1),
                new RewardItem("ArmorTrollLeatherChest", 1, 1),
                new RewardItem("BowFineWood", 1, 1),
            }},
            { BiomeTier.Swamp, new[] {
                new RewardItem("SwordIron", 1, 1),
                new RewardItem("MaceIron", 1, 1),
                new RewardItem("AxeIron", 1, 1),
                new RewardItem("ShieldBanded", 1, 1),
                new RewardItem("ArmorIronChest", 1, 1),
                new RewardItem("ArmorIronLegs", 1, 1),
                new RewardItem("HelmetIron", 1, 1),
                new RewardItem("BowHuntsman", 1, 1),
                new RewardItem("AtgeirIron", 1, 1),
                new RewardItem("Battleaxe", 1, 1),
            }},
            { BiomeTier.Mountain, new[] {
                new RewardItem("SwordSilver", 1, 1),
                new RewardItem("MaceSilver", 1, 1),
                new RewardItem("SpearWolfFang", 1, 1),
                new RewardItem("ShieldSilver", 1, 1),
                new RewardItem("ArmorWolfChest", 1, 1),
                new RewardItem("ArmorWolfLegs", 1, 1),
                new RewardItem("HelmetDrake", 1, 1),
                new RewardItem("BowDraugrFang", 1, 1),
                new RewardItem("CapeWolf", 1, 1),
            }},
            { BiomeTier.Plains, new[] {
                new RewardItem("SwordBlackmetal", 1, 1),
                new RewardItem("AxeBlackMetal", 1, 1),
                new RewardItem("KnifeBlackMetal", 1, 1),
                new RewardItem("ShieldBlackmetal", 1, 1),
                new RewardItem("AtgeirBlackmetal", 1, 1),
                new RewardItem("ArmorPaddedCuirass", 1, 1),
                new RewardItem("ArmorPaddedGreaves", 1, 1),
                new RewardItem("HelmetPadded", 1, 1),
            }},
            { BiomeTier.Mistlands, new[] {
                new RewardItem("SwordMistwalker", 1, 1),
                new RewardItem("THSwordKrom", 1, 1),
                new RewardItem("BowSpineSnap", 1, 1),
                new RewardItem("CrossbowArbalest", 1, 1),
                new RewardItem("ShieldCarapace", 1, 1),
                new RewardItem("ArmorCarapaceChest", 1, 1),
                new RewardItem("ArmorCarapaceLegs", 1, 1),
                new RewardItem("HelmetCarapace", 1, 1),
            }},
            { BiomeTier.Ashlands, new[] {
                new RewardItem("SwordFlametal", 1, 1),
                new RewardItem("AxeFlametal", 1, 1),
                new RewardItem("MaceEldner", 1, 1),
                new RewardItem("ShieldFlametal", 1, 1),
                new RewardItem("ArmorFlametalChest", 1, 1),
                new RewardItem("ArmorFlametalLegs", 1, 1),
                new RewardItem("HelmetFlametal", 1, 1),
                new RewardItem("CrossbowRipper", 1, 1),
            }},
        };

        // ── RESOURCES (crafting materials) ──
        public static readonly Dictionary<BiomeTier, RewardItem[]> ResourcePool = new Dictionary<BiomeTier, RewardItem[]>
        {
            { BiomeTier.Meadows, new[] {
                new RewardItem("Wood", 20, 40),
                new RewardItem("Stone", 15, 30),
                new RewardItem("Flint", 8, 15),
                new RewardItem("LeatherScraps", 8, 15),
                new RewardItem("DeerHide", 5, 10),
                new RewardItem("Resin", 10, 20),
            }},
            { BiomeTier.BlackForest, new[] {
                new RewardItem("Bronze", 3, 8),
                new RewardItem("CopperOre", 8, 15),
                new RewardItem("TinOre", 8, 15),
                new RewardItem("FineWood", 10, 20),
                new RewardItem("Coal", 10, 20),
                new RewardItem("SurtlingCore", 2, 4),
                new RewardItem("BoneFragments", 8, 15),
                new RewardItem("Thistle", 5, 10),
            }},
            { BiomeTier.Swamp, new[] {
                new RewardItem("Iron", 3, 8),
                new RewardItem("IronScrap", 8, 15),
                new RewardItem("ElderBark", 8, 15),
                new RewardItem("Guck", 3, 8),
                new RewardItem("Bloodbag", 5, 10),
                new RewardItem("Entrails", 5, 10),
                new RewardItem("Chain", 1, 3),
            }},
            { BiomeTier.Mountain, new[] {
                new RewardItem("Silver", 3, 8),
                new RewardItem("SilverOre", 5, 12),
                new RewardItem("WolfPelt", 3, 6),
                new RewardItem("FreezeGland", 3, 8),
                new RewardItem("Obsidian", 5, 12),
                new RewardItem("Crystal", 3, 6),
            }},
            { BiomeTier.Plains, new[] {
                new RewardItem("BlackMetalScrap", 5, 12),
                new RewardItem("BlackMetal", 3, 6),
                new RewardItem("Needle", 5, 10),
                new RewardItem("Flax", 5, 12),
                new RewardItem("Barley", 10, 20),
                new RewardItem("LoxPelt", 2, 5),
                new RewardItem("Tar", 5, 10),
            }},
            { BiomeTier.Mistlands, new[] {
                new RewardItem("Sap", 5, 10),
                new RewardItem("Softtissue", 5, 10),
                new RewardItem("Carapace", 5, 10),
                new RewardItem("BlackCore", 1, 2),
                new RewardItem("Eitr", 3, 6),
                new RewardItem("RoyalJelly", 3, 6),
            }},
            { BiomeTier.Ashlands, new[] {
                new RewardItem("FlametalNew", 3, 6),
                new RewardItem("GemstoneRed", 1, 3),
                new RewardItem("GemstoneBlue", 1, 3),
                new RewardItem("GemstoneGreen", 1, 3),
                new RewardItem("Grausten", 10, 20),
                new RewardItem("CharredBone", 5, 10),
            }},
        };

        // ── CONSUMABLES (food, mead, potions) ──
        public static readonly Dictionary<BiomeTier, RewardItem[]> ConsumablePool = new Dictionary<BiomeTier, RewardItem[]>
        {
            { BiomeTier.Meadows, new[] {
                new RewardItem("CookedMeat", 3, 6),
                new RewardItem("CookedDeerMeat", 3, 5),
                new RewardItem("QueensJam", 3, 5),
                new RewardItem("Honey", 3, 6),
                new RewardItem("MeadHealthMinor", 3, 5),
            }},
            { BiomeTier.BlackForest, new[] {
                new RewardItem("CarrotSoup", 3, 5),
                new RewardItem("MeadStaminaMinor", 3, 5),
                new RewardItem("MeadPoisonResist", 3, 5),
                new RewardItem("MeadHealthMedium", 3, 5),
                new RewardItem("Sausages", 3, 5),
            }},
            { BiomeTier.Swamp, new[] {
                new RewardItem("MeadPoisonResist", 5, 8),
                new RewardItem("MeadStaminaMedium", 3, 5),
                new RewardItem("TurnipStew", 3, 5),
                new RewardItem("Sausages", 5, 8),
                new RewardItem("BlackSoup", 3, 5),
            }},
            { BiomeTier.Mountain, new[] {
                new RewardItem("WolfSkewer", 3, 5),
                new RewardItem("EyeScream", 3, 5),
                new RewardItem("MeadFrostResist", 3, 5),
                new RewardItem("MeadStaminaMedium", 5, 8),
                new RewardItem("OnionSoup", 3, 5),
            }},
            { BiomeTier.Plains, new[] {
                new RewardItem("LoxPie", 2, 4),
                new RewardItem("BloodPudding", 2, 4),
                new RewardItem("FishAndBread", 2, 4),
                new RewardItem("MeadHealthMajor", 3, 5),
                new RewardItem("MeadStaminaLingering", 3, 5),
            }},
            { BiomeTier.Mistlands, new[] {
                new RewardItem("YggdrasilPorridge", 2, 3),
                new RewardItem("SeekerAspic", 2, 3),
                new RewardItem("MagicallyStuffedShroom", 2, 3),
                new RewardItem("MeadEitrMinor", 3, 5),
                new RewardItem("MushroomOmelette", 2, 4),
            }},
            { BiomeTier.Ashlands, new[] {
                new RewardItem("MeatPlatter", 2, 3),
                new RewardItem("ScorchingMedley", 2, 3),
                new RewardItem("FierySvinstew", 2, 3),
                new RewardItem("MeadEitrLingering", 2, 4),
                new RewardItem("PiquantPie", 2, 3),
            }},
        };

        /// <summary>Maps a bounty's RequiredBoss to a BiomeTier for reward selection.</summary>
        public static BiomeTier GetBiomeTier(BountyEntry entry)
        {
            switch (entry.RequiredBoss ?? "")
            {
                case "": return BiomeTier.Meadows;
                case "defeated_eikthyr": return BiomeTier.BlackForest;
                case "defeated_gdking": return BiomeTier.Swamp;
                case "defeated_bonemass": return BiomeTier.Mountain;
                case "defeated_dragon": return BiomeTier.Plains;
                case "defeated_goblinking": return BiomeTier.Mistlands;
                case "defeated_queen": return BiomeTier.Ashlands;
                default: return BiomeTier.Meadows;
            }
        }
    }
}
