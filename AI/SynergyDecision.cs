using MCG_AutoPlay.Core;

namespace MCG_AutoPlay.AI;

/// <summary>
/// Build & Synergy engine (PRD Phase 10).
/// Mendeteksi synergy aktif dari board/bench dan memberi skor build.
///
/// Definisi build diisi dari data statis game (belum terverifikasi via dump).
/// </summary>
internal static class SynergyDecision
{
    /// <summary>Hitung jumlah hero per synergy berdasarkan hero di board/bench.</summary>
    internal static Dictionary<string, int> CountSynergies(GameState state, IReadOnlyDictionary<int, HeroDefinition> defs)
    {
        var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        void Add(HeroState? hero)
        {
            if (hero == null || !defs.TryGetValue(hero.Id, out var def) || string.IsNullOrEmpty(def.Synergy))
                return;
            counts.TryGetValue(def.Synergy, out var cur);
            counts[def.Synergy] = cur + 1;
        }

        foreach (var slot in state.Board.Heroes)
            Add(slot.Hero);
        foreach (var hero in state.Bench.Heroes)
            Add(hero);

        return counts;
    }

    /// <summary>Skor kecocokan build: synergy aktif + total progress.</summary>
    internal static int Score(GameState state, IReadOnlyDictionary<int, HeroDefinition> defs)
    {
        var counts = CountSynergies(state, defs);
        var score = 0;

        foreach (var synergy in state.Synergies)
        {
            if (string.IsNullOrEmpty(synergy.Name))
                continue;
            counts.TryGetValue(synergy.Name, out var count);
            if (count >= synergy.Required)
                score += 50 + count * 10;
            else
                score += count * 5;
        }

        return score;
    }

    /// <summary>Daftar synergy yang belum aktif tapi sedang dikejar (missing synergy).</summary>
    internal static IReadOnlyList<string> MissingSynergies(GameState state, IReadOnlyDictionary<int, HeroDefinition> defs)
    {
        var counts = CountSynergies(state, defs);
        var missing = new List<string>();

        foreach (var synergy in state.Synergies)
        {
            if (string.IsNullOrEmpty(synergy.Name))
                continue;
            counts.TryGetValue(synergy.Name, out var count);
            if (count < synergy.Required)
                missing.Add(synergy.Name);
        }

        return missing;
    }
}
