using MCG_AutoPlay.Actions;
using MCG_AutoPlay.Core;

namespace MCG_AutoPlay.AI;

/// <summary>
/// Mengambil GameState dan menghasilkan Decision. Murni logika, tanpa akses game.
/// (PRD Phase 7/8: Economy AI).
///
/// Aturan awal (PRD Phase 8):
///   Gold >= 30          -> prioritaskan interest (simpan)
///   HP rendah           -> belanjakan gold
///   Board kuat          -> simpan
///   Board lemah         -> roll
///   Dekat level breakpoint -> pertimbangkan level up
/// </summary>
internal static class DecisionEngine
{
    private const int InterestThreshold = 30;
    private const int LowHpThreshold = 30;

    internal static Decision Decide(GameState state)
    {
        var player = state.Player;
        var reason = "";

        // Cari hero yang cocok di shop untuk dibeli (placeholder sederhana:
        // beli hero cost tinggi saat gold melimpah).
        if (player.Gold >= InterestThreshold)
        {
            // Prioritaskan beli yang upgrade-able bila ada
            for (var i = 0; i < state.Shop.AvailableCount; i++)
            {
                var hero = state.Shop.Slots[i];
                if (hero != null && hero.Cost >= 3 && player.Gold - hero.Cost >= InterestThreshold)
                {
                    return Decision.Buy(i, $"High-value hero cost {hero.Cost}, gold {player.Gold}");
                }
            }

            return Decision.None($"Gold {player.Gold} >= {InterestThreshold}: hold for interest");
        }

        if (player.Hp <= LowHpThreshold)
        {
            // HP rendah: belanjakan gold untuk memperkuat board
            for (var i = 0; i < state.Shop.AvailableCount; i++)
            {
                var hero = state.Shop.Slots[i];
                if (hero != null && player.Gold >= hero.Cost)
                {
                    return Decision.Buy(i, $"Low HP {player.Hp}: spend to strengthen");
                }
            }

            return Decision.Refresh($"Low HP {player.Hp}, board weak: roll");
        }

        return Decision.None(reason);
    }
}
