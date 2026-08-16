namespace MCG_AutoPlay.AI;

/// <summary>Peran hero dalam komposisi board.</summary>
internal enum HeroRole
{
    Unknown,
    Carry,
    Tank,
    Support,
}

/// <summary>
/// Meta data hero yang digunakan untuk menilai keputusan shop.
/// Dapat diisi dari data statis game (belum terverifikasi via dump — kosongkan default).
/// (PRD Phase 9)
/// </summary>
internal sealed class HeroDefinition
{
    internal int Id { get; init; }
    internal string Name { get; init; } = "";
    internal int Cost { get; init; }
    internal HeroRole Role { get; init; } = HeroRole.Unknown;
    internal string Synergy { get; init; } = "";
    internal int PowerScore { get; init; }
    internal int CarryScore { get; init; }
    internal int TankScore { get; init; }
}
