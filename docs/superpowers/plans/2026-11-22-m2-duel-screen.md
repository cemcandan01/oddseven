# Kademe 2: Düello Ekranı (Unity) — İş Planı

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Unity 6'da tek bir düelloyu baştan sona oynatan ekranı kurmak. Ekranda el, enerji, niyet, can çubukları, durum etkileri, kart animasyonları ve yer tutucu çöp adamlar olacak. Dört düello türü bir seçim ekranından açılacak: hücumda saha, savunmada saha, şutçu olarak şut, kaleci olarak şut. Kademe sonunda oyun hem editörde Play ile hem de tek başına çalışan bir Windows `.exe` ile oynanır.

**Architecture:**
- Kurallar `WeirdLand.Core`'da kalır. Unity çekirdeği derlenmiş DLL olarak alır: `dotnet build` çıktısı `unity/Assets/Plugins/WeirdLand/` klasörüne kopyalanır.
- Unity tarafında üç katman var:
  - Saf C# denetleyici (`DuelController`): komut gönderir, gelen olayları sırayla `IDuelView`'a oynatır, oynatma sürerken yeni girdiyi reddeder.
  - `MonoBehaviour` ekranlar: `DuelScreen`, `ScenarioScreen`, `AppFlow`.
  - Editör script'leriyle üretilen prefab'lar: kart, çöp adam, durum rozeti, düğme.
- Ekranlar bu prefab'ları çalışma anında koddan dizer, sahne neredeyse boştur.
- **Sözleşme:** animasyon olayın kendi değerlerini kullanır. Bir komutun olayları bitince ekran her şeyi durumdan yeniden okur (`Sync`).

**Tech Stack:**
- Unity 6000.3.9f1, uGUI ve TextMeshPro (`com.unity.ugui`).
- PrimeTween 1.3.3 ve UniTask 2.5.11 (ikisi de OpenUPM'den).
- Input System, Newtonsoft JSON 3.2.2 (`com.unity.nuget.newtonsoft-json`), Unity Test Framework (EditMode ve PlayMode).
- Çekirdek tarafı Kademe 1 ile aynı: .NET 10 SDK, C# 9, netstandard2.1, NUnit.

**Spec:** `docs/superpowers/specs/2026-11-22-weirdland-design.md` (Kademe 2 için §5.4, §7 madde 2, §8). Devreden işler: `docs/superpowers/m2-backlog.md` ("Kademe 2'nin ilk görevinde" bölümü).

## Global Constraints

- Çekirdek kuralları aynen geçerli: `netstandard2.1`, `LangVersion 9.0`, C# 10+ özelliği yok, `UnityEngine` yok, tüm oyun rastgeleliği `WeirdLand.Core.Rng` üzerinden.
- Unity kodu çekirdek durumunu **sadece** `Duel` komutlarıyla değiştirir: `Begin`, `PlayCard`, `Pass`, `EndTurn`. Görev 1'den sonra bu kural derleme düzeyinde zorlanır.
- Oyun rastgeleliği Unity'de de sadece çekirdekten gelir. Unity'nin rastgele bir şey yapmasına tek izin seed seçmektir: `(uint)Environment.TickCount`. Seçilen seed ekranda görünür.
- Oyuncuya görünen her metin `data/ui_tr.json` içinden `UiText` ile gelir. Kart, takım ve oyuncu adları zaten `cards.json` ve `teams.json` içinde.
  - Tek istisna: veri yüklenemediğinde gösterilen hata metni. O an `UiText` de yoktur, bu yüzden `AppFlow` içinde sabit bir Türkçe metin olarak durur.
- Kültürden bağımsız biçimleme:
  - Sayı ve yüzde gösterimi `UiText.Percent` ile yapılır, sonuç `%65` biçiminde olur.
  - Olay parmak izi değişmez kültürle (invariant culture) üretilir.
  - Kodda `ToUpper()` ya da `ToLower()` kullanılmaz. Gerekirse `ToUpperInvariant()` kullanılır. Makinenin yerel ayarı tr-TR.
- Ekran 1920×1080 temel alınarak tasarlanır: `CanvasScaler` ScaleWithScreenSize, eşleşme 0.5. Canvas `ScreenSpaceCamera` modundadır, böylece ekran görüntüsü testi kameradan görüntü alabilir.
- Unity kodu C# 9 ile yazılır, namespace'ler blok biçiminde. Namespace'ler: `WeirdLand.Unity`, editör kodu için `WeirdLand.Unity.EditorTools`, testler için `WeirdLand.Unity.Tests`.
- Animasyon süreleri tek bir yerde durur: `AnimTimings` (Görev 7). Her süre `Scale` ile çarpılır. Testler `Scale`'i `0.01` yapar. Süre 0 ya da daha azsa tween hiç başlatılmaz, son değer doğrudan atanır.
- Unity'ye bağlanan her komut `tools/unity.sh` üzerinden çalışır (Görev 5). Unity editörü projede açıkken toplu (batch) komut çalışmaz. Script bunu fark edip uyarır.
- Görev 4'ten sonra çekirdeğin davranışı bu kademede değişmez. Golden parmak izi (Görev 4) bunu bekçi olarak tutar.
- Commit önekleri: `core:`, `data:`, `unity:`, `docs:`. Dal: `m2-duel-screen`.

## Review Focus

1. **Animasyon sürerken çift tıklama ya da tuşa art arda basma.** Beklenen: ikinci komut sessizce yok sayılır, enerji bir kez düşer, çekirdek istisna fırlatmaz. Test: Görev 6 `PlayCard_WhileBusy_ReturnsFalse_DuelUntouched` ve Görev 8 `CardClick_Twice_SpendsEnergyOnce`.
2. **Türkçe karakterler** (ğ ş ı İ ö ç ü). Beklenen: TextMeshPro'da kutu karakter çıkmaz. Test: Görev 5 `TurkishGlyphs_AvailableInDefaultFont`.
3. **`.exe` veri klasörünü bulamaz** (StreamingAssets'e kopyalanmamış, silinmiş ya da yolu yanlış). Beklenen: boş ekran ya da çökme değil, yolu yazan bir hata paneli. Test: Görev 5 `Load_MissingDirectory_ThrowsDataExceptionNamingPath`, Görev 9 `DataMissing_ShowsErrorPanelWithPath`, Görev 10 duman çalıştırması.
4. **Animasyon ortasında ekrandan çıkmak** ("Seçime dön" ya da Play'i durdurmak). Beklenen: istisna yok, tween'ler durur, denetleyici meşgul durumda takılı kalmaz. Test: Görev 6 `Cancel_DuringPlayback_ReturnsFalseAndClearsBusy` ve Görev 9 `BackDuringAnimation_NoErrors`.
5. **Türkçe yerel ayar biçimlemesi.** Pas ihtimali `0,65` ya da `65%` olarak değil `%65` olarak görünmeli. Olay parmak izi tr-TR altında da aynı olmalı. Test: Görev 4 `Fingerprint_SameUnderTurkishAndInvariantCulture` ve `Percent_TurkishStyle`.

---

## Dosya yapısı

```
core/WeirdLand.Core/
  Duel/Combatant.cs, Duel/DuelSetup.cs        (Görev 1: salt okunur yüzey)
  Duel/EffectResolver.cs                      (Görev 1: ComputeBlock)
  Duel/DuelScenario.cs                        (Görev 2)
  Duel/DuelPreview.cs                         (Görev 3)
  Data/UiText.cs, Data/UiKeys.cs              (Görev 4)
  EventLog.cs                                 (Görev 4)
  WeirdLand.Core.csproj                       (Görev 5: Unity'ye kopyalama hedefi)
core/WeirdLand.Core.Tests/
  CombatantSurfaceTests.cs, DuelScenarioTests.cs, DuelPreviewTests.cs, UiTextTests.cs, EventLogTests.cs
  TestKit.cs (RealData() eklenir)
data/ui_tr.json                               (Görev 4)
tools/unity.sh                                (Görev 5)
unity/                                        (Görev 5: Unity projesi)
  .gitignore, Packages/manifest.json, ProjectSettings/
  Assets/link.xml
  Assets/Plugins/WeirdLand/                   (DLL kopyası, git'e girmez)
  Assets/WeirdLand/
    Runtime/WeirdLand.Unity.asmdef
      GameDataProvider.cs                     (Görev 5)
      Duel/IDuelView.cs, Duel/DuelController.cs        (Görev 6)
      Visual/VisualCatalog.cs, Visual/AnimTimings.cs, Visual/HandLayout.cs,
      Visual/CardView.cs, Visual/FighterView.cs, Visual/StatusChip.cs, Visual/IntentBubble.cs  (Görev 7)
      Duel/DuelScreen.cs, Duel/PassPicker.cs, Duel/ResultOverlay.cs, Visual/TweenAwait.cs      (Görev 8)
      Scenario/ScenarioOptions.cs, Scenario/ScenarioScreen.cs, App/AppFlow.cs                 (Görev 9)
    Editor/WeirdLand.Unity.Editor.asmdef
      ProjectSetup.cs (Görev 5), PrefabBuilder.cs (Görev 7), SceneBuilder.cs (Görev 8), BuildScript.cs (Görev 10)
    Resources/VisualCatalog.asset             (üretilir, Görev 7)
    Generated/Prefabs/*.prefab                (üretilir, Görev 7)
    Scenes/DuelSandbox.unity                  (üretilir, Görev 8)
    Tests/EditMode/WeirdLand.Unity.Tests.EditMode.asmdef  + *Tests.cs
    Tests/PlayMode/WeirdLand.Unity.Tests.PlayMode.asmdef  + *Tests.cs
docs/unity-contract.md                        (Görev 10)
```

---

### Task 1 (Görev 1): Çekirdeğin salt okunur yüzeyi ve tek blok hesabı

Devreden işlerden "blok hesabı tek yerde değil" ve "`Combatant` için salt okunur yüzey" maddeleri.

**Files:**
- Modify: `core/WeirdLand.Core/Duel/Combatant.cs`, `core/WeirdLand.Core/Duel/DuelSetup.cs` (`EnemyIntent`), `core/WeirdLand.Core/Duel/EffectResolver.cs`, `core/WeirdLand.Core/Duel/Duel.cs`, `core/WeirdLand.Core/Duel/EnemyAi.cs`, `core/WeirdLand.Core/Ai/GoodPolicy.cs`
- Modify (sadece mekanik yeniden adlandırma): `Combatant` alanlarını değiştiren testler. Bunlar `EffectResolverTests.cs`, `MatchEngineTests.cs`, `PassAndShotTests.cs`, `PolicyTests.cs` ve `StatusTimingTests.cs`.
- Test: `core/WeirdLand.Core.Tests/CombatantSurfaceTests.cs`, `EffectResolverTests.cs`

**Interfaces:**
- Produces:
  - `Combatant`: `public PlayerDef Player { get; internal set; }`. `IsUser`, `Hp`, `MaxHp`, `Block`, `Energy`, `KondisyonSpent` ve `PowerScalePercent` (varsayılan 100) aynı biçimde `{ get; internal set; }` olur.
  - `Combatant` içindeki değiştirilebilir koleksiyonlar `internal` olur: `StatusMap` (`Dictionary<StatusId,int>`), `DrawList`, `HandList`, `DiscardList` (`List<CardInstance>`).
  - Dışarıya açılan görünümler bir kez oluşturulup saklanır: `IReadOnlyDictionary<StatusId,int> Statuses` (`ReadOnlyDictionary`) ile `IReadOnlyList<CardInstance> DrawPile`, `Hand`, `Discard` (`List.AsReadOnly()`). `StatusOf` aynen kalır.
  - `EnemyIntent`: `Card`, `PassTarget` ve `PassChance` `{ get; internal set; }` olur.
  - `public static int EffectResolver.ComputeBlock(EffectDef e, Combatant src)`: `e.Type != Block` ise 0 döner, değilse `Amount + ölçekli stat bonusu`.

- [ ] **Step 1: Failing testleri yaz**

`CombatantSurfaceTests.cs`:
```csharp
[Test] public void Combatant_HasNoPublicFieldsOrSetters()
{
    var t = typeof(Combatant);
    Assert.That(t.GetFields(BindingFlags.Public | BindingFlags.Instance), Is.Empty);
    foreach (var p in t.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        Assert.That(p.SetMethod == null || !p.SetMethod.IsPublic, p.Name);
}
[Test] public void Combatant_CollectionsCannotBeCastBackToMutable()
{
    var c = new Combatant();
    Assert.That(c.Hand, Is.Not.InstanceOf<List<CardInstance>>());
    Assert.That(c.Statuses, Is.Not.InstanceOf<Dictionary<StatusId, int>>());
}
[Test] public void EnemyIntent_HasNoPublicSetters() { /* EnemyIntent için aynı özellik döngüsü */ }
```
`EffectResolverTests.cs` içine eklenecekler:
```csharp
[Test] public void ComputeBlock_Flat() => Assert.That(EffectResolver.ComputeBlock(Blk(4), src), Is.EqualTo(4));
[Test] public void ComputeBlock_ScalesWithStat()
{ // src.Player.Stats.Power == 2 olacak şekilde kur
    var e = new EffectDef { Type = EffectType.Block, Target = EffectTarget.Self, Amount = 3, ScaleStat = Stat.Power, Scale = 1 };
    Assert.That(EffectResolver.ComputeBlock(e, src), Is.EqualTo(5));
}
[Test] public void ComputeBlock_NonBlockEffect_IsZero() => Assert.That(EffectResolver.ComputeBlock(Dmg(5), src), Is.EqualTo(0));
```

- [ ] **Step 2: Testlerin başarısız olduğunu gör**

Run: `cd core && dotnet test --filter "FullyQualifiedName~CombatantSurface|FullyQualifiedName~ComputeBlock"`
Expected: derleme hatası (`ComputeBlock` yok) ya da FAIL.

- [ ] **Step 3: Yüzeyi değiştir**

Önce `ComputeBlock`'u ekle. `EffectResolver.Apply` içindeki Block dalı, `EnemyAi.Score` ve `GoodPolicy.BlockAmount` bu fonksiyonu çağırsın, kopya hesaplar silinsin. Sonra `Combatant` ve `EnemyIntent` yüzeyini değiştir. Çekirdek içindeki yazma yerleri `StatusMap`, `HandList`, `DrawList` ve `DiscardList` kullansın. Testlerdeki atamalar da aynı adlara mekanik olarak çevrilsin, test mantığı değişmesin. `Combatant` nesne başlatıcıları (`new Combatant { Player = ..., IsUser = ... }`) `internal set` ile çalışmaya devam eder.

- [ ] **Step 4: Tüm testleri çalıştır**

Run: `cd core && dotnet test`
Expected: 157 eski test ve yeni testler PASS. Davranış değişmedi.

- [ ] **Step 5: Simülatörle davranışın aynı kaldığını doğrula**

Run: `cd core && dotnet run --project WeirdLand.Sim -- --matches 2000 --seed 1 --policy good --opp kirmizi`
Expected: `docs/balance/m1-baseline.md` bölüm 3 ile aynı satır: %81 / %11 / %8, gol 2.80 - 0.47.

- [ ] **Step 6: Commit**

```bash
git add core && git commit -m "core: read-only Combatant surface and single ComputeBlock"
```

---

### Task 2 (Görev 2): Düello senaryosu kurucusu

Maç motoru olmadan tek bir düello kurar. Unity'deki seçim ekranı bunu kullanır.

**Files:**
- Create: `core/WeirdLand.Core/Duel/DuelScenario.cs`
- Modify: `core/WeirdLand.Core.Tests/TestKit.cs`. Eklenecek `RealData()` test klasöründen yukarı doğru `data/cards.json` dosyasını arar. `RealDataTests` da bunu kullansın. Bu, devreden işlerdeki "kırılgan yol" maddesini kapatır.
- Test: `core/WeirdLand.Core.Tests/DuelScenarioTests.cs`

**Interfaces:**
- Consumes: `Duel`, `DuelSetup`, `MatchRules.PowerScalePercent(int tier)`, `GameData`
- Produces (namespace `WeirdLand.Core.Duels`):
  - `public enum ScenarioKind { FieldAttack, FieldDefense, ShotAsShooter, ShotAsKeeper }`
  - `public sealed record DuelScenario(string UserTeamId, string OpponentTeamId, ScenarioKind Kind, Slot UserSlot, Slot EnemySlot);`
  - `public static class DuelScenarios`:
    - `IReadOnlyList<Slot> UserSlots(ScenarioKind k)`
    - `IReadOnlyList<Slot> EnemySlots(ScenarioKind k)`
    - `Slot DefaultEnemySlot(ScenarioKind k, Slot userSlot)`
    - `DuelSetup BuildSetup(GameData data, DuelScenario s)`
    - `Duel Create(GameData data, DuelScenario s, uint seed)`, bu `new Duel(BuildSetup(data, s), new Rng(seed))` döndürür.
  - `public static GameData TestKit.RealData()`

Kurallar:

| Kind | Kullanıcı slotları | Rakip slotları | Kim hücumda | Düello türü |
|---|---|---|---|---|
| FieldAttack | GK dışı hepsi | GK dışı hepsi | kullanıcı | Field |
| FieldDefense | GK dışı hepsi | GK dışı hepsi | rakip | Field |
| ShotAsShooter | GK dışı hepsi | yalnız GK | kullanıcı | Shot |
| ShotAsKeeper | yalnız GK | GK dışı hepsi | rakip | Shot |

- Slot listeleri `Slot` enum sırasıyla döner.
- `DefaultEnemySlot`:
  - FieldAttack: kullanıcının kulvarı Left ise `RB`, Right ise `LB`, Center ise `CM`.
  - FieldDefense: Left ise `RW`, Right ise `LW`, Center ise `CM`.
  - ShotAsShooter: `GK`. ShotAsKeeper: `ST`.
  - Kulvar `SlotInfo.LaneOf` ile bulunur.
- Pas hedefleri sadece Field düellosunda vardır. Hücum eden takımın GK ve taşıyıcı dışındaki tüm oyuncularıdır, takım sırasıyla. Hücum eden kullanıcıysa `UserPassTargets`'a, rakipse `EnemyPassTargets`'a yazılır.
- `EnemyPowerScalePercent = MatchRules.PowerScalePercent(opponent.Tier)`. Diğer bonuslar 0'dır: momentum, enerji ve can bonusu.
- Desteler `PlayerDef.Deck` içindeki kimliklerden, yükseltilmemiş `CardInstance` olarak kurulur.
- Hatalar `ArgumentException` fırlatır. Mesaj sorunlu değeri adlandırır:
  - bilinmeyen takım kimliği,
  - türe uymayan slot,
  - takımda o slot yoksa.

- [ ] **Step 1: Failing testleri yaz** (`DuelScenarioTests.cs`, gerçek veriyle: `mavi` ve `kirmizi`)

```csharp
static Duel Make(ScenarioKind k, Slot u, Slot e, uint seed = 1)
    => DuelScenarios.Create(RealData(), new DuelScenario("mavi", "kirmizi", k, u, e), seed);

[Test] public void FieldAttack_UserCarriesAgainstChosenDefender()
{
    var d = Make(ScenarioKind.FieldAttack, Slot.LW, Slot.RB); d.Begin();
    Assert.That(d.Kind, Is.EqualTo(DuelKind.Field));
    Assert.That(d.UserIsAttacker, Is.True);
    Assert.That(d.User.Player.Id, Is.EqualTo("mavi_lw"));
    Assert.That(d.Enemy.Player.Id, Is.EqualTo("kirmizi_rb"));
    Assert.That(d.UserPassTargets.Select(p => p.Slot),
        Is.EqualTo(new[] { Slot.LB, Slot.RB, Slot.CM, Slot.RW, Slot.ST }));
}
[Test] public void FieldDefense_EnemyCarries_EnemyHasPassTargets()
{ var d = Make(ScenarioKind.FieldDefense, Slot.LB, Slot.RW); d.Begin();
  Assert.That(d.UserIsAttacker, Is.False); Assert.That(d.EnemyPassTargets.Count, Is.EqualTo(5)); Assert.That(d.UserPassTargets, Is.Empty); }
[Test] public void ShotAsShooter_EnemyIsKeeper_TwoTurns()
{ var d = Make(ScenarioKind.ShotAsShooter, Slot.ST, Slot.GK); d.Begin();
  Assert.That(d.Kind, Is.EqualTo(DuelKind.Shot)); Assert.That(d.MaxTurns, Is.EqualTo(2));
  Assert.That(d.Enemy.Player.Slot, Is.EqualTo(Slot.GK)); Assert.That(d.UserPassTargets, Is.Empty); }
[Test] public void ShotAsKeeper_UserIsKeeper()
{ var d = Make(ScenarioKind.ShotAsKeeper, Slot.GK, Slot.ST); d.Begin();
  Assert.That(d.UserIsAttacker, Is.False); Assert.That(d.User.Player.Slot, Is.EqualTo(Slot.GK)); }
[Test] public void EnemyPower_FollowsOpponentTier()
{ var d = Make(ScenarioKind.FieldAttack, Slot.ST, Slot.CM);
  Assert.That(d.Enemy.PowerScalePercent, Is.EqualTo(MatchRules.PowerScalePercent(RealData().Team("kirmizi").Tier))); }
[TestCase(ScenarioKind.FieldAttack, Slot.GK, Slot.CM)]
[TestCase(ScenarioKind.ShotAsShooter, Slot.ST, Slot.CM)]
[TestCase(ScenarioKind.ShotAsKeeper, Slot.ST, Slot.ST)]
public void SlotNotAllowedForKind_Throws(ScenarioKind k, Slot u, Slot e)
    => Assert.Throws<ArgumentException>(() => Make(k, u, e));
[Test] public void UnknownTeam_ThrowsNamingId()
{ var ex = Assert.Throws<ArgumentException>(() => DuelScenarios.Create(RealData(),
      new DuelScenario("yok_boyle", "kirmizi", ScenarioKind.FieldAttack, Slot.ST, Slot.CM), 1));
  Assert.That(ex.Message, Does.Contain("yok_boyle")); }
[Test] public void SameSeed_SameOpeningHandAndIntent()
{ var a = Make(ScenarioKind.FieldAttack, Slot.ST, Slot.CM, 5); a.Begin();
  var b = Make(ScenarioKind.FieldAttack, Slot.ST, Slot.CM, 5); b.Begin();
  Assert.That(b.User.Hand.Select(c => c.Def.Id), Is.EqualTo(a.User.Hand.Select(c => c.Def.Id)));
  Assert.That(b.Intent.Card?.Def.Id, Is.EqualTo(a.Intent.Card?.Def.Id)); }
[TestCase(ScenarioKind.FieldAttack, Slot.LW, Slot.RB)] [TestCase(ScenarioKind.FieldAttack, Slot.RB, Slot.LB)]
[TestCase(ScenarioKind.FieldAttack, Slot.ST, Slot.CM)] [TestCase(ScenarioKind.FieldDefense, Slot.LB, Slot.RW)]
[TestCase(ScenarioKind.FieldDefense, Slot.RW, Slot.LW)] [TestCase(ScenarioKind.FieldDefense, Slot.CM, Slot.CM)]
[TestCase(ScenarioKind.ShotAsShooter, Slot.LW, Slot.GK)] [TestCase(ScenarioKind.ShotAsKeeper, Slot.GK, Slot.ST)]
public void DefaultEnemySlot_Table(ScenarioKind k, Slot u, Slot expected)
    => Assert.That(DuelScenarios.DefaultEnemySlot(k, u), Is.EqualTo(expected));
[Test] public void Slots_PerKind()
{ Assert.That(DuelScenarios.EnemySlots(ScenarioKind.ShotAsShooter), Is.EqualTo(new[] { Slot.GK }));
  Assert.That(DuelScenarios.UserSlots(ScenarioKind.ShotAsKeeper), Is.EqualTo(new[] { Slot.GK }));
  Assert.That(DuelScenarios.UserSlots(ScenarioKind.FieldAttack), Does.Not.Contain(Slot.GK).And.Count.EqualTo(6)); }
```

- [ ] **Step 2: Testlerin başarısız olduğunu gör**

Run: `cd core && dotnet test --filter FullyQualifiedName~DuelScenario`
Expected: derleme hatası (`DuelScenarios` yok).

- [ ] **Step 3: `DuelScenario.cs`'i ve `TestKit.RealData()`'yı yaz, `RealDataTests`'i ona geçir**

- [ ] **Step 4: Tüm testleri çalıştır**

Run: `cd core && dotnet test`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add core && git commit -m "core: duel scenario builder for single-duel play"
```

---

### Task 3 (Görev 3): Önizleme: kart sayıları ve niyet özeti

Ekranın kart üstünde ("Hasar 7", "Koruma 4") ve niyet balonunda göstereceği sayılar çekirdekten gelir. Unity hesap yapmaz.

**Files:**
- Create: `core/WeirdLand.Core/Duel/DuelPreview.cs`
- Test: `core/WeirdLand.Core.Tests/DuelPreviewTests.cs`

**Interfaces:**
- Consumes: `EffectResolver.ComputeDamage`, `EffectResolver.ComputeBlock` (Görev 1), `Duel.IncomingDamage()`
- Produces (namespace `WeirdLand.Core.Duels`):
  - `public sealed record CardPreview(int Damage, int Block);`
  - `public enum IntentKind { Wait, Card, Pass }`
  - `public sealed record StatusPreview(StatusId Status, int Stacks, bool OnUser);`
  - `public sealed record IntentView(IntentKind Kind, string CardId, string CardName, int Damage, int Block, IReadOnlyList<StatusPreview> Statuses, string PassTargetId, string PassTargetName, double PassChance);`
  - `public static class DuelPreview`:
    - `CardPreview Card(Duel d, CardInstance c)`
    - `bool CanPlay(Duel d, int handIndex)`
    - `IntentView Intent(Duel d)`

Kurallar:
- `Card`: kullanıcının kartı şu anki durumda kaç hasar ve kaç koruma verir, onu döndürür. Hasar `ComputeDamage(e, d.User, d.Enemy, d.Kind)` toplamıdır. Aynı kart içinde önce gelen bir etkinin sonraki etkiyi değiştirmesi hesaba katılmaz. StS'deki gibi, önizleme şu anki durumu gösterir.
- `CanPlay`: düello bitmemiş, dizin geçerli ve `Cost <= Energy` ise true.
- `Intent`:
  - `Intent == null` ya da hem kart hem pas yoksa `Wait`.
  - Pas varsa `Pass`. Hedef adı ve ihtimali `EnemyIntent`'ten alınır.
  - Kart varsa `Card`:
    - `Damage = d.IncomingDamage()`.
    - `Block = ComputeBlock` toplamı, kaynak `d.Enemy`.
    - `Statuses`: kartın Status etkileri sırasıyla. `Target == Self` ise `OnUser = false`.
  - Liste hiçbir zaman null değildir.

- [ ] **Step 1: Failing testleri yaz**

`DuelFlowTests.Make` kalıbıyla (`TestKit` kartları, `DuelSetup`) yerel bir `Make` yaz. Deste 4 kart olsun ki hepsi ele gelsin. Kartları kimliğe göre bul.
```csharp
[Test] public void Card_Damage_IncludesMomentumAndUnbalanced()
{ // UserMomentum = 2, Enemy.StatusMap[Unbalanced] = 1, kart Dmg(4)
  Assert.That(DuelPreview.Card(d, Find(d, "hit")).Damage, Is.EqualTo(9)); } // (4+2)*1.5
[Test] public void Card_Block_UsesComputeBlock() => Assert.That(DuelPreview.Card(d, Find(d, "wall")), Is.EqualTo(new CardPreview(0, 4)));
[Test] public void CanPlay_FalseWhenTooExpensive() { /* maliyet 4 olan kart, enerji 3 */ }
[Test] public void CanPlay_FalseAfterOutcome() { /* ForceOutcome sonrası */ }
[Test] public void CanPlay_FalseForBadIndex() { Assert.That(DuelPreview.CanPlay(d, 99), Is.False); Assert.That(DuelPreview.CanPlay(d, -1), Is.False); }
[Test] public void Intent_DamageCard_ReportsNameAndIncomingDamage()
{ // rakip destesi 6× "tackle" Dmg(6), kullanıcı hücumda
  var i = DuelPreview.Intent(d);
  Assert.That(i.Kind, Is.EqualTo(IntentKind.Card)); Assert.That(i.CardName, Is.EqualTo("tackle"));
  Assert.That(i.Damage, Is.EqualTo(d.IncomingDamage())); Assert.That(i.Statuses, Is.Empty); }
[Test] public void Intent_StatusCard_ListsStatusOnUser()
{ // rakip destesi Sts(Unbalanced, 1)
  Assert.That(DuelPreview.Intent(d).Statuses, Is.EqualTo(new[] { new StatusPreview(StatusId.Unbalanced, 1, true) })); }
[Test] public void Intent_SelfStatus_IsNotOnUser() { /* Sts(Momentum, 1, Self) → OnUser false */ }
[Test] public void Intent_EnemyPass_ReportsTargetAndChance()
{ // kullanıcı savunmada, rakip Passing 10, EnemyPassTargets = [Player("mate", Slot.ST)]
  var i = DuelPreview.Intent(d);
  Assert.That(i.Kind, Is.EqualTo(IntentKind.Pass)); Assert.That(i.PassTargetId, Is.EqualTo("mate"));
  Assert.That(i.PassChance, Is.EqualTo(0.9).Within(1e-9)); }
[Test] public void Intent_NoEnemyCards_IsWait() => Assert.That(DuelPreview.Intent(dEmpty).Kind, Is.EqualTo(IntentKind.Wait));
```

- [ ] **Step 2: Başarısız olduğunu gör**

Run: `cd core && dotnet test --filter FullyQualifiedName~DuelPreview`
Expected: derleme hatası.

- [ ] **Step 3: `DuelPreview.cs`'i yaz**

- [ ] **Step 4: Tüm testleri çalıştır**

Run: `cd core && dotnet test`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add core && git commit -m "core: card and intent preview for the duel screen"
```

---

### Task 4 (Görev 4): Arayüz metin tablosu ve olay parmak izi

**Files:**
- Create: `data/ui_tr.json`, `core/WeirdLand.Core/Data/UiText.cs`, `core/WeirdLand.Core/Data/UiKeys.cs`, `core/WeirdLand.Core/EventLog.cs`
- Modify: `core/WeirdLand.Core/Data/DataLoader.cs`. `LoadUiText` ve `UiTextFromJson` eklenir. Mevcut sıkı `Parse<T>` kullanılır, yani bilinmeyen alan hata sayılır.
- Test: `core/WeirdLand.Core.Tests/UiTextTests.cs`, `core/WeirdLand.Core.Tests/EventLogTests.cs`

**Interfaces:**
- Produces:
  - `public sealed class UiText` (namespace `WeirdLand.Core.Data`):
    - `UiText(IReadOnlyDictionary<string,string> strings)`
    - `string Get(string key)`: anahtar yoksa `DataException` fırlatır, mesaj `ui_tr.json: missing key '<key>'`.
    - `bool Has(string key)`
    - `string Format(string key, params object[] args)`: `string.Format(CultureInfo.InvariantCulture, Get(key), args)`.
    - `static string Percent(double p)`: `"%" + (int)Math.Round(p * 100, MidpointRounding.AwayFromZero)`.
  - `public static GameData`'ya dokunulmaz. Yeni yükleyiciler: `public static UiText DataLoader.LoadUiText(string dataDir)` ve `public static UiText DataLoader.UiTextFromJson(string json)`.
    - Dosya biçimi: `{ "schemaVersion": 1, "strings": { "<key>": "<metin>" } }`.
    - Boş metin `DataException` verir ve anahtarı adlandırır.
  - `public static class UiKeys`:
    - Aşağıdaki JSON'daki sabit anahtarlar için `public const string` alanları. Ad kuralı: `duel.endTurn` → `DuelEndTurn`.
    - Anahtar kuran fonksiyonlar: `Status(StatusId)` → `status.X`, `StatusDesc(StatusId)` → `status.X.desc`, `SlotName(Slot)` → `slot.X`, `Role(CardRole)` → `role.X`, `Scenario(ScenarioKind)` → `scenario.X`.
    - `HpLabel(DuelKind kind, bool isAttacker)`: hücum eden için `hp.kontrol`. Savunan Field'daysa `hp.denge`, Shot ya da Penalty'deyse `hp.kale`.
    - `Result(DuelKind kind, DuelOutcome o, bool userIsAttacker)` → `result.<field|shot>.<Outcome>.<attacker|defender>`. Penalty, shot gibi davranır.
    - `IEnumerable<string> All()`: sabitler (yansıma ile) ve kurucuların geçerli bütün kombinasyonları. Field için 4 sonuç × 2 rol, shot için `AttackerWon`/`DefenderWon` × 2 rol.
  - `public static class EventLog` (namespace `WeirdLand.Core`):
    - `string Format(IEnumerable<GameEvent> events)`: her olay için bir satır, record'un `ToString()`'i, `\n` ile birleşir. Çağrı süresince `CultureInfo.CurrentCulture` değişmez kültüre alınır, `finally` içinde eski haline döner.
    - `string Fingerprint(IEnumerable<GameEvent> events)`: `Format` çıktısının UTF-8 baytları üzerinde FNV-1a 32 bit. Sonuç 8 haneli küçük harfli hex.
  - `public static class GoldenRun` (`EventLog.cs` içinde):
    - `public const uint MatchSeed = 20261122, PolicySeed = 7;`
    - `public static string Fingerprint(GameData data)`: `mavi` ile `kirmizi` arasında `MatchEngine(data, mavi, kirmizi, MatchSeed)` kurar, `MatchRunner.Play(m, new GoodPolicy(), new Rng(PolicySeed))` ile oynatır, `Events`'in parmak izini döndürür.

`data/ui_tr.json` içeriği (tam metin):
```json
{
  "schemaVersion": 1,
  "strings": {
    "duel.endTurn": "Turu bitir",
    "duel.pass": "Pas",
    "duel.passChance": "Başarı {0}",
    "duel.passPickTitle": "Pası kime atacaksın?",
    "duel.cancel": "Vazgeç",
    "duel.turn": "Tur {0}/{1}",
    "duel.energy": "Enerji",
    "duel.drawPile": "Deste {0}",
    "duel.discardPile": "Atılan {0}",
    "duel.noCards": "Bu rolde kartın yok. Pas atabilir ya da turu bitirebilirsin.",
    "duel.enemyPlayed": "{0}: {1}",
    "duel.back": "Seçime dön",
    "duel.seed": "Seed {0}",
    "duel.block": "Koruma {0}",
    "hp.kontrol": "Kontrol",
    "hp.denge": "Denge",
    "hp.kale": "Kale",
    "card.damage": "Hasar {0}",
    "card.block": "Koruma {0}",
    "role.Attack": "Hücum",
    "role.Defense": "Savunma",
    "role.Both": "İkisi de",
    "intent.wait": "Bekliyor",
    "intent.damage": "{0} hasar",
    "intent.block": "{0} koruma",
    "intent.pass": "Pas: {0} ({1})",
    "status.Unbalanced": "Dengesiz",
    "status.Unbalanced.desc": "Aldığı hasar %50 artar.",
    "status.Momentum": "Momentum",
    "status.Momentum.desc": "Verdiği hasar yığın sayısı kadar artar.",
    "status.Tired": "Yorgun",
    "status.Tired.desc": "Verdiği hasar %25 azalır.",
    "status.CoverNear": "Yakın köşede",
    "status.CoverNear.desc": "Yakın köşeyi kapatıyor. Uzak köşeye giden şut çift hasar verir.",
    "status.CoverFar": "Uzak köşede",
    "status.CoverFar.desc": "Uzak köşeyi kapatıyor. Yakın köşeye giden şut çift hasar verir.",
    "slot.GK": "Kaleci",
    "slot.LB": "Sol bek",
    "slot.RB": "Sağ bek",
    "slot.CM": "Orta saha",
    "slot.LW": "Sol kanat",
    "slot.RW": "Sağ kanat",
    "slot.ST": "Santrafor",
    "scenario.title": "Düello seç",
    "scenario.userTeam": "Takımın",
    "scenario.opponentTeam": "Rakip takım",
    "scenario.kind": "Düello türü",
    "scenario.userPlayer": "Oyuncun",
    "scenario.enemyPlayer": "Rakip oyuncu",
    "scenario.seed": "Seed",
    "scenario.start": "Başla",
    "scenario.FieldAttack": "Saha düellosu, hücumdasın",
    "scenario.FieldDefense": "Saha düellosu, savunmadasın",
    "scenario.ShotAsShooter": "Şut, sen vuruyorsun",
    "scenario.ShotAsKeeper": "Şut, kalecisin",
    "result.field.AttackerWon.attacker": "Rakibi geçtin!",
    "result.field.AttackerWon.defender": "Geçildin.",
    "result.field.DefenderWon.attacker": "Topu kaptırdın.",
    "result.field.DefenderWon.defender": "Topu kaptın!",
    "result.field.PassSucceeded.attacker": "Pas yerini buldu!",
    "result.field.PassSucceeded.defender": "Rakip pasla kurtuldu.",
    "result.field.PassFailed.attacker": "Pas kesildi.",
    "result.field.PassFailed.defender": "Pası kestin!",
    "result.shot.AttackerWon.attacker": "GOL!",
    "result.shot.AttackerWon.defender": "Gol yedin.",
    "result.shot.DefenderWon.attacker": "Kaleci kurtardı.",
    "result.shot.DefenderWon.defender": "Kurtardın!",
    "result.timeout": "Süre doldu, savunmaya yardım geldi.",
    "result.again": "Aynı seed ile tekrar",
    "result.newSeed": "Yeni seed",
    "result.back": "Seçime dön"
  }
}
```

- [ ] **Step 1: Failing testleri yaz**

`UiTextTests.cs`:
```csharp
static UiText Real() => DataLoader.LoadUiText(TestKit.RealDataDir());   // TestKit'e RealDataDir() de ekle
[Test] public void RealUiText_HasEveryKey() { var t = Real(); foreach (var k in UiKeys.All()) Assert.That(t.Has(k), k); }
[Test] public void Get_MissingKey_ThrowsNamingKey()
{ var ex = Assert.Throws<DataException>(() => Real().Get("yok.boyle")); Assert.That(ex.Message, Does.Contain("yok.boyle")); }
[Test] public void Format_FillsPlaceholders() => Assert.That(Real().Format(UiKeys.DuelTurn, 2, 3), Is.EqualTo("Tur 2/3"));
[Test] public void TurkishCharactersSurviveLoading() => Assert.That(Real().Get(UiKeys.Role(CardRole.Both)), Is.EqualTo("İkisi de"));
[Test] public void WrongSchemaVersion_Throws() => Assert.Throws<DataException>(() => DataLoader.UiTextFromJson("{\"schemaVersion\":2,\"strings\":{}}"));
[Test] public void UnknownRootField_Throws() => Assert.Throws<DataException>(() => DataLoader.UiTextFromJson("{\"schemaVersion\":1,\"strings\":{},\"x\":1}"));
[Test] public void EmptyValue_ThrowsNamingKey()
{ var ex = Assert.Throws<DataException>(() => DataLoader.UiTextFromJson("{\"schemaVersion\":1,\"strings\":{\"a.b\":\"\"}}"));
  Assert.That(ex.Message, Does.Contain("a.b")); }
[TestCase(0.654, "%65")] [TestCase(0.05, "%5")] [TestCase(1.0, "%100")] [TestCase(0.655, "%66")]
public void Percent_TurkishStyle(double p, string s) => Assert.That(UiText.Percent(p), Is.EqualTo(s));
[Test] public void HpLabel_Keys()
{ Assert.That(UiKeys.HpLabel(DuelKind.Field, true), Is.EqualTo("hp.kontrol"));
  Assert.That(UiKeys.HpLabel(DuelKind.Field, false), Is.EqualTo("hp.denge"));
  Assert.That(UiKeys.HpLabel(DuelKind.Shot, false), Is.EqualTo("hp.kale")); }
[Test] public void Result_PenaltyUsesShotKeys()
  => Assert.That(UiKeys.Result(DuelKind.Penalty, DuelOutcome.AttackerWon, true), Is.EqualTo("result.shot.AttackerWon.attacker"));
```
`EventLogTests.cs`:
```csharp
[Test] public void Format_OneLinePerEvent()
  => Assert.That(EventLog.Format(new GameEvent[] { new TurnStarted(1), new EnergyChanged(true, 3) }).Split('\n').Length, Is.EqualTo(2));
[Test] public void Fingerprint_SameUnderTurkishAndInvariantCulture()
{
    var ev = new GameEvent[] { new PassAttempted(true, "x", 0.655, true) };
    var old = CultureInfo.CurrentCulture;
    try
    {
        CultureInfo.CurrentCulture = new CultureInfo("tr-TR");
        var tr = EventLog.Fingerprint(ev);
        StringAssert.Contains("0.655", EventLog.Format(ev));
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        Assert.That(EventLog.Fingerprint(ev), Is.EqualTo(tr));
    }
    finally { CultureInfo.CurrentCulture = old; }
}
[Test] public void Format_RestoresCulture() { /* tr-TR ayarla, Format çağır, CurrentCulture.Name hâlâ "tr-TR" */ }
[Test] public void Fingerprint_IsEightLowerHex() => Assert.That(EventLog.Fingerprint(new GameEvent[0]), Does.Match("^[0-9a-f]{8}$"));
[Test] public void Golden_MatchFingerprint() => Assert.That(GoldenRun.Fingerprint(TestKit.RealData()), Is.EqualTo("XXXXXXXX"));
```

- [ ] **Step 2: Başarısız olduğunu gör**

Run: `cd core && dotnet test --filter "FullyQualifiedName~UiText|FullyQualifiedName~EventLog"`
Expected: derleme hatası.

- [ ] **Step 3: `ui_tr.json`, `UiText`, `UiKeys`, `EventLog` ve `GoldenRun`'ı yaz**

- [ ] **Step 4: Golden değeri sabitle**

Run: `cd core && dotnet test --filter FullyQualifiedName~Golden_MatchFingerprint`
Expected: FAIL. NUnit mesajında gerçek hex değeri görünür. O değeri `"XXXXXXXX"` yerine yaz ve tekrar çalıştır: PASS. Bu değer Görev 5'teki Unity testine de aynen yazılacak.

- [ ] **Step 5: Tüm testleri çalıştır**

Run: `cd core && dotnet test`
Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add core data/ui_tr.json && git commit -m "core: UI text table, event fingerprint and golden run"
```

---

### Task 5 (Görev 5): Unity projesi ve çekirdek köprüsü

Unity projesi kurulur, çekirdek DLL olarak bağlanır, ilk EditMode testleri çalışır. Bu görevin çıktısı şunu kanıtlar: Unity aynı veriyi okur ve aynı maçı birebir aynı oynar. Devreden işlerdeki üç Unity uyumluluğu maddesi burada kapanır: IL2CPP için `link.xml`, ondalık sayı determinizmi ve DLL ile UPM arasındaki karar.

**Files:**
- Create: `unity/` (Unity'nin `-createProject` komutuyla), `unity/.gitignore`, `unity/Assets/link.xml`, `tools/unity.sh`
- Create: `unity/Assets/WeirdLand/Runtime/WeirdLand.Unity.asmdef`, `unity/Assets/WeirdLand/Runtime/GameDataProvider.cs`
- Create: `unity/Assets/WeirdLand/Editor/WeirdLand.Unity.Editor.asmdef`, `unity/Assets/WeirdLand/Editor/ProjectSetup.cs`
- Create: `unity/Assets/WeirdLand/Tests/EditMode/WeirdLand.Unity.Tests.EditMode.asmdef`, `unity/Assets/WeirdLand/Tests/EditMode/BridgeTests.cs`
- Modify: `unity/Packages/manifest.json`, `unity/ProjectSettings/ProjectSettings.asset` (`activeInputHandler: 1`), `core/WeirdLand.Core/WeirdLand.Core.csproj` (kopyalama hedefi)

**Interfaces:**
- Consumes: `DataLoader.Load`, `DataLoader.LoadUiText`, `GoldenRun.Fingerprint` (Görev 4)
- Produces:
  - `public sealed class LoadedData { public GameData Game; public UiText Text; }`
  - `public static class GameDataProvider`:
    - `string DataDirectory()`: editörde `Path.GetFullPath(Path.Combine(Application.dataPath, "../../data"))`, oyuncuda `Path.Combine(Application.streamingAssetsPath, "data")`.
    - `LoadedData Load(string dir)`: klasör yoksa `DataException("Data directory not found: <tam yol>")` fırlatır.
  - `tools/unity.sh <komut>`:
    - `setup`: `ProjectSetup.Run` çalışır.
    - `tests [editmode|playmode|all]`: varsayılan `all`.
    - `scenes`: Görev 8'de eklenir.
    - `build`: Görev 10'da eklenir.
    - `shot`: Görev 8'de eklenir.
  - Assembly adları: `WeirdLand.Unity`, `WeirdLand.Unity.Editor`, `WeirdLand.Unity.Tests.EditMode`, `WeirdLand.Unity.Tests.PlayMode` (sonuncusu Görev 8'de).

- [ ] **Step 1: Unity projesini oluştur**

```bash
"/c/Program Files/Unity/Hub/Editor/6000.3.9f1/Editor/Unity.exe" -batchmode -quit -createProject "D:/WeirdLand/unity" -logFile -
```
Expected: çıkış kodu 0, `unity/Assets`, `unity/Packages/manifest.json` ve `unity/ProjectSettings` oluşur. Lisans hatası çıkarsa dur ve kullanıcıdan Unity Hub'da oturum açmasını iste.

- [ ] **Step 2: Paketleri ekle**

`unity/Packages/manifest.json` dosyasına şunları ekle. Var olan `com.unity.ugui` ve `com.unity.test-framework` satırlarına dokunma.
```json
"scopedRegistries": [
  { "name": "package.openupm.com", "url": "https://package.openupm.com",
    "scopes": ["com.cysharp.unitask", "com.kyrylokuzyk.primetween"] }
],
"dependencies": {
  "com.cysharp.unitask": "2.5.11",
  "com.kyrylokuzyk.primetween": "1.3.3",
  "com.unity.nuget.newtonsoft-json": "3.2.2",
  "com.unity.inputsystem": "1.20.1"
}
```
`ProjectSettings.asset` içinde `activeInputHandler` değerini `1` yap. Input System sürümü çözülemezse, Unity günlüğünde önerilen sürümü kullan ve bunu commit mesajına yaz.

- [ ] **Step 3: Çekirdek DLL'ini Unity'ye kopyalayan hedefi ekle**

`WeirdLand.Core.csproj` içine:
```xml
<Target Name="CopyToUnity" AfterTargets="Build" Condition="Exists('$(MSBuildThisFileDirectory)../../unity/Assets')">
  <Copy SourceFiles="$(TargetPath);$(TargetDir)$(TargetName).pdb"
        DestinationFolder="$(MSBuildThisFileDirectory)../../unity/Assets/Plugins/WeirdLand" SkipUnchangedFiles="true" />
</Target>
```
- Newtonsoft.Json.dll kopyalanmaz. Unity onu `com.unity.nuget.newtonsoft-json` paketinden alır, iki taraf da aynı 13.0.0.0 assembly sürümünü kullanır.
- `unity/Assets/link.xml`: `<linker><assembly fullname="WeirdLand.Core" preserve="all"/></linker>`

Run: `cd core && dotnet build`
Expected: `unity/Assets/Plugins/WeirdLand/WeirdLand.Core.dll` oluşur.

- [ ] **Step 4: `unity/.gitignore` ve asmdef'leri yaz**

`.gitignore` içeriği: `Library/`, `Temp/`, `Obj/`, `Build/`, `Builds/`, `Logs/`, `UserSettings/`, `MemoryCaptures/`, `Screenshots/`, `*.csproj`, `*.sln`, `*.slnx`, `/Assets/Plugins/WeirdLand/WeirdLand.Core.*`, `/Assets/StreamingAssets/data*`. Kök `.gitignore`'a `builds/` ekle.

asmdef'ler:
- `WeirdLand.Unity`: references `UniTask`, `PrimeTween.Runtime`, `Unity.TextMeshPro`, `UnityEngine.UI`. `autoReferenced: true`. Çekirdek DLL'i otomatik referans alır.
- `WeirdLand.Unity.Editor`: references `WeirdLand.Unity`, `Unity.TextMeshPro`, `UnityEngine.UI`, `Unity.InputSystem`. `includePlatforms: ["Editor"]`.
- `WeirdLand.Unity.Tests.EditMode`:
  - references `WeirdLand.Unity`, `WeirdLand.Unity.Editor`, `UniTask`, `Unity.TextMeshPro`, `UnityEngine.UI`, `UnityEngine.TestRunner`, `UnityEditor.TestRunner`.
  - `includePlatforms: ["Editor"]`.
  - `overrideReferences: true`, `precompiledReferences: ["nunit.framework.dll", "WeirdLand.Core.dll", "Newtonsoft.Json.dll"]`.
  - `defineConstraints: ["UNITY_INCLUDE_TESTS"]`.

Paket asmdef adlarını varsayma, kontrol et: `ls unity/Library/PackageCache/com.kyrylokuzyk.primetween*/**/*.asmdef unity/Library/PackageCache/com.cysharp.unitask*/**/*.asmdef`. Ad farklıysa referansı ona göre düzelt.

- [ ] **Step 5: `tools/unity.sh`'ı yaz**

- `UNITY_EXE` ortam değişkeni yoksa varsayılan yol: `/c/Program Files/Unity/Hub/Editor/6000.3.9f1/Editor/Unity.exe`.
- Ön kontroller:
  - `unity/Assets/Plugins/WeirdLand/WeirdLand.Core.dll` yoksa önce `dotnet build core` çalıştırır.
  - `tasklist` içinde `Unity.exe` varsa ve `unity/Temp/UnityLockfile` mevcutsa, şu mesajla 1 koduyla çıkar: `Unity editörü bu projeyi açık tutuyor, kapatıp tekrar dene.`
- `setup`: `-batchmode -quit -projectPath unity -executeMethod WeirdLand.Unity.EditorTools.ProjectSetup.Run -logFile unity/Logs/setup.log`.
- `tests <platform>`:
  - Her platform için `-batchmode -projectPath unity -runTests -testPlatform <EditMode|PlayMode> -testResults unity/Logs/tests-<platform>.xml -logFile unity/Logs/tests-<platform>.log` çalışır. `-quit` verilmez.
  - XML'deki kök `test-run` özniteliklerinden şu satırı yazar: `EditMode: <passed> passed, <failed> failed, <skipped> skipped`. Başarısız testlerin tam adlarını listeler.
  - Unity çıkış kodu 0 değilse aynı kodla çıkar. Kodlar: 2 başarısız test, 3 çalıştırma hatası. 3 durumunda log dosyasının son 40 satırını basar.

- [ ] **Step 6: `ProjectSetup.Run`'ı yaz ve çalıştır**

`ProjectSetup.Run()` şunları yapar:
- `AssetDatabase.ImportPackage(Path.GetFullPath("Packages/com.unity.ugui/Package Resources/TMP Essential Resources.unitypackage"), false)` ile TMP temel kaynaklarını içe aktarır.
- `EditorSettings.serializationMode = ForceText` yapar.
- `AssetDatabase.SaveAssets()` çağırır.

Run: `tools/unity.sh setup`
Expected: `unity/Assets/TextMesh Pro/` oluşur, çıkış 0.

- [ ] **Step 7: Failing köprü testlerini yaz** (`BridgeTests.cs`)

```csharp
[Test] public void DataLoadsFromRepo()
{ var d = GameDataProvider.Load(GameDataProvider.DataDirectory());
  Assert.That(d.Game.Cards.Count, Is.GreaterThanOrEqualTo(40));
  Assert.That(d.Text.Get(UiKeys.DuelEndTurn), Is.EqualTo("Turu bitir")); }
[Test] public void Golden_MatchFingerprint_MatchesDotnet()
  => Assert.That(GoldenRun.Fingerprint(GameDataProvider.Load(GameDataProvider.DataDirectory()).Game), Is.EqualTo("<Görev 4'teki değer>"));
[Test] public void Load_MissingDirectory_ThrowsDataExceptionNamingPath()
{ var ex = Assert.Throws<DataException>(() => GameDataProvider.Load("Z:/yok/data"));
  Assert.That(ex.Message, Does.Contain("Z:/yok/data").Or.Contain("Z:\\yok\\data")); }
[Test] public void TurkishGlyphs_AvailableInDefaultFont()
{ var font = TMP_Settings.defaultFontAsset; Assert.That(font, Is.Not.Null);
  Assert.That(font.HasCharacters("ğĞüÜşŞıİöÖçÇ", out List<char> missing, true, true), Is.True,
      "missing: " + new string(missing?.ToArray() ?? new char[0])); }
```

- [ ] **Step 8: Testleri çalıştır, başarısızlığı gör, `GameDataProvider`'ı yaz, tekrar çalıştır**

Run: `tools/unity.sh tests editmode`
- İlk çalıştırma: `GameDataProvider` olmadığı için derleme hatası, çıkış 3.
- Yazdıktan sonra beklenen çıktı: `EditMode: 4 passed, 0 failed`.

Golden testi başarısız olursa çekirdeğin Mono'da farklı sonuç verdiği anlaşılır. Bu durumda **dur** ve iki ortamın `EventLog.Format` çıktılarını karşılaştırıp ilk farklı satırı raporla. Golden değeri değiştirerek testi geçirme.

Türkçe karakter testi başarısız olursa, `ProjectSetup.Run`'a bir adım ekle: `LiberationSans.ttf`'den dinamik bir TMP font asset'i üretip `TMP_Settings` varsayılanının fallback listesine koysun. Sonra testi tekrar çalıştır.

- [ ] **Step 9: Commit**

`git status` çıktısında `Library/` ya da DLL görünmemeli.
```bash
git add .gitignore core/WeirdLand.Core/WeirdLand.Core.csproj tools/unity.sh unity && git commit -m "unity: project, core DLL bridge and EditMode bridge tests"
```

---

### Task 6 (Görev 6): Düello denetleyicisi (saf C#)

**Files:**
- Create: `unity/Assets/WeirdLand/Runtime/Duel/IDuelView.cs`, `unity/Assets/WeirdLand/Runtime/Duel/DuelController.cs`
- Test: `unity/Assets/WeirdLand/Tests/EditMode/DuelControllerTests.cs`, `unity/Assets/WeirdLand/Tests/EditMode/TestDecks.cs`

**Interfaces:**
- Consumes: `Duel`, `DuelPreview.CanPlay` (Görev 3)
- Produces:
  - `public interface IDuelView`:
    - `UniTask Animate(GameEvent e, Duel duel, CancellationToken ct)`
    - `void Sync(Duel duel)`
  - `public sealed class DuelController`:
    - `DuelController(Duel duel, IDuelView view)`
    - `Duel Duel { get; }`, `bool IsBusy { get; }`
    - `UniTask<bool> Begin(CancellationToken ct)`
    - `UniTask<bool> PlayCard(int handIndex, CancellationToken ct)`
    - `UniTask<bool> Pass(string targetId, CancellationToken ct)`
    - `UniTask<bool> EndTurn(CancellationToken ct)`
    - `event Action<DuelOutcome> Ended`
  - `TestDecks` (sadece testler): `CardDef`/`PlayerDef` nesnelerinden küçük `DuelSetup`'lar kurar. Görev 7 ve 8 testleri de bunu kullanır.

Kurallar:
- Her komut önce geçerliliğe bakar:
  - meşgul değil,
  - düello bitmemiş, başlamış (Begin dışında),
  - `PlayCard` için `DuelPreview.CanPlay`,
  - `Pass` için `Duel.CanPass` ve hedef `UserPassTargets` içinde,
  - Begin daha önce çağrılmamış.
- Geçersizse **çekirdeğe dokunmadan** `false` döner.
- Geçerliyse sıra şöyle:
  1. `IsBusy = true`.
  2. Çekirdek komutu çağrılır.
  3. Her olay sırayla `await view.Animate(e, duel, ct)` ile oynatılır.
  4. `view.Sync(duel)` çağrılır.
  5. Düello bittiyse `Ended` bir kez tetiklenir.
  6. `true` döner.
- `IsBusy` her durumda `finally` içinde `false` olur.
- Hata durumları:
  - `OperationCanceledException` yakalanır, `false` döner. `Sync` ve `Ended` çağrılmaz.
  - `Animate` başka bir istisna fırlatırsa `Debug.LogException` ile kaydedilir, kalan olaylar atlanır, `Sync` yine çağrılır. Böylece ekran durumla tutarlı kalır.

- [ ] **Step 1: Failing testleri yaz**

`FakeView` her çağrıyı kaydetsin: `"Animate:" + e.GetType().Name` ya da `"Sync"`. İsteğe bağlı olarak `Gate` (`UniTaskCompletionSource`) açılana kadar `Animate` içinde bekleyebilsin. Testler `[UnityTest] IEnumerator X() => UniTask.ToCoroutine(async () => { ... })` biçiminde yazılır.
```csharp
[UnityTest] public IEnumerator Begin_AnimatesEveryEventInOrder_ThenSyncs() => UniTask.ToCoroutine(async () =>
{   var twin = TestDecks.FieldDuel(seed: 3); var expected = twin.Begin().Select(e => "Animate:" + e.GetType().Name).ToList();
    var view = new FakeView(); var c = new DuelController(TestDecks.FieldDuel(seed: 3), view);
    Assert.That(await c.Begin(default), Is.True);
    Assert.That(view.Calls, Is.EqualTo(expected.Append("Sync").ToList())); });
[UnityTest] public IEnumerator PlayCard_WhileBusy_ReturnsFalse_DuelUntouched() => UniTask.ToCoroutine(async () =>
{   // Begin bitir; view.Gate kapalıyken PlayCard(0) başlat (await etme); IsBusy true;
    // ikinci PlayCard(0) false; Hand.Count ve Energy ilk komuttan sonraki değerde; Gate aç, ilkini bekle → true
});
[UnityTest] public IEnumerator PlayCard_Unaffordable_ReturnsFalse()  // maliyet 4 kartlık deste
[UnityTest] public IEnumerator Pass_UnknownTarget_ReturnsFalse()
[UnityTest] public IEnumerator Pass_WhenUserDefends_ReturnsFalse()
[UnityTest] public IEnumerator CommandsBeforeBegin_ReturnFalse()
[UnityTest] public IEnumerator BeginTwice_SecondReturnsFalse()
[UnityTest] public IEnumerator AfterOutcome_AllCommandsReturnFalse()   // EndTurn ile süre doldurulur
[UnityTest] public IEnumerator Ended_RaisedOnce_AfterFinalSync()       // Ended içinde view.Calls.Last() == "Sync"
[UnityTest] public IEnumerator Cancel_DuringPlayback_ReturnsFalseAndClearsBusy()
[UnityTest] public IEnumerator ViewThrows_StillSyncs_AndClearsBusy()   // LogAssert.Expect(LogType.Exception, ...)
```

- [ ] **Step 2: Başarısız olduğunu gör**

Run: `tools/unity.sh tests editmode`
Expected: derleme hatası, çıkış 3.

- [ ] **Step 3: `IDuelView` ve `DuelController`'ı yaz**

- [ ] **Step 4: Testleri çalıştır**

Run: `tools/unity.sh tests editmode`
Expected: `0 failed`.

- [ ] **Step 5: Commit**

```bash
git add unity/Assets/WeirdLand && git commit -m "unity: duel controller with input lock and ordered event playback"
```

---

### Task 7 (Görev 7): Görsel parçalar ve prefab üreticisi

Yer tutucu görseller koddan çizilir: düz renkli kart çerçevesi ve çöp adam. Çöp adamın başı Unity'nin yerleşik `UI/Skin/Knob.psd` sprite'ıdır, gövde ve uzuvları ince döndürülmüş `Image`'lardır. Spec §8.5 gereği varlıklar `VisualCatalog` üzerinden bağlanır, gerçek çizimler sonra bu katalogdan takılır.

**Files:**
- Create: `unity/Assets/WeirdLand/Runtime/Visual/VisualCatalog.cs`, `AnimTimings.cs`, `HandLayout.cs`, `CardView.cs`, `FighterView.cs`, `StatusChip.cs`, `IntentBubble.cs`
- Create: `unity/Assets/WeirdLand/Editor/PrefabBuilder.cs`
- Generated (commit edilir): `unity/Assets/WeirdLand/Resources/VisualCatalog.asset`, `unity/Assets/WeirdLand/Generated/Prefabs/{Card,Fighter,StatusChip,Button,IntentBubble}.prefab`
- Test: `unity/Assets/WeirdLand/Tests/EditMode/VisualTests.cs`

**Interfaces:**
- Consumes: `CardPreview`, `IntentView`, `StatusPreview` (Görev 3), `UiText`, `UiKeys` (Görev 4), `Combatant`
- Produces:
  - `[Serializable] public class AnimTimings`:
    - `float Scale = 1; float Draw = 0.25f, DrawStagger = 0.06f, CardFly = 0.35f, EnemyCardHold = 0.5f, Lunge = 0.25f, Hit = 0.3f, FloatNumber = 0.6f, Bar = 0.3f, Banner = 0.6f, Pass = 0.5f`
    - `float Of(float d) => d * Scale`
  - `public class VisualCatalog : ScriptableObject`:
    - Prefab alanları: `CardView Card; FighterView Fighter; StatusChip StatusChip; Button Button; IntentBubble IntentBubble`.
    - Renkler: `Color AttackCard, DefenseCard, BothCard, UserTeam, EnemyTeam, Keeper, Background`.
    - `AnimTimings Timings`.
    - `static VisualCatalog Load()`: `Resources.Load<VisualCatalog>("VisualCatalog")` çağırır, bulamazsa açık mesajlı bir istisna fırlatır.
  - `public static class HandLayout`:
    - `Vector2[] Positions(int count, float cardWidth, float maxWidth, float spacing)`
    - Kartlar ortalanır, `y = 0`. Toplam genişlik `maxWidth`'i aşarsa aralık eksiye düşer (üst üste binme), ama kartların dış kenarları `±maxWidth/2` içinde kalır.
  - `public class CardView : MonoBehaviour`:
    - `CardInstance Card { get; }`
    - `void Bind(CardInstance c, CardPreview p, bool playable, UiText t, VisualCatalog cat)`
    - `event Action<CardView> Clicked`
    - Gösterilenler: ad, maliyet, `Def.Text`, rol etiketi, önizleme satırı. Önizleme satırı hasar > 0 ise `card.damage`, blok > 0 ise `card.block` gösterir, ikisi de varsa iki satır olur.
    - Çerçeve rengi role göre değişir.
    - `playable == false` ise `CanvasGroup.alpha = 0.5`, düğme etkileşimsiz.
  - `public class FighterView : MonoBehaviour`:
    - `RectTransform Body { get; }`, `RectTransform NumberAnchor { get; }`
    - `void Setup(string playerName, string hpLabel, Color color, bool facingLeft)`
    - `void SetHp(int hp, int max)`, `void SetBlock(int block)` (0 ise gizli), `void SetBall(bool hasBall)`
    - `void SetStatuses(IReadOnlyDictionary<StatusId,int> s, UiText t)`: `StatusId` enum sırasıyla `StatusChip` dizer, sözlük sırası kullanılmaz. Etiket biçimi `Dengesiz 2`.
  - `public class StatusChip : MonoBehaviour`: `void Bind(string label, string tooltip)`.
  - `public class IntentBubble : MonoBehaviour`:
    - `void Show(IntentView v, UiText t)`, `void Hide()`, `string Text { get; }`
    - Wait için `intent.wait`.
    - Card için önce kart adı, sonra hasar > 0 ise `intent.damage`, blok > 0 ise `intent.block`, sonra her durum için `status.X` (yığın > 1 ise sayısıyla).
    - Pass için `intent.pass` (hedef adı, `UiText.Percent`).
  - `public static class PrefabBuilder`: `void BuildAll()`, menü yolu `WeirdLand/Build Prefabs`. Var olan üretilmiş prefab'ların üzerine yazar. Katalog asset'i varsa renk ve süre değerlerini korur, sadece prefab referanslarını yeniler.

- [ ] **Step 1: Failing testleri yaz**

```csharp
[Test] public void HandLayout_FourCards_CenteredWithSpacing()
  => Assert.That(HandLayout.Positions(4, 200, 1200, 20).Select(v => v.x), Is.EqualTo(new[] { -330f, -110f, 110f, 330f }));
[Test] public void HandLayout_EightCards_FitWithinMaxWidth()
{ var xs = HandLayout.Positions(8, 200, 1200, 20).Select(v => v.x).ToArray();
  Assert.That(xs.Max() + 100, Is.LessThanOrEqualTo(600.001f)); Assert.That(xs.Min() - 100, Is.GreaterThanOrEqualTo(-600.001f));
  Assert.That(xs, Is.Ordered); }
[Test] public void HandLayout_Zero_Empty() => Assert.That(HandLayout.Positions(0, 200, 1200, 20), Is.Empty);
[Test] public void PrefabBuilder_CreatesCatalogWithAllPrefabs()
{ PrefabBuilder.BuildAll(); var cat = VisualCatalog.Load();
  Assert.That(cat.Card, Is.Not.Null); Assert.That(cat.Fighter, Is.Not.Null); Assert.That(cat.StatusChip, Is.Not.Null);
  Assert.That(cat.Button, Is.Not.Null); Assert.That(cat.IntentBubble, Is.Not.Null); }
[Test] public void CardView_Bind_ShowsPreviewAndDimsWhenUnplayable()
{ // cat.Card örneklenir, Dmg kartı, new CardPreview(7, 0), playable false
  // önizleme metni "Hasar 7"; CanvasGroup.alpha 0.5; Button.interactable false; ad metni kart adı
}
[Test] public void FighterView_StatusChips_FollowEnumOrder()
{ // {Tired:1, Unbalanced:2} → çip etiketleri ["Dengesiz 2", "Yorgun 1"]
}
[Test] public void FighterView_BlockZero_Hidden()
[Test] public void IntentBubble_Card_ShowsNameDamageAndStatus()   // "tackle", "7 hasar", "Dengesiz"
[Test] public void IntentBubble_Pass_ShowsTargetAndPercent()     // "Pas: Mate (%72)"
```
Testler örneklenen nesneleri `Object.DestroyImmediate` ile temizler.

- [ ] **Step 2: Başarısız olduğunu gör**

Run: `tools/unity.sh tests editmode`
Expected: derleme hatası.

- [ ] **Step 3: Bileşenleri ve `PrefabBuilder`'ı yaz**

Prefab ölçüleri: kart 200×280, çöp adam 220×360, düğme 260×72. TMP yazı boyutları: kart adı 26, metin 20, maliyet 32. Varsayılan renkler:

| Alan | Renk |
|---|---|
| Hücum kartı | `#C8553D` |
| Savunma kartı | `#3D7EC8` |
| İkisi de | `#7D5BA6` |
| Kullanıcı takımı | `#2E6FD8` |
| Rakip takım | `#D83A2E` |
| Kaleci | `#E0B000` |
| Arka plan | `#1E5B2E` (çim yeşili) |

- [ ] **Step 4: Prefab'ları üret ve testleri çalıştır**

`tools/unity.sh`'a `prefabs` komutunu ekle: `-executeMethod WeirdLand.Unity.EditorTools.PrefabBuilder.BuildAll -quit`.

Run: `tools/unity.sh prefabs && tools/unity.sh tests editmode`
Expected: prefab'lar ve `VisualCatalog.asset` oluşur, `0 failed`.

- [ ] **Step 5: Commit**

```bash
git add tools/unity.sh unity/Assets/WeirdLand && git commit -m "unity: placeholder visuals, catalog and prefab builder"
```

---

### Task 8 (Görev 8): Düello ekranı, animasyonlar ve ekran görüntüsü aracı

**Files:**
- Create: `unity/Assets/WeirdLand/Runtime/Duel/DuelScreen.cs`, `Duel/PassPicker.cs`, `Duel/ResultOverlay.cs`, `Visual/TweenAwait.cs`, `App/AppFlow.cs` (ilk sürüm)
- Create: `unity/Assets/WeirdLand/Editor/SceneBuilder.cs`
- Generated: `unity/Assets/WeirdLand/Scenes/DuelSandbox.unity`, `ProjectSettings/EditorBuildSettings.asset`
- Create: `unity/Assets/WeirdLand/Tests/PlayMode/WeirdLand.Unity.Tests.PlayMode.asmdef`, `PlayMode/DuelScreenTests.cs`, `PlayMode/ScreenshotTests.cs`, `PlayMode/TestRig.cs`

**Interfaces:**
- Consumes: `DuelController`, `IDuelView` (Görev 6), görsel parçalar ve `VisualCatalog` (Görev 7), `DuelPreview` (Görev 3), `UiKeys` ve `UiText` (Görev 4), `DuelScenarios` (Görev 2), `LoadedData` (Görev 5), `GoodPolicy` (Kademe 1; `match` parametresi `null` olabilir)
- Produces:
  - `public class DuelScreen : MonoBehaviour, IDuelView`:
    - `void Open(Duel duel, LoadedData data, uint seed)`
    - `DuelController Controller { get; }`
    - `event Action BackRequested, RetrySameSeed, RetryNewSeed`
    - Testler için düğmelerle aynı yolu kullanan kancalar: `void ClickCard(int handIndex)`, `void ClickEndTurn()`, `void ClickPass()`, `void ClickPassTarget(string id)`, `void ClickCancelPass()`.
    - Durum okuyucular: `bool ResultShown { get; }`, `string ResultText { get; }`, `bool PassButtonVisible { get; }`, `bool PassPickerOpen { get; }`.
    - `float UserBarValue { get; }`, `float EnemyBarValue { get; }`: ekranda gösterilen can değeri.
  - `public static class TweenAwait`: `UniTask Wait(Tween t, CancellationToken ct)`. İptal olunca tween'i `Stop()` eder. PrimeTween'in `ToYieldInstruction()` çıktısı UniTask'ın `ToUniTask(cancellationToken: ct)` dönüşümüyle beklenir. API adlarını paket kaynağında doğrula.
  - `public class AppFlow : MonoBehaviour`. Bu görevde sadece veriyi yükler ve varsayılan senaryoyu açar: `mavi` ile `kirmizi`, FieldAttack, ST'ye karşı CM, seed `(uint)Environment.TickCount`. Görev 9'da genişler.
  - `public static class SceneBuilder`: `void BuildAll()` önce `PrefabBuilder.BuildAll()`'u, sonra sahneyi kurar. Menü yolu `WeirdLand/Build Scenes`.
  - `tools/unity.sh scenes` ve `tools/unity.sh shot`.

Ekran düzeni (1920×1080):
- Üst şerit: tur (`duel.turn`), düello türü (`scenario.X`), seed (`duel.seed`), "Seçime dön" düğmesi.
- Sol tarafta kullanıcının `FighterView`'ı. Can etiketi `UiKeys.HpLabel(kind, userIsAttacker)` ile seçilir. Rengi kaleci ise `Keeper`, değilse `UserTeam`.
- Sağ tarafta rakibin `FighterView`'ı ve üstünde `IntentBubble`. Topu taşıyan tarafta top işareti görünür.
- Alt şerit:
  - Solda enerji (`3/3`) ve deste/atılan sayıları.
  - Ortada el, `HandLayout` ile, `maxWidth` 1100.
  - Sağda "Turu bitir" ve `CanPass` ise üstünde "Pas" düğmesi. Pas düğmesinde `duel.passChance` ile `UiText.Percent(duel.PassChance())` görünür.
  - El boşsa ortada `duel.noCards` yazar.
- `PassPicker`: yarı saydam bir katman. Başlık `duel.passPickTitle`, her hedef için bir düğme (oyuncu adı ve `slot.X`), bir de `duel.cancel` düğmesi var. Vazgeç enerji harcamaz.
- `ResultOverlay`:
  - Sonuç metni: `UiKeys.Result(...)`.
  - Süre dolduysa, yani `DefenderWon` ve hücum edenin `Hp > 0` ise, altında `result.timeout`.
  - Üç düğme: `result.again`, `result.newSeed`, `result.back`.

`Animate` her olayda şunu yapar (süreler `AnimTimings`'ten, olay değerleri kullanılır):

| Olay | Animasyon |
|---|---|
| `TurnStarted` | Ortada "Tur n/m" yazısı belirip kaybolur (Banner) |
| `EnergyChanged` | Enerji sayısı güncellenir, kısa bir büyüme |
| `CardsDrawn(user, n)` | `duel.User.Hand` içinde henüz gösterilmeyen ilk n kart, el sırasıyla `CardView` olur ve deste noktasından yerine uçar (Draw, DrawStagger). Rakip için animasyon yok. |
| `IntentRevealed` | `IntentBubble.Show(DuelPreview.Intent(duel))` ve kısa bir büyüme |
| `CardPlayed(user)` | Tıklanan `CardView` merkeze uçar, büyür, solar, sonra yok edilir (CardFly) |
| `CardPlayed(enemy)` | Kart adı (`GameData.Card(id).Name`) merkezde `duel.enemyPlayed` ile görünür, rakip ileri atılır (Lunge, EnemyCardHold) |
| `DamageDealt` | Hedef sarsılır (Hit), yukarı süzülüp solan bir sayı çıkar (FloatNumber), çubuk `HpAfter`'a iner (Bar). `Blocked > 0` ise koruma göstergesi o kadar azalır. |
| `BlockGained` | Ekrandaki koruma değeri `Amount` kadar artar, kalkan simgesi büyüyüp küçülür |
| `BlockReset` | Koruma göstergesi 0 olur |
| `StatusApplied` / `StatusChanged` | O tarafın durum çipleri yenilenir |
| `PassAttempted` | Top işareti hedef yöne kayar, ekranda ihtimal ve başarılı ya da başarısız yazısı görünür (Pass) |
| `DuelEnded` | `ResultOverlay` açılır |

`Sync` her şeyi durumdan yeniden kurar: çubuklar, koruma, çipler, enerji, sayılar, düğme görünürlükleri ve niyet. Elde, durumda olmayan `CardView`'lar yok edilir, eksikler eklenir, sonra `HandLayout`'a göre dizilir. Her `CardView` `DuelPreview.Card` ve `DuelPreview.CanPlay` ile yeniden bağlanır.

Kart tıklaması akışı:
1. `Controller.IsBusy` ise hiçbir şey olmaz.
2. Tıklanan `CardView` "oynanıyor" olarak işaretlenir.
3. `Controller.PlayCard` çağrılır.
4. `false` dönerse kart yerine geri döner.

`OnDisable` ve `OnDestroy` sırasında `CancellationTokenSource` iptal edilir ve ekranın tween'leri `Tween.StopAll` ile durdurulur.

- [ ] **Step 1: PlayMode asmdef'ini ve test düzeneğini yaz**

PlayMode asmdef'i EditMode ile aynıdır. Farkı: `includePlatforms` boş, `WeirdLand.Unity.Editor` referansı yok.

`TestRig.Create(ScenarioKind kind, uint seed)` şunları yapar:
- Kamera, Canvas (ScreenSpaceCamera, 1920×1080) ve EventSystem kurar.
- `DuelScreen` ekler.
- `VisualCatalog.Load().Timings.Scale = 0.01f` yapar.
- `GameDataProvider.Load` ile veriyi, `DuelScenarios.Create` ile düelloyu kurar.
- `Open` çağırır ve `(screen, duel)` döndürür.

Slot olarak `DuelScenarios.UserSlots(kind)[0]` ve `DefaultEnemySlot` kullanılır. Teardown kurulan her şeyi yok eder ve `Scale`'i 1'e geri alır. Katalog asset'i değişmiş olarak kaydedilmemeli.

- [ ] **Step 2: Failing PlayMode testlerini yaz** (`DuelScreenTests.cs`)

```csharp
static IEnumerable<TestCaseData> Kinds() { foreach (ScenarioKind k in Enum.GetValues(typeof(ScenarioKind))) foreach (uint s in new uint[] { 1, 2, 3 }) yield return new TestCaseData(k, s); }

[UnityTest, TestCaseSource(nameof(Kinds)), Timeout(60000)]
public IEnumerator Autoplay_ReachesResultWithoutErrors(ScenarioKind kind, uint seed)
{ // GoodPolicy.Decide(duel, null, new Rng(seed)) ile karar ver; IsBusy değilken ClickCard/ClickPass+ClickPassTarget/ClickEndTurn;
  // ResultShown olana kadar döngü (en fazla 200 kare); sonunda ResultShown true, ResultText boş değil.
  // Herhangi bir Error/Exception günlüğü testi düşürür (UTF varsayılanı).
}
[UnityTest] public IEnumerator CardClick_Twice_SpendsEnergyOnce()     // aynı karede iki ClickCard(0): Energy tam bir kart maliyeti düşer
[UnityTest] public IEnumerator Bars_AfterEachCommand_MatchState()     // her komuttan ve IsBusy false olduktan sonra UserBarValue == duel.User.Hp, EnemyBarValue == duel.Enemy.Hp
[UnityTest] public IEnumerator PassButton_HiddenWhenDefending()       // FieldDefense → PassButtonVisible false
[UnityTest] public IEnumerator PassPicker_CancelDoesNotSpendEnergy()  // FieldAttack: ClickPass → PassPickerOpen; ClickCancelPass → kapalı, Energy aynı
[UnityTest] public IEnumerator EndTurnOnly_TimeoutLineShown()         // FieldAttack, sadece ClickEndTurn → ResultText "Süre doldu" içerir
[UnityTest] public IEnumerator HandMatchesStateAfterDrawEffects()     // el kartı sayısı == duel.User.Hand.Count her komuttan sonra
```

- [ ] **Step 3: Başarısız olduğunu gör**

Run: `tools/unity.sh tests playmode`
Expected: derleme hatası.

- [ ] **Step 4: `TweenAwait`, `DuelScreen`, `PassPicker` ve `ResultOverlay`'i yaz**

- [ ] **Step 5: Testleri çalıştır**

Run: `tools/unity.sh tests all`
Expected: EditMode ve PlayMode `0 failed`.

- [ ] **Step 6: Sahneyi üret**

`SceneBuilder.BuildAll()` sahneyi kurar:
- Orthographic `Main Camera`, arka plan `VisualCatalog.Background`.
- `EventSystem` ve `InputSystemUIInputModule`.
- `Canvas`: ScreenSpaceCamera, CanvasScaler 1920×1080, eşleşme 0.5.
- Kök nesnede `AppFlow`.

Sahne `Assets/WeirdLand/Scenes/DuelSandbox.unity` olarak kaydedilir ve `EditorBuildSettings`'teki tek sahne olur. `tools/unity.sh scenes` bu metodu çalıştırır.

Run: `tools/unity.sh scenes`
Expected: sahne dosyası oluşur, çıkış 0.

- [ ] **Step 7: Ekran görüntüsü aracını yaz**

`ScreenshotTests.Capture`:
- `Environment.GetEnvironmentVariable("WEIRDLAND_SHOTS") != "1"` ise `Assert.Ignore` çağırır.
- Her `ScenarioKind` için `TestRig` ile seed 1'de açar ve üç görüntü alır:
  1. ilk tur bittikten sonra,
  2. bir kart oynandıktan sonra,
  3. sonuç ekranında.
- Görüntü alma: kamerayı 1920×1080 bir `RenderTexture`'a çizer, `ReadPixels` ve `EncodeToPNG` ile `unity/Screenshots/<kind>-<n>.png` olarak kaydeder.

`tools/unity.sh shot`: `WEIRDLAND_SHOTS=1` ile `tests playmode` çalıştırır, `-testFilter WeirdLand.Unity.Tests.ScreenshotTests` verir ve PNG yollarını listeler. `-nographics` kullanılmaz.

Run: `tools/unity.sh shot`
Expected: 12 PNG. Her birini Read aracıyla aç ve kontrol et:
- yazılar okunuyor mu, kutu karakter var mı,
- kartlar ekrana sığıyor mu,
- niyet balonu görünüyor mu,
- iki çöp adam ayırt ediliyor mu.

Görüntüler siyah çıkarsa, toplu modda çizim çalışmıyor demektir. Bunu raporla, görüntü kontrolünü kullanıcıya bırak.

- [ ] **Step 8: Görsel düzeltmeleri yap, testleri tekrar çalıştır**

Run: `tools/unity.sh tests all`
Expected: `0 failed`.

- [ ] **Step 9: Commit**

```bash
git add tools/unity.sh unity && git commit -m "unity: duel screen with animations, sandbox scene and screenshot tool"
```

- [ ] **Step 10: Kontrol noktası (ana oturum, kullanıcıyla)**

Kullanıcıdan Unity'de `DuelSandbox` sahnesini açıp Play'e basmasını iste. Bir düello oynasın, bir ekran görüntüsü ve kısa bir izlenim paylaşsın. Görev 9'a geçmeden önce okunurluk ve akış sorunlarını düzelt.

---

### Task 9 (Görev 9): Senaryo seçim ekranı ve uygulama akışı

**Files:**
- Create: `unity/Assets/WeirdLand/Runtime/Scenario/ScenarioOptions.cs`, `Scenario/ScenarioScreen.cs`
- Modify: `unity/Assets/WeirdLand/Runtime/App/AppFlow.cs`, `unity/Assets/WeirdLand/Editor/SceneBuilder.cs` (gerekirse)
- Test: `unity/Assets/WeirdLand/Tests/EditMode/ScenarioOptionsTests.cs`, `unity/Assets/WeirdLand/Tests/PlayMode/AppFlowTests.cs`

**Interfaces:**
- Consumes: `DuelScenarios` (Görev 2), `DuelScreen` (Görev 8), `GameDataProvider` (Görev 5)
- Produces:
  - `public sealed record ScenarioSelection(string UserTeamId, string OpponentTeamId, ScenarioKind Kind, Slot UserSlot, Slot EnemySlot, uint Seed)`, ayrıca `DuelScenario ToScenario()`.
  - `public static class ScenarioOptions`:
    - `IReadOnlyList<TeamDef> Teams(GameData d)`: önce `Tier`, sonra `Id`'ye göre sıralı.
    - `ScenarioSelection Default(GameData d, uint seed)`: kullanıcı `mavi`, yoksa ilk takım. Rakip, kullanıcı dışındaki ilk takım. FieldAttack, ST, `DefaultEnemySlot`.
    - `ScenarioSelection Normalize(GameData d, ScenarioSelection s)`:
      - bilinmeyen takım varsayılana döner,
      - türe uymayan kullanıcı slotu `UserSlots(kind)[0]` olur,
      - türe uymayan rakip slotu `DefaultEnemySlot(kind, userSlot)` olur.
    - `bool TryParseSeed(string text, out uint seed)`: baştaki ve sondaki boşluklar kırpılır, sadece rakam kabul edilir, `uint` sınırı aşılırsa false.
  - `public class ScenarioScreen : MonoBehaviour`:
    - `void Open(LoadedData data, ScenarioSelection initial)`
    - `ScenarioSelection Current { get; }`
    - `event Action<ScenarioSelection> StartRequested`
    - Test kancaları: `void SetKind(ScenarioKind k)`, `void SetSeedText(string s)`, `void ClickStart()`.
  - `AppFlow`:
    - `string DataDirectoryOverride` (test kancası).
    - `bool ErrorShown { get; }`, `string ErrorText { get; }`.
    - `ScenarioScreen Scenario { get; }`, `DuelScreen DuelView { get; }`.

Ekran ve akış:
- `ScenarioScreen`:
  - Başlık `scenario.title`.
  - Beş `TMP_Dropdown`:
    - takımın ve rakip takım: takım adlarıyla,
    - düello türü: `scenario.X` ile,
    - oyuncun ve rakip oyuncu: her biri `slot.X — oyuncu adı` biçiminde, sadece türe uyan slotlar.
  - Bir `TMP_InputField` (seed) ve `scenario.start` düğmesi.
  - Tür ya da takım değişince `Normalize` uygulanır ve açılır listeler yenilenir. Geçersiz seed metni girilirse son geçerli seed kalır.
- `AppFlow.Start()` veriyi yükler:
  - `DataException` ya da IO hatası olursa hata paneli açılır. Panelde sabit metin `"Oyun verisi yüklenemedi."`, alt satırda istisna mesajı (tam yolu içerir) görünür.
  - Başarılıysa `ScenarioScreen` açılır, `Default(data, (uint)Environment.TickCount)` ile.
- `StartRequested` gelince senaryo ekranı gizlenir, `DuelScreen.Open(DuelScenarios.Create(...), data, seed)` çağrılır.
- `DuelScreen` olayları:
  - `BackRequested`: düello ekranı kapanır, işi iptal edilir, senaryo ekranı son seçimle açılır.
  - `RetrySameSeed`: aynı seçimle yeni düello açılır.
  - `RetryNewSeed`: seed `(uint)Environment.TickCount` ile yenilenir.

- [ ] **Step 1: Failing testleri yaz**

`ScenarioOptionsTests.cs` (EditMode):
```csharp
[TestCase("123", true, 123u)] [TestCase(" 42 ", true, 42u)] [TestCase("", false, 0u)]
[TestCase("abc", false, 0u)] [TestCase("-1", false, 0u)] [TestCase("4294967296", false, 0u)]
public void TryParseSeed(string text, bool ok, uint expected)
{ Assert.That(ScenarioOptions.TryParseSeed(text, out var s), Is.EqualTo(ok)); if (ok) Assert.That(s, Is.EqualTo(expected)); }
[Test] public void Teams_SortedByTierThenId()
[Test] public void Default_IsMaviFieldAttackStriker()
[Test] public void Normalize_KindToShotAsKeeper_MovesUserToGkAndEnemyToSt()
[Test] public void Normalize_UnknownTeam_FallsBackToDefault()
```
`AppFlowTests.cs` (PlayMode). Sahne yüklenmez. Test, `AppFlow`'u bir test düzeneğinde kurar, düzenek Görev 8'deki `TestRig` ile aynı Canvas kurulumunu kullanır.
```csharp
[UnityTest] public IEnumerator Start_ShowsScenarioScreen()
[UnityTest] public IEnumerator DataMissing_ShowsErrorPanelWithPath()      // DataDirectoryOverride = "Z:/yok/data" → ErrorShown, ErrorText "Z:" içerir
[UnityTest] public IEnumerator StartThenBack_KeepsSelection()             // SetKind(ShotAsShooter), ClickStart, BackRequested → Current.Kind == ShotAsShooter
[UnityTest] public IEnumerator BackDuringAnimation_NoErrors()              // kart oynat, IsBusy iken geri dön; hata günlüğü yok, sonraki Start çalışır
[UnityTest] public IEnumerator RetrySameSeed_SameOpeningHand()            // ilk el kimlikleri iki açılışta aynı
[UnityTest] public IEnumerator RetryNewSeed_ChangesSeed()
```

- [ ] **Step 2: Başarısız olduğunu gör**

Run: `tools/unity.sh tests all`
Expected: derleme hatası.

- [ ] **Step 3: `ScenarioOptions`, `ScenarioScreen` ve `AppFlow`'u yaz, sahneyi yeniden üret**

Run: `tools/unity.sh scenes && tools/unity.sh tests all`
Expected: `0 failed`.

- [ ] **Step 4: Senaryo ekranının görüntüsünü al ve kontrol et**

`ScreenshotTests`'e senaryo ekranı için bir görüntü ekle: `scenario-1.png`.

Run: `tools/unity.sh shot`
Read ile kontrol et: açılır listeler okunuyor mu, Türkçe karakterler doğru mu.

- [ ] **Step 5: Commit**

```bash
git add unity && git commit -m "unity: scenario picker and app flow"
```

---

### Task 10 (Görev 10): Windows sürümü ve belgeler

**Files:**
- Create: `unity/Assets/WeirdLand/Editor/BuildScript.cs`, `docs/unity-contract.md`
- Modify: `unity/Assets/WeirdLand/Runtime/App/AppFlow.cs` (duman bayrağı), `tools/unity.sh` (`build`), `CLAUDE.md`, `docs/superpowers/specs/2026-11-22-weirdland-design.md` (§8.1, §8.5, §8.6), `docs/superpowers/m2-backlog.md`
- Test: `unity/Assets/WeirdLand/Tests/EditMode/BuildScriptTests.cs`

**Interfaces:**
- Produces:
  - `public static class BuildScript`:
    - `void CopyData(string srcDir, string dstDir)`: hedef klasörü silip yeniden oluşturur ve sadece `*.json` dosyalarını kopyalar.
    - `void BuildWindows()`:
      1. `CopyData(<repo>/data, Assets/StreamingAssets/data)` ve `AssetDatabase.Refresh()`.
      2. `BuildPipeline.BuildPlayer` çağrılır: sahne `DuelSandbox`, hedef `StandaloneWindows64`, yol `<repo>/builds/Windows/WeirdLand.exe`, ürün adı `WeirdLand`, 1920×1080 pencere.
      3. Sonuç `Succeeded` değilse `EditorApplication.Exit(1)`.
  - `AppFlow`: komut satırında `-weirdland-smoke` varsa veriyi yükler ve şunu yazar: `WEIRDLAND_SMOKE_OK cards=<n> teams=<n> ui=<n>`. Başarılıysa `Application.Quit(0)`, hata olursa `WEIRDLAND_SMOKE_FAIL <mesaj>` ve `Application.Quit(1)`.

- [ ] **Step 1: Failing testi yaz**

```csharp
[Test] public void CopyData_CopiesJsonOnly_AndRemovesStale()
{ // geçici src: a.json, b.json, notes.txt; dst içinde eski stale.json
  // CopyData sonrası dst = {a.json, b.json}
}
```

- [ ] **Step 2: Başarısız olduğunu gör, `BuildScript`'i yaz, testi geçir**

Run: `tools/unity.sh tests editmode`
Expected: önce derleme hatası, sonra `0 failed`.

- [ ] **Step 3: Sürümü al ve duman testi yap**

`tools/unity.sh build`: `-batchmode -quit -projectPath unity -executeMethod WeirdLand.Unity.EditorTools.BuildScript.BuildWindows -logFile unity/Logs/build.log`.

Run:
```bash
tools/unity.sh build && builds/Windows/WeirdLand.exe -batchmode -nographics -weirdland-smoke -logFile builds/smoke.log; echo "exit $?"; grep WEIRDLAND_SMOKE builds/smoke.log
```
Expected: `exit 0` ve `WEIRDLAND_SMOKE_OK cards=4x teams=4 ui=7x` biçiminde bir satır. Sayılar gerçek veriden gelir.

- [ ] **Step 4: Belgeleri yaz**

- **`docs/unity-contract.md`** (kısa):
  - Hangi bilgi durumdan okunur, hangisi olaydan.
  - Her olay türü için animasyonun kullandığı alan.
  - `Sync` kuralı.
  - `CardsDrawn` kuralı: eldeki henüz gösterilmeyen ilk n kart.
  - Kart dışında çekirdeğe dokunmama kuralı.
  - Golden parmak izinin ne zaman güncelleneceği: kural ya da veri değişince, iki testte birlikte.
  - Yığın yenileme kuralı: rakip yeni yığın eklediğinde tick sonrası sonuç `eski − 1 + yeni` olur. Bu devreden işlerdeki maddedir.
- **`CLAUDE.md`** "Komutlar" bölümüne:
  - `tools/unity.sh setup|prefabs|scenes|tests|shot|build`,
  - Unity editörü açıkken toplu komutların çalışmadığı,
  - DLL'in `dotnet build` ile kopyalandığı.
- **Spec güncellemesi:**
  - §8.1'e karar notu: DLL seçildi.
  - §8.5 ve §8.6'da DOTween yerine PrimeTween (OpenUPM) yazılır, sebebiyle: DOTween paket yöneticisiyle kurulamıyor.
- **`m2-backlog.md`:** Kademe 2'de kapanan maddeler işaretlenir. Bunlar ComputeBlock, salt okunur yüzey, Unity sözleşmesi, `link.xml`, ondalık determinizm, DLL kararı ve `RealDataTests` yolu.

- [ ] **Step 5: Son doğrulama**

Run: `cd core && dotnet test && cd .. && tools/unity.sh tests all`
Expected: hepsi `0 failed`.

- [ ] **Step 6: Commit**

```bash
git add unity tools CLAUDE.md docs && git commit -m "unity: Windows build with bundled data; docs: Unity contract and M2 notes"
```

- [ ] **Step 7: Kullanıcı oyun testi**

Kullanıcı `builds/Windows/WeirdLand.exe`'yi açar ve dört türün her birinden en az bir düello oynar. İzlenimleri `docs/balance/m2-playtest.md` dosyasına yazılır. Örnek sorular:
- Niyet okunuyor mu?
- Rakip tek kartla fazla mı vuruyor?
- Taşıyıcı çok mu kırılgan?

Denge değişikliği bu kademede yapılmaz, Kademe 3 planına girer.
