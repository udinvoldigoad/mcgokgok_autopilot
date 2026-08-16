namespace MCG_AutoPlay.Actions;

/// <summary>
/// Menampung keputusan sebagai aksi konkret yang akan dieksekusi oleh ActionExecutor.
/// AI menghasilkan Decision; ActionExecutor menerjemahkannya menjadi panggilan Game API.
/// Ini menjaga AI tidak memanggil API game secara langsung (PRD Phase 6).
/// </summary>
internal readonly struct Decision
{
    internal DecisionKind Kind { get; }
    internal int Slot { get; }
    internal string? Reason { get; }

    private Decision(DecisionKind kind, int slot, string? reason)
    {
        Kind = kind;
        Slot = slot;
        Reason = reason;
    }

    internal static Decision Buy(int slot, string reason) => new(DecisionKind.Buy, slot, reason);
    internal static Decision Sell(int slot, string reason) => new(DecisionKind.Sell, slot, reason);
    internal static Decision Refresh(string reason) => new(DecisionKind.RefreshShop, -1, reason);
    internal static Decision LevelUp(string reason) => new(DecisionKind.LevelUp, -1, reason);
    internal static Decision LockShop(string reason) => new(DecisionKind.LockShop, -1, reason);
    internal static Decision None(string reason) => new(DecisionKind.None, -1, reason);
}

internal enum DecisionKind
{
    None,
    Buy,
    Sell,
    RefreshShop,
    LevelUp,
    LockShop,
}
