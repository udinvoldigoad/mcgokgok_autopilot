using MCG_AutoPlay.Core;

var state = new GameState();

state.Player.Hp = 74;
state.Player.Gold = 32;
state.Player.Level = 7;
state.Player.Stage = 8;
state.Player.Round = 2;
state.RoundLabel = state.Player.RoundLabel;

state.Shop.Slots.Add(new HeroState { Id = 101, Name = "HeroA", Cost = 2, Star = 1 });
state.Shop.Slots.Add(new HeroState { Id = 102, Name = "HeroB", Cost = 3, Star = 1 });
state.Board.Heroes.Add(new BoardSlot(new HeroState { Id = 101, Star = 2 }, 0, 1));
state.Bench.Heroes.Add(new HeroState { Id = 103, Cost = 1, Star = 1 });
state.Synergies.Add(new SynergyState { Name = "Marksman", Current = 3, Required = 4 });

Console.WriteLine($"[STATE]");
Console.WriteLine($"Round : {state.Player.RoundLabel}");
Console.WriteLine($"HP    : {state.Player.Hp}");
Console.WriteLine($"Gold  : {state.Player.Gold}");
Console.WriteLine($"Level : {state.Player.Level}");
Console.WriteLine($"Board : {state.TotalBoardHeroes}");
Console.WriteLine($"Bench : {state.TotalBenchHeroes}");
Console.WriteLine($"Shop  : {state.Shop.AvailableCount}");
foreach (var syn in state.Synergies)
    Console.WriteLine($"Synergy {syn.Name}: {syn.Current}/{syn.Required} active={syn.IsActive} progress={syn.Progress}%");

return 0;
