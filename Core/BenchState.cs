namespace MCG_AutoPlay.Core;

public sealed class BenchState
{
    public IList<HeroState> Heroes { get; } = new List<HeroState>();

    public int Count => Heroes.Count;
}
