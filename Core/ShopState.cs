namespace MCG_AutoPlay.Core;

public sealed class ShopState
{
    public const int SlotCount = 5;

    public IList<HeroState> Slots { get; } = new List<HeroState>(SlotCount);
    public bool IsLocked { get; set; }
    public int RefreshCost { get; set; }

    public int AvailableCount => Slots.Count;
}
