namespace MCG_AutoPlay.Core;

public sealed class HeroState
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public int Cost { get; set; }
    public int Star { get; set; }
    public bool IsLocked { get; set; }
}
