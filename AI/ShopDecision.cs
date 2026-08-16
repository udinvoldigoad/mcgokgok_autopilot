using MCG_AutoPlay.Core;

namespace MCG_AutoPlay.AI;

/// <summary>
/// Shop / Hero AI (PRD Phase 9). Mengevaluasi tiap hero di shop dan
/// memilih pembelian terbaik berdasarkan skor.
///
/// Skor: synergy match + upgrade + carry/tank. Contoh PRD:
///   Synergy match : +30
///   Upgrade       : +40
///   Carry         : +20
///   Total = 90
/// </summary>
internal static class ShopDecision
{
    internal static int EvaluateHero(HeroState hero, GameState state, HeroDefinition? def)
    {
        var score = 0;

        // Base value dari cost
        score += hero.Cost * 10;

        // Synergy match (bila definisi & synergy state tersedia)
        if (def != null && !string.IsNullOrEmpty(def.Synergy))
        {
            foreach (var synergy in state.Synergies)
            {
                if (string.Equals(synergy.Name, def.Synergy, StringComparison.OrdinalIgnoreCase)
                    && synergy.Current > 0 && !synergy.IsActive)
                {
                    score += 30; // synergy match yang belum penuh
                    break;
                }
            }
        }

        // Upgrade: hero yang sudah ada di board/bench dengan cost sama layak dibeli
        var existing = CountHero(state, hero.Id);
        if (existing > 0)
            score += 40;

        // Role strength
        if (def != null)
        {
            if (def.Role == HeroRole.Carry && def.CarryScore > 0)
                score += Math.Min(def.CarryScore, 30);
            if (def.Role == HeroRole.Tank && def.TankScore > 0)
                score += Math.Min(def.TankScore, 30);
            if (def.PowerScore > 0)
                score += Math.Min(def.PowerScore, 20);
        }

        return score;
    }

    /// <summary>Pilih slot shop dengan skor tertinggi yang masih terjangkau.</summary>
    internal static int BestBuySlot(GameState state, IReadOnlyDictionary<int, HeroDefinition>? defs = null)
    {
        var bestSlot = -1;
        var bestScore = 0;

        for (var i = 0; i < state.Shop.AvailableCount; i++)
        {
            var hero = state.Shop.Slots[i];
            if (hero == null || hero.Cost > state.Player.Gold)
                continue;

            HeroDefinition? def = null;
            if (defs != null && defs.TryGetValue(hero.Id, out var d))
                def = d;

            var score = EvaluateHero(hero, state, def);
            if (score > bestScore)
            {
                bestScore = score;
                bestSlot = i;
            }
        }

        return bestSlot;
    }

    private static int CountHero(GameState state, int heroId)
    {
        var count = 0;
        foreach (var slot in state.Board.Heroes)
            if (slot.Hero.Id == heroId)
                count++;
        foreach (var hero in state.Bench.Heroes)
            if (hero.Id == heroId)
                count++;
        return count;
    }
}
