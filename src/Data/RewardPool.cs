using System.Collections.Generic;

namespace HaldorBounties
{
    public enum RewardCategory { Coins, Ingots, Resources, Consumables }

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
        // ── INGOTS (refined metals and processed bars) ──
        // Stacks scaled to be roughly equivalent to 60-80% of the coin reward value
        public static readonly Dictionary<BiomeTier, RewardItem[]> IngotPool = new Dictionary<BiomeTier, RewardItem[]>
        {
            { BiomeTier.Meadows, new[] {
                new RewardItem("CopperOre", 5, 10),
                new RewardItem("TinOre", 5, 10),
                new RewardItem("Flint", 8, 14),
            }},
            { BiomeTier.BlackForest, new[] {
                new RewardItem("Bronze", 4, 8),
                new RewardItem("Copper", 5, 8),
                new RewardItem("Tin", 5, 8),
                new RewardItem("Coal", 8, 14),
            }},
            { BiomeTier.Swamp, new[] {
                new RewardItem("Iron", 4, 8),
                new RewardItem("IronScrap", 6, 12),
                new RewardItem("Chain", 1, 3),
            }},
            { BiomeTier.Mountain, new[] {
                new RewardItem("Silver", 4, 8),
                new RewardItem("SilverOre", 6, 10),
                new RewardItem("Crystal", 3, 6),
            }},
            { BiomeTier.Plains, new[] {
                new RewardItem("BlackMetal", 4, 8),
                new RewardItem("BlackMetalScrap", 6, 12),
                new RewardItem("LinenThread", 4, 8),
            }},
            { BiomeTier.Mistlands, new[] {
                new RewardItem("BlackCore", 1, 2),
                new RewardItem("Eitr", 3, 6),
                new RewardItem("Carapace", 5, 10),
                new RewardItem("Sap", 4, 8),
            }},
            { BiomeTier.Ashlands, new[] {
                new RewardItem("FlametalNew", 4, 8),
                new RewardItem("GemstoneRed", 1, 3),
                new RewardItem("GemstoneBlue", 1, 3),
                new RewardItem("GemstoneGreen", 1, 3),
                new RewardItem("CelestialFeather", 1, 2),
            }},
        };

        // ── RESOURCES (crafting materials — no food, no ingots) ──
        // Stacks scaled to feel useful but not overpowered for the tier
        public static readonly Dictionary<BiomeTier, RewardItem[]> ResourcePool = new Dictionary<BiomeTier, RewardItem[]>
        {
            { BiomeTier.Meadows, new[] {
                new RewardItem("Wood", 15, 30),
                new RewardItem("Stone", 12, 25),
                new RewardItem("LeatherScraps", 6, 12),
                new RewardItem("DeerHide", 4, 8),
                new RewardItem("Resin", 8, 16),
                new RewardItem("BoneFragments", 6, 12),
            }},
            { BiomeTier.BlackForest, new[] {
                new RewardItem("FineWood", 10, 20),
                new RewardItem("SurtlingCore", 2, 4),
                new RewardItem("BoneFragments", 8, 14),
                new RewardItem("TrollHide", 3, 6),
                new RewardItem("Thistle", 5, 10),
                new RewardItem("GreydwarfEye", 6, 12),
            }},
            { BiomeTier.Swamp, new[] {
                new RewardItem("ElderBark", 8, 15),
                new RewardItem("Guck", 4, 8),
                new RewardItem("Bloodbag", 4, 8),
                new RewardItem("Entrails", 5, 10),
                new RewardItem("WitheredBone", 2, 4),
                new RewardItem("Thistle", 6, 12),
            }},
            { BiomeTier.Mountain, new[] {
                new RewardItem("WolfPelt", 3, 6),
                new RewardItem("FreezeGland", 4, 8),
                new RewardItem("Obsidian", 6, 12),
                new RewardItem("DragonTear", 1, 1),
                new RewardItem("JuteRed", 2, 4),
            }},
            { BiomeTier.Plains, new[] {
                new RewardItem("Needle", 6, 12),
                new RewardItem("Flax", 6, 10),
                new RewardItem("Barley", 8, 14),
                new RewardItem("LoxPelt", 2, 5),
                new RewardItem("Tar", 5, 10),
            }},
            { BiomeTier.Mistlands, new[] {
                new RewardItem("Softtissue", 5, 10),
                new RewardItem("RoyalJelly", 3, 6),
                new RewardItem("ScaleHide", 4, 8),
                new RewardItem("YggdrasilWood", 8, 14),
                new RewardItem("DvergrNeedle", 2, 4),
            }},
            { BiomeTier.Ashlands, new[] {
                new RewardItem("Grausten", 8, 14),
                new RewardItem("CharredBone", 5, 10),
                new RewardItem("AskHide", 3, 6),
                new RewardItem("MorgenSinew", 1, 3),
                new RewardItem("BellFragment", 1, 2),
            }},
        };

        // ── CONSUMABLES (food, mead, potions) ──
        // Smaller stacks that feel like a welcome resupply, not a hoard
        public static readonly Dictionary<BiomeTier, RewardItem[]> ConsumablePool = new Dictionary<BiomeTier, RewardItem[]>
        {
            { BiomeTier.Meadows, new[] {
                new RewardItem("CookedMeat", 3, 6),
                new RewardItem("CookedDeerMeat", 3, 5),
                new RewardItem("QueensJam", 3, 5),
                new RewardItem("Honey", 3, 6),
                new RewardItem("MeadHealthMinor", 3, 5),
                new RewardItem("MeadStaminaMinor", 3, 5),
            }},
            { BiomeTier.BlackForest, new[] {
                new RewardItem("CarrotSoup", 3, 5),
                new RewardItem("MeadPoisonResist", 3, 5),
                new RewardItem("MeadHealthMedium", 3, 5),
                new RewardItem("Sausages", 3, 6),
                new RewardItem("DeerStew", 3, 5),
            }},
            { BiomeTier.Swamp, new[] {
                new RewardItem("MeadPoisonResist", 4, 6),
                new RewardItem("MeadStaminaMedium", 3, 5),
                new RewardItem("TurnipStew", 3, 5),
                new RewardItem("Sausages", 4, 6),
                new RewardItem("BlackSoup", 3, 5),
                new RewardItem("MeadHealthMedium", 3, 6),
            }},
            { BiomeTier.Mountain, new[] {
                new RewardItem("WolfSkewer", 3, 5),
                new RewardItem("EyeScream", 3, 5),
                new RewardItem("MeadFrostResist", 3, 5),
                new RewardItem("MeadStaminaMedium", 4, 6),
                new RewardItem("OnionSoup", 3, 5),
                new RewardItem("MeadHealthMajor", 2, 4),
            }},
            { BiomeTier.Plains, new[] {
                new RewardItem("LoxPie", 2, 4),
                new RewardItem("BloodPudding", 2, 4),
                new RewardItem("FishAndBread", 2, 4),
                new RewardItem("MeadHealthMajor", 3, 5),
                new RewardItem("MeadStaminaLingering", 3, 5),
                new RewardItem("Bread", 3, 6),
            }},
            { BiomeTier.Mistlands, new[] {
                new RewardItem("YggdrasilPorridge", 2, 4),
                new RewardItem("SeekerAspic", 2, 4),
                new RewardItem("MagicallyStuffedShroom", 2, 4),
                new RewardItem("MeadEitrMinor", 3, 5),
                new RewardItem("MushroomOmelette", 2, 4),
                new RewardItem("MisthareSupreme", 2, 4),
            }},
            { BiomeTier.Ashlands, new[] {
                new RewardItem("MeatPlatter", 2, 4),
                new RewardItem("ScorchingMedley", 2, 4),
                new RewardItem("FierySvinstew", 2, 3),
                new RewardItem("MeadEitrLingering", 2, 4),
                new RewardItem("PiquantPie", 2, 3),
                new RewardItem("SparklingShroomshake", 2, 3),
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
