namespace MCG_AutoPlay.Core;

public sealed class SynergyState
{
    public string? Name { get; set; }
    public int Current { get; set; }
    public int Required { get; set; }

    public bool IsActive => Current >= Required;
    public int Progress => Required > 0 ? (int)(100f * Current / Required) : 0;
}
