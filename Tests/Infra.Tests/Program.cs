using MCG_AutoPlay.Core;
using MCG_AutoPlay.Game;

// GameStateReader: pastikan compile OK dan membaca round.
var state = GameStateReader.Read();
Console.WriteLine($"Round label: {state.RoundLabel}");

// GameState: populate + aggregate count (dipakai LogGameState mod).
var gs = new GameState();
gs.Player.Hp = 74;
gs.Player.Gold = 32;
gs.Player.Level = 7;
gs.Shop.Slots.Add(new HeroState { Id = 101, Cost = 2, Star = 1 });
gs.Board.Heroes.Add(new BoardSlot(new HeroState { Id = 101, Star = 2 }, 0, 1));
gs.Bench.Heroes.Add(new HeroState { Id = 103, Cost = 1, Star = 1 });
gs.Synergies.Add(new SynergyState { Name = "Marksman", Current = 3, Required = 4 });
Console.WriteLine($"[State] board={gs.TotalBoardHeroes} bench={gs.TotalBenchHeroes} shop={gs.Shop.AvailableCount} syn={gs.Synergies.Count}");

return 0;