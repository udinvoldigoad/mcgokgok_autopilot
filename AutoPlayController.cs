using MelonLoader;

namespace MCG_AutoPlay;

internal static class AutoPlayController
{
    private static bool _battleActive;
    private static ulong _localAccId;
    private static bool _aiRegistered;
    private static int _lastLoggedRound = -1;
    private static DateTime _lastDirectApiTick = DateTime.MinValue;

    internal static bool IsBattleActive => _battleActive;

    internal static ulong LocalAccId
    {
        get
        {
            if (_localAccId != 0)
                return _localAccId;
            _localAccId = ResolveLocalAccId();
            return _localAccId;
        }
    }

    internal static void OnBattleStart()
    {
        if (!AutoPlayConfig.Enabled.Value)
            return;

        _battleActive = true;
        _aiRegistered = false;
        _lastLoggedRound = -1;
        _localAccId = ResolveLocalAccId();
        AutoPlayWatch.Status($"Battle started — Tier 3 autopilot engaging (acc={_localAccId})");
        EnsureAutopilot("StartBattle");
    }

    internal static void OnBattleEnd()
    {
        if (_battleActive)
            AutoPlayWatch.Status("Battle ended — autopilot disengaged");

        _battleActive = false;
        _aiRegistered = false;
        _localAccId = 0;
        _lastLoggedRound = -1;
    }

    internal static void OnPreparePhase()
    {
        if (!AutoPlayConfig.Enabled.Value || !_battleActive)
            return;

        EnsureAutopilot("PreparePhase");
        ForceAutoDeploy();

        var roundInfo = GetRoundInfo();
        var roundKey = roundInfo.Key;
        if (roundKey != _lastLoggedRound)
        {
            _lastLoggedRound = roundKey;
            AutoPlayWatch.Status($"Prepare phase — round {roundInfo.Label} (AI diff {AutoPlayConfig.AIDifficulty.Value})");
        }

        NotifyChessAiPrepare();
        RunDirectApiPass("prepare");

        if (AutoPlayConfig.LogGameState.Value)
            LogGameState();
    }

    private static void LogGameState()
    {
        var state = Game.GameStateReader.Read();
        AutoPlayWatch.Log($"[STATE] {Game.GameStateReader.DebugInfo}");
        AutoPlayWatch.Log($"[STATE] Round {state.RoundLabel} | HP {state.Player.Hp} | Gold {state.Player.Gold} | Level {state.Player.Level} | Exp {state.Player.Exp}");
        AutoPlayWatch.Log($"[STATE] Board {state.TotalBoardHeroes} | Bench {state.TotalBenchHeroes} | Shop {state.Shop.AvailableCount} | Synergies {state.Synergies.Count}");

        for (var i = 0; i < state.Shop.Slots.Count; i++)
        {
            var slot = state.Shop.Slots[i];
            AutoPlayWatch.Log($"  Shop slot={i} heroId={slot.Id} cost={slot.Cost} star={slot.Star}");
        }

        foreach (var board in state.Board.Heroes)
            AutoPlayWatch.Log($"  Board heroId={board.Hero.Id} star={board.Hero.Star}");

        foreach (var bench in state.Bench.Heroes)
            AutoPlayWatch.Log($"  Bench heroId={bench.Id} star={bench.Star}");

        foreach (var syn in state.Synergies)
            AutoPlayWatch.Log($"  Synergy id={syn.Name} count={syn.Current} req={syn.Required}");
    }

    internal static void OnFightCountdown()
    {
        if (!AutoPlayConfig.Enabled.Value || !_battleActive)
            return;

        AutoPlayWatch.Log($"Fight countdown — round {GetRoundLabel()}");
    }

    internal static void Tick(int deltaMs)
    {
        if (!AutoPlayConfig.Enabled.Value || !_battleActive)
            return;

        if (!_aiRegistered)
            EnsureAutopilot("Tick");

        var aiMgr = GetAiManager();
        if (aiMgr != null)
        {
            Il2CppGameAccess.SetMemberValue(aiMgr, "m_isCloseAi", false);
            Il2CppGameAccess.Invoke(aiMgr, "UpdateChessPlayerAIO", deltaMs);

            if (_localAccId != 0)
            {
                var chessAi = Il2CppGameAccess.Invoke(aiMgr, "GetChessPlayerNew", _localAccId);
                Il2CppGameAccess.Invoke(chessAi, "LogicUpdate", deltaMs);
            }
        }

        if (IsPreparePhase())
            RunDirectApiPass("tick");
    }

    internal static bool ShouldLogApi(object? apiInstance)
    {
        if (!AutoPlayConfig.VerboseWatch.Value || !_battleActive || apiInstance == null)
            return false;

        var localAccId = ResolveLocalAccId();
        if (localAccId == 0)
            return false;

        var apiAccId = GetApiAccountId(apiInstance);
        return apiAccId == localAccId;
    }

    internal static void LogApiAction(string verb, int slot, bool result)
    {
        if (!result)
            return;

        AutoPlayWatch.Action($"{verb} slot={slot} round={GetRoundLabel()}");
    }

    internal static void LogApiAction(string verb, bool result)
    {
        if (!result)
            return;

        AutoPlayWatch.Action($"{verb} round={GetRoundLabel()}");
    }

    internal static string GetRoundLabel() => GetRoundInfo().Label;

    internal static int GetCurrentRoundForLog() => GetRoundInfo().Round;

    private static void EnsureAutopilot(string reason)
    {
        var aiMgr = GetAiManager();
        if (aiMgr == null)
        {
            AutoPlayWatch.Log($"MCAIManager not ready ({reason})");
            return;
        }

        Il2CppGameAccess.SetMemberValue(aiMgr, "m_isCloseAi", false);

        var accId = ResolveLocalAccId();
        if (accId == 0)
        {
            AutoPlayWatch.Log($"Local account id unavailable ({reason})");
            return;
        }

        _localAccId = accId;

        if (!_aiRegistered)
        {
            var difficulty = AutoPlayConfig.AIDifficulty.Value;
            Il2CppGameAccess.Invoke(aiMgr, "AddChessPlayerAI", accId, difficulty);

            var chessAi = Il2CppGameAccess.Invoke(aiMgr, "GetChessPlayerNew", accId);
            if (chessAi != null)
            {
                var typeName = chessAi.GetType().Name;
                if (typeName.Contains("AIO", StringComparison.Ordinal))
                    Il2CppGameAccess.Invoke(chessAi, "SetOTree", difficulty);

                Il2CppGameAccess.Invoke(chessAi, "OnEnterPreparePharse");
            }

            _aiRegistered = true;
            AutoPlayWatch.Status($"Registered built-in MCAI for acc={accId} diff={difficulty} ({reason})");
        }

        ForceAutoDeploy();
    }

    private static void NotifyChessAiPrepare()
    {
        if (_localAccId == 0)
            return;

        var aiMgr = GetAiManager();
        var chessAi = aiMgr == null ? null : Il2CppGameAccess.Invoke(aiMgr, "GetChessPlayerNew", _localAccId);
        Il2CppGameAccess.Invoke(chessAi, "OnEnterPreparePharse");
    }

    private static void ForceAutoDeploy()
    {
        if (!AutoPlayConfig.ForceAutoDeploy.Value)
            return;

        var settings = Il2CppGameAccess.GetStaticMember("SystemData", "m_SystemSetting");
        if (settings != null)
            Il2CppGameAccess.SetMemberValue(settings, "m_bAutoSetChess", true);

        var battleMgr = Il2CppGameAccess.GetSingleton("MCBattleManager");
        Il2CppGameAccess.Invoke(battleMgr, "CheckAndUpdateBattleSetting", true);
    }

    private static void RunDirectApiPass(string source)
    {
        if ((DateTime.UtcNow - _lastDirectApiTick).TotalMilliseconds < 400)
            return;

        _lastDirectApiTick = DateTime.UtcNow;

        var api = GetBehaviorApi();
        if (api == null)
            return;

        TryInvoke(api, "AISelectGoGoCard", "GoGoCard");
        TryInvoke(api, "AISelectMagicCrystal", "MagicCrystal");
        TryInvoke(api, "AISelectCrystalEffect", "CrystalEffect");
        TryInvoke(api, "AISelectSuperCrystal", "SuperCrystal");
        TryInvoke(api, "GoGoPick", "GoGoPick", 25, 25, 25, 25);
        TryInvoke(api, "TryAutoDistributeEquipmentByWeight", "AutoEquip", 20, 20, 20, 20, 20);

        AutoPlayWatch.Log($"Direct API pass ({source}) round={GetRoundLabel()}");
    }

    private static void TryInvoke(object api, string method, string label, params object?[] args)
    {
        var result = Il2CppGameAccess.Invoke(api, method, args);
        if (result is bool ok && ok)
            AutoPlayWatch.Action($"{label} OK round={GetRoundLabel()}");
    }

    private static ulong GetApiAccountId(object apiInstance)
    {
        var accId = Il2CppGameAccess.GetUInt64(apiInstance, "m_uAccoundId");
        if (accId != 0)
            return accId;

        return Il2CppGameAccess.GetUInt64(apiInstance, "m_uAccountId");
    }

    private static object? GetAiManager() =>
        Il2CppGameAccess.GetSingleton("MCAIManager", "Battle")
        ?? Il2CppGameAccess.GetSingleton("MCAIManager");

    private static object? GetLogicChessManager() =>
        Il2CppGameAccess.GetSingleton("LogicChessManager");

    private static object? GetBehaviorApi()
    {
        if (_localAccId == 0)
            _localAccId = ResolveLocalAccId();

        if (_localAccId == 0)
            return null;

        var lcm = GetLogicChessManager();
        if (lcm == null)
            return null;

        var dict = Il2CppGameAccess.GetMemberValue(lcm, "chessPlayerAIDict");
        if (Il2CppGameAccess.TryDictionaryGet(dict, _localAccId, out var api))
            return api;

        return null;
    }

    private static ulong ResolveLocalAccId()
    {
        if (_localAccId != 0)
            return _localAccId;

        var fromSystem = Il2CppGameAccess.GetStaticUInt64("SystemData", "m_uiID");
        if (fromSystem != 0)
            return fromSystem;

        var lbm = GetLocalBattleManager();
        if (lbm != null)
        {
            var localPlayer = Il2CppGameAccess.Invoke(lbm, "GetLocalPlayer");
            if (localPlayer != null)
            {
                var accId = Il2CppGameAccess.GetUInt64(localPlayer, "m_GamerAccID");
                if (accId != 0)
                    return accId;

                accId = Il2CppGameAccess.GetUInt64(localPlayer, "m_uAccountId");
                if (accId != 0)
                    return accId;
            }
        }

        var battleMgr = Il2CppGameAccess.GetSingleton("MCBattleManager");
        var localShow = battleMgr == null ? null : Il2CppGameAccess.GetMemberValue(battleMgr, "m_LocalPlayerShow");
        if (localShow != null)
        {
            var fighter = Il2CppGameAccess.GetMemberValue(localShow, "logicFighter");
            var accId = Il2CppGameAccess.GetUInt64(fighter, "m_GamerAccID");
            if (accId != 0)
                return accId;
        }

        return 0;
    }

    private static object? GetLocalBattleManager()
    {
        var accId = _localAccId != 0 ? _localAccId : Il2CppGameAccess.GetStaticUInt64("SystemData", "m_uiID");
        var lcm = GetLogicChessManager();
        if (lcm != null && accId != 0)
        {
            var byAcc = Il2CppGameAccess.Invoke(lcm, "GetBattleManagerByAccid", accId);
            if (byAcc != null)
                return byAcc;
        }

        if (lcm != null)
        {
            var list = Il2CppGameAccess.GetMemberValue(lcm, "m_AlivedBattleManagers");
            var first = Il2CppGameAccess.GetListItem(list, 0);
            if (first != null)
                return first;

            list = Il2CppGameAccess.GetMemberValue(lcm, "m_ListBattleManagers");
            var fromList = Il2CppGameAccess.GetListItem(list, 0);
            if (fromList != null)
                return fromList;
        }

        return null;
    }

    private static bool IsPreparePhase()
    {
        var roundMgr = GetRoundMgr();
        return Il2CppGameAccess.GetBoolProperty(roundMgr, "IsPreparePhase");
    }

    private static object? GetRoundMgr()
    {
        var fromComp = Il2CppGameAccess.GetStaticMember("MCComp", "m_LogicRoundMgr");
        if (fromComp != null)
            return fromComp;

        var lcm = GetLogicChessManager();
        return lcm == null ? null : Il2CppGameAccess.GetMemberValue(lcm, "m_LogicRoundMgr");
    }

    private static RoundInfo GetRoundInfo()
    {
        var roundMgr = GetRoundMgr();
        if (roundMgr == null)
            return RoundInfo.Unknown;

        var stage = (int)ReadRoundValue(roundMgr, "m_uiRoundStage");
        var round = (int)ReadRoundValue(roundMgr, "m_uiRound");

        if (stage > 0 && round > 0)
            return new RoundInfo(stage * 100 + round, $"{stage}-{round}", round);

        if (round > 0)
            return new RoundInfo(round, round.ToString(), round);

        return RoundInfo.Unknown;
    }

    private static uint ReadRoundValue(object roundMgr, string memberName)
    {
        var value = Il2CppGameAccess.GetMemberValue<uint>(roundMgr, memberName);
        if (value != 0)
            return value;

        var invoked = Il2CppGameAccess.Invoke(roundMgr, $"get_{memberName}");
        if (invoked == null)
            return 0;

        try
        {
            return Convert.ToUInt32(invoked);
        }
        catch
        {
            return 0;
        }
    }

    private readonly struct RoundInfo
    {
        internal static RoundInfo Unknown => new(-1, "?", 0);

        internal RoundInfo(int key, string label, int round)
        {
            Key = key;
            Label = label;
            Round = round;
        }

        internal int Key { get; }
        internal string Label { get; }
        internal int Round { get; }
    }
}
