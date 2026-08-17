using MCG_AutoPlay.Core;

namespace MCG_AutoPlay.Game;

/// <summary>
/// Membaca state game dari memori IL2CPP via akses string dan mengisinya
/// ke GameState (plain object). Terpisah dari AI: AI hanya melihat GameState.
///
/// PRD Phase 5. Mapping diverifikasi dari dump.cs build PC (Magic Chess Go Go):
///   gamer = LogicChessManager.GetBattleManagerByAccid(accId)   // MCLogicBattleManager
///   playerData = gamer.m_PlayerData                            // MCChessPlayerData -> .HP .Level
///   localPlayer = gamer.GetLocalPlayer()                       // LogicChessPlayer -> .m_TotalGold .m_TotalExp
///   shop = LogicChessManager.GetLogicShop(accId)               // MCLogicHeroShop -> .GetItemInfo(slot)
///   board = gamer.ChessList                                    // List<MCLogicFighter> (MCLogicChessmen)
///   bench = LogicChessManager.GetLogicReserveComp(accId)       // MCLogicReserveComp -> .GetLogicFighterFromSlot(slot)
///   bond = LogicChessManager.GetBondComp(accId)                // MCLogicBondComp -> .curActiveBondDict
///
/// Semua pembacaan best-effort dan tidak boleh melempar exception.
/// </summary>
internal static class GameStateReader
{
    internal static GameState Read()
    {
        var state = new GameState();

        if (!AutoPlayController.IsBattleActive)
            return state;

        var accId = AutoPlayController.LocalAccId;
        if (accId == 0)
            return state;

        state.RoundLabel = AutoPlayController.GetRoundLabel();
        state.Player.Round = AutoPlayController.GetCurrentRoundForLog();

        var lcm = GetLogicChessManager();
        if (lcm == null)
            return state;

        var gamer = Il2CppGameAccess.Invoke(lcm, "GetBattleManagerByAccid", accId);
        if (gamer == null)
            return state;

        ReadPlayer(state, gamer);
        ReadShop(state, lcm, accId);
        ReadBoard(state, gamer);
        ReadBench(state, lcm, accId);
        ReadSynergies(state, lcm, accId);

        return state;
    }

    private static object? GetLogicChessManager() =>
        Il2CppGameAccess.GetSingleton("LogicChessManager");

    private static void ReadPlayer(GameState state, object gamer)
    {
        var playerData = Il2CppGameAccess.GetMemberValue(gamer, "m_PlayerData");
        state.Player.Hp = GetInt(playerData, "HP");
        state.Player.Level = GetInt(playerData, "Level");

        var localPlayer = Il2CppGameAccess.Invoke(gamer, "GetLocalPlayer");
        state.Player.Gold = GetInt(localPlayer, "m_TotalGold");
        state.Player.Exp = GetInt(localPlayer, "m_TotalExp");
    }

    private static void ReadShop(GameState state, object lcm, ulong accId)
    {
        var shop = Il2CppGameAccess.Invoke(lcm, "GetLogicShop", accId);
        if (shop == null)
            return;

        state.Shop.RefreshCost = GetInt(shop, "refreshShopCost");
        var lockStatus = Il2CppGameAccess.Invoke(shop, "GetShopLockStatus");
        state.Shop.IsLocked = lockStatus is bool locked && locked;

        for (var i = 0; i < ShopState.SlotCount; i++)
        {
            var item = Il2CppGameAccess.Invoke(shop, "GetItemInfo", i);
            if (item == null)
                continue;

            var heroId = GetInt(item, "m_iHeroId");
            if (heroId <= 0)
                continue;

            state.Shop.Slots.Add(new HeroState
            {
                Id = heroId,
                Cost = GetInt(item, "m_iPrice"),
                Star = GetInt(item, "m_iStarLv"),
            });
        }
    }

    private static void ReadBoard(GameState state, object gamer)
    {
        var list = Il2CppGameAccess.GetMemberValue(gamer, "ChessList");
        if (list == null)
            return;

        for (var i = 0; i < 64; i++)
        {
            var fighter = Il2CppGameAccess.GetListItem(list, i);
            if (fighter == null)
                break;

            var heroId = GetInt(fighter, "m_ID");
            if (heroId <= 0)
                continue;

            state.Board.Heroes.Add(new BoardSlot(
                new HeroState
                {
                    Id = heroId,
                    Star = GetInt(fighter, "m_iStarLevel"),
                },
                0, i));
        }
    }

    private static void ReadBench(GameState state, object lcm, ulong accId)
    {
        var reserve = Il2CppGameAccess.Invoke(lcm, "GetLogicReserveComp", accId);
        if (reserve == null)
            return;

        for (var slot = 0; slot < 16; slot++)
        {
            var fighter = Il2CppGameAccess.Invoke(reserve, "GetLogicFighterFromSlot", slot);
            if (fighter == null)
                continue;

            var heroId = GetInt(fighter, "m_ID");
            if (heroId <= 0)
                continue;

            state.Bench.Heroes.Add(new HeroState
            {
                Id = heroId,
                Star = GetInt(fighter, "m_iStarLevel"),
            });
        }
    }

    private static void ReadSynergies(GameState state, object lcm, ulong accId)
    {
        var bond = Il2CppGameAccess.Invoke(lcm, "GetBondComp", accId);
        if (bond == null)
            return;

        var dict = Il2CppGameAccess.GetMemberValue(bond, "curActiveBondDict");
        var count = dict == null ? 0 : Math.Max(0, GetInt(dict, "Count"));
        for (var i = 0; i < count; i++)
            state.Synergies.Add(new SynergyState { Name = $"bond{i}", Current = 1, Required = 1 });
    }

    private static int GetInt(object? instance, string member)
    {
        var value = Il2CppGameAccess.GetMemberValue<int>(instance, member);
        if (value != 0)
            return value;

        var invoked = Il2CppGameAccess.Invoke(instance, "get_" + member);
        try
        {
            return Convert.ToInt32(invoked);
        }
        catch
        {
            return 0;
        }
    }
}
