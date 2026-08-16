using MCG_AutoPlay.Actions;
using MCG_AutoPlay.AI;
using MCG_AutoPlay.Core;
using MCG_AutoPlay.Game;

// GameStateReader: pastikan compile OK dan membaca round.
var state = GameStateReader.Read();
Console.WriteLine($"Round label: {state.RoundLabel}");

// ReflectionCache: pastikan compile OK (stub pakai System.Type).
var type = MCG_AutoPlay.Infrastructure.ReflectionCache.GetType("System.String");
Console.WriteLine($"Resolved type: {type?.Name ?? "null"}");

// DecisionEngine: gold tinggi -> hold / beli hero mahal.
var rich = new GameState();
rich.Player.Gold = 40;
rich.Player.Hp = 80;
rich.Shop.Slots.Add(new HeroState { Id = 201, Cost = 5, Star = 1 });
var d1 = DecisionEngine.Decide(rich);
Console.WriteLine($"[Decision] gold=40 hp=80 cost5 -> Kind={d1.Kind} slot={d1.Slot} ({d1.Reason})");
Console.WriteLine($"  executed: {ActionExecutor.Execute(d1)}");

// HP rendah, board lemah -> belanjakan / roll.
var poor = new GameState();
poor.Player.Gold = 12;
poor.Player.Hp = 18;
var d2 = DecisionEngine.Decide(poor);
Console.WriteLine($"[Decision] gold=12 hp=18 -> Kind={d2.Kind} slot={d2.Slot} ({d2.Reason})");
Console.WriteLine($"  executed: {ActionExecutor.Execute(d2)}");

// Synergy scoring + missing.
var syn = new GameState();
syn.Synergies.Add(new SynergyState { Name = "Marksman", Current = 3, Required = 4 });
syn.Synergies.Add(new SynergyState { Name = "Warrior", Current = 2, Required = 4 });
Console.WriteLine($"[Build] score={DecisionEngine.BuildScore(syn)} missing={string.Join(",", DecisionEngine.MissingSynergies(syn))}");

// ShopDecision: eval hero dengan synergy match.
var shop = new GameState();
shop.Player.Gold = 50;
shop.Synergies.Add(new SynergyState { Name = "Marksman", Current = 3, Required = 4 });
shop.Shop.Slots.Add(new HeroState { Id = 1, Cost = 2, Star = 1 });
shop.Shop.Slots.Add(new HeroState { Id = 2, Cost = 3, Star = 1 });
shop.Board.Heroes.Add(new BoardSlot(new HeroState { Id = 2, Star = 1 }, 0, 0)); // upgrade target
var bestSlot = ShopDecision.BestBuySlot(shop);
Console.WriteLine($"[Shop] best slot={bestSlot} (expect 1 = upgrade)");

return 0;
