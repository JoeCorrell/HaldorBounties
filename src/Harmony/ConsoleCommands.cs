using System.Collections.Generic;
using HarmonyLib;

namespace HaldorBounties
{
    [HarmonyPatch(typeof(Terminal), "InitTerminal")]
    public static class ConsoleCommands
    {
        [HarmonyPostfix]
        private static void Postfix()
        {
            new Terminal.ConsoleCommand("BountyReset",
                "Reset all bounty progress, active bounties, and claimed states.",
                (Terminal.ConsoleEventArgs args) =>
                {
                    var player = Player.m_localPlayer;
                    if (player == null)
                    {
                        args.Context.AddString("No player found.");
                        return;
                    }

                    int removed = BountyManager.Instance?.ResetAll(player) ?? 0;
                    args.Context.AddString($"Bounty reset complete. Cleared {removed} bounty entries.");
                    ((Character)player).Message(MessageHud.MessageType.Center, "All bounties have been reset!");
                });
        }
    }
}
