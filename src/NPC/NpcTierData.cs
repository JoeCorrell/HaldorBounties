namespace HaldorBounties
{
    public class TierData
    {
        public string PrefabName;
        public int Tier;
        public float Health;
        public float ScaleX, ScaleY, ScaleZ;
        public float RunSpeed;
        public float WalkSpeed;
        public float AttackInterval; // minimum seconds between attacks (higher = slower)
        public string[][] ArmorSets;
        public string[] Weapons1H;
        public string[] Weapons2H;
        public string[] WeaponsBow;
        public string[] Shields;
    }

    public static class NpcTierData
    {
        public static readonly TierData[] Tiers = new TierData[]
        {
            // T1 — Meadows (equipped with Black Forest gear — one tier up)
            new TierData
            {
                PrefabName = "HB_BountyNpc_T1",
                Tier = 1,
                Health = 500f,
                ScaleX = 1.25f, ScaleY = 1.25f, ScaleZ = 1.25f,
                RunSpeed = 4.5f,
                WalkSpeed = 1.5f,
                AttackInterval = 4f,
                ArmorSets = new[]
                {
                    new[] { "ArmorBronzeChest", "ArmorBronzeLegs", "HelmetBronze" },
                    new[] { "ArmorTrollLeatherChest", "ArmorTrollLeatherLegs", "HelmetTrollLeather", "CapeTrollHide" },
                },
                Weapons1H  = new[] { "AxeBronze", "MaceBronze", "SwordBronze", "SpearBronze", "KnifeCopper" },
                Weapons2H  = new[] { "AtgeirBronze", "SledgeStagbreaker" },
                WeaponsBow = new[] { "HB_BowFineWood" },
                Shields    = new[] { "ShieldBronzeBuckler", "ShieldBoneTower" },
            },

            // T2 — Black Forest (equipped with Swamp gear — one tier up)
            new TierData
            {
                PrefabName = "HB_BountyNpc_T2",
                Tier = 2,
                Health = 900f,
                ScaleX = 1.3f, ScaleY = 1.3f, ScaleZ = 1.3f,
                RunSpeed = 5f,
                WalkSpeed = 1.6f,
                AttackInterval = 3f,
                ArmorSets = new[]
                {
                    new[] { "ArmorIronChest", "ArmorIronLegs", "HelmetIron", "CapeTrollHide" },
                    new[] { "ArmorRootChest", "ArmorRootLegs", "HelmetRoot" },
                },
                Weapons1H  = new[] { "AxeIron", "MaceIron", "SwordIron", "SwordIronFire", "SpearElderbark" },
                Weapons2H  = new[] { "SledgeIron", "AtgeirIron", "Battleaxe" },
                WeaponsBow = new[] { "HB_BowHuntsman" },
                Shields    = new[] { "ShieldIronBuckler", "ShieldIronTower" },
            },

            // T3 — Swamp (equipped with Mountain gear — one tier up)
            new TierData
            {
                PrefabName = "HB_BountyNpc_T3",
                Tier = 3,
                Health = 1800f,
                ScaleX = 1.35f, ScaleY = 1.35f, ScaleZ = 1.35f,
                RunSpeed = 5.5f,
                WalkSpeed = 1.6f,
                AttackInterval = 2.5f,
                ArmorSets = new[]
                {
                    new[] { "ArmorWolfChest", "ArmorWolfLegs", "HelmetDrake", "CapeWolf" },
                    new[] { "ArmorFenringChest", "ArmorFenringLegs", "HelmetDrake", "CapeWolf" },
                },
                Weapons1H  = new[] { "KnifeSilver", "MaceSilver", "SwordSilver", "SpearWolfFang" },
                Weapons2H  = new[] { "FistFenrirClaw", "BattleaxeCrystal" },
                WeaponsBow = new[] { "HB_BowDraugrFang" },
                Shields    = new[] { "ShieldSilver", "ShieldSerpentscale" },
            },

            // T4 — Mountain (equipped with Plains gear — one tier up)
            new TierData
            {
                PrefabName = "HB_BountyNpc_T4",
                Tier = 4,
                Health = 3500f,
                ScaleX = 1.4f, ScaleY = 1.4f, ScaleZ = 1.4f,
                RunSpeed = 5.5f,
                WalkSpeed = 1.6f,
                AttackInterval = 2f,
                ArmorSets = new[]
                {
                    new[] { "ArmorPaddedCuirass", "ArmorPaddedGreaves", "HelmetPadded", "CapeLinen" },
                    new[] { "ArmorPaddedCuirass", "ArmorPaddedGreaves", "HelmetPadded" },
                },
                Weapons1H  = new[] { "AxeBlackMetal", "KnifeBlackMetal", "SwordBlackmetal", "MaceNeedle" },
                Weapons2H  = new[] { "AtgeirBlackmetal" },
                WeaponsBow = new[] { "HB_BowDraugrFang" },
                Shields    = new[] { "ShieldBlackmetal", "ShieldBlackmetalTower" },
            },

            // T5 — Plains (equipped with Mistlands gear — one tier up)
            new TierData
            {
                PrefabName = "HB_BountyNpc_T5",
                Tier = 5,
                Health = 6000f,
                ScaleX = 1.45f, ScaleY = 1.45f, ScaleZ = 1.45f,
                RunSpeed = 6f,
                WalkSpeed = 1.6f,
                AttackInterval = 1.5f,
                ArmorSets = new[]
                {
                    new[] { "ArmorCarapaceChest", "ArmorCarapaceLegs", "HelmetCarapace", "CapeFeather" },
                    new[] { "ArmorMageChest", "ArmorMageLegs", "HelmetMage", "CapeFeather" },
                },
                Weapons1H  = new[] { "AxeJotunBane", "SwordMistwalker" },
                Weapons2H  = new[] { "THSwordKrom", "SledgeDemolisher", "AtgeirHimminAfl" },
                WeaponsBow = new[] { "HB_BowDraugrFang", "HB_CrossbowArbalest" },
                Shields    = new[] { "ShieldCarapace", "ShieldCarapaceBuckler" },
            },

            // T6 — Mistlands (equipped with Ashlands gear — one tier up)
            new TierData
            {
                PrefabName = "HB_BountyNpc_T6",
                Tier = 6,
                Health = 9000f,
                ScaleX = 1.5f, ScaleY = 1.5f, ScaleZ = 1.5f,
                RunSpeed = 6.5f,
                WalkSpeed = 1.8f,
                AttackInterval = 1f,
                ArmorSets = new[]
                {
                    new[] { "ArmorFlametalChest", "ArmorFlametalLegs", "HelmetFlametal" },
                    new[] { "ArmorFlametalChest", "ArmorFlametalLegs", "HelmetFlametal" },
                },
                Weapons1H  = new[] { "AxeBerzerkr", "MaceEldner", "KnifeSkollAndHati", "SpearSplitner" },
                Weapons2H  = new[] { "THSwordSlayer" },
                WeaponsBow = new[] { "HB_BowSpineSnap", "HB_CrossbowRipper" },
                Shields    = new[] { "ShieldFlametal", "ShieldFlametalTower" },
            },
        };
    }
}
