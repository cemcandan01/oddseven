# Kademe 1: Çekirdek ve Simülasyon — İş Planı

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** WeirdLand'in kurallarını (düello, kartlar, durum etkileri, pas, şut, maç akışı, Üstünlük, hype/Alev, kondisyon) Unity'den bağımsız, deterministik, test edilmiş bir C# kütüphanesi olarak yazmak. Yanına binlerce maçı oynatıp denge raporu veren bir komut satırı simülatörü eklemek.

**Architecture:** `core/WeirdLand.Core` (netstandard2.1) saf kurallar motoru. Arayüz motora komut gönderir (`PlayCard`, `Pass`, `EndTurn`, `StartNextDuel`, `CompleteDuel`), motor durumu günceller ve `GameEvent` listesi döndürür. Tüm rastgelelik seed alan `Rng`'den gelir. Oyun verisi `data/*.json`'da durur. `core/WeirdLand.Core.Tests` (NUnit) ve `core/WeirdLand.Sim` (konsol) bu kütüphaneyi kullanır. Kullanıcı tarafını simülasyonda `IUserPolicy` uygulamaları oynar.

**Tech Stack:** .NET 10 SDK, C# 9 (`LangVersion 9.0`, Unity 6 uyumu için), netstandard2.1, Newtonsoft.Json 13.0.3, NUnit (`dotnet new nunit` şablonu).

**Spec:** `docs/superpowers/specs/2026-11-22-weirdland-design.md`

## Global Constraints

- Çekirdek `netstandard2.1` hedefler, `LangVersion` 9.0. C# 10+ özellikleri kullanılmaz: file-scoped namespace, global using, record struct, `required` yok. `record` kullanılabilir (bkz. `IsExternalInit` shim, Görev 1).
- Çekirdekte `UnityEngine`, `System.Random`, `DateTime.Now`, `Guid.NewGuid` kullanılmaz. Tüm rastgelelik `WeirdLand.Core.Rng` üzerinden gelir. Aynı seed ve aynı girdiler aynı sonucu ve aynı olay listesini vermelidir.
- Kod tanımlayıcıları İngilizce, oyuncuya görünen metinler (kart adı, açıklama, takım ve oyuncu adı) Türkçe ve JSON'da.
- JSON tek kaynak. Enum'lar JSON'da string. Kök nesnede `"schemaVersion": 1`. Bilinmeyen alan hata sayılır.
- Kadro 7 kişi, slotlar: `GK, LB, RB, CM, LW, RW, ST`.
- Deste 6-10 kart. Kart rolü: `Attack`, `Defense`, `Both`.
- Düello: tur başına 3 enerji, 4 kart. Saha düellosu en fazla 3 tur, şut düellosu 2, penaltı 1. Süre dolarsa **savunan kazanır**.
- Maç: 8-12 kritik an. Hedef süre 5-10 dk (tahmin: tur × 15 sn + düello × 5 sn).
- Denge hedefi (spec §6): rastgele oynayan model 1. kademe rakibine karşı %10'dan az kazanmalı.
- Tüm komutlar `core/` klasöründen çalıştırılır. Klasörde tek bir çözüm dosyası (`.sln` ya da `.slnx`) olur, `dotnet test` ve `dotnet build` onu bulur.

## Review Focus

1. **Rolüne uyan kartı olmayan oyuncu düelloya girer.** Örnek: pasla topu alan bir stoperin hiç Hücum kartı yoktur. Beklenen: el boş gelir, oyuncu yine Pas ve EndTurn yapabilir, çökme olmaz. Test: Görev 4, `Begin_WithNoUsableCards_HandEmptyButTurnCanEnd`.
2. **Düello ortasında çekme destesi biter.** Beklenen: atılanlar karıştırılıp yeni çekme destesi olur. Toplam kart 4'ten azsa el eksik dolar. Test: Görev 4, `Draw_ReshufflesDiscardWhenDrawPileEmpty` ve `Draw_WithFewerCardsThanHandSize_DrawsAll`.
3. **Düello bittikten sonra komut gelir** (arayüz çift tıklama gibi). Beklenen: `InvalidOperationException`, durum değişmez. Test: Görev 4, `Commands_AfterOutcome_Throw`.
4. **Aynı seed ile tüm maç tekrar oynanır.** Beklenen: skor, düello sayısı ve olay listesi birebir aynı. Test: Görev 7, `SameSeed_SameMatch`.
5. **Hatalı veri dosyası.** Bilinmeyen kart kimliği, bilinmeyen enum, bilinmeyen alan, yanlış mevki, eksik slot olabilir. Beklenen: oyun sırasında null hatası değil, yüklemede sorunlu kartı, oyuncuyu veya JSON yolunu adlandıran `DataException`. Test: Görev 2.

---

## Dosya yapısı

```
.gitignore                                   (bin/, obj/, *.user, .vs/)
data/cards.json, data/teams.json              (Görev 8)
docs/balance/m1-baseline.md                   (Görev 10)
core/WeirdLand.sln[x]
core/WeirdLand.Core/
  WeirdLand.Core.csproj, IsExternalInit.cs, Rng.cs, Events.cs
  Data/  Enums.cs, Stats.cs, EffectDef.cs, CardDef.cs, PlayerDef.cs, TeamDef.cs, GameData.cs, DataLoader.cs, DataException.cs
  Duel/  DuelRules.cs, CardInstance.cs, Combatant.cs, EffectResolver.cs, DuelSetup.cs, Duel.cs, EnemyAi.cs
  Match/ Tactic.cs, PlayerState.cs, TeamState.cs, MatchRules.cs, MatchEngine.cs
  Ai/    DuelAction.cs, IUserPolicy.cs, RandomPolicy.cs, GoodPolicy.cs, MatchRunner.cs
core/WeirdLand.Core.Tests/
  TestKit.cs (test verisi kurucuları), RngTests.cs, DataLoaderTests.cs, EffectResolverTests.cs,
  DuelFlowTests.cs, PassAndShotTests.cs, MatchEngineTests.cs, PolicyTests.cs, RealDataTests.cs
core/WeirdLand.Sim/
  Program.cs, SimReport.cs
```

---

### Görev 1: Çözüm iskeleti ve deterministik RNG

**Files:**
- Create: `.gitignore`, `core/WeirdLand.Core/WeirdLand.Core.csproj`, `core/WeirdLand.Core/IsExternalInit.cs`, `core/WeirdLand.Core/Rng.cs`
- Create: `core/WeirdLand.Core.Tests/` (NUnit şablonu), `core/WeirdLand.Sim/` (konsol şablonu)
- Test: `core/WeirdLand.Core.Tests/RngTests.cs`

**Interfaces:**
- Produces: `public sealed class Rng { Rng(uint seed); uint State {get;} double NextDouble(); int Next(int maxExclusive); bool Chance(double p); void Shuffle<T>(IList<T> list); Rng Fork(); }`

- [ ] **Step 1: İskeleti kur**

```bash
cd core
dotnet new sln -n WeirdLand
dotnet new classlib -n WeirdLand.Core -f netstandard2.1
dotnet new nunit -n WeirdLand.Core.Tests -f net10.0
dotnet new console -n WeirdLand.Sim -f net10.0
dotnet sln add WeirdLand.Core WeirdLand.Core.Tests WeirdLand.Sim
dotnet add WeirdLand.Core.Tests reference WeirdLand.Core
dotnet add WeirdLand.Sim reference WeirdLand.Core
dotnet add WeirdLand.Core package Newtonsoft.Json --version 13.0.3
```

`WeirdLand.Core.csproj` içine `<LangVersion>9.0</LangVersion>` ve `<Nullable>disable</Nullable>` ekle, şablonun `Class1.cs` dosyasını sil. `IsExternalInit.cs`: `namespace System.Runtime.CompilerServices { internal static class IsExternalInit {} }`. Kökte `.gitignore`: `bin/ obj/ *.user .vs/`.

- [ ] **Step 2: Başarısız testleri yaz (`RngTests.cs`)**

```csharp
[Test] public void SameSeed_SameSequence() {
  var a = new Rng(42); var b = new Rng(42);
  for (int i = 0; i < 100; i++) Assert.That(a.NextDouble(), Is.EqualTo(b.NextDouble()));
}
[Test] public void NextDouble_InUnitRange() {
  var r = new Rng(1);
  for (int i = 0; i < 10000; i++) { var x = r.NextDouble(); Assert.That(x, Is.GreaterThanOrEqualTo(0.0).And.LessThan(1.0)); }
}
[Test] public void Next_CoversAllValues() {
  var r = new Rng(7); var seen = new HashSet<int>();
  for (int i = 0; i < 1000; i++) seen.Add(r.Next(5));
  Assert.That(seen, Is.EquivalentTo(new[] { 0, 1, 2, 3, 4 }));
}
[Test] public void Shuffle_IsPermutation_AndDeterministic() {
  var l1 = Enumerable.Range(0, 20).ToList(); var l2 = Enumerable.Range(0, 20).ToList();
  new Rng(9).Shuffle(l1); new Rng(9).Shuffle(l2);
  Assert.That(l1, Is.EqualTo(l2)); Assert.That(l1, Is.EquivalentTo(Enumerable.Range(0, 20)));
  Assert.That(l1, Is.Not.EqualTo(Enumerable.Range(0, 20)));
}
[Test] public void Fork_IsIndependentButDeterministic() {
  var a = new Rng(5); var fa = a.Fork(); var b = new Rng(5); var fb = b.Fork();
  Assert.That(fa.NextDouble(), Is.EqualTo(fb.NextDouble()));
  Assert.That(a.NextDouble(), Is.EqualTo(b.NextDouble()));
}
[Test] public void ZeroSeed_StillProducesValues() {
  var r = new Rng(0); Assert.That(Enumerable.Range(0, 10).Select(_ => r.NextDouble()).Distinct().Count(), Is.GreaterThan(1));
}
```

- [ ] **Step 3: Çalıştır, başarısız olduğunu gör**

Run: `dotnet test` · Expected: derleme hatası, `Rng` tanımsız.

- [ ] **Step 4: `Rng`'yi yaz (`core/WeirdLand.Core/Rng.cs`)**

Algoritma mulberry32 (prototiptekiyle aynı). `State` mevcut iç durumdur. `Fork()` = `new Rng((uint)(NextDouble() * uint.MaxValue) ^ 0x9E3779B9)`. `Shuffle` Fisher–Yates. `Chance(p)` = `NextDouble() < p`.

- [ ] **Step 5: Testler geçsin**

Run: `dotnet test` · Expected: 6 test PASS.

- [ ] **Step 6: Commit**

```bash
git add .gitignore core/
git commit -m "core: solution skeleton and deterministic Rng"
```

---

### Görev 2: Veri modeli ve JSON yükleyici

**Files:**
- Create: `core/WeirdLand.Core/Data/*.cs` (Enums, Stats, EffectDef, CardDef, PlayerDef, TeamDef, GameData, DataLoader, DataException)
- Create: `core/WeirdLand.Core.Tests/TestKit.cs`
- Test: `core/WeirdLand.Core.Tests/DataLoaderTests.cs`

**Interfaces:**
- Produces (namespace `WeirdLand.Core.Data`):
  - `enum Slot { GK, LB, RB, CM, LW, RW, ST }`, `enum Position { GK, DEF, MID, WING, ST }`, `enum Lane { Left, Center, Right }`
  - `static class SlotInfo { Position PositionOf(Slot s); Lane LaneOf(Slot s); }`. Eşleme: LB/RB→DEF, CM→MID, LW/RW→WING. Şerit: LB/LW→Left, RB/RW→Right, GK/CM/ST→Center.
  - `enum CardRole { Attack, Defense, Both }`, `enum Rarity { Common, Rare, Legendary }`
  - `enum Stat { Speed, Power, Passing, Dribbling, Shooting, Defending, Aerial, Near, Far }`
  - `enum StatusId { Unbalanced, Momentum, Tired, CoverNear, CoverFar }`
  - `enum EffectType { Damage, Block, Status, Draw, Energy, Kondisyon }`, `enum EffectTarget { Self, Opponent }`, `enum Corner { None, Near, Far, Top }`
  - `class Stats { int Speed, Power, Passing, Dribbling, Shooting, Defending, Aerial, Near, Far; int Get(Stat s); }`
  - `class EffectDef { EffectType Type; EffectTarget Target = Opponent; int Amount; Stat? ScaleStat; double Scale; StatusId? Status; int Stacks; Corner Corner; StatusId? BonusIfTargetHas; double BonusMultiplier = 1; }`
  - `class CardDef { string Id, Name, Text; int Cost; CardRole Role; List<Position> Positions; Rarity Rarity; List<EffectDef> Effects; int? UpgradedCost; List<EffectDef> UpgradedEffects; bool UsableWhen(bool attacking); }`. `UsableWhen(true)` → Attack/Both, `UsableWhen(false)` → Defense/Both.
  - `class PlayerDef { string Id, Name; Slot Slot; Stats Stats; List<string> Deck; Position Position => SlotInfo.PositionOf(Slot); }`
  - `class TeamDef { string Id, Name, Style; int Tier; List<PlayerDef> Players; PlayerDef BySlot(Slot s); }`
  - `class GameData { IReadOnlyDictionary<string, CardDef> Cards; IReadOnlyDictionary<string, TeamDef> Teams; CardDef Card(string id); TeamDef Team(string id); }`
  - `static class DataLoader { GameData Load(string dataDir); GameData FromJson(string cardsJson, string teamsJson); }` ve `class DataException : Exception`
  - `TestKit` (test projesi): `CardDef Card(string id, int cost, CardRole role, params EffectDef[] fx)` (tüm mevkiler açık), `EffectDef Dmg(int amount, Stat? stat = null, double scale = 0)`, `EffectDef Blk(int amount)`, `EffectDef Sts(StatusId s, int stacks, EffectTarget t = EffectTarget.Opponent)`, `EffectDef AtCorner(EffectDef e, Corner c)`, `PlayerDef Player(string id, Slot slot, Stats stats = null, params string[] deck)` (stats null ise tüm statlar 3), `string CardsJson(params object[] cards)` ve `string TeamsJson(params object[] teams)` (anonim nesneleri `schemaVersion: 1` ile sarar).

JSON biçimi. `cards.json`: `{ "schemaVersion": 1, "cards": [ { "id": "calim", "name": "Çalım", "text": "...", "cost": 2, "role": "Attack", "positions": ["MID","WING","ST"], "rarity": "Common", "effects": [ { "type": "Damage", "amount": 4, "scaleStat": "Dribbling", "scale": 1, "bonusIfTargetHas": "Unbalanced", "bonusMultiplier": 2 } ] } ] }`. `teams.json`: `{ "schemaVersion": 1, "teams": [ { "id", "name", "style", "tier", "players": [ { "id", "name", "slot", "stats": {...}, "deck": ["..."] } ] } ] }`.

- [ ] **Step 1: Başarısız testleri yaz (`DataLoaderTests.cs`)**

Her test `TestKit.CardsJson` / `TeamsJson` ile geçerli bir tabanı tek bir noktada bozar:

```csharp
[Test] public void ValidData_Loads() { var gd = DataLoader.FromJson(ValidCards, ValidTeams);
  Assert.That(gd.Cards["calim"].Effects[0].ScaleStat, Is.EqualTo(Stat.Dribbling));
  Assert.That(gd.Team("t1").BySlot(Slot.LW).Position, Is.EqualTo(Position.WING)); }
[Test] public void WrongSchemaVersion_Throws()        // schemaVersion 2 → DataException, mesaj "schemaVersion" içerir
[Test] public void UnknownField_Throws()              // kartta "foo": 1 → DataException, mesaj "foo" içerir
[Test] public void UnknownEnum_Throws()               // effect type "Explode" → DataException, mesaj JSON yolunu ("effects") içerir
[Test] public void UnknownCardInDeck_Throws()         // deck: ["yok"] → DataException, mesaj "yok" ve oyuncu id'sini içerir
[Test] public void CardNotAllowedForPosition_Throws() // GK destesine positions:["ST"] kart → DataException, mesaj kart ve oyuncu id'si
[Test] public void DeckSizeOutOfRange_Throws()        // 5 kart ve 11 kart → DataException
[Test] public void MissingOrDuplicateSlot_Throws()    // iki LW, ST yok → DataException, mesaj takım id'si
[Test] public void DuplicateCardId_Throws()
```

- [ ] **Step 2: Çalıştır, başarısız olduğunu gör** · Run: `dotnet test` · Expected: derleme hatası.

- [ ] **Step 3: Veri sınıflarını ve `DataLoader`'ı yaz**

Newtonsoft ayarları: `MissingMemberHandling.Error`, `StringEnumConverter` (tamsayı enum değerine izin verme), camelCase. Newtonsoft'un `JsonException`'ı yakalanıp `DataException`'a sarılır, mesaja `Path` eklenir. Doğrulama (hepsi `DataException`, mesajda ilgili kimlikler):
- schemaVersion == 1
- kart kimliği benzersiz
- deste kartı var olmalı ve `Positions` oyuncunun mevkisini içermeli
- deste boyutu 6-10
- her takımda 7 benzersiz slot

- [ ] **Step 4: Testler geçsin** · Run: `dotnet test` · Expected: tümü PASS.

- [ ] **Step 5: Commit** · `git add core/ && git commit -m "core: data model and validated JSON loader"`

---

### Görev 3: Etki çözümleyici ve durum etkileri

**Files:**
- Create: `core/WeirdLand.Core/Duel/DuelRules.cs`, `CardInstance.cs`, `Combatant.cs`, `EffectResolver.cs`, `core/WeirdLand.Core/Events.cs`
- Test: `core/WeirdLand.Core.Tests/EffectResolverTests.cs`

**Interfaces:**
- Consumes: Görev 2'nin veri tipleri.
- Produces:
  - `enum DuelKind { Field, Shot, Penalty }`
  - `class CardInstance { CardDef Def; bool Upgraded; int Cost; IReadOnlyList<EffectDef> Effects; CardInstance(CardDef def, bool upgraded); }`. Upgraded ise `UpgradedCost`/`UpgradedEffects` (varsa) kullanılır.
  - `class Combatant { PlayerDef Player; int Hp, MaxHp, Block, Energy, KondisyonSpent; int PowerScalePercent = 100; Dictionary<StatusId,int> Statuses; List<CardInstance> DrawPile, Hand, Discard; int StatusOf(StatusId s); }`
  - `static class EffectResolver { int ComputeDamage(EffectDef e, Combatant src, Combatant tgt, DuelKind kind); IEnumerable<GameEvent> Apply(EffectDef e, Combatant src, Combatant tgt, DuelKind kind, Action<Combatant,int> draw); }`
  - `static class DuelRules` sabitleri: `EnergyPerTurn = 3`, `HandSize = 4`, `MaxTurns(DuelKind)` → 3/2/1, `UnbalancedMult = 1.5`, `TiredMult = 0.75`, `NonCornerShotMult = 0.5`, `UncoveredCornerMult = 2`, `int KontrolMax(Stats s) => 10 + s.Power + s.Dribbling`, `int DengeMax(Stats s) => 10 + s.Defending + s.Power`, `int ShooterKontrolMax(Stats s) => 8 + s.Power`, `int KaleMax(Stats gk) => 8 + gk.Near + gk.Far`.
  - `Events.cs` (namespace `WeirdLand.Core`): `abstract record GameEvent;` ve şu türevler: `CardPlayed(bool ByUser, string CardId)`, `DamageDealt(bool ToUser, int Amount, int Blocked, int HpAfter)`, `BlockGained(bool User, int Amount)`, `StatusApplied(bool ToUser, StatusId Status, int Stacks)`, `CardsDrawn(bool User, int Count)`, `EnergyChanged(bool User, int Energy)`, `IntentRevealed(string CardId, string PassTargetId)`, `PassAttempted(bool ByUser, string TargetId, double Chance, bool Success)`, `DuelEnded(DuelOutcome Outcome)`, `MomentStarted(int Index, bool UserAttacks, Lane Lane)`, `GoalScored(bool ByUser, string ScorerId)`, `HypeChanged(string PlayerId, int Hype, bool Alev)`, `KondisyonChanged(string PlayerId, int Value)`, `HalfTime()`, `MatchEnded(int UserScore, int OppScore)`. `enum DuelOutcome { AttackerWon, DefenderWon, PassSucceeded, PassFailed }` de bu dosyada tanımlanır. Sonraki görevler onu kullanır.

Hasar sırası (`ComputeDamage`):
1. `Amount + floor(Scale × src.stat)`
2. `+ src.Momentum`
3. `× TiredMult` (kaynak Tired ise)
4. `× BonusMultiplier` (hedefte `BonusIfTargetHas` varsa)
5. `× UnbalancedMult` (hedef Unbalanced ise)
6. Şut ve penaltı düellosunda:
   - `Corner == None` → `× NonCornerShotMult`
   - Near/Far → `− hedef kalecinin Near/Far statı`
   - kalecide başka köşe için Cover varsa → `× UncoveredCornerMult`
   - Top → ne indirim ne çarpan
7. `× src.PowerScalePercent / 100`
8. Her çarpımdan sonra aşağı yuvarla, en az 0.

`Apply`:
- Damage önce Block'tan düşer.
- Block: `Amount + floor(Scale × stat)` kadar src'ye blok ekler.
- Status: hedef `Target`'a göre seçilir, yığın eklenir.
- Draw: `draw(src, Amount)` çağırır.
- Energy: src'nin enerjisini artırır.
- Kondisyon: çekirdekte sadece olay üretir, düşüşü maç motoru uygular. Bunun için `KondisyonChanged` değil, `Combatant.KondisyonSpent` sayacı artar.

- [ ] **Step 1: Başarısız testleri yaz**

```csharp
// TestKit ile src/tgt Combatant kur: src.Player Dribbling=4, Power=3; tgt Hp=20
[Test] public void Damage_AddsScaledStat() => Assert.That(Compute(Dmg(4, Stat.Dribbling, 1)), Is.EqualTo(8));
[Test] public void Momentum_AddsFlat()     { src.Statuses[StatusId.Momentum] = 2; Assert.That(Compute(Dmg(4)), Is.EqualTo(6)); }
[Test] public void Tired_Reduces()         { src.Statuses[StatusId.Tired] = 1; Assert.That(Compute(Dmg(8)), Is.EqualTo(6)); }
[Test] public void Unbalanced_Increases()  { tgt.Statuses[StatusId.Unbalanced] = 1; Assert.That(Compute(Dmg(5)), Is.EqualTo(7)); }
[Test] public void BonusIfTargetHas_Multiplies() { tgt.Statuses[StatusId.Unbalanced] = 1;
  var e = Dmg(4); e.BonusIfTargetHas = StatusId.Unbalanced; e.BonusMultiplier = 2; Assert.That(Compute(e), Is.EqualTo(12)); } // 4×2=8, ×1.5=12
[Test] public void Block_AbsorbsFirst() { tgt.Block = 3; Apply(Dmg(5)); Assert.That(tgt.Hp, Is.EqualTo(18)); Assert.That(tgt.Block, Is.EqualTo(0)); }
[Test] public void Shot_NonCorner_Halved()          => Assert.That(ComputeShot(Dmg(8)), Is.EqualTo(4));
[Test] public void Shot_NearCorner_ReducedByGkNear() { gk.Stats.Near = 3; Assert.That(ComputeShot(AtCorner(Dmg(8), Corner.Near)), Is.EqualTo(5)); }
[Test] public void Shot_UncoveredCorner_Doubled()   { gk.Stats.Far = 2; gk.Statuses[StatusId.CoverNear] = 1; Assert.That(ComputeShot(AtCorner(Dmg(8), Corner.Far)), Is.EqualTo(12)); }
[Test] public void Shot_CoveredCorner_NotDoubled()  { gk.Stats.Near = 2; gk.Statuses[StatusId.CoverNear] = 1; Assert.That(ComputeShot(AtCorner(Dmg(8), Corner.Near)), Is.EqualTo(6)); }
[Test] public void Shot_Top_NoReductionNoDouble()   { gk.Statuses[StatusId.CoverNear] = 1; Assert.That(ComputeShot(AtCorner(Dmg(6), Corner.Top)), Is.EqualTo(6)); }
[Test] public void PowerScale_Applies()   { src.PowerScalePercent = 120; Assert.That(Compute(Dmg(10)), Is.EqualTo(12)); }
[Test] public void Damage_NeverNegative() { gk.Stats.Near = 9; Assert.That(ComputeShot(AtCorner(Dmg(2), Corner.Near)), Is.EqualTo(0)); }
[Test] public void StatusEffect_TargetsSelfOrOpponent() // Sts(Momentum,1,Self) → src'de; Sts(Unbalanced,2) → tgt'de, yığınlar toplanır
[Test] public void CardInstance_Upgraded_UsesUpgradedValues()
```

- [ ] **Step 2: Çalıştır, başarısız olduğunu gör** · Run: `dotnet test` · Expected: derleme hatası.
- [ ] **Step 3: Dosyaları yaz**, sıralama yukarıda.
- [ ] **Step 4: Testler geçsin** · Run: `dotnet test` · Expected: PASS.
- [ ] **Step 5: Commit** · `git commit -am "core: effect resolver, statuses and damage rules"` (yeni dosyalar için önce `git add core/`).

---

### Görev 4: Düello akışı ve rakip niyeti

**Files:**
- Create: `core/WeirdLand.Core/Duel/DuelSetup.cs`, `Duel.cs`, `EnemyAi.cs`
- Test: `core/WeirdLand.Core.Tests/DuelFlowTests.cs`

**Interfaces:**
- Consumes: Görev 3.
- Produces:
  - `class DuelSetup { DuelKind Kind; bool UserIsAttacker; PlayerDef UserPlayer, EnemyPlayer; List<CardInstance> UserDeck, EnemyDeck; int UserEnergyBonusTurn1; int UserEnergyPenalty; int UserMomentum, EnemyMomentum; int EnemyPowerScalePercent = 100; int DefenderMaxHpBonus; List<PlayerDef> UserPassTargets, EnemyPassTargets; }`
  - `class EnemyIntent { CardInstance Card; PlayerDef PassTarget; double PassChance; }`. Tam olarak biri dolu.
  - `class Duel { Duel(DuelSetup setup, Rng rng); DuelKind Kind; bool UserIsAttacker; Combatant User, Enemy; int Turn, MaxTurns; EnemyIntent Intent; DuelOutcome? Outcome; PlayerDef PassTarget; IReadOnlyList<PlayerDef> UserPassTargets; bool CanPass; double PassChance(); int IncomingDamage(); IReadOnlyList<GameEvent> Begin(); IReadOnlyList<GameEvent> PlayCard(int handIndex); IReadOnlyList<GameEvent> Pass(string targetId); IReadOnlyList<GameEvent> EndTurn(); }`. Pas Görev 5'te. Bu görevde `Pass` `NotImplementedException` atabilir, `CanPass` false döner.
  - `static class EnemyAi { EnemyIntent ChooseIntent(Duel d, Rng rng); }`

Akış:
- **`Begin()`:**
  - Her taraf kendi destesinden role uyan kartları alır (`UsableWhen(attacking)`), karıştırır ve `DrawPile` yapar.
  - Hp: saldıranda Kontrol, savunanda Denge (+`DefenderMaxHpBonus`). Şut ve penaltıda şutçu `ShooterKontrolMax`, kaleci `KaleMax`.
  - Momentum'lar uygulanır, `Enemy.PowerScalePercent` ayarlanır, Turn=1 ve kullanıcı turu başlar.
- **Kullanıcı turu başlangıcı:**
  - `User.Block = 0`
  - Enerji = `3 + (Turn==1 ? bonus : 0) − penalty`, en az 0
  - 4 kart çekilir. Çekme destesi biterse atılanlar karıştırılır, ikisi de boşsa çekim durur.
  - Ardından `EnemyAi.ChooseIntent`, `IntentRevealed` olayı üretilir.
- **`PlayCard(i)`:**
  - Maliyet enerjiyi aşarsa `InvalidOperationException`.
  - Kart el'den çıkar, `Discard`'a gider, etkileri sırayla uygulanır.
  - Her etkiden sonra sonuç kontrol edilir: bir tarafın Hp ≤ 0 → saldıranın Hp'si biterse `DefenderWon`, savunanınki biterse `AttackerWon`. Sonuç olunca kalan etkiler uygulanmaz.
- **`EndTurn()`:**
  - Kullanıcının eli atılır.
  - `Enemy.Block = 0`, sonra niyet uygulanır: kart etkileri, kaynak düşman.
  - Sonuç kontrolü.
  - Unbalanced, Tired, CoverNear ve CoverFar yığınları her iki tarafta 1 azalır. Momentum azalmaz.
  - `Turn == MaxTurns` ve sonuç yoksa → `DefenderWon` (süre doldu). Değilse Turn++ ve yeni kullanıcı turu.
- **`EnemyAi.ChooseIntent`:**
  - Düşmanın çekme destesinden 2 kart çekilir (gerekirse karıştırma). Kart yoksa niyet boş bir "bekle" olur: Card=null, PassTarget=null.
  - Rakip öldürücü hasar veriyorsa o kart seçilir.
  - Değilse puan hesaplanır:
    - `Hasar + 0.8×Blok`
    - `+3` rakibe Unbalanced/Tired uygularsa
    - `+2` Momentum/Cover başına
  - En yüksek puanlı kart seçilir, eşitlikte ilk çekilen. Diğeri atılır. Düşman enerji kullanmaz.
- **Sonuç olayı:** `DuelEnded` tam bir kez üretilir. Sonuç varken her komut `InvalidOperationException` atar.

- [ ] **Step 1: Başarısız testleri yaz**

```csharp
[Test] public void Begin_SetsHpFromStats()             // saldıran Power3 Dribbling4 → User.Hp 17; savunan Defending4 Power3 → Enemy.Hp 17
[Test] public void Begin_DrawsFour_EnergyThree()
[Test] public void Begin_OnlyRoleCardsInDeck()         // 3 Attack + 3 Defense kartı; kullanıcı saldıran → 6 çekimde yalnızca Attack/Both görünür
[Test] public void Begin_WithNoUsableCards_HandEmptyButTurnCanEnd() // Review Focus 1
[Test] public void Draw_ReshufflesDiscardWhenDrawPileEmpty()        // Review Focus 2: 5 kartlık deste, 2. turda 4 kart elde
[Test] public void Draw_WithFewerCardsThanHandSize_DrawsAll()       // 2 kartlık deste → el 2
[Test] public void PlayCard_SpendsEnergy_AndRejectsTooExpensive()
[Test] public void LethalDamage_EndsDuel_AttackerWon_StopsRemainingEffects()
[Test] public void EnemyIntent_IsRevealedBeforeUserActs_AndResolvedOnEndTurn()
[Test] public void EnemyAi_PrefersLethal()
[Test] public void Timeout_DefenderWins_AfterMaxTurns()             // Field: 3 EndTurn → DefenderWon
[Test] public void StatusesTickDown_MomentumStays()
[Test] public void Turn1Bonus_AndPenalty_ApplyToEnergy()            // bonus 1 → 4; penalty 1 → 2; 2. tur bonus yok
[Test] public void Commands_AfterOutcome_Throw()                    // Review Focus 3; Outcome ve el değişmez
[Test] public void SameSeed_SameEvents()                            // iki Duel aynı seed + aynı komutlar → olay listeleri eşit
```

- [ ] **Step 2: Çalıştır, başarısız olduğunu gör** · Run: `dotnet test` · Expected: derleme hatası.
- [ ] **Step 3: `DuelSetup`, `Duel`, `EnemyAi`'yi yaz**
- [ ] **Step 4: Testler geçsin** · Run: `dotnet test` · Expected: PASS.
- [ ] **Step 5: Commit** · `git add core/ && git commit -m "core: duel turn flow and enemy intent"`

---

### Görev 5: Pas ve şut düellosu

**Files:**
- Modify: `core/WeirdLand.Core/Duel/Duel.cs`, `DuelRules.cs`, `EnemyAi.cs`
- Test: `core/WeirdLand.Core.Tests/PassAndShotTests.cs`

**Interfaces:**
- Produces:
  - `DuelRules.PassChance(Combatant carrier, Combatant defender)`. Formül: `0.30 + 0.06×carrier.Passing + 0.30×(1 − def.Hp/def.MaxHp) + (def Unbalanced ? 0.15 : 0) − 0.25×(1 − car.Hp/car.MaxHp)`, [0.05, 0.95] aralığına kısılır.
  - `DuelRules.EnemyPassThreshold = 0.6`, `DuelRules.PassCost = 1`.
  - `Duel.CanPass`: `Kind == Field && UserIsAttacker && Outcome == null && User.Energy >= 1 && UserPassTargets.Count > 0`.
  - `Duel.Pass(targetId)`:
    - 1 enerji harcar, `rng.Chance(PassChance())` ile zar atar.
    - Başarılıysa `PassSucceeded` ve `PassTarget` = hedef. Başarısızsa `PassFailed`.
    - `PassAttempted` olayı üretilir.
    - Hedef listede yoksa veya `!CanPass` ise `InvalidOperationException`.
  - **Düşman pası:** düşman saldıran ve `Kind == Field` ise ve `PassChance(enemy, user) ≥ 0.6` ise, `EnemyAi` niyeti pas olur. Hedef, `EnemyPassTargets` içinde `Dribbling + Shooting` toplamı en yüksek olan oyuncudur. `EndTurn`'de zar atılır, `PassSucceeded`/`PassFailed` olur.
  - **Şut düellosu:** `Kind == Shot`, `MaxTurns == 2`. Hasar kuralları Görev 3'te. Kaleci kullanıcıysa kaleci kartlarıyla (Defense) oynar, şutçunun niyeti köşesiyle birlikte görünür.

- [ ] **Step 1: Başarısız testleri yaz**

```csharp
[Test] public void PassChance_Formula()         // Passing 4, def Hp tam, own Hp tam → 0.54; def Hp yarı → 0.69; def Unbalanced → +0.15
[Test] public void PassChance_IsClamped()       // aşırı değerler → 0.05 / 0.95
[Test] public void Pass_Success_SetsOutcomeAndTarget()  // seed ile Chance true gelen durum
[Test] public void Pass_Failure_SetsPassFailed()
[Test] public void Pass_NotAllowedWhenDefending_OrShotDuel_OrNoEnergy_Throws()
[Test] public void Pass_UnknownTarget_Throws()
[Test] public void EnemyCarrier_PassesWhenChanceHigh()  // düşman saldıran, kullanıcı Hp düşük → Intent.PassTarget dolu
[Test] public void ShotDuel_GoalWhenKaleZero_AttackerWon()
[Test] public void ShotDuel_TimeoutAfterTwoTurns_DefenderWon()
[Test] public void UserGoalkeeper_CoverMatchingIntentCorner_ReducesDamage() // düşman şutçu Near niyeti; kullanıcı CoverNear oynar → hasar ×1
```

- [ ] **Step 2: Başarısız olduğunu gör** · Run: `dotnet test`
- [ ] **Step 3: Pas ve şut kurallarını ekle**
- [ ] **Step 4: Testler geçsin** · Run: `dotnet test` · Expected: PASS.
- [ ] **Step 5: Commit** · `git commit -am "core: pass action and shot duels"`

---

### Görev 6: Maç motoru

**Files:**
- Create: `core/WeirdLand.Core/Match/Tactic.cs`, `PlayerState.cs`, `TeamState.cs`, `MatchRules.cs`, `MatchEngine.cs`
- Test: `core/WeirdLand.Core.Tests/MatchEngineTests.cs`

**Interfaces:**
- Consumes: Görev 4-5'in `Duel`, `DuelSetup`, `DuelOutcome`.
- Produces:
  - `enum Tactic { Balanced, Attacking, Defensive }`, `enum MatchPhase { BeforeKickoff, AwaitingDuel, InDuel, HalfTime, Ended }`, `enum ChainStage { Midfield, FinalThird, Shot }`
  - `class PlayerState { PlayerDef Def; int Kondisyon = 100; int Hype; bool Alev; List<CardInstance> Deck; }`
  - `class TeamState { TeamDef Def; List<PlayerState> Players; Tactic Tactic; int Score; PlayerState BySlot(Slot s); PlayerState ById(string id); int HypeSum; }`
  - `class MatchEngine { MatchEngine(GameData data, TeamDef user, TeamDef opponent, uint seed); TeamState User, Opponent; MatchPhase Phase; int Ustunluk; int MomentsTotal, MomentsPlayed, Minute; ChainStage Stage; Duel CurrentDuel; void SetTactic(bool user, Tactic t); IReadOnlyList<GameEvent> StartNextDuel(); IReadOnlyList<GameEvent> CompleteDuel(); }`
  - `static class MatchRules`: formüller ve sabitler (aşağıda).

Kurallar (`MatchRules`'te sabit olarak):
- **Kritik an sayısı:** `MomentsTotal = 8 + rng.Next(5)`.
- **Üstünlük güncelleme (her yeni anda):**
  - `U = clamp(round(0.5×U + 8×(userMid − oppMid) + 4×(userHypeSum − oppHypeSum)/3 + tac(user) − tac(opp) + (rng.NextDouble()×40 − 20)), −100, 100)`
  - `mid = (CM.Passing + CM.Power)/2.0`
  - `tac`: Attacking +10, Defensive −10, Balanced 0
- **Kim atak yapar:** kullanıcının atak yapma ihtimali `0.5 + U/250`.
- **Yeni an:**
  - Şerit `rng.Next(3)` ile seçilir, aşama Midfield, `MomentStarted` olayı.
  - Taşıyıcı: Left→LW, Center→CM, Right→RW.
  - Midfield savunmacısı: Center→rakip CM, Left→rakip RW, Right→rakip LW.
  - FinalThird savunmacısı: Left→rakip RB, Right→rakip LB, Center→rakip LB ile RB'den Defending'i yüksek olan (eşitse LB).
  - Shot → rakip GK. Şerit, taşıyıcının slotundan `SlotInfo.LaneOf` ile gelir.
- **Pas hedefleri:** taşıyıcının takımındaki GK ve taşıyıcı dışındaki herkes.
- **`DuelSetup` kurulumu:**
  - Kullanıcı tarafı her zaman kullanıcı takımının oyuncusudur.
  - `UserEnergyPenalty` = 1, kondisyon < 30 ise.
  - `UserEnergyBonusTurn1` = 1, Alev varsa.
  - Düşman Alev'liyse `EnemyMomentum += 2`.
  - Zincirde taşınan Momentum ilgili tarafa verilir.
  - `EnemyPowerScalePercent = 100 + 10×(opponent.Tier − 1)`.
  - Savunan takımın taktiği Defensive ise `DefenderMaxHpBonus = 3`.
- **`CompleteDuel()` (CurrentDuel.Outcome dolu olmalı, yoksa `InvalidOperationException`):**
  - **Kondisyon:** her iki katılımcı −10, artı kartların harcadığı kondisyon. En az 0.
  - **Hype:**
    - Kazanan katılımcı +1, kazandığında Hp ≤ %30 MaxHp ise +1 daha.
    - 3'e ulaşınca `Alev = true`, hype 3'te sınırlanır.
    - Kaybeden Alev'liyse Alev'i söner, Hype 0 olur, kazanan doğrudan `Alev = true`, Hype 3 olur.
    - Pas sonuçları: `PassSucceeded` kimseye hype vermez. `PassFailed` savunmanın kazancıdır.
  - **Üstünlük:** kazanan katılımcının takımı yönünde ±10.
  - **Zincir:**
    - Saldıran kazandıysa (`AttackerWon` ya da `PassSucceeded`): aşama Shot ise gol olur. Skor +1, `GoalScored` olayı, gol atan takımın herkesine +1 hype (Alev kuralıyla), zincir biter. Değilse aşama +1, taşıyıcı = pas hedefi veya aynı oyuncu. Momentum, zincirdeki yeni taşıyıcıya taşınır.
    - Saldıran kaybettiyse zincir biter. Saldıran takımın taktiği Attacking ise, aşama Shot değilse ve an kaldıysa, kontra olur: diğer takım FinalThird aşamasından başlar. Taşıyıcı, düelloyu kazanan savunmacıdır. Kontra bir an sayılır.
  - **Faz:** zincir bittiyse:
    - `MomentsPlayed == MomentsTotal / 2` ve devre arası olmadıysa → `HalfTime` (`HalfTime` olayı).
    - `MomentsPlayed == MomentsTotal` → `Ended` (`MatchEnded` olayı).
    - Değilse `AwaitingDuel`.
  - `Minute = 90 × MomentsPlayed / MomentsTotal`.
- **`StartNextDuel()`:** `BeforeKickoff`, `AwaitingDuel` ya da `HalfTime` fazında çağrılabilir. Bekleyen bir zincir adımı varsa onu başlatır, yoksa yeni an başlatır. `CurrentDuel.Begin()` olaylarını da döndürür. `SetTactic` sadece `BeforeKickoff` ve `HalfTime` fazında çalışır, yoksa `InvalidOperationException`.
- **Kart örnekleri:** maç başında her oyuncunun `Deck`'i `CardInstance`'lara çevrilir (yükseltilmemiş). Kademe 4 yükseltmeyi buraya bağlayacak.

- [ ] **Step 1: Başarısız testleri yaz**

```csharp
[Test] public void MomentsTotal_Between8And12()          // 200 seed
[Test] public void Ustunluk_StaysInRange()
[Test] public void HighUstunluk_UserAttacksMoreOften()   // U zorla +100 → 1000 anda kullanıcı atağı > %80
[Test] public void FirstDuel_CarrierAndDefender_ByLane() // seed ile Left şerit → LW vs rakip RW
[Test] public void Win_AdvancesStage_ThenShot_ThenGoal()  // sahte sonuçlarla: Outcome'u test yardımcısıyla zorla
[Test] public void PassSucceeded_TargetBecomesCarrier()
[Test] public void Loss_EndsChain_AttackingTacticGivesCounterAtFinalThird()
[Test] public void Kondisyon_DropsTen_PenaltyBelow30()
[Test] public void Hype_WinGivesHype_ThreeGivesAlev_LosingAlevTransfersIt()
[Test] public void Goal_GivesWholeTeamHype()
[Test] public void HalfTime_ReachedOnce_TacticChangeAllowedOnlyThen()
[Test] public void Match_Ends_WithMatchEndedEvent()
[Test] public void CompleteDuel_WithoutOutcome_Throws()
```

Sonuçları zorlamak için `Duel`'e `internal void ForceOutcome(DuelOutcome o, PlayerDef passTarget = null)` ekle ve test projesine `InternalsVisibleTo("WeirdLand.Core.Tests")` ver.

- [ ] **Step 2: Başarısız olduğunu gör** · Run: `dotnet test`
- [ ] **Step 3: Maç dosyalarını yaz**
- [ ] **Step 4: Testler geçsin** · Run: `dotnet test` · Expected: PASS.
- [ ] **Step 5: Commit** · `git add core/ && git commit -m "core: match engine with chains, Ustunluk, hype and kondisyon"`

---

### Görev 7: Oyuncu modelleri ve maç oynatıcı

**Files:**
- Create: `core/WeirdLand.Core/Ai/DuelAction.cs`, `IUserPolicy.cs`, `RandomPolicy.cs`, `GoodPolicy.cs`, `MatchRunner.cs`
- Test: `core/WeirdLand.Core.Tests/PolicyTests.cs`

**Interfaces:**
- Produces:
  - `abstract record DuelAction; record PlayCardAction(int HandIndex) : DuelAction; record PassAction(string TargetId) : DuelAction; record EndTurnAction : DuelAction;`
  - `interface IUserPolicy { DuelAction Decide(Duel duel, MatchEngine match, Rng rng); }`
  - `class RandomPolicy : IUserPolicy`: oynanabilir kartlar, `EndTurn` ve (`CanPass` ise) rastgele hedefli Pas arasından eşit olasılıkla seçer.
  - `class GoodPolicy : IUserPolicy`: aşağıdaki öncelik sırası.
  - `class MatchResult { int UserScore, OppScore, Duels, Turns, Passes, Moments; List<GameEvent> Events; }`
  - `static class MatchRunner { MatchResult Play(MatchEngine m, IUserPolicy p, Rng rng, Func<MatchEngine, Tactic> halfTimeTactic = null); const int MaxActions = 10000; }`. Sınır aşılırsa `InvalidOperationException`.

`GoodPolicy` öncelikleri:
1. `CanPass && PassChance ≥ 0.75` → Pas. Hedef: aşama FinalThird ise Shooting'i en yüksek oyuncu, değilse Dribbling'i en yüksek.
2. Öldürücü hasar veren oynanabilir kart varsa onu oyna.
3. `IncomingDamage() ≥ User.Hp + User.Block` ise en çok blok veren oynanabilir kartı oyna.
4. Sırayla şu kartları oyna:
   - rakibe Unbalanced/Tired uygulayan
   - kaleciyse niyet köşesine uyan Cover
   - Momentum
   - enerji başına en çok hasar veren
5. Son turdaysa, saldıransa, Field düellosundaysa ve `CanPass && PassChance ≥ 0.4` ise Pas.
6. Hiçbiri değilse EndTurn.

- [ ] **Step 1: Başarısız testleri yaz**

```csharp
[Test] public void RandomPolicy_OnlyLegalActions()        // 500 karar, hiçbiri exception atmaz
[Test] public void GoodPolicy_TakesLethal()
[Test] public void GoodPolicy_BlocksIncomingLethal()
[Test] public void GoodPolicy_PassesWhenChanceHigh()
[Test] public void MatchRunner_CompletesMatch()            // TestKit ile kurulan iki takım, Phase == Ended
[Test] public void SameSeed_SameMatch()                    // Review Focus 4: skor, Duels, Turns ve Events (sıra dahil) eşit
[Test] public void MatchRunner_ThrowsOnRunawayPolicy()     // hep PlayCard(0) döndüren ama kart olmayan politika → InvalidOperationException
```

- [ ] **Step 2: Başarısız olduğunu gör** · Run: `dotnet test`
- [ ] **Step 3: Dosyaları yaz**
- [ ] **Step 4: Testler geçsin** · Run: `dotnet test` · Expected: PASS.
- [ ] **Step 5: Commit** · `git add core/ && git commit -m "core: user policies and match runner"`

---

### Görev 8: Gerçek oyun verisi

**Files:**
- Create: `data/cards.json`, `data/teams.json`
- Test: `core/WeirdLand.Core.Tests/RealDataTests.cs`

**İçerik kuralları:**
- **Kartlar:** en az 40, Türkçe ad ve açıklama.
  - Spec §5.5'teki örnekler bulunur: Topu sakla, Sırtını koy, Kolunu uzat, Çalım, Kayarak müdahale. Ayrıca köşe hedefli şutlar (Yakın köşe, Uzak köşe, Üst köşe) ve kaleci kartları (Sağa yat = CoverNear, Sola yat = CoverFar, Çık ve kap = şutçuya hasar).
  - Her mevki için başlangıç kartları ve havuz kartları ayrı olur, havuz kartları Rare/Legendary ağırlıklı.
- **Takımlar:**
  - `mavi`: kullanıcının başlangıç takımı, tier 1. Statlar prototipteki `ROSTER_A`'dan gelir (`prototype/index.html`, sıra: hız, güç, pas, çalım, şut, savunma, hava; kaleci yakın 3, uzak 3).
  - `kirmizi`: tier 1, `ROSTER_B`'den.
  - İki rakip daha: tier 2 ve tier 3. Statlar kademeli artar, desteler daha çok Rare kart içerir.
  - Her oyuncunun destesi 6-8 kart.
  - GK dışındaki her oyuncuda en az 2 Attack/Both ve en az 2 Defense/Both kart.
  - LW, RW, ST destesinde en az 1 köşe hedefli şut kartı.
  - GK destesinde CoverNear, CoverFar ve en az 1 hasar kartı.

- [ ] **Step 1: Başarısız testleri yaz**

```csharp
static GameData Load() => DataLoader.Load(Path.Combine(TestContext.CurrentContext.TestDirectory, "../../../../../data"));
[Test] public void RealData_LoadsAndValidates()
[Test] public void RealData_HasAtLeast40Cards()
[Test] public void RealData_HasUserTeamAndTiers()  // "mavi" ve tier 1, 2, 3 rakipleri
[Test] public void RealData_DeckRoleRules()        // yukarıdaki iki-rol, köşe-şut ve kaleci kuralları
```

- [ ] **Step 2: Başarısız olduğunu gör** · Run: `dotnet test --filter RealData` · Expected: dosya yok.
- [ ] **Step 3: `data/cards.json` ve `data/teams.json`'u yaz**
- [ ] **Step 4: Testler geçsin** · Run: `dotnet test` · Expected: tümü PASS.
- [ ] **Step 5: Commit** · `git add data/ core/ && git commit -m "data: starter cards and teams"`

---

### Görev 9: Simülatör

**Files:**
- Modify: `core/WeirdLand.Sim/Program.cs`
- Create: `core/WeirdLand.Sim/SimReport.cs`

**Interfaces:**
- Consumes: `DataLoader.Load`, `MatchEngine`, `MatchRunner.Play`, `RandomPolicy`, `GoodPolicy`.
- Komut: `dotnet run --project WeirdLand.Sim -- --matches 1000 --seed 1 --policy good|random --user mavi --opp kirmizi|all --data ../data`
- Varsayılanlar: 1000 maç, seed 1, good, mavi, kirmizi, `../data`.
- `--opp all`: her rakip için ayrı rapor verir.
- Her maçın seed'i `seed + i`. Böylece rapor tekrar üretilebilir.
- **Rapor satırları (Türkçe):**
  - başlık
  - Galibiyet / Beraberlik / Mağlubiyet yüzdesi
  - Ortalama gol (kullanıcı – rakip)
  - Ortalama düello, tur ve pas
  - Tahmini süre (dk) = `(Turns×15 + Duels×5)/60`, ortalama ve %90'lık dilim
  - En sık 8 skor

- [ ] **Step 1: `SimReport`'u yaz.** Girdi `List<MatchResult>`, çıktı satır listesi. Yüzdeler tam sayıya yuvarlanır.
- [ ] **Step 2: `Program.cs` argümanları ayrıştırsın.** Bilinmeyen argümanda kullanım metni basılır, çıkış kodu 1.
- [ ] **Step 3: Çalıştır**

Run: `dotnet run --project WeirdLand.Sim -- --matches 200 --seed 1 --policy good --opp all`
Expected: her rakip için rapor, istisna yok. Aynı komut ikinci kez çalıştırılınca birebir aynı çıktı.

- [ ] **Step 4: Commit** · `git add core/ && git commit -m "sim: command line balance simulator"`

---

### Görev 10: Denge ayarı ve rapor

**Files:**
- Modify: `data/cards.json`, `data/teams.json`, gerekirse `core/WeirdLand.Core/Duel/DuelRules.cs` ve `Match/MatchRules.cs` sabitleri
- Create: `docs/balance/m1-baseline.md`

**Hedefler** (2000 maç, seed 1):

| Ölçüm | Hedef |
|---|---|
| random vs tier 1 (kirmizi), kullanıcı galibiyeti | < %10 |
| good vs tier 1, galibiyet | ≥ %55 |
| good vs tier 3, galibiyet | %25 – %50 |
| good, ortalama tahmini süre | 5 – 10 dk |
| good, maç başı toplam gol ortalaması | 1.5 – 4.5 |

- [ ] **Step 1: Mevcut durumu ölç**
  - Run: `dotnet run --project WeirdLand.Sim -- --matches 2000 --policy random --opp kirmizi`
  - Run: `dotnet run --project WeirdLand.Sim -- --matches 2000 --policy good --opp all`
- [ ] **Step 2: Sayıları ayarla, tekrar ölç.**
  - Önce veri sayıları (kart değerleri, rakip statları), sonra kural sabitleri değişir.
  - Kural sabiti değişirse ilgili birim testindeki beklenen değer de güncellenir ve `dotnet test` geçmelidir.
- [ ] **Step 3: Sonucu kaydet.** `docs/balance/m1-baseline.md` dosyasına yaz:
  - son ölçümler
  - hedefle karşılaştırma tablosu
  - değiştirilen sabitler
  - ulaşılamayan hedef varsa nedeni
- [ ] **Step 4: Son doğrulama** · Run: `dotnet test` · Expected: tümü PASS.
- [ ] **Step 5: Commit** · `git add data/ core/ docs/balance/ && git commit -m "balance: M1 baseline tuning"`

---

## Kapsam dışı (sonraki kademeler)

- **Unity projesi ve paketleri:** Kademe 2.
- **Saha haritası ve görsel akış:** Kademe 3.
- **Kupa, mağaza, EXP/seviye, kart yükseltme seçimi, meyveler, penaltı serisi, kayıt:** Kademe 4. Çekirdek bunlara hazır: `CardInstance.Upgraded` ve `DuelKind.Penalty` var.
