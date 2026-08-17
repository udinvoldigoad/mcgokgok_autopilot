using System.Text;
using MCG_AutoPlay.Core;

namespace MCG_AutoPlay.Game;

/// <summary>
/// Membaca state game dari memori IL2CPP via akses string dan mengisinya
/// ke GameState (plain object). Terpisah dari AI: AI hanya melihat GameState.
///
/// PRD Phase 5. Mapping diverifikasi dari dump.cs build PC (Magic Chess Go Go):
///   gamer = MCComp.GetGamer(accId)   /  LogicChessManager.Instance.GetBattleManagerByAccid(accId)
///   playerData = gamer.m_PlayerData                            // MCChessPlayerData -> .HP .Level
///   localPlayer = gamer.GetLocalPlayer()                       // LogicChessPlayer -> .m_TotalGold .m_TotalExp
///   shop = LogicChessManager.Instance.GetLogicShop(accId)      // MCLogicHeroShop -> .GetItemInfo(slot)
///   board = gamer.ChessList                                    // List<MCLogicFighter> (MCLogicChessmen)
///   bench = LogicChessManager.Instance.GetLogicReserveComp(accId) -> GetLogicFighterFromSlot(slot)
///   bond = LogicChessManager.Instance.GetBondComp(accId)       // MCLogicBondComp -> .curActiveBondDict
///
/// Semua pembacaan best-effort; DebugInfo mencatat langkah resolve untuk debugging.
/// </summary>
internal static class GameStateReader
{
    /// <summary>Info resolve terakhir (untuk log OBSERVE / debugging).</summary>
    internal static string DebugInfo { get; private set; } = "";

    internal static GameState Read()
    {
        var state = new GameState();
        var dbg = new StringBuilder();

        if (!AutoPlayController.IsBattleActive)
        {
            DebugInfo = "battle inactive";
            return state;
        }

        var accId = AutoPlayController.LocalAccId;
        if (accId == 0)
        {
            DebugInfo = "accId=0";
            return state;
        }

        state.RoundLabel = AutoPlayController.GetRoundLabel();
        state.Player.Round = AutoPlayController.GetCurrentRoundForLog();
        dbg.Append($"round={state.RoundLabel} acc={accId} ");

        var gamer = GetGamer(accId, dbg);
        if (gamer == null)
        {
            DebugInfo = dbg.ToString();
            return state;
        }

        ReadPlayer(state, gamer, dbg);
        ReadShop(state, accId, dbg);
        ReadBoard(state, gamer, dbg);
        ReadBench(state, accId, dbg);
        ReadSynergies(state, accId, dbg);

        DebugInfo = dbg.ToString();
        return state;
    }

    private static object? GetGamer(ulong accId, StringBuilder dbg)
    {
        // Path A: MCComp.GetGamer(accId) — static, MCComp terbukti bekerja (round)
        var gamer = Il2CppGameAccess.InvokeStatic("MCComp", "GetGamer", accId);
        if (gamer != null)
        {
            dbg.Append($"gamer=MCComp(");
            dbg.Append(gamer.GetType().Name);
            dbg.Append(") ");
            return gamer;
        }

        // Path B: LogicChessManager.Instance.GetBattleManagerByAccid(accId)
        var lcm = GetLogicChessManager();
        if (lcm != null)
        {
            gamer = Il2CppGameAccess.Invoke(lcm, "GetBattleManagerByAccid", accId);
            if (gamer != null)
            {
                dbg.Append($"gamer=LCM(");
                dbg.Append(gamer.GetType().Name);
                dbg.Append(") ");
                return gamer;
            }
            dbg.Append("LCM:GetBattleManagerByAccid=null ");
        }
        else
        {
            dbg.Append("LCM=null ");
        }

        // Path C: battle manager pertama dari list LogicChessManager
        if (lcm != null)
        {
            foreach (var listName in new[] { "m_AlivedBattleManagers", "m_ListBattleManagers" })
            {
                var list = Il2CppGameAccess.GetMemberValue(lcm, listName);
                var first = Il2CppGameAccess.GetListItem(list, 0);
                if (first != null)
                {
                    dbg.Append($"gamer=first({listName}) ");
                    return first;
                }
            }
        }

        return null;
    }

    private static object? GetLogicChessManager()
    {
        var lcm = Il2CppGameAccess.GetSingleton("LogicChessManager");
        if (lcm != null)
            return lcm;
        return Il2CppGameAccess.InvokeStatic("LogicChessManager", "get_Instance");
    }

    private static void ReadPlayer(GameState state, object gamer, StringBuilder dbg)
    {
        var playerData = Il2CppGameAccess.GetMemberValue(gamer, "m_PlayerData");
        dbg.Append($"playerData={(playerData == null ? "null" : playerData.GetType().Name)} ");
        state.Player.Hp = GetInt(playerData, "HP");
        state.Player.Level = GetInt(playerData, "Level");

        var localPlayer = Il2CppGameAccess.Invoke(gamer, "GetLocalPlayer");
        dbg.Append($"localPlayer={(localPlayer == null ? "null" : localPlayer.GetType().Name)} ");
        state.Player.Gold = GetInt(localPlayer, "m_TotalGold");
        state.Player.Exp = GetInt(localPlayer, "m_TotalExp");
    }

    private static void ReadShop(GameState state, ulong accId, StringBuilder dbg)
    {
        var lcm = GetLogicChessManager();
        var shop = lcm == null ? null : Il2CppGameAccess.Invoke(lcm, "GetLogicShop", accId);
        dbg.Append($"shop={(shop == null ? "null" : "ok")} ");
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

    private static void ReadBoard(GameState state, object gamer, StringBuilder dbg)
    {
        var list = Il2CppGameAccess.GetMemberValue(gamer, "ChessList");
        dbg.Append($"boardList={(list == null ? "null" : list.GetType().Name)} ");
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

    private static void ReadBench(GameState state, ulong accId, StringBuilder dbg)
    {
        var lcm = GetLogicChessManager();
        var reserve = lcm == null ? null : Il2CppGameAccess.Invoke(lcm, "GetLogicReserveComp", accId);
        dbg.Append($"reserve={(reserve == null ? "null" : "ok")} ");
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

    private static void ReadSynergies(GameState state, ulong accId, StringBuilder dbg)
    {
        var lcm = GetLogicChessManager();
        var bond = lcm == null ? null : Il2CppGameAccess.Invoke(lcm, "GetBondComp", accId);
        dbg.Append($"bond={(bond == null ? "null" : "ok")} ");
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
