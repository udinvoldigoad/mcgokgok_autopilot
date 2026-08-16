# MCG AutoPlay

A MelonLoader mod for **Magic Chess: Go Go** (`mulonggame/MagicChessGoGo`) that provides full Tier 3 autopilot during battles — automatically managing AI registration, board deployment, shop actions, GoGo card selection, and equipment distribution.

---

## Features

- **Full battle autopilot** — registers and drives the game's built-in MCAI engine for your local account on every battle
- **Forced auto-deploy** — sets `m_bAutoSetChess` each prepare phase so your board is placed automatically
- **Direct API pass** — calls shop/GoGo/crystal/equipment APIs every ~400 ms during prepare phases
- **Harmony patches** — hooks `StartBattle`, `EndBattle`, `OnStartPreparePhase`, `OnBeginFightCountDown`, and key `MCBehaviorThreeApi` methods
- **Configurable tick rate** — AI update interval is user-adjustable (50–2000 ms)
- **Verbose watch mode** — logs every BUY / SELL / REFRESH / GoGo / deploy action to the MelonLoader console
- **Resilient patching** — retries Il2Cpp type resolution for up to 60 seconds after load, then continues retrying in the background

---

## Requirements

| Dependency | Notes |
|---|---|
| [MelonLoader](https://melonwiki.xyz/) | v0.6.x or later (net6 build) |
| HarmonyLib | Bundled with MelonLoader |
| Il2CppInterop.Runtime | Bundled with MelonLoader |
| Magic Chess: Go Go | PC build at your configured `GamePath` |

> **Build environment:** .NET 6 SDK, x64, `LangVersion latest`, nullable enabled.

---

## Installation

1. Install MelonLoader into your Magic Chess: Go Go directory.
2. Build the project or grab `MCG_AutoPlay.dll` from `bin/Release/net6.0/`.
3. Drop `MCG_AutoPlay.dll` into `<GameDir>\Mods\`.
4. Launch the game — MelonLoader will load the mod automatically.

The post-build target in the `.csproj` copies the DLL to `<GamePath>\Mods\` automatically on every build:

```xml
<GamePath>E:\MagicChessGoGo</GamePath>
```

Update this path in `MCG_AutoPlay.csproj` to match your install location before building.

---

## Configuration

Settings are written to `UserData/MelonPreferences.cfg` under the `[MCG_AutoPlay]` category on first run.

| Key | Type | Default | Description |
|---|---|---|---|
| `Enabled` | bool | `true` | Master switch — disables all autopilot when false |
| `AIDifficulty` | int | `101` | Built-in MCAI difficulty level (101 = default, up to 1006) |
| `ForceAutoDeploy` | bool | `true` | Forces auto board placement each prepare phase |
| `VerboseWatch` | bool | `true` | Logs shop/deploy/GoGo actions to the MelonLoader console |
| `TickMs` | int | `150` | AI update tick interval in milliseconds (clamped 50–2000) |

Example `MelonPreferences.cfg` block:

```ini
[MCG_AutoPlay]
Enabled = true
AIDifficulty = 101
ForceAutoDeploy = true
VerboseWatch = true
TickMs = 150
```

---

## Console Output

When `VerboseWatch = true`, the MelonLoader console shows live autopilot activity:

```
[AUTOPILOT] Battle started — Tier 3 autopilot engaging (acc=123456789)
[AUTOPILOT] Registered built-in MCAI for acc=123456789 diff=101 (StartBattle)
[AUTOPILOT] Prepare phase — round 1-1 (AI diff 101)
[WATCH]     BUY slot=2 round=1-1
[WATCH]     GOGO_CARD OK round=1-1
[WATCH]     Direct API pass (prepare) round=1-1
[AUTOPILOT] Battle ended — autopilot disengaged
```

Set `VerboseWatch = false` to suppress `[WATCH]` lines and keep only `[AUTOPILOT]` status messages.

---

## Project Structure

```
MCG_AutoPlay/
├── AutoPlayMod.cs          # MelonMod entry point, patch retry loop, tick loop
├── AutoPlayConfig.cs       # MelonPreferences definitions
├── AutoPlayController.cs   # Core autopilot logic (battle lifecycle, tick, AI mgmt)
├── AutoPlayHarmony.cs      # Harmony patch registration and type resolution guard
├── AutoPlayPatches.cs      # Harmony postfix patch implementations
├── AutoPlayWatch.cs        # Console logging helpers
├── BattleBridgeHelper.cs   # MCBattleData bridge wrappers (shop, level-up, GoGo)
├── Il2CppGameAccess.cs     # Reflection-based Il2Cpp member/method access layer
├── Il2CppNativeTypes.cs    # Il2Cpp type resolution with caching and pointer fallback
│
├── Core/                   # Plain GameState (no IL2CPP/MelonLoader dependency)
│   ├── GameState.cs        # State lengkap pemain (player, shop, board, bench, synergy)
│   ├── PlayerState.cs      # HP / Gold / Level / Exp / Round / streak
│   ├── ShopState.cs        # 5 slot shop
│   ├── HeroState.cs        # id / name / cost / star
│   ├── BoardState.cs       # hero + posisi di board
│   ├── BenchState.cs       # hero cadangan
│   └── SynergyState.cs     # synergy aktif
│
├── Infrastructure/         # Reflection cache + safe access (PRD Phase 4)
│   └── ReflectionCache.cs  # Cache Type/Method/Field agar tidak resolusi tiap tick
│
├── Game/                   # GameStateReader (PRD Phase 5)
│   └── GameStateReader.cs  # Isi GameState dari game (round terbukti; lain TODO)
│
├── Actions/                # Action layer (PRD Phase 6)
│   ├── Decision.cs         # Keputusan sebagai aksi konkret
│   └── ActionExecutor.cs   # Decision -> Game API (via BattleBridgeHelper)
│
├── AI/                     # Decision engine (PRD Phase 7/8)
│   └── DecisionEngine.cs   # Economy AI dasar (interest / HP rendah / roll)
│
├── Tests/                  # Proyek test tanpa MelonLoader (net8)
│   ├── Core.Tests/         # Validasi Core/ (plain C#)
│   └── Infra.Tests/        # Validasi Core/ + Infrastructure/ + Game/ + Actions/ + AI/
│
└── MCG_AutoPlay.csproj
```

---

## How It Works

1. **On load** — `AutoPlayMod` starts two coroutines: a patch retry loop (attempts Harmony patching every 0.5 s until Il2Cpp interop types are ready) and a continuous tick loop.
2. **On `MCAIManager.StartBattle`** — `AutoPlayController.OnBattleStart` fires, resolves the local account ID, and calls `EnsureAutopilot` which registers the built-in MCAI for the local player at the configured difficulty.
3. **Each prepare phase** — `OnPreparePhase` re-ensures AI registration, forces auto-deploy, notifies the chess AI to enter prepare state, and runs a direct API pass (GoGo cards, crystals, equipment).
4. **Each tick** — drives `UpdateChessPlayerAIO` and `LogicUpdate` on the registered AI; also runs an API pass if currently in prepare phase.
5. **On `MCAIManager.EndBattle`** — all state is cleared and the autopilot disengages.

---

## Known Limitations & Open Issues

- `deltaMs` passed to `UpdateChessPlayerAIO` / `LogicUpdate` is the configured tick interval rather than actual elapsed wall-clock time — AI timing accuracy depends on tick regularity.
- `TryDictionaryGet` uses boxed `ulong` key lookup which may fail silently on some Il2Cpp dictionary implementations; if `GetBehaviorApi()` returns null, direct API passes are skipped for that tick.
- The tick coroutine has no shutdown path — a MelonLoader hot-reload will leave a zombie coroutine running.
- Type cache (`Il2CppNativeTypes`) is not thread-safe under concurrent patch callbacks.

---

## Version

**1.0.4** — MCG AutoPlay Tier 3

> Assembly metadata currently reports `1.0.0`; a version sync to `1.0.4` across `AssemblyInfo.cs` and `deps.json` is pending.

---

## Author

**MCG** — built for personal use with Magic Chess: Go Go on PC via MelonLoader.
