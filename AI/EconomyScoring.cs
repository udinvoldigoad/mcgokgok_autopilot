using MCG_AutoPlay.Core;

namespace MCG_AutoPlay.AI;

/// <summary>
/// Economy AI (PRD Phase 8). Menghasilkan skor untuk tiap aksi ekonomi
/// berdasarkan GameState, lalu DecisionEngine memilih aksi skor tertinggi.
///
/// Aturan (dari PRD):
///   Gold >= 30           -> prioritas interest (Save)
///   HP rendah            -> Spend gold (Buy/Roll)
///   Board kuat           -> Save
///   Board lemah          -> Roll
///   Dekat level breakpoint -> LevelUp
/// </summary>
internal static class EconomyScoring
{
    private const int InterestThreshold = 30;
    private const int LowHpThreshold = 30;
    private const int StrongBoardHeroes = 8;
    private const int WeakBoardHeroes = 5;

    internal static EconomyDecision Evaluate(GameState state)
    {
        var player = state.Player;
        var boardCount = state.TotalBoardHeroes;
        var gold = player.Gold;
        var hp = player.Hp;

        // Save: kunci ekonomi (interest)
        var saveScore = 0;
        if (gold >= InterestThreshold)
            saveScore = 80 + Math.Min(gold, 60) - InterestThreshold;
        else if (boardCount >= StrongBoardHeroes)
            saveScore = 50;
        else
            saveScore = 20;

        // Roll: board lemah / HP rendah
        var rollScore = 0;
        if (boardCount <= WeakBoardHeroes)
            rollScore = 70;
        if (hp <= LowHpThreshold)
            rollScore = Math.Max(rollScore, 75);
        if (gold < InterestThreshold)
            rollScore = Math.Max(rollScore, 40);

        // LevelUp: dekat breakpoint (contoh level 7->8 saat exp menumpuk)
        var levelUpScore = 0;
        if (player.Level >= 4 && player.Level < 9 && player.Exp >= 30)
            levelUpScore = 60;

        // Buy dinilai di ShopDecision; baseline rendah di sini.
        var buyScore = 0;

        var best = EconomyAction.None;
        var bestScore = 0;
        var reason = "";

        if (rollScore > bestScore) { best = EconomyAction.Roll; bestScore = rollScore; reason = $"Roll: board={boardCount} hp={hp}"; }
        if (levelUpScore > bestScore) { best = EconomyAction.LevelUp; bestScore = levelUpScore; reason = $"LevelUp: exp={player.Exp} lvl={player.Level}"; }
        if (buyScore > bestScore) { best = EconomyAction.Buy; bestScore = buyScore; reason = "Buy: strong shop"; }
        if (saveScore > bestScore) { best = EconomyAction.Save; bestScore = saveScore; reason = $"Save: gold={gold} interest"; }

        return EconomyDecision.Create(best, bestScore, reason);
    }
}
