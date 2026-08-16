using MCG_AutoPlay.Core;

namespace MCG_AutoPlay.Game;

/// <summary>
/// Membaca state game dari memori IL2CPP via akses string dan mengisinya
/// ke GameState (plain object). Terpisah dari AI: AI hanya melihat GameState.
///
/// PRD Phase 5 — saat ini hanya round yang terbukti terbaca (via AutoPlayController).
/// Field player/shop/board lain ditandai TODO sampai mapping dikonfirmasi dari dump.
/// Semua pembacaan best-effort dan tidak boleh melempar exception.
/// </summary>
internal static class GameStateReader
{
    /// <summary>Baca state saat ini. Kembalikan GameState kosong bila game tidak aktif.</summary>
    internal static GameState Read()
    {
        var state = new GameState();

        if (!AutoPlayController.IsBattleActive)
            return state;

        // Round — terbukti terbaca (AutoPlayController.GetRoundInfo)
        var round = AutoPlayController.GetCurrentRoundForLog();
        var roundLabel = AutoPlayController.GetRoundLabel();
        state.RoundLabel = roundLabel;
        state.Player.Round = round;

        ReadPlayer(state);
        ReadShop(state);
        ReadBoard(state);
        ReadBench(state);
        ReadSynergies(state);

        return state;
    }

    // TODO(Phase 2 dump): ganti string field di bawah dengan mapping terverifikasi.
    // Jangan menebak nama field yang belum terbukti — kembalikan nilai default aman.

    private static void ReadPlayer(GameState state)
    {
        // Contoh mapping yang akan dikonfirmasi setelah dump tersedia:
        //   var player = ...; state.Player.Hp = GetInt(player, "m_uiHP"); dst.
        // Untuk sekarang biarkan default 0 / "-".
    }

    private static void ReadShop(GameState state)
    {
        // state.Shop.Slots.Clear();
        // state.Shop.IsLocked = ...;
    }

    private static void ReadBoard(GameState state)
    {
        // state.Board.Heroes.Clear();
    }

    private static void ReadBench(GameState state)
    {
        // state.Bench.Heroes.Clear();
    }

    private static void ReadSynergies(GameState state)
    {
        // state.Synergies.Clear();
    }
}
