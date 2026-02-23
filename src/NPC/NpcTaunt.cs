using UnityEngine;

namespace HaldorBounties
{
    /// <summary>
    /// Gives bounty NPCs floating speech bubbles during combat,
    /// using the same Chat.SetNpcText API as Haldor and Dvergr NPCs.
    /// </summary>
    public class NpcTaunt : MonoBehaviour
    {
        private Character _character;
        private MonsterAI _ai;
        private float _lastTauntTime;
        private float _nextTauntInterval;
        private bool _hasAggrod;
        private float _lastHealth;

        private static readonly string[] AggroLines =
        {
            "You dare challenge me?",
            "Odin sends another fool!",
            "Your grave awaits, Viking!",
            "Finally, a worthy opponent!",
            "You picked the wrong fight!",
            "I've been waiting for this!",
            "Come, let us see your steel!",
            "Another bounty hunter? Pathetic.",
            "You won't leave here alive!",
            "The Valkyries won't find you here!",
        };

        private static readonly string[] CombatLines =
        {
            "Feel my steel!",
            "Stand and fight!",
            "Is that all you've got?",
            "You fight like a Greyling!",
            "Odin watches us fight!",
            "I'll wear your bones!",
            "Too slow!",
            "My blade hungers!",
            "You cannot defeat me!",
            "Fight harder, coward!",
        };

        private static readonly string[] HitLines =
        {
            "A lucky strike!",
            "You'll pay for that!",
            "Barely a scratch!",
            "Ha! I've had worse!",
            "That almost hurt!",
            "Now I'm angry!",
        };

        private static readonly string[] DeathLines =
        {
            "This isn't... over...",
            "Valhalla... awaits...",
            "Well fought...",
            "I'll return... stronger...",
            "Odin... take me...",
            "A worthy... death...",
        };

        private void Start()
        {
            _character = GetComponent<Character>();
            _ai = GetComponent<MonsterAI>();
            _nextTauntInterval = Random.Range(8f, 14f);

            if (_character != null)
                _lastHealth = _character.GetHealth();
        }

        private void Update()
        {
            if (_character == null || _ai == null || _character.IsDead()) return;
            if (Chat.instance == null) return;

            var target = _ai.GetTargetCreature();

            // Aggro line — first time we acquire a target
            if (target != null && !_hasAggrod)
            {
                _hasAggrod = true;
                Say(AggroLines[Random.Range(0, AggroLines.Length)]);
                return;
            }

            // Reset aggro state if target lost
            if (target == null)
            {
                _hasAggrod = false;
                return;
            }

            // Hit reaction — check if health dropped since last frame
            float currentHealth = _character.GetHealth();
            if (currentHealth < _lastHealth && Random.value < 0.2f)
            {
                if (Time.time - _lastTauntTime > 5f)
                    Say(HitLines[Random.Range(0, HitLines.Length)]);
            }
            _lastHealth = currentHealth;

            // Periodic combat taunt
            if (Time.time - _lastTauntTime > _nextTauntInterval)
            {
                Say(CombatLines[Random.Range(0, CombatLines.Length)]);
                _nextTauntInterval = Random.Range(8f, 14f);
            }
        }

        /// <summary>Called externally (e.g., from a death patch) to show a death line.</summary>
        public void SayDeathLine()
        {
            if (Chat.instance != null)
                Say(DeathLines[Random.Range(0, DeathLines.Length)]);
        }

        private void Say(string text)
        {
            if (Time.time - _lastTauntTime < 3f) return; // cooldown
            _lastTauntTime = Time.time;

            string name = _character != null ? _character.m_name : "";
            Chat.instance.SetNpcText(gameObject, Vector3.up * 2f, 20f, 4f, name, text, false);
        }
    }
}
