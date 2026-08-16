namespace MCG_AutoPlay.Core;

public sealed class BoardState
{
    public IList<BoardSlot> Heroes { get; } = new List<BoardSlot>();

    public int Count => Heroes.Count;
}

public sealed class BoardSlot
{
    public HeroState Hero { get; }
    public int Row { get; }
    public int Col { get; }

    public BoardSlot(HeroState hero, int row, int col)
    {
        Hero = hero;
        Row = row;
        Col = col;
    }
}
