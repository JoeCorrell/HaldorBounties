using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace HaldorBounties
{
    public static class NpcPrefabs
    {
        private static GameObject _container;
        private static readonly FieldInfo _namedPrefabsField = AccessTools.Field(typeof(ZNetScene), "m_namedPrefabs");

        public static void Init(ZNetScene zNetScene)
        {
            if (_container == null)
            {
                _container = new GameObject("HB_NpcPrefabs");
                Object.DontDestroyOnLoad(_container);
                _container.SetActive(false);
            }

            // Create NPC-friendly weapon clones before tier prefabs (tiers reference them)
            NpcWeapons.Init(zNetScene);

            for (int i = 0; i < NpcTierData.Tiers.Length; i++)
            {
                CreateTierPrefab(zNetScene, NpcTierData.Tiers[i], i);
            }

            HaldorBounties.Log.LogInfo($"[NpcPrefabs] Registered {NpcTierData.Tiers.Length} bounty NPC prefabs.");
        }

        private static void CreateTierPrefab(ZNetScene zNetScene, TierData tier, int tierIndex)
        {
            if (zNetScene.GetPrefab(tier.PrefabName) != null)
                return;

            GameObject playerPrefab = zNetScene.GetPrefab("Player");
            if (playerPrefab == null)
            {
                HaldorBounties.Log.LogError("[NpcPrefabs] Player prefab not found!");
                return;
            }

            GameObject go = Object.Instantiate(playerPrefab, _container.transform, false);
            go.name = tier.PrefabName;
            go.transform.localScale = new Vector3(tier.ScaleX, tier.ScaleY, tier.ScaleZ);

            // Strip player-specific components
            DestroyComponent<PlayerController>(go);
            DestroyComponent<Player>(go);
            DestroyComponent<Talker>(go);
            DestroyComponent<Skills>(go);

            // Humanoid (Character base)
            Humanoid humanoid = go.AddComponent<Humanoid>();
            SetupHumanoid(humanoid, zNetScene, tier);

            // MonsterAI (combat behavior)
            MonsterAI ai = go.AddComponent<MonsterAI>();
            SetupMonsterAI(ai, zNetScene, tier);

            // Networking
            ZNetView nview = go.GetComponent<ZNetView>();
            nview.m_persistent = true;
            nview.m_distant = false;
            nview.m_type = (ZDO.ObjectType)0;
            nview.m_syncInitialScale = false;

            ZSyncTransform syncTransform = go.GetComponent<ZSyncTransform>();
            syncTransform.m_syncPosition = true;
            syncTransform.m_syncRotation = true;
            syncTransform.m_syncScale = false;
            syncTransform.m_syncBodyVelocity = false;
            syncTransform.m_characterParentSync = false;

            go.GetComponent<ZSyncAnimation>().m_smoothCharacterSpeeds = true;

            // Per-instance visual + equipment setup
            NpcSetup setup = go.AddComponent<NpcSetup>();
            setup.TierIndex = tierIndex;

            // Combat speech bubbles (same API as Haldor's NpcTalk)
            go.AddComponent<NpcTaunt>();

            // Idle animation variety
            go.AddComponent<RandomAnimation>().m_values = new List<RandomAnimation.RandomValue>
            {
                new RandomAnimation.RandomValue
                {
                    m_name = "idle",
                    m_values = 5,
                    m_interval = 3f,
                    m_floatValue = false,
                    m_floatTransition = 1f
                }
            };

            // Physics
            go.GetComponent<Rigidbody>().mass = 50f;

            // Register with ZNetScene
            int hash = StringExtensionMethods.GetStableHashCode(go.name);
            zNetScene.m_prefabs.Add(go);
            var namedPrefabs = _namedPrefabsField.GetValue(zNetScene) as Dictionary<int, GameObject>;
            namedPrefabs[hash] = go;

            HaldorBounties.Log.LogInfo($"[NpcPrefabs] Registered prefab: {tier.PrefabName}");
        }

        private static void SetupHumanoid(Humanoid humanoid, ZNetScene zNetScene, TierData tier)
        {
            // Identity
            ((Character)humanoid).m_name = tier.PrefabName;
            ((Character)humanoid).m_group = tier.PrefabName;
            ((Character)humanoid).m_faction = (Character.Faction)8; // Boss
            ((Character)humanoid).m_boss = true;
            ((Character)humanoid).m_dontHideBossHud = false;
            ((Character)humanoid).m_bossEvent = "";

            // Movement
            ((Character)humanoid).m_crouchSpeed = 2f;
            ((Character)humanoid).m_walkSpeed = tier.WalkSpeed;
            ((Character)humanoid).m_speed = 2f;
            ((Character)humanoid).m_turnSpeed = 300f;
            ((Character)humanoid).m_runSpeed = tier.RunSpeed;
            ((Character)humanoid).m_runTurnSpeed = 300f;
            ((Character)humanoid).m_acceleration = 0.6f;
            ((Character)humanoid).m_jumpForce = 8f;
            ((Character)humanoid).m_jumpForceForward = 2f;
            ((Character)humanoid).m_jumpForceTiredFactor = 0.6f;

            // Swimming
            ((Character)humanoid).m_canSwim = true;
            ((Character)humanoid).m_swimDepth = 1f;
            ((Character)humanoid).m_swimSpeed = 2f;
            ((Character)humanoid).m_swimTurnSpeed = 100f;
            ((Character)humanoid).m_swimAcceleration = 0.05f;

            // Ground
            ((Character)humanoid).m_groundTilt = (Character.GroundTiltType)0;
            ((Character)humanoid).m_groundTiltSpeed = 50f;
            ((Character)humanoid).m_eye = Utils.FindChild(humanoid.gameObject.transform, "EyePos");

            // Health
            ((Character)humanoid).m_health = tier.Health;
            ((Character)humanoid).m_tolerateWater = true;
            ((Character)humanoid).m_staggerWhenBlocked = true;
            ((Character)humanoid).m_staggerDamageFactor = 0f;

            // Hit effects
            ((Character)humanoid).m_hitEffects.m_effectPrefabs = new EffectList.EffectData[]
            {
                new EffectList.EffectData { m_prefab = zNetScene.GetPrefab("vfx_player_hit"), m_enabled = true, m_variant = -1 },
                new EffectList.EffectData { m_prefab = zNetScene.GetPrefab("sfx_hit"), m_enabled = true, m_variant = -1 }
            };
            ((Character)humanoid).m_critHitEffects.m_effectPrefabs = new EffectList.EffectData[]
            {
                new EffectList.EffectData { m_prefab = zNetScene.GetPrefab("fx_crit"), m_enabled = true, m_variant = -1 }
            };
            ((Character)humanoid).m_backstabHitEffects.m_effectPrefabs = new EffectList.EffectData[]
            {
                new EffectList.EffectData { m_prefab = zNetScene.GetPrefab("fx_backstab"), m_enabled = true, m_variant = -1 }
            };
            ((Character)humanoid).m_deathEffects.m_effectPrefabs = new EffectList.EffectData[]
            {
                new EffectList.EffectData { m_prefab = zNetScene.GetPrefab("vfx_ghost_death"), m_enabled = true, m_variant = -1 },
                new EffectList.EffectData { m_prefab = zNetScene.GetPrefab("sfx_ghost_death"), m_enabled = true, m_variant = -1 }
            };

            // Damage modifiers: immune to chop, pickaxe, spirit
            ((Character)humanoid).m_damageModifiers = new HitData.DamageModifiers
            {
                m_blunt = (HitData.DamageModifier)0,
                m_slash = (HitData.DamageModifier)0,
                m_pierce = (HitData.DamageModifier)0,
                m_chop = (HitData.DamageModifier)3,
                m_pickaxe = (HitData.DamageModifier)3,
                m_fire = (HitData.DamageModifier)0,
                m_frost = (HitData.DamageModifier)0,
                m_lightning = (HitData.DamageModifier)0,
                m_spirit = (HitData.DamageModifier)3
            };
        }

        private static void SetupMonsterAI(MonsterAI ai, ZNetScene zNetScene, TierData tier)
        {
            // Detection — extremely aggressive, wide vision
            ((BaseAI)ai).m_viewRange = 80f;
            ((BaseAI)ai).m_viewAngle = 120f;
            ((BaseAI)ai).m_hearRange = 9999f;
            ((BaseAI)ai).m_mistVision = true;

            // Alert sound
            ((BaseAI)ai).m_alertedEffects.m_effectPrefabs = new EffectList.EffectData[]
            {
                new EffectList.EffectData { m_prefab = zNetScene.GetPrefab("sfx_dverger_vo_alerted"), m_enabled = true, m_variant = -1 }
            };
            ((BaseAI)ai).m_idleSound.m_effectPrefabs = new EffectList.EffectData[]
            {
                new EffectList.EffectData { m_prefab = zNetScene.GetPrefab("sfx_dverger_vo_idle"), m_enabled = true, m_variant = 0 }
            };
            ((BaseAI)ai).m_idleSoundInterval = 10f;
            ((BaseAI)ai).m_idleSoundChance = 0.5f;

            // Pathfinding
            ((BaseAI)ai).m_pathAgentType = (Pathfinding.AgentType)2;
            ((BaseAI)ai).m_moveMinAngle = 90f;
            ((BaseAI)ai).m_smoothMovement = true;
            ((BaseAI)ai).m_serpentMovement = false;
            ((BaseAI)ai).m_jumpInterval = 0f;
            ((BaseAI)ai).m_randomCircleInterval = 2f;
            ((BaseAI)ai).m_randomMoveInterval = 10f;
            ((BaseAI)ai).m_randomMoveRange = 10f;

            // Behavior
            ((BaseAI)ai).m_avoidFire = false;
            ((BaseAI)ai).m_afraidOfFire = false;
            ((BaseAI)ai).m_avoidWater = true;
            ((BaseAI)ai).m_aggravatable = false;

            // Combat — relentless aggression, hunt players, never flee
            ai.m_alertRange = 9999f;
            ai.m_fleeIfHurtWhenTargetCantBeReached = false;
            ai.m_fleeIfNotAlerted = false;
            ai.m_fleeIfLowHealth = 0f;
            ai.m_circulateWhileCharging = true;
            ai.m_circulateWhileChargingFlying = false;
            ai.m_enableHuntPlayer = true;
            ai.m_attackPlayerObjects = true;
            ai.m_privateAreaTriggerTreshold = 1;
            ai.m_interceptTimeMax = 4f;
            ai.m_interceptTimeMin = 0f;
            ai.m_maxChaseDistance = 0f;
            ai.m_minAttackInterval = tier.AttackInterval;
            ai.m_circleTargetInterval = 3f;
            ai.m_circleTargetDuration = 1f;
            ai.m_circleTargetDistance = 4f;
        }

        private static void DestroyComponent<T>(GameObject go) where T : Component
        {
            T comp = go.GetComponent<T>();
            if (comp != null)
                Object.DestroyImmediate(comp);
        }
    }
}
