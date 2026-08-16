namespace MCG_AutoPlay.Core;

public sealed class GameState
{
    public PlayerState Player { get; } = new();
    public ShopState Shop { get; } = new();
    public BoardState Board { get; } = new();
    public BenchState Bench { get; } = new();
    public IList<SynergyState> Synergies { get; } = new List<SynergyState>();

    public string RoundLabel { get; set; } = "?";

    public int TotalBoardHeroes => Board.Heroes.Count;
    public int TotalBenchHeroes => Bench.Heroes.Count;
}
