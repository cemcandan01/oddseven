# Izgara maç akışı: uygulama planı

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Hedef:** 12 × 8 ızgarada, Pas at / İlerle seçimleriyle oynanan, rakibe bitişik olunca düello açan, yeni bir maç akışı. Çekirdekte, simülatörde ve Unity'de oynanabilir olacak.

**Mimari:** Çekirdekte yeni bir `GridMatch` sınıfı. Eski `MatchEngine`'e dokunulmaz. Düellolar mevcut `Duel` sınıfıyla oynanır. Izgara geometrisi (`GridGeometry`), hareket yapay zekâsı (`GridMovement`), pas hesabı (`GridPass`) ve karar politikaları (`GridPolicy`) ayrı dosyalarda durur. Unity'de yeni bir `GridMatch` sahnesi sahayı çizer ve düello açılınca mevcut düello ekranını kullanır.

**Teknoloji:** C# 9 / `netstandard2.1` çekirdek, NUnit, `WeirdLand.Sim` konsol uygulaması, Unity 6000.3.9f1 + UniTask + PrimeTween.

**Spec:** [docs/superpowers/specs/2026-11-23-grid-match-flow-design.md](../specs/2026-11-23-grid-match-flow-design.md)

## Genel kısıtlar
- `netstandard2.1`, `LangVersion 9.0`. File-scoped namespace, global using, record struct ve `required` kullanılmaz.
- Çekirdekte `UnityEngine` referansı yok.
- Tüm rastgelelik `WeirdLand.Core.Rng` üzerinden gelir. `HashSet` ve `Dictionary` üzerinde sırası önemli dolaşım yapılmaz.
- Komut girer, `GameEvent` listesi çıkar.
- Denge sayıları `MatchRules` içinde durur.
- Kod tanımlayıcıları İngilizce, oyuncuya görünen metinler Türkçe ve `data/ui_tr.json` içinde.
- Her görev sonunda `cd core && dotnet test` geçer.
- Commit önekleri: `core:`, `sim:`, `unity:`, `balance:`, `docs:`.

## İnceleme odağı
Spec'in ima ettiği ama görev testlerinin doğrudan yakalamadığı, kullanıcıyı en çok etkileyebilecek durumlar:
1. **Bütün komşu kutuları dolu taşıyıcı.** Ulaşılabilir kutu yoksa İlerle seçeneği boş liste döner, Pas hâlâ seçilebilir. Hiçbir şey çökmez. Görev 3'e test eklenir.
2. **Hiç uygun alıcı olmayan pas.** Kaleci hariç tüm takım arkadaşları geçerli alıcıdır, liste hiçbir zaman boş olmaz. Kendine pas ya da rakibe pas `ArgumentException` fırlatır. Görev 6'ya test eklenir.
3. **Aynı ikili arasında düello döngüsü.** Sersemlik kuralı aynı iki oyuncu arasında üst üste 3'ten fazla düelloyu engeller. Görev 8'deki 200 seed'lik test bunu doğrular.
4. **Maç süresi düello ortasında biterse.** Son hamledeki düello bitmeden maç bitmez. Hamle sayacı düello çözülünce artar. Görev 7'ye test eklenir.
5. **Hareket hedefi doluysa.** İki oyuncu aynı kutuyu hedeflerse sıradaki oyuncu en yakın boş kutuya gider. Kutu başına bir oyuncu kuralı hiç bozulmaz. Görev 4'e bir değişmezlik testi eklenir (her hamleden sonra konumlar benzersiz).

---

### Görev 1: Izgara geometrisi ve kurallar

**Dosyalar:**
- Oluştur: `core/WeirdLand.Core/Match/Grid/GridGeometry.cs`
- Değiştir: `core/WeirdLand.Core/Match/MatchRules.cs` (yeni sabitler)
- Test: `core/WeirdLand.Core.Tests/GridGeometryTests.cs`

**Arayüzler:**
- Üretir:
  - `public readonly struct Cell : IEquatable<Cell> { public int X { get; } public int Y { get; } public Cell(int x, int y) }`
  - `static class GridGeometry`:
    - `int Width = 12, Height = 8` (sabitler `MatchRules` içinden: `GridWidth`, `GridHeight`)
    - `bool InBounds(Cell c)`
    - `int Distance(Cell a, Cell b)` (Chebyshev)
    - `bool Adjacent(Cell a, Cell b)` (Distance == 1)
    - `IReadOnlyList<Cell> Line(Cell from, Cell to)` (Bresenham, iki ucu da dahil)
    - `Cell Home(Slot slot, bool userSide)` (spec 6.2 tablosu. Rakip için `x → 11 − x`)
    - `bool InShotZone(Cell c, bool forUser)` (kullanıcı: x ≥ 10 ve 2 ≤ y ≤ 5. Rakip: x ≤ 1 ve 2 ≤ y ≤ 5)
    - `int AttackDir(bool userSide)` (kullanıcı +1, rakip −1)
- `MatchRules` içine eklenecek sabitler: `GridWidth = 12`, `GridHeight = 8`, `GridMatchMoves = 90`, `StunMoves = 2`, `FastSpeed = 4`, `PassBase = 0.95`, `PassPerDistance = 0.04`, `PassPerStat = 0.03`, `PassPerInterceptor = 0.20`, `PassPerReceiverMarker = 0.10`, `PassMin = 0.05`, `PassMax = 0.95`, `ShiftFactor = 0.5`.

- [ ] **Adım 1: Başarısız testleri yaz**

```csharp
[Test] public void Distance_IsChebyshev() => Assert.That(GridGeometry.Distance(new Cell(0,0), new Cell(3,1)), Is.EqualTo(3));
[Test] public void Adjacent_IncludesDiagonal() => Assert.That(GridGeometry.Adjacent(new Cell(2,2), new Cell(3,3)), Is.True);
[Test] public void Line_HasBothEnds_AndIsContiguous()
{
    var l = GridGeometry.Line(new Cell(0,0), new Cell(5,2));
    Assert.That(l[0], Is.EqualTo(new Cell(0,0)));
    Assert.That(l[l.Count-1], Is.EqualTo(new Cell(5,2)));
    for (int i = 1; i < l.Count; i++) Assert.That(GridGeometry.Adjacent(l[i-1], l[i]), Is.True);
}
[Test] public void Home_UserCm_Is_4_3() => Assert.That(GridGeometry.Home(Slot.CM, true), Is.EqualTo(new Cell(4,3)));
[Test] public void Home_OpponentIsMirrored() => Assert.That(GridGeometry.Home(Slot.LB, false), Is.EqualTo(new Cell(9,1)));
[Test] public void ShotZone_User() { Assert.That(GridGeometry.InShotZone(new Cell(10,2), true), Is.True); Assert.That(GridGeometry.InShotZone(new Cell(10,1), true), Is.False); Assert.That(GridGeometry.InShotZone(new Cell(1,4), false), Is.True); }
```

- [ ] **Adım 2: Çalıştır, başarısız olduğunu gör**

Çalıştır: `cd core && dotnet test --filter GridGeometryTests`
Beklenen: derleme hatası, `GridGeometry` tanımlı değil.

- [ ] **Adım 3: `Cell`, `GridGeometry` ve `MatchRules` sabitlerini yaz**

- [ ] **Adım 4: Testleri çalıştır, geçtiğini gör**

Çalıştır: `cd core && dotnet test`
Beklenen: hepsi geçer.

- [ ] **Adım 5: Commit**

```bash
git add core && git commit -m "core: grid geometry and grid match constants"
```

---

### Görev 2: GridMatch durumu, başlama ve olaylar

**Dosyalar:**
- Oluştur: `core/WeirdLand.Core/Match/Grid/GridMatch.cs`, `core/WeirdLand.Core/Match/Grid/GridPlayer.cs`
- Değiştir: `core/WeirdLand.Core/Events.cs` (yeni olaylar)
- Test: `core/WeirdLand.Core.Tests/GridMatchTests.cs`

**Arayüzler:**
- Kullanır: Görev 1'deki `Cell`, `GridGeometry`.
- Üretir:
  - `public sealed class GridPlayer { PlayerDef Def; bool IsUser; Cell Pos; int Stun; List<CardInstance> Deck; }` Dışarıya salt okunur yüzey verir: `Def`, `IsUser`, `Pos`, `Stun`.
  - `public enum GridPhase { AwaitingAction, InDuel, Ended }`
  - `public sealed class GridMatch`:
    - `GridMatch(GameData data, TeamDef user, TeamDef opponent, uint seed)`
    - `IReadOnlyList<GridPlayer> Players` (sıra: önce kullanıcı, sonra rakip, her takım içinde `Slot` sırası)
    - `GridPlayer Carrier`, `bool UserHasBall`, `GridPhase Phase`, `int Move`, `int UserScore`, `int OppScore`, `Duel CurrentDuel`
    - `IReadOnlyList<GameEvent> Kickoff()`. Herkesi ev kutusuna koyar, ilk sahipliği `Rng` ile belirler, taşıyıcı o takımın CM'si olur.
    - `GridPlayer PlayerAt(Cell c)` (boşsa null)
- Yeni olaylar (`Events.cs`):
  - `GridKickoff(bool UserHasBall, string CarrierId)`
  - `PlayerMoved(string PlayerId, int FromX, int FromY, int ToX, int ToY)`
  - `BallPassed(string FromId, string ToId)`
  - `PassIntercepted(string FromId, string IntendedId, string InterceptorId)`
  - `DuelTriggered(string CarrierId, string DefenderId)`
  - `PlayerStunned(string PlayerId, int Moves)`
  - `PossessionChanged(bool UserHasBall, string CarrierId)`
  - `ShotChance(bool ByUser, string PlayerId)`
  - `MatchMoveAdvanced(int Move)`

- [ ] **Adım 1: Başarısız testleri yaz**

```csharp
[Test] public void Kickoff_PlacesAll14OnHomeCells_Unique()
{
    var m = TestKit.NewGridMatch(seed: 1);
    m.Kickoff();
    Assert.That(m.Players.Count, Is.EqualTo(14));
    Assert.That(m.Players.Select(p => p.Pos).Distinct().Count(), Is.EqualTo(14));
    foreach (var p in m.Players) Assert.That(p.Pos, Is.EqualTo(GridGeometry.Home(p.Def.Slot, p.IsUser)));
}
[Test] public void Kickoff_CarrierIsCmOfTeamWithBall()
{
    var m = TestKit.NewGridMatch(seed: 1); var ev = m.Kickoff();
    Assert.That(m.Carrier.Def.Slot, Is.EqualTo(Slot.CM));
    Assert.That(m.Carrier.IsUser, Is.EqualTo(m.UserHasBall));
    Assert.That(ev.OfType<GridKickoff>().Single().CarrierId, Is.EqualTo(m.Carrier.Def.Id));
    Assert.That(m.Phase, Is.EqualTo(GridPhase.AwaitingAction));
}
[Test] public void Kickoff_SameSeed_SamePossession() { /* seed 1..20 için iki eşleşmenin UserHasBall değeri aynı */ }
```

`TestKit.NewGridMatch(uint seed)`: `TestKit.RealData()` üzerinden `mavi` ve `kirmizi` ile kurar. Bu yardımcıyı da bu adımda ekle.

- [ ] **Adım 2: Çalıştır, başarısız olduğunu gör**

Çalıştır: `cd core && dotnet test --filter GridMatchTests`
Beklenen: derleme hatası.

- [ ] **Adım 3: `GridPlayer`, `GridMatch` (kurucu + `Kickoff`) ve olayları yaz**

Desteler `DuelSetupHelpers.BuildDeck` ile kurulur. `Rng` alanı kurucudaki seed'den gelir.

- [ ] **Adım 4: Testleri çalıştır, geçtiğini gör**

Çalıştır: `cd core && dotnet test`

- [ ] **Adım 5: Commit**

```bash
git add core && git commit -m "core: grid match state, kickoff and grid events"
```

---

### Görev 3: İlerle eylemi ve ulaşılabilir kutular

**Dosyalar:**
- Değiştir: `core/WeirdLand.Core/Match/Grid/GridMatch.cs`
- Test: `core/WeirdLand.Core.Tests/GridMatchTests.cs`

**Arayüzler:**
- Üretir:
  - `IReadOnlyList<Cell> ReachableCells()`: taşıyıcı için. Mesafe 1 olan boş kutular. Taşıyıcının Hız'ı ≥ `MatchRules.FastSpeed` ise mesafe 2 olan boş kutular da eklenir, ama en az bir boş ara kutu varsa. Ara kutu: hem başlangıca hem varışa bitişik olan kutu. Sıralama x sonra y.
  - `IReadOnlyList<GameEvent> Advance(Cell to)`: `Phase == AwaitingAction` değilse `InvalidOperationException`. `to` ulaşılabilir değilse `ArgumentException`. Taşıyıcıyı taşır, `PlayerMoved` üretir, sonra hamle kuyruğunu çalıştırır (Görev 4'te hareket, Görev 5'te düello, Görev 7'de bölge ve süre). Bu görevde kuyruk sadece taşımayı yapar.

- [ ] **Adım 1: Başarısız testleri yaz**

```csharp
[Test] public void Reachable_SlowCarrier_OnlyDistance1() { /* Hız 3 olan taşıyıcı: her sonuç Distance == 1 */ }
[Test] public void Reachable_FastCarrier_IncludesDistance2() { /* Hız 4: en az bir Distance == 2 sonucu var */ }
[Test] public void Reachable_CannotHopOverOpponentWall() { /* taşıyıcı (5,3), Hız 4. Rakipler (6,2),(6,3),(6,4): (7,3) listede yok */ }
[Test] public void Reachable_AllNeighboursOccupied_ReturnsEmpty() { /* İnceleme odağı 1 */ }
[Test] public void Advance_ToUnreachableCell_Throws() => Assert.Throws<ArgumentException>(...);
[Test] public void Advance_MovesCarrier_EmitsPlayerMoved() { }
```

Konumları zorlamak için `internal void SetPos(string playerId, Cell c)` ve `internal void SetCarrier(string playerId)` test kancalarını ekle. Bu, `MatchEngine`'deki `Forced*` kalıbıyla aynı.

- [ ] **Adım 2: Çalıştır, başarısız olduğunu gör**
- [ ] **Adım 3: `ReachableCells` ve `Advance`'i yaz**
- [ ] **Adım 4: `cd core && dotnet test` geçer**
- [ ] **Adım 5: Commit** `core: grid advance action and reachable cells`

---

### Görev 4: Hareket yapay zekâsı

**Dosyalar:**
- Oluştur: `core/WeirdLand.Core/Match/Grid/GridMovement.cs`
- Değiştir: `GridMatch.cs` (hamle kuyruğuna hareket adımı)
- Test: `core/WeirdLand.Core.Tests/GridMovementTests.cs`

**Arayüzler:**
- Üretir: `internal static class GridMovement { static void Step(GridMatch m, List<GameEvent> ev) }`
- Kurallar (spec 6.6):
  - Sıra: önce savunan takım, sonra hücum eden takım. Takım içinde `Slot` sırası. Taşıyıcı ve `Stun > 0` olan oyuncular hareket etmez.
  - Her oyuncu en fazla 1 kutu gider. Bir hedef kutu belirlenir, sonra hedefe Chebyshev mesafesini en çok azaltan boş komşuya geçilir. Eşitlikte x sonra y. Hiçbir boş komşu mesafeyi azaltmıyorsa yerinde kalır.
  - **Baskıcı:** savunan takımda, kaleci hariç ve `Stun == 0` olanlar içinde taşıyıcıya en yakın oyuncu. Eşitlikte `Slot` sırası. Hedefi taşıyıcının kutusu.
  - **Diğer saha oyuncuları (iki takım):** hedef = ev kutusu, x'i `ev.x + round((carrier.x − 5.5) × ShiftFactor)` ile kaydırılmış ve sınırlar içine kırpılmış hali. Yuvarlama `Math.Round(v, MidpointRounding.AwayFromZero)`.
  - **Hücum eden takımın topsuz oyuncuları:** aynı hedef. Hedefe eşit uzaklıkta birden fazla komşu varsa, bitişiğinde en az rakip olan seçilir.
  - **Kaleciler:** x ev kutusunda sabit kalır, y = topun y'si 2..5 aralığına kırpılmış hali.

- [ ] **Adım 1: Başarısız testleri yaz**

```csharp
[Test] public void Presser_IsNearestNonKeeper_MovesOneCellTowardCarrier() { }
[Test] public void StunnedPlayer_DoesNotMove() { }
[Test] public void Keeper_TracksBallY_ClampedTo2_5() { }
[Test] public void DefensiveLine_ShiftsWithBall() { /* taşıyıcı x=10 iken rakip LB'nin hedef x'i ev x'inden büyük değil: geri kaçmıyor, topa doğru kayıyor */ }
[Test] public void Positions_StayUnique_Over200Moves() { /* İnceleme odağı 5: seed 1..10, her Advance sonrası 14 konum benzersiz */ }
[Test] public void Movement_IsDeterministic() { /* aynı seed ve aynı komut dizisi aynı PlayerMoved listesi */ }
```

- [ ] **Adım 2: Çalıştır, başarısız olduğunu gör**
- [ ] **Adım 3: `GridMovement.Step`'i yaz, `Advance` kuyruğuna bağla**
- [ ] **Adım 4: `cd core && dotnet test` geçer**
- [ ] **Adım 5: Commit** `core: grid movement ai`

---

### Görev 5: Düello tetikleme ve düello sonrası

**Dosyalar:**
- Değiştir: `GridMatch.cs`
- Test: `core/WeirdLand.Core.Tests/GridDuelTests.cs`

**Arayüzler:**
- Kullanır: `Duel`, `DuelSetup`, `MatchRules.PowerScalePercent(int tier)`.
- Üretir:
  - Hamle kuyruğunda, hareketten sonra düello kontrolü: taşıyıcıya bitişik, `Stun == 0`, kaleci olmayan rakipler içinden en yakını, eşitlikte `Slot` sırası. Varsa `DuelTriggered` üretilir, `CurrentDuel` kurulur ve `Begin()` olayları eklenir, `Phase = InDuel`.
  - Düello kurulumu (spec 6.4.1): `Kind = Field`, `UserIsAttacker = UserHasBall`, `EnemyPowerScalePercent = MatchRules.PowerScalePercent(opponent.Tier)`, iki taraf kendi `PlayerDef` ve destesiyle. Pas hedefi listeleri boş. Momentum, Alev, Kondisyon ve taktik bonusları yok.
  - `IReadOnlyList<GameEvent> CompleteDuel()`: `Phase == InDuel` ve `CurrentDuel.Outcome` dolu olmalı.
    - Hücum kazanırsa: savunmacı `Stun = StunMoves`, `PlayerStunned` üretilir. Taşıyıcı aynı kalır.
    - Savunma kazanırsa: eski taşıyıcı `Stun = StunMoves`, top savunmacıya geçer, `PossessionChanged` üretilir. Yeni taşıyıcı için hemen düello kontrolü yapılır (spec 6.9).
    - Kontrol yeni bir düello açmazsa hamle kuyruğunun kalanı (Görev 7) çalışır ve `Phase = AwaitingAction`.
  - Sersemlik sayacı her hamlenin sonunda 1 azalır (0'ın altına inmez).

- [ ] **Adım 1: Başarısız testleri yaz**

```csharp
[Test] public void AdvanceNextToDefender_OpensDuel_WithBothOwnDecks() { }
[Test] public void Duel_HasNoPassTargets() => Assert.That(m.CurrentDuel.CanPass, Is.False);
[Test] public void Keeper_NeverTriggersDuel() { }
[Test] public void StunnedDefender_NeverTriggersDuel() { }
[Test] public void AttackerWins_DefenderStunned2_CarrierKeepsBall() { /* duel.ForceOutcome(AttackerWon) ile */ }
[Test] public void DefenderWins_PossessionFlips_OldCarrierStunned() { }
[Test] public void DefenderWins_NewCarrierNextToFreshOpponent_DuelOpensImmediately() { }
[Test] public void Stun_TicksDownOncePerMove_AndBlocksForTwoMoves() { }
[Test] public void UserDefending_ThreeTurnRule_AttackerLosesBall() { /* rakip taşıyıcı, kullanıcı savunur, üç tur EndTurn: Outcome DefenderWon, top kullanıcıya geçer */ }
```

- [ ] **Adım 2: Çalıştır, başarısız olduğunu gör**
- [ ] **Adım 3: Düello kontrolünü, kurulumu ve `CompleteDuel`'i yaz**
- [ ] **Adım 4: `cd core && dotnet test` geçer**
- [ ] **Adım 5: Commit** `core: grid duel trigger and post-duel resolution`

---

### Görev 6: Pas

**Dosyalar:**
- Oluştur: `core/WeirdLand.Core/Match/Grid/GridPass.cs`
- Değiştir: `GridMatch.cs`
- Test: `core/WeirdLand.Core.Tests/GridPassTests.cs`

**Arayüzler:**
- Üretir:
  - `internal static class GridPass`:
    - `IReadOnlyList<GridPlayer> Interceptors(GridMatch m, GridPlayer from, GridPlayer to)`: pas çizgisinin uçlar hariç her kutusu için o kutuda ya da bitişiğinde duran, kaleci olmayan rakipler. Tekrar yok, sıra çizgi sırası sonra `Slot`.
    - `double Chance(GridMatch m, GridPlayer from, GridPlayer to)`: spec 6.5 formülü, `MatchRules` sabitleriyle, `PassMin..PassMax` aralığına kırpılır.
  - `GridMatch` üzerinde:
    - `IReadOnlyList<GridPlayer> PassTargets()`: taşıyıcının takımındaki, taşıyıcı ve kaleci hariç herkes.
    - `bool PassLineContested(string targetId)`: sadece görsel ipucu için. `Interceptors` boş değilse true. İhtimal dışarı açılmaz.
    - `IReadOnlyList<GameEvent> Pass(string targetId)`: `Rng.Chance(Chance)`. Başarılıysa `BallPassed` ve alıcı taşıyıcı olur. Başarısızsa top, kesme adaylarından alıcıya en yakın olana geçer. Aday yoksa alıcının bitişiğindeki en yakın rakibe, o da yoksa alıcıya en yakın rakibe geçer. `PassIntercepted` ve `PossessionChanged` üretilir. İki durumda da hamle kuyruğu (hareket, düello, bölge, süre) çalışır.
    - Kendine pas, kaleciye pas ya da rakibe pas `ArgumentException` fırlatır.

- [ ] **Adım 1: Başarısız testleri yaz**

```csharp
[Test] public void Chance_OpenShortPass_IsHigh() { /* mesafe 2, aday yok, Pas 4: 0.95 − 0.08 + 0.12 → 0.95'e kırpılır */ }
[Test] public void Chance_EachInterceptorCosts020() { /* aynı pas, bir aday eklenince fark 0.20 */ }
[Test] public void Chance_ReceiverMarkerCosts010() { }
[Test] public void Chance_ClampedTo005() { /* uzun pas + 4 aday */ }
[Test] public void Interceptors_IncludeAdjacentToLine_ExcludeEndpointsAndKeepers() { }
[Test] public void Pass_Success_ReceiverBecomesCarrier() { /* seed seçilerek */ }
[Test] public void Pass_Fail_BallGoesToInterceptorNearestReceiver() { }
[Test] public void Pass_ToSelf_OrKeeper_OrOpponent_Throws() { /* İnceleme odağı 2 */ }
[Test] public void PassTargets_NeverEmpty() { }
```

- [ ] **Adım 2: Çalıştır, başarısız olduğunu gör**
- [ ] **Adım 3: `GridPass` ve `GridMatch.Pass`'i yaz**
- [ ] **Adım 4: `cd core && dotnet test` geçer**
- [ ] **Adım 5: Commit** `core: grid pass with hidden chance and interceptions`

---

### Görev 7: Şut bölgesi, hamle sayacı ve maç sonu

**Dosyalar:**
- Değiştir: `GridMatch.cs`
- Test: `core/WeirdLand.Core.Tests/GridMatchTests.cs`

**Arayüzler:**
- Üretir (hamle kuyruğunun sonu, düello varsa `CompleteDuel` sonrasında):
  1. Taşıyıcı topa sahip takımın şut bölgesindeyse: `ShotChance` ve skor +1 (geçici gol kuralı, spec 6.8). Yeni atak: gol yiyen takım topu alır, herkes ev kutusuna döner, sersemlikler sıfırlanır, taşıyıcı CM olur. `GridKickoff` üretilir.
  2. Sersemlik sayaçları 1 azalır.
  3. `Move++` ve `MatchMoveAdvanced`.
  4. `Move >= MatchRules.GridMatchMoves` ise `MatchEnded(UserScore, OppScore)` ve `Phase = Ended`.
- Düello sürerken hamle sayacı artmaz. Son hamledeki düello bitmeden maç bitmez.

- [ ] **Adım 1: Başarısız testleri yaz**

```csharp
[Test] public void AdvanceIntoShotZone_ScoresAndResets() { }
[Test] public void PassToTeammateInShotZone_Scores() { }
[Test] public void ShotZoneWithAdjacentDefender_DuelFirst() { /* düello hücum kazanırsa gol, savunma kazanırsa gol yok */ }
[Test] public void MatchEnds_AfterGridMatchMoves() { }
[Test] public void MatchDoesNotEnd_MidDuel() { /* İnceleme odağı 4: Move = 89 iken düello açan hamle; CompleteDuel'e kadar Phase InDuel */ }
[Test] public void ActionsAfterEnd_Throw() { }
```

- [ ] **Adım 2: Çalıştır, başarısız olduğunu gör**
- [ ] **Adım 3: Kuyruğun sonunu yaz**
- [ ] **Adım 4: `cd core && dotnet test` geçer**
- [ ] **Adım 5: Commit** `core: grid shot zone placeholder goal and match clock`

---

### Görev 8: Karar politikaları, koşturucu ve altın test

**Dosyalar:**
- Oluştur: `core/WeirdLand.Core/Ai/GridPolicy.cs`, `core/WeirdLand.Core/Ai/GridRunner.cs`
- Değiştir: `GridMatch.cs` (`ResolveOpponentMove`)
- Test: `core/WeirdLand.Core.Tests/GridRunnerTests.cs`, `core/WeirdLand.Core.Tests/EventLogTests.cs`

**Arayüzler:**
- Üretir:
  - `public abstract record GridAction; record GridAdvance(Cell To); record GridPassTo(string TargetId);`
  - `public interface IGridPolicy { GridAction Decide(GridMatch m, Rng rng); }`
  - `public sealed class GreedyGridPolicy : IGridPolicy`: spec 6.7. Her alıcı için pas puanı = `Chance × (1 − kaleye mesafe / 11)`. Her ulaşılabilir kutu için ilerleme puanı = `(1 − kaleye mesafe / 11) − 0.25 × bitişikteki rakip sayısı`. "Kaleye mesafe" = hücum yönündeki son sütuna kadar x farkı. En yüksek puan kazanır. Eşitlikte önce İlerle, sonra x, sonra y, sonra `Slot`. Hem kullanıcı hem rakip tarafı için çalışır.
  - `public sealed class RandomGridPolicy : IGridPolicy`: ulaşılabilir kutular ve alıcılar arasından eşit olasılıkla seçer.
  - `GridMatch.ResolveOpponentMove()`: rakip topa sahipken `GreedyGridPolicy` ile kendi kararını uygular. İç `Rng`'yi kullanır.
  - `public static class GridRunner { static GridResult Play(GridMatch m, IGridPolicy userPolicy, IUserPolicy duelPolicy, Rng rng) }`. `GridResult { int UserScore, OppScore, Duels, Moves; int MaxSameDuelStreak; List<GameEvent> Events; }`. Düellolar mevcut `IUserPolicy` (`GoodPolicy` / `RandomPolicy`) ile oynanır. `match` parametresine null geçilir.

- [ ] **Adım 1: Başarısız testleri yaz**

```csharp
[Test] public void Runner_FinishesMatch_Seeds1to50() { }
[Test] public void Runner_NoSamePairDuelsMoreThan3InARow_200Seeds() { /* İnceleme odağı 3: MaxSameDuelStreak <= 3 */ }
[Test] public void Greedy_PrefersOpenForwardPassOverBlockedOne() { }
[Test] public void Golden_GridMatchFingerprint() { /* seed 1, Greedy + GoodPolicy: EventLog.Fingerprint sabit bir değere eşit. İlk çalıştırmada değeri kaydet */ }
```

- [ ] **Adım 2: Çalıştır, başarısız olduğunu gör**
- [ ] **Adım 3: Politikaları, `ResolveOpponentMove`'u ve `GridRunner`'ı yaz**
- [ ] **Adım 4: `cd core && dotnet test` geçer**
- [ ] **Adım 5: Commit** `core: grid policies, runner and golden fingerprint`

---

### Görev 9: Simülatörde ızgara modu ve denge ölçümü

**Dosyalar:**
- Değiştir: `core/WeirdLand.Sim/Program.cs`, `core/WeirdLand.Sim/SimReport.cs`
- Oluştur: `docs/balance/2026-11-23-grid-match-baseline.md`
- Değiştir: `CLAUDE.md` (Komutlar bölümüne `--mode grid` örneği)

**Arayüzler:**
- `--mode classic|grid` seçeneği, varsayılan `classic`. `grid` modunda `--policy good` = `GreedyGridPolicy` + `GoodPolicy`, `--policy random` = `RandomGridPolicy` + `RandomPolicy`.
- Rapor satırları: galibiyet / beraberlik / mağlubiyet, ortalama gol, ortalama düello sayısı, ortalama pas sayısı, pas başarı oranı, en uzun aynı ikili düello serisi.

- [ ] **Adım 1: Seçeneği ve raporu ekle**
- [ ] **Adım 2: Ölç**

Çalıştır: `cd core && dotnet run --project WeirdLand.Sim -- --mode grid --matches 1000 --seed 1 --policy good --opp all` ve aynısını `--policy random --opp kirmizi` ile.

Spec bölüm 8 hedefleri: ortalama düello sayısı 10–25, iyi ve rastgele politika arasında belirgin fark. Tutmazsa `MatchRules` katsayıları ayarlanır (`GridMatchMoves`, pas katsayıları, `StunMoves`) ve yeniden ölçülür. Değişiklik altın parmak izini değiştirirse Görev 8'deki değer güncellenir.

- [ ] **Adım 3: Sonuçları ve yapılan ayarları `docs/balance/2026-11-23-grid-match-baseline.md` dosyasına yaz**
- [ ] **Adım 4: `cd core && dotnet test` geçer**
- [ ] **Adım 5: Commit** `sim: grid mode; balance: grid match baseline`

---

### Görev 10: Unity'de ızgara maç sahnesi

**Dosyalar:**
- Oluştur: `unity/Assets/WeirdLand/Runtime/Grid/GridMatchController.cs`, `GridBoardView.cs`, `GridActionPanel.cs`
- Değiştir: `unity/Assets/WeirdLand/Editor/SceneBuilder.cs` (yeni `GridMatch` sahnesi), `unity/Assets/WeirdLand/Runtime/App/AppFlow.cs` (sandbox seçim ekranına "Maç (ızgara)" girişi), `data/ui_tr.json` (yeni metinler)
- Test: `unity/Assets/WeirdLand/Tests/PlayMode/GridMatchTests.cs`

**Arayüzler:**
- Kullanır: `GridMatch`, `DuelController`, mevcut `DuelScreen`.
- `GridMatchController`: `DuelController`'daki gibi giriş kilidi ve sıralı olay oynatma. Komutlar: `Advance(Cell)`, `Pass(string)`, düello komutları. Rakip topa sahipken `ResolveOpponentMove()`'u kendiliğinden çağırır ve olayları sırayla oynatır.
- `GridBoardView`: 12 × 8 sahayı yukarıdan açılı çizer. Oyuncular yer tutucu dairelerle gösterilir, mavi ve kırmızı. Sersemleyen oyuncu soluk görünür. `PlayerMoved` için kısa PrimeTween hareketi.
- `GridActionPanel`: üç düğme.
  - **İlerle:** `ReachableCells()` vurgulanır, tıklanınca `Advance`.
  - **Pas at:** her `PassTargets()` alıcısına çizgi. `PassLineContested` true ise kesik çizgi. Yüzde yok. Alıcıya tıklanınca `Pass`.
  - **Saha:** paneli gizler, tekrar basınca gösterir. Hamle harcamaz.
- `DuelTriggered` gelince düello ekranı açılır. `DuelEnded` gelince `CompleteDuel()` çağrılır ve sahaya dönülür.
- Yeni `ui_tr.json` anahtarları: `grid.advance` "İlerle", `grid.pass` "Pas at", `grid.field` "Saha", `grid.move` "Hamle {0}/{1}", `grid.shotChance` "Gol fırsatı!", `grid.possessionLost` "Top kaybedildi".

- [ ] **Adım 1: PlayMode testini yaz**

```csharp
[UnityTest] public IEnumerator GridMatch_AdvanceAndPass_UpdateBoard() { /* sahne yüklenir, kullanıcı topa sahip kurulur, Advance sonrası taşıyıcı görseli yeni kutuda */ }
[UnityTest] public IEnumerator GridMatch_DuelOpensDuelScreen_AndReturns() { }
[UnityTest] public IEnumerator GridMatch_FieldButton_HidesPanel_NoMoveSpent() { }
```

- [ ] **Adım 2: `tools/unity.sh tests playmode` çalıştır, başarısız olduğunu gör**
- [ ] **Adım 3: Controller, görünüm, panel ve sahneyi yaz. `tools/unity.sh scenes` ile sahneyi üret**
- [ ] **Adım 4: `cd core && dotnet test && cd .. && tools/unity.sh tests all` geçer**
- [ ] **Adım 5: `tools/unity.sh shot` ile ekran görüntüsü al, `unity/Screenshots/` altına kaydet**
- [ ] **Adım 6: Commit** `unity: grid match scene with advance, pass and field view`
- [ ] **Adım 7: Kullanıcı oyun testi.** Kullanıcı bir maçı baştan sona oynar. İzlenimler `docs/balance/grid-playtest.md` dosyasına yazılır.
