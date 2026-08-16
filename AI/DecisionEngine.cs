using MCG_AutoPlay.Actions;
using MCG_AutoPlay.Core;

namespace MCG_AutoPlay.AI;

/// <summary>
/// Mengambil GameState dan menghasilkan Decision. Murni logika, tanpa akses game.
/// (PRD Phase 7). Mengorkestrasi:
///   - EconomyScoring (Phase 8)
///   - ShopDecision  (Phase 9)
///   - SynergyDecision (Phase 10)
/// </summary>
internal static class DecisionEngine
{
    /// <summary>Definisi hero statis (isi saat mapping dump tersedia).</summary>
    private static readonly Dictionary<int, HeroDefinition> HeroDefs = new();

    internal static Decision Decide(GameState state)
    {
        var economy = EconomyScoring.Evaluate(state);

        switch (economy.Action)
        {
            case EconomyAction.Save:
                return Decision.None(economy.Reason);

            case EconomyAction.Roll:
                // Coba beli dulu kalau ada hero layak; jika tidak, roll.
                var buySlot = ShopDecision.BestBuySlot(state, HeroDefs);
                if (buySlot >= 0 && state.Shop.Slots[buySlot].Cost <= state.Player.Gold)
                    return Decision.Buy(buySlot, $"Buy before roll: {economy.Reason}");
                return Decision.Refresh(economy.Reason);

            case EconomyAction.LevelUp:
                return Decision.LevelUp(economy.Reason);

            case EconomyAction.Buy:
                buySlot = ShopDecision.BestBuySlot(state, HeroDefs);
                if (buySlot >= 0)
                    return Decision.Buy(buySlot, economy.Reason);
                return Decision.None(economy.Reason);

            default:
                return Decision.None(economy.Reason);
        }
    }

    internal static int BuildScore(GameState state) => SynergyDecision.Score(state, HeroDefs);
    internal static IReadOnlyList<string> MissingSynergies(GameState state) =>
        SynergyDecision.MissingSynergies(state, HeroDefs);
}
