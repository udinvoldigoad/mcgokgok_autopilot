namespace MCG_AutoPlay;

internal static class BattleBridgeHelper
{
    private const int DefaultShopSelectType = 39;

    internal static object? GetBridge() =>
        Il2CppGameAccess.GetStaticMember("MCBattleData", "m_BattleBridge");

    internal static bool IsGoGoCardPanelOpen()
    {
        var bridge = GetBridge();
        if (bridge == null)
            return false;

        var result = Il2CppGameAccess.Invoke(bridge, "IsGoGoCardPanelOpen");
        return result is bool open && open;
    }

    internal static void KeyBoardShopSelect(int index)
    {
        var bridge = GetBridge();
        if (bridge == null)
            return;

        Il2CppGameAccess.Invoke(bridge, "KeyBoardShopSelect", index, DefaultShopSelectType);
    }

    internal static void KeyBoardRefreshShop()
    {
        var bridge = GetBridge();
        Il2CppGameAccess.Invoke(bridge, "KeyBoardRefreshShop");
    }

    internal static void KeyBoardLevelUp()
    {
        var bridge = GetBridge();
        Il2CppGameAccess.Invoke(bridge, "KeyBoardLevelUp");
    }

    internal static void KeyBoardOpenShop()
    {
        var bridge = GetBridge();
        Il2CppGameAccess.Invoke(bridge, "KeyBoardOpenShop");
    }

    // --- ActionExecutor bridge ---

    internal static bool InvokeShopSelect(int index)
    {
        var bridge = GetBridge();
        if (bridge == null)
            return false;
        Il2CppGameAccess.Invoke(bridge, "KeyBoardShopSelect", index, DefaultShopSelectType);
        return true;
    }

    internal static bool InvokeRefreshShop()
    {
        var bridge = GetBridge();
        if (bridge == null)
            return false;
        Il2CppGameAccess.Invoke(bridge, "KeyBoardRefreshShop");
        return true;
    }

    internal static bool InvokeLevelUp()
    {
        var bridge = GetBridge();
        if (bridge == null)
            return false;
        Il2CppGameAccess.Invoke(bridge, "KeyBoardLevelUp");
        return true;
    }

    internal static bool InvokeSell(int index)
    {
        var bridge = GetBridge();
        if (bridge == null)
            return false;
        Il2CppGameAccess.Invoke(bridge, "KeyBoardSell", index);
        return true;
    }

    internal static bool InvokeLockShop()
    {
        var bridge = GetBridge();
        if (bridge == null)
            return false;
        Il2CppGameAccess.Invoke(bridge, "KeyBoardLockShop");
        return true;
    }
}
