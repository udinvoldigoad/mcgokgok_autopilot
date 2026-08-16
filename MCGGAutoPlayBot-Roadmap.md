# MCGG AutoPlay Bot — Development Roadmap

## Tujuan

Mengembangkan MCGG AutoPlay Bot dari bot autoplay berbasis API/MCAI menjadi bot dengan arsitektur:

```text
Game
 ↓
IL2CPP / Game API
 ↓
GameState Reader
 ↓
Decision Engine
 ↓
Action Executor
 ↓
Game
```

Prinsip utama:

- Jangan menebak class, field, method, enum, atau API IL2CPP.
- Pertahankan behavior yang sudah bekerja.
- Ubah sesedikit mungkin file untuk setiap task.
- Setiap perubahan harus bisa dibuild dan diuji.
- Jangan membuat AI sebelum data GameState terbukti akurat.

---

# PHASE 0 — Persiapan Project

## 0.1 Clone repository

```bash
git clone https://github.com/eendor/MCGGAutoPlayBot.git
cd MCGGAutoPlayBot
```

## 0.2 Buat branch development

```bash
git checkout -b development
```

Jangan melakukan pengembangan langsung di `main`.

## 0.3 Buka dengan OpenCode

```bash
opencode
```

## 0.4 Prompt awal untuk DeepSeek

Berikan prompt:

> Analisis seluruh repository ini. Jangan mengubah file apa pun. Jelaskan arsitektur, entry point, dependency, alur AutoPlayController, bagaimana IL2CPP diakses, bagaimana Harmony patch bekerja, bagaimana Direct API bekerja, dan identifikasi technical debt serta risiko compatibility. Setelah selesai buat laporan dan tunggu instruksi berikutnya.

Tujuan: DeepSeek memahami project terlebih dahulu sebelum melakukan perubahan.

---

# PHASE 1 — Audit & Baseline

Sebelum refactor:

- [ ] Project bisa dibuild.
- [ ] Mod bisa diload oleh MelonLoader.
- [ ] AutoPlay lama masih berjalan.
- [ ] Tidak ada exception fatal.
- [ ] Catat versi game.
- [ ] Catat versi MelonLoader.
- [ ] Catat target .NET / Unity environment.
- [ ] Catat architecture Android.

Baseline saat ini:

```text
Game       : Magic Chess: Go Go
Package    : com.mobilechess.gp
Version    : 1.3.08.3241
ABI        : arm64-v8a
Platform   : LDPlayer 9
```

Jangan melakukan refactor besar sebelum baseline berhasil.

---

# PHASE 2 — IL2CPP Reverse Engineering

## Tujuan

Mendapatkan mapping API game yang benar.

Target:

```text
Player
├── HP
├── Gold
├── Level
├── EXP
└── Round

Shop
├── Slots
├── Hero ID
├── Cost
└── Availability

Board
├── Hero
├── Position
├── Star
└── Equipment

Bench
└── Hero[]

Synergy
├── Name
├── Current
└── Required

Equipment
├── Item ID
├── Type
└── Stats
```

## Data yang dicari

```text
libil2cpp.so
global-metadata.dat
```

Game saat ini menggunakan:

```text
arm64-v8a
```

## Tool yang dapat digunakan

- Il2CppDumper
- Il2CppInspector
- ILSpy / dnSpy untuk assembly managed jika diperlukan
- ADB LDPlayer

## Prompt DeepSeek

> Jangan implementasi dulu. Berdasarkan dump IL2CPP yang saya berikan, cari class, field, property, enum, method, dan singleton yang berhubungan dengan Player, Shop, Hero, Board, Battle, Equipment, dan Synergy. Buat mapping lengkap berupa Game Concept -> Class -> Field/Property -> Type -> Access Method. Jangan menebak nama field yang tidak ditemukan.

Output yang diharapkan:

```text
Game Concept
    ↓
Class
    ↓
Field / Property
    ↓
Type
    ↓
Access Method
```

---

# PHASE 3 — GameState Abstraction

Setelah mapping asli ditemukan, buat:

```text
Core/
├── GameState.cs
├── PlayerState.cs
├── ShopState.cs
├── BoardState.cs
├── HeroState.cs
├── EquipmentState.cs
└── SynergyState.cs
```

Struktur:

```text
GameState
│
├── Player
├── Shop
├── Board
├── Bench
├── Synergies
├── Equipment
└── Battle
```

Arsitektur harus dipisahkan:

```text
IL2CPP
 ↓
GameStateReader
 ↓
Plain GameState
 ↓
AI
```

AI tidak boleh bergantung langsung pada object IL2CPP.

---

# PHASE 4 — IL2CPP / Reflection Infrastructure

Rapikan:

```text
Infrastructure/
├── Il2CppGameAccess.cs
├── ReflectionCache.cs
├── TypeCache.cs
└── MethodResolver.cs
```

Masalah yang ingin dihindari:

```text
Setiap tick:
FindType()
FindMethod()
Invoke()
FindType()
FindMethod()
Invoke()
```

Menjadi:

```text
Initialize
 ↓
Resolve Type
 ↓
Resolve Method
 ↓
Cache
 ↓
Game Loop
 ↓
Invoke cached method
```

Target:

- [ ] Reflection cache.
- [ ] Thread-safe type cache.
- [ ] Safe method resolution.
- [ ] Null checking.
- [ ] Exception isolation.
- [ ] Handle destroyed IL2CPP objects.
- [ ] Tidak melakukan reflection berat setiap tick.

---

# PHASE 5 — GameStateReader

Buat:

```text
Game/
└── GameStateReader.cs
```

Alur:

```text
Read()
 ↓
Player
 ↓
Shop
 ↓
Board
 ↓
Bench
 ↓
Synergy
 ↓
Equipment
 ↓
GameState
```

Tambahkan:

- [ ] Safe read.
- [ ] Null checking.
- [ ] Version checking.
- [ ] Logging.
- [ ] Validation.

Contoh log:

```text
[STATE]

Round : 8-2
HP    : 74
Gold  : 32
Level : 7

Board : 7
Bench : 4
Shop  : 5
```

## Aturan

Pada fase ini **belum ada AI**.

Tujuan hanya memastikan data yang dibaca dari game benar.

---

# PHASE 6 — Action Layer

Pisahkan aksi dari keputusan.

```text
Actions/
├── ShopActions.cs
├── BoardActions.cs
├── EquipmentActions.cs
├── LevelActions.cs
└── BattleActions.cs
```

Contoh action:

```text
BuyHero()
SellHero()
RefreshShop()
LevelUp()
MoveHero()
EquipItem()
LockShop()
```

Arsitektur:

```text
Decision
 ↓
ActionExecutor
 ↓
Game API
```

AI tidak boleh langsung memanggil API game.

---

# PHASE 7 — Decision Engine

Setelah GameState dan Action Layer stabil:

```text
AI/
├── DecisionEngine.cs
├── EconomyDecision.cs
├── ShopDecision.cs
├── FormationDecision.cs
├── EquipmentDecision.cs
└── CombatDecision.cs
```

Alur:

```text
GameState
 ↓
DecisionEngine
 ↓
Decision
 ↓
ActionExecutor
```

Contoh:

```text
HP = 18
Gold = 24
Level = 7

Decision:
→ Roll Shop
```

Bukan:

```text
HP = 18
→ AutoDeploy()
```

---

# PHASE 8 — Economy AI

Fitur pertama Decision Engine.

Aturan awal:

```text
Gold >= 30
→ Prioritize interest

HP rendah
→ Spend gold

Strong board
→ Save

Weak board
→ Roll

Near level breakpoint
→ Consider level up
```

Gunakan scoring:

```text
Roll Score
Level Up Score
Save Score
Buy Score
```

Contoh:

```text
Roll      = 82
Level Up  = 45
Save      = 20

Decision:
ROLL
```

---

# PHASE 9 — Hero / Shop AI

Buat:

```text
HeroDefinition

ID
Name
Cost
Role
Synergy
Power
CarryScore
TankScore
```

Alur:

```text
Shop
 ↓
Evaluate each hero
 ↓
Score
 ↓
Select best purchase
```

Contoh:

```text
Hero A

Synergy match : +30
Upgrade       : +40
Carry         : +20

Total = 90
```

---

# PHASE 10 — Build & Synergy Engine

Buat:

```text
BuildDefinition
```

Contoh:

```text
Marksman Build

Core:
- Hero A
- Hero B

Synergy:
- Marksman
- Warrior

Carry:
- Hero A

Tank:
- Hero B
```

Bot mengejar build berdasarkan GameState.

Target:

- [ ] Detect current synergy.
- [ ] Detect missing synergy.
- [ ] Hero priority.
- [ ] Core hero.
- [ ] Carry hero.
- [ ] Tank hero.
- [ ] Upgrade priority.

---

# PHASE 11 — Formation AI

Buat:

```text
FormationEngine
```

Pertimbangan:

```text
Role
Range
HP
Damage
Enemy position
Enemy assassin
Enemy AoE
Carry protection
```

Output:

```text
Hero A → Front 1
Hero B → Front 2
Hero C → Back 3
Hero D → Back 4
```

---

# PHASE 12 — Enemy Analyzer

Buat:

```text
EnemyState
```

Baca:

```text
Enemy Synergy
Enemy Heroes
Enemy Formation
Enemy Carry
Enemy Threat
```

Contoh keputusan:

```text
Enemy Assassin
→ Protect backline

Enemy AoE
→ Spread formation

Enemy Tank
→ Focus damage

Enemy Carry
→ Counter positioning
```

---

# PHASE 13 — Equipment AI

Evaluasi setiap item terhadap setiap hero:

```text
Item
 ↓
Evaluate Hero
 ↓
Compatibility Score
 ↓
Best Recipient
```

Contoh:

```text
Crit Item

Hero A = 91
Hero B = 42
Hero C = 18

Decision:
→ Hero A
```

---

# PHASE 14 — Overlay / Dashboard

Buat UI sederhana:

```text
┌─────────────────────────────┐
│       MCGG AUTOPLAY         │
├─────────────────────────────┤
│ HP       72                 │
│ GOLD     31                 │
│ LEVEL    7                  │
│ ROUND    12-3               │
├─────────────────────────────┤
│ BUILD                       │
│ Marksman 3/4                │
│ Warrior 4/6                 │
├─────────────────────────────┤
│ DECISION                    │
│ SAVE GOLD                   │
│ Confidence: 87%             │
└─────────────────────────────┘
```

Dashboard minimal harus menampilkan:

- [ ] Status.
- [ ] HP.
- [ ] Gold.
- [ ] Level.
- [ ] Round.
- [ ] Current build.
- [ ] Current decision.
- [ ] Decision reason.
- [ ] Confidence.

---

# PHASE 15 — Logging & Replay

Simpan:

```text
Round
GameState
Decision
Action
Result
```

Contoh:

```text
12-3
Gold=32
HP=74

Decision:
SAVE

Reason:
Gold threshold 30
Board strength sufficient

Result:
Gold=36
```

Tujuan:

- Debug keputusan.
- Mencari penyebab kekalahan.
- Membandingkan strategi.
- Membuat test case.
- Evaluasi AI.

---

# PHASE 16 — Testing

Buat tiga mode:

## OBSERVE

Bot hanya membaca game.

```text
Game
 ↓
GameState
 ↓
Log
```

Tidak melakukan action.

## ASSIST

Bot membaca dan memberikan rekomendasi.

```text
Game
 ↓
GameState
 ↓
Decision
 ↓
Log
```

Tidak mengeksekusi action.

## AUTO

Bot menjalankan keputusan.

```text
Game
 ↓
GameState
 ↓
Decision
 ↓
Action
 ↓
Verify
```

Jangan langsung mengaktifkan AUTO sebelum OBSERVE dan ASSIST stabil.

---

# PHASE 17 — Optimization

Setelah fitur stabil:

- [ ] Reflection cache.
- [ ] Reduce polling.
- [ ] Cache game objects.
- [ ] Avoid unnecessary allocations.
- [ ] Reduce logging.
- [ ] Coroutine lifecycle.
- [ ] Thread safety.
- [ ] Exception isolation.
- [ ] IL2CPP object lifetime handling.
- [ ] Performance profiling.

---

# PHASE 18 — Release

Struktur final:

```text
MCGGAutoPlay
│
├── Core
├── Game
├── AI
├── Actions
├── Infrastructure
├── Harmony
├── UI
├── Config
└── Tests
```

Versioning:

```text
1.0.x = Existing bot

1.1.x = Refactor + GameState

1.2.x = Economy + Shop AI

1.3.x = Build + Synergy

1.4.x = Formation

1.5.x = Enemy Analyzer

1.6.x = Equipment

2.0.0 = Full Decision Engine
```

---

# Workflow Pengembangan dengan OpenCode + DeepSeek

Jangan memberikan task terlalu besar.

Gunakan siklus:

```text
1. Analisis
2. Buat rencana
3. Sebutkan file yang akan diubah
4. Tunggu approval
5. Implementasi
6. Build
7. Test
8. Review diff
9. Commit
```

## Aturan wajib untuk DeepSeek

Gunakan instruksi ini sebagai system/project instruction:

> Jangan mengarang class, field, method, enum, atau API IL2CPP. Jika tidak ditemukan di source atau dump, tandai sebagai UNKNOWN dan jangan implementasikan berdasarkan asumsi.
>
> Jangan mengubah file yang tidak berkaitan dengan task.
>
> Pertahankan behavior yang sudah bekerja.
>
> Sebelum mengubah kode, jelaskan file yang akan diubah dan alasan perubahan.
>
> Setelah implementasi, lakukan review terhadap perubahan dan cari kemungkinan regression.
>
> Jangan melakukan refactor besar ketika task hanya membutuhkan perubahan kecil.
>
> Jangan membuat placeholder yang terlihat seperti implementasi nyata.
>
> Jika informasi game API belum tersedia, berhenti dan minta data yang diperlukan.

---

# Urutan Eksekusi Praktis

Kerjakan tepat dalam urutan berikut:

```text
[ ] 01. Clone repository
[ ] 02. Buat development branch
[ ] 03. Audit repository
[ ] 04. Build baseline
[ ] 05. Test bot versi lama
[ ] 06. Ambil informasi game
[ ] 07. Ambil libil2cpp.so
[ ] 08. Ambil / resolve global-metadata.dat
[ ] 09. Dump IL2CPP
[ ] 10. Mapping Player
[ ] 11. Mapping Shop
[ ] 12. Mapping Hero
[ ] 13. Mapping Board
[ ] 14. Mapping Battle
[ ] 15. Mapping Equipment
[ ] 16. Mapping Synergy
[ ] 17. Implement GameState
[ ] 18. Implement GameStateReader
[ ] 19. Validasi GameState di game
[ ] 20. Implement Action Layer
[ ] 21. Implement Decision Engine
[ ] 22. Economy AI
[ ] 23. Shop AI
[ ] 24. Build AI
[ ] 25. Synergy AI
[ ] 26. Formation AI
[ ] 27. Enemy Analyzer
[ ] 28. Equipment AI
[ ] 29. Overlay
[ ] 30. Logging / Replay
[ ] 31. OBSERVE testing
[ ] 32. ASSIST testing
[ ] 33. AUTO testing
[ ] 34. Performance optimization
[ ] 35. Release 2.0
```

# Prinsip Utama

**Data dulu → State → Action → Decision → AI → Optimization.**

Jangan membalik urutan menjadi:

```text
AI dulu
 ↓
cari data belakangan
```

Karena itu akan menghasilkan bot yang banyak asumsi dan sulit diperbaiki.
