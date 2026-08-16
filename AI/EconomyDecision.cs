namespace MCG_AutoPlay.AI;

/// <summary>Hasil scoring ekonomi. Aksi dengan skor tertinggi dipilih (PRD Phase 8).</summary>
internal enum EconomyAction
{
    None,
    Roll,
    LevelUp,
    Save,
    Buy,
}

/// <summary>Skor keputusan ekonomi. Semakin tinggi, semakin prioritas.</summary>
internal readonly struct EconomyDecision
{
    internal EconomyAction Action { get; }
    internal int Score { get; }
    internal string Reason { get; }

    private EconomyDecision(EconomyAction action, int score, string reason)
    {
        Action = action;
        Score = score;
        Reason = reason;
    }

    internal static EconomyDecision Create(EconomyAction action, int score, string reason) =>
        new(action, score, reason);
}
