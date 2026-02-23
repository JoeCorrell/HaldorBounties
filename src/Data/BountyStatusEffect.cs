using UnityEngine;

namespace HaldorBounties
{
    /// <summary>
    /// A non-expiring status effect that shows bounty progress on the player's HUD.
    /// One instance is created per active bounty and removed when it's completed or abandoned.
    /// </summary>
    public class BountyStatusEffect : StatusEffect
    {
        public string BountyId;
        private int _target;

        public void Setup(string bountyId, string title, int target, Sprite icon)
        {
            BountyId  = bountyId;
            _target   = target;
            m_name    = title;
            m_ttl     = 0f; // No time limit — removed explicitly
            m_icon    = icon;

            // base.name drives NameHash() — must be unique per bounty
            ((UnityEngine.Object)this).name = "HaldorBounty_" + bountyId;
        }

        public override string GetIconText()
        {
            if (BountyManager.Instance == null || string.IsNullOrEmpty(BountyId)) return "";
            int progress = BountyManager.Instance.GetProgress(BountyId);
            int clamped  = Mathf.Min(progress, _target);
            return $"{clamped}/{_target}";
        }

        public override string GetTooltipString()
        {
            if (BountyManager.Instance == null || string.IsNullOrEmpty(BountyId))
                return m_name;

            int progress = BountyManager.Instance.GetProgress(BountyId);
            var state    = BountyManager.Instance.GetState(BountyId);

            if (state == BountyState.Ready)
                return $"{m_name}\nCompleted! Return to Haldor to choose your reward.";

            return $"{m_name}\nProgress: {Mathf.Min(progress, _target)} / {_target}";
        }

        // Do not tick time — we manage lifetime manually.
        public override void UpdateStatusEffect(float dt) { }

        // Never auto-expire.
        public override bool IsDone() => false;
    }
}
