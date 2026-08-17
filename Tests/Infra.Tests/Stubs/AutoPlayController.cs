namespace MCG_AutoPlay;

/// <summary>Stub minimal AutoPlayController agar Game/GameStateReader bisa di-compile tanpa MelonLoader.</summary>
internal static class AutoPlayController
{
    internal static bool IsBattleActive => true;
    internal static ulong LocalAccId => 123456789;
    internal static string GetRoundLabel() => "8-2";
    internal static int GetCurrentRoundForLog() => 2;
}
