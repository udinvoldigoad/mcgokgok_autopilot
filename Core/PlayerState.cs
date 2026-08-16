namespace MCG_AutoPlay.Core;

public sealed class PlayerState
{
    public int Hp { get; set; }
    public int Gold { get; set; }
    public int Level { get; set; }
    public int Exp { get; set; }
    public int Round { get; set; }
    public int Stage { get; set; }
    public int WinStreak { get; set; }
    public int LoseStreak { get; set; }

    public string RoundLabel => Stage > 0 && Round > 0 ? $"{Stage}-{Round}" : Round > 0 ? Round.ToString() : "?";
}
