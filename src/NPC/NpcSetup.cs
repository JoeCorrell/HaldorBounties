using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace HaldorBounties
{
    public class NpcSetup : MonoBehaviour
    {
        public int TierIndex;

        /// <summary>Set before Instantiate to force male(1) or female(2) model. 0=random. Reset after use.</summary>
        public static int ForceGender;

        private static readonly string[] Beards = new string[]
        {
            "Beard2", "Beard3", "Beard4", "Beard5", "Beard6", "Beard7", "Beard8", "Beard9", "Beard10",
            "Beard11", "Beard12", "Beard13", "Beard14", "Beard15", "Beard16", "Beard17", "Beard18",
            "Beard19", "Beard20", "Beard21"
        };

        private static readonly string[] Hairs = new string[]
        {
            "Hair1", "Hair2", "Hair3", "Hair4", "Hair5", "Hair6", "Hair7", "Hair8", "Hair9", "Hair10",
            "Hair11", "Hair12", "Hair13", "Hair14", "Hair15", "Hair16", "Hair17", "Hair18", "Hair19",
            "Hair20", "Hair21", "Hair22", "Hair23", "Hair24", "Hair25", "Hair26", "Hair27", "Hair28",
            "Hair29", "Hair30", "Hair31"
        };

        // ZDO hash keys
        private static readonly int EquippedHash    = StringExtensionMethods.GetStableHashCode("HB_NpcEquipped");
        private static readonly int CombatStyleHash = StringExtensionMethods.GetStableHashCode("HB_CombatStyle");
        private static readonly int WeaponIdxHash   = StringExtensionMethods.GetStableHashCode("HB_WeaponIdx");
        private static readonly int ShieldIdxHash   = StringExtensionMethods.GetStableHashCode("HB_ShieldIdx");
        private static readonly int ArmorIdxHash    = StringExtensionMethods.GetStableHashCode("HB_ArmorIdx");

        // Cached reflection for private VisEquipment members
        private static readonly FieldInfo _visNviewField          = AccessTools.Field(typeof(VisEquipment), "m_nview");
        private static readonly FieldInfo _leftItemInstanceField  = AccessTools.Field(typeof(VisEquipment), "m_leftItemInstance");
        private static readonly FieldInfo _rightItemInstanceField = AccessTools.Field(typeof(VisEquipment), "m_rightItemInstance");
        private static readonly MethodInfo _setRightHandEquipped  = AccessTools.Method(typeof(VisEquipment), "SetRightHandEquipped");
        private static readonly MethodInfo _setLeftHandEquipped   = AccessTools.Method(typeof(VisEquipment), "SetLeftHandEquipped");
        private static readonly MethodInfo _updateEquipVisuals    = AccessTools.Method(typeof(VisEquipment), "UpdateEquipmentVisuals");

        private ZNetView _nview;
        private VisEquipment _visEquip;
        private Humanoid _humanoid;

        private void Awake()
        {
            _nview = GetComponent<ZNetView>();
            _visEquip = GetComponent<VisEquipment>();
            _humanoid = GetComponent<Humanoid>();

            if (_nview == null || _nview.GetZDO() == null) return;
            if (TierIndex < 0 || TierIndex >= NpcTierData.Tiers.Length) return;

            var tier = NpcTierData.Tiers[TierIndex];
            bool firstSpawn = !_nview.GetZDO().GetBool(EquippedHash, false);

            if (firstSpawn)
                SetupVisuals();

            SetupEquipment(tier, firstSpawn);

            // GiveDefaultItems processes m_randomWeapon/m_randomShield/m_randomSets
            // into actual inventory items so the AI can fight with them.
            // Must be called AFTER SetupEquipment sets the m_random* fields.
            _humanoid.GiveDefaultItems();

            _updateEquipVisuals?.Invoke(_visEquip, null);

            if (firstSpawn)
                _nview.GetZDO().Set(EquippedHash, true);
        }

        private void SetupVisuals()
        {
            _visNviewField?.SetValue(_visEquip, _nview);

            float skinBrightness = 0.2f + Random.Range(0f, 0.8f);
            Color hairColor = Color.HSVToRGB(0.13f + Random.Range(0f, 0.03f), Random.value, Random.value);

            // ForceGender: 1=male(model 0), 2=female(model 1), 0=random
            int model = ForceGender == 1 ? 0 : ForceGender == 2 ? 1 : Random.Range(0, 2);
            ForceGender = 0; // reset after use
            _visEquip.SetModel(model);

            string hair = Hairs[Random.Range(0, Hairs.Length)];
            _visEquip.SetHairItem(hair);
            _visEquip.SetHairColor(new Vector3(hairColor.r, hairColor.g, hairColor.b));
            _visEquip.SetSkinColor(new Vector3(skinBrightness, skinBrightness, skinBrightness));

            if (model == 0)
            {
                string beard = Beards[Random.Range(0, Beards.Length)];
                _visEquip.SetBeardItem(beard);
            }
        }

        private void SetupEquipment(TierData tier, bool firstSpawn)
        {
            int combatStyle, weaponIdx, shieldIdx, armorIdx;

            if (firstSpawn)
            {
                bool hasBow = tier.WeaponsBow.Length > 0;
                int roll = Random.Range(0, 10);

                if (!hasBow)
                    combatStyle = (roll < 5) ? 0 : 1;
                else
                    combatStyle = (roll < 2) ? 0 : (roll < 5) ? 1 : 2; // 20% 1H+shield, 30% 2H, 50% ranged

                string[] weaponArray = GetWeaponArray(tier, combatStyle);
                weaponIdx = Random.Range(0, weaponArray.Length);
                shieldIdx = Random.Range(0, tier.Shields.Length);
                armorIdx  = Random.Range(0, tier.ArmorSets.Length);

                _nview.GetZDO().Set(CombatStyleHash, combatStyle);
                _nview.GetZDO().Set(WeaponIdxHash, weaponIdx);
                _nview.GetZDO().Set(ShieldIdxHash, shieldIdx);
                _nview.GetZDO().Set(ArmorIdxHash, armorIdx);
            }
            else
            {
                combatStyle = _nview.GetZDO().GetInt(CombatStyleHash, 0);
                weaponIdx   = _nview.GetZDO().GetInt(WeaponIdxHash, 0);
                shieldIdx   = _nview.GetZDO().GetInt(ShieldIdxHash, 0);
                armorIdx    = _nview.GetZDO().GetInt(ArmorIdxHash, 0);
            }

            switch (combatStyle)
            {
                case 0: Equip1HAndShield(tier, weaponIdx, shieldIdx); break;
                case 1: Equip2H(tier, weaponIdx); break;
                case 2: EquipBow(tier, weaponIdx); break;
            }

            SetupArmor(tier, armorIdx);
        }

        private static string[] GetWeaponArray(TierData tier, int combatStyle)
        {
            switch (combatStyle)
            {
                case 0:  return tier.Weapons1H;
                case 1:  return tier.Weapons2H;
                case 2:  return tier.WeaponsBow;
                default: return tier.Weapons1H;
            }
        }

        /// <summary>Try to resolve a weapon prefab by name. If not found, try each name in the fallback array.</summary>
        private static GameObject ResolveWeapon(string primary, string[] fallbacks)
        {
            var prefab = ObjectDB.instance.GetItemPrefab(primary);
            if (prefab != null) return prefab;
            HaldorBounties.Log.LogWarning($"[NpcSetup] Weapon not found: {primary}, trying fallbacks");
            foreach (var name in fallbacks)
            {
                prefab = ObjectDB.instance.GetItemPrefab(name);
                if (prefab != null) return prefab;
            }
            return null;
        }

        private void Equip1HAndShield(TierData tier, int weaponIdx, int shieldIdx)
        {
            weaponIdx = Mathf.Clamp(weaponIdx, 0, tier.Weapons1H.Length - 1);
            shieldIdx = Mathf.Clamp(shieldIdx, 0, tier.Shields.Length - 1);

            var weaponPrefab = ResolveWeapon(tier.Weapons1H[weaponIdx], tier.Weapons1H);
            if (weaponPrefab != null)
            {
                _humanoid.m_randomWeapon = new GameObject[] { weaponPrefab };
                _visEquip.SetRightItem(weaponPrefab.name);
                _setRightHandEquipped?.Invoke(_visEquip, new object[] { StringExtensionMethods.GetStableHashCode(weaponPrefab.name) });
            }

            var shieldPrefab = ObjectDB.instance.GetItemPrefab(tier.Shields[shieldIdx]);
            if (shieldPrefab != null)
            {
                _humanoid.m_randomShield = new GameObject[] { shieldPrefab };
                _visEquip.SetLeftItem(shieldPrefab.name, 0);
                _setLeftHandEquipped?.Invoke(_visEquip, new object[] { StringExtensionMethods.GetStableHashCode(shieldPrefab.name), 0 });
            }
        }

        private void Equip2H(TierData tier, int weaponIdx)
        {
            weaponIdx = Mathf.Clamp(weaponIdx, 0, tier.Weapons2H.Length - 1);

            var weaponPrefab = ResolveWeapon(tier.Weapons2H[weaponIdx], tier.Weapons2H);
            if (weaponPrefab != null)
            {
                _humanoid.m_randomWeapon = new GameObject[] { weaponPrefab };
                _visEquip.SetRightItem(weaponPrefab.name);
                _setRightHandEquipped?.Invoke(_visEquip, new object[] { StringExtensionMethods.GetStableHashCode(weaponPrefab.name) });
            }

            _visEquip.SetLeftItem("", 0);
            var leftInstance = _leftItemInstanceField?.GetValue(_visEquip) as GameObject;
            if (leftInstance != null)
            {
                Object.Destroy(leftInstance);
                _leftItemInstanceField.SetValue(_visEquip, null);
            }
        }

        private void EquipBow(TierData tier, int weaponIdx)
        {
            weaponIdx = Mathf.Clamp(weaponIdx, 0, tier.WeaponsBow.Length - 1);

            // Try the selected bow; if HB_ clone is missing, fall back to any available bow,
            // then fall back to a 1H weapon so the NPC is never unarmed
            var weaponPrefab = ResolveWeapon(tier.WeaponsBow[weaponIdx], tier.WeaponsBow);
            if (weaponPrefab == null)
            {
                // Bow registration failed entirely — give a 1H weapon instead
                HaldorBounties.Log.LogWarning("[NpcSetup] All bows failed, falling back to 1H weapon");
                weaponPrefab = ResolveWeapon(tier.Weapons1H[0], tier.Weapons1H);
                if (weaponPrefab != null)
                {
                    _humanoid.m_randomWeapon = new GameObject[] { weaponPrefab };
                    _visEquip.SetRightItem(weaponPrefab.name);
                    _setRightHandEquipped?.Invoke(_visEquip, new object[] { StringExtensionMethods.GetStableHashCode(weaponPrefab.name) });
                }
                return;
            }

            _humanoid.m_randomWeapon = new GameObject[] { weaponPrefab };
            _visEquip.SetLeftItem(weaponPrefab.name, 0);
            _setLeftHandEquipped?.Invoke(_visEquip, new object[] { StringExtensionMethods.GetStableHashCode(weaponPrefab.name), 0 });

            _visEquip.SetRightItem("");
            var rightInstance = _rightItemInstanceField?.GetValue(_visEquip) as GameObject;
            if (rightInstance != null)
            {
                Object.Destroy(rightInstance);
                _rightItemInstanceField.SetValue(_visEquip, null);
            }
        }

        private void SetupArmor(TierData tier, int armorIdx)
        {
            armorIdx = Mathf.Clamp(armorIdx, 0, tier.ArmorSets.Length - 1);
            string[] armorNames = tier.ArmorSets[armorIdx];

            var items = new List<GameObject>();
            foreach (var name in armorNames)
            {
                var prefab = ObjectDB.instance.GetItemPrefab(name);
                if (prefab != null)
                    items.Add(prefab);
            }

            if (items.Count > 0)
            {
                var set = new Humanoid.ItemSet();
                set.m_items = items.ToArray();
                _humanoid.m_randomSets = new Humanoid.ItemSet[] { set };
            }
        }
    }
}
