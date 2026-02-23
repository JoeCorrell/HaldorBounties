using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace HaldorBounties
{
    /// <summary>
    /// Registers NPC-usable variants of vanilla ranged weapons.
    /// Vanilla bows require ammo and stamina which AI-controlled humanoids cannot provide.
    /// Each clone strips ammo requirements and assigns a direct-fire projectile.
    /// </summary>
    public static class NpcWeapons
    {
        private static GameObject _weaponRoot;
        private static readonly FieldInfo _itemHashMap = AccessTools.Field(typeof(ObjectDB), "m_itemByHash");

        private struct WeaponDef
        {
            public string Source;
            public string Projectile;
            public float Range;
            public float Interval;
            public float Accuracy;
            public float MaxAngle;
            public bool IsCrossbow;
        }

        private static readonly WeaponDef[] Definitions =
        {
            // Bows — use draugr arrow projectile, long range, slower fire rate
            new WeaponDef { Source = "BowFineWood",   Projectile = "draugr_bow_projectile", Range = 25f, Interval = 5f, Accuracy = 1f,  MaxAngle = 15f, IsCrossbow = false },
            new WeaponDef { Source = "BowHuntsman",   Projectile = "draugr_bow_projectile", Range = 25f, Interval = 5f, Accuracy = 1f,  MaxAngle = 15f, IsCrossbow = false },
            new WeaponDef { Source = "BowDraugrFang", Projectile = "draugr_bow_projectile", Range = 30f, Interval = 4f, Accuracy = 0.5f, MaxAngle = 12f, IsCrossbow = false },
            new WeaponDef { Source = "BowSpineSnap",  Projectile = "draugr_bow_projectile", Range = 30f, Interval = 4f, Accuracy = 0.5f, MaxAngle = 12f, IsCrossbow = false },

            // Crossbows — use dverger bolt projectile, shorter range, faster fire rate
            new WeaponDef { Source = "CrossbowArbalest", Projectile = "DvergerArbalest_projectile", Range = 18f, Interval = 3f, Accuracy = 2f, MaxAngle = 20f, IsCrossbow = true },
            new WeaponDef { Source = "CrossbowRipper",  Projectile = "DvergerArbalest_projectile", Range = 18f, Interval = 3f, Accuracy = 2f, MaxAngle = 20f, IsCrossbow = true },
        };

        public static void Init(ZNetScene zNetScene)
        {
            if (_weaponRoot == null)
            {
                _weaponRoot = new GameObject("HB_NpcWeapons");
                Object.DontDestroyOnLoad(_weaponRoot);
                _weaponRoot.SetActive(false);
            }

            var hashMap = _itemHashMap?.GetValue(ObjectDB.instance) as Dictionary<int, GameObject>;
            if (hashMap == null)
            {
                HaldorBounties.Log.LogError("[NpcWeapons] ObjectDB hash map unavailable.");
                return;
            }

            int registered = 0;
            foreach (var def in Definitions)
            {
                if (RegisterWeaponClone(def, zNetScene, hashMap))
                    registered++;
            }

            HaldorBounties.Log.LogInfo($"[NpcWeapons] Registered {registered} NPC ranged weapon variants.");
        }

        private static bool RegisterWeaponClone(WeaponDef def, ZNetScene zNetScene, Dictionary<int, GameObject> hashMap)
        {
            string cloneName = "HB_" + def.Source;
            int hash = cloneName.GetStableHashCode();
            if (hashMap.ContainsKey(hash))
                return false;

            var source = ObjectDB.instance.GetItemPrefab(def.Source);
            if (source == null)
            {
                HaldorBounties.Log.LogWarning($"[NpcWeapons] Missing source: {def.Source}");
                return false;
            }

            var clone = Object.Instantiate(source, _weaponRoot.transform);
            clone.name = cloneName;

            var drop = clone.GetComponent<ItemDrop>();
            if (drop == null) return false;

            var data = drop.m_itemData.m_shared;

            // Strip ammo/stamina requirements so AI can fire freely
            data.m_ammoType = "";

            if (def.IsCrossbow)
                ConfigureCrossbow(data, def);
            else
                ConfigureBow(data, def);

            // Assign projectile
            var projectile = zNetScene.GetPrefab(def.Projectile);
            if (projectile != null)
                data.m_attack.m_attackProjectile = projectile;

            // AI targeting parameters
            data.m_aiAttackRange = def.Range;
            data.m_aiAttackRangeMin = def.IsCrossbow ? 3f : 0f;
            data.m_aiAttackInterval = def.Interval;
            data.m_aiAttackMaxAngle = def.MaxAngle;

            ObjectDB.instance.m_items.Add(clone);
            hashMap[hash] = clone;

            HaldorBounties.Log.LogInfo($"[NpcWeapons] Registered: {cloneName}");
            return true;
        }

        private static void ConfigureBow(ItemDrop.ItemData.SharedData data, WeaponDef def)
        {
            // Disable the draw mechanic — NPC humanoids cannot hold/release attack input.
            // Keep the draw animation state and attack animation from the vanilla bow
            // so the NPC still plays the bow fire animation visually.
            data.m_attack.m_bowDraw = false;
            data.m_attack.m_drawStaminaDrain = 0f;
            data.m_attack.m_speedFactor = 0.2f;
            data.m_attack.m_speedFactorRotation = 0.4f;
            data.m_attack.m_projectileAccuracy = def.Accuracy;
            data.m_attack.m_useCharacterFacingYAim = true;
        }

        private static void ConfigureCrossbow(ItemDrop.ItemData.SharedData data, WeaponDef def)
        {
            // Disable reload mechanic — NPCs cannot perform the reload sequence
            data.m_attack.m_requiresReload = false;
            data.m_attack.m_reloadStaminaDrain = 0f;
            data.m_attack.m_speedFactor = 0.1f;
            data.m_attack.m_speedFactorRotation = 0.4f;
            data.m_attack.m_projectileVel = 80f;
            data.m_attack.m_projectileAccuracy = def.Accuracy;
            data.m_attack.m_useCharacterFacingYAim = true;
        }
    }
}
