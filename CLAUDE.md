# WeirdLand

Futbol temalı, sıra tabanlı deckbuilder roguelike. Slay the Spire'ın futbol versiyonu: maç sahada akar, iki oyuncu karşılaşınca kartlarla oynanan bir düello açılır.

- Tasarım: `docs/superpowers/specs/2026-11-22-weirdland-design.md`
- Aktif plan: `docs/superpowers/plans/2026-11-22-m1-core-and-sim.md`
- `prototype/index.html`: eski HTML prototip. **Sadece referans, değiştirme.**

## Depo düzeni
- `core/`: saf C# kurallar motoru (`WeirdLand.Core`), testler (`WeirdLand.Core.Tests`), simülatör (`WeirdLand.Sim`)
- `data/`: oyun verisi (JSON). **Tek kaynak.** Unity ve simülatör aynı dosyaları okur.
- `unity/`: Unity 6000.3.9f1 projesi (Kademe 2'den itibaren)
- `docs/`: tasarım, planlar, denge raporları

## Komutlar (hepsi `core/` klasöründen)
- Test: `dotnet test`
- Derleme: `dotnet build`
- Simülasyon: `dotnet run --project WeirdLand.Sim -- --matches 1000 --seed 1 --policy good --opp all`
- Izgara modu: `dotnet run --project WeirdLand.Sim -- --mode grid --matches 1000 --seed 1 --policy good --opp all`

## Çekirdek kuralları (WeirdLand.Core)
- `netstandard2.1`, `LangVersion 9.0`. Unity 6 uyumu için C# 10+ özelliklerini kullanma: file-scoped namespace, global using, record struct, `required` yok.
- `UnityEngine` referansı yok.
- **Determinizm:** tüm rastgelelik `WeirdLand.Core.Rng` üzerinden gelir. `System.Random`, `DateTime.Now`, `Guid.NewGuid`, sırası belirsiz koleksiyon dolaşımı (`HashSet`/`Dictionary` üzerinde sıra önemliyse) kullanma. Aynı seed ve aynı girdiler aynı sonucu ve aynı olay listesini vermeli.
- **Mimari:** komut girer, olay çıkar. Arayüz komut gönderir, çekirdek durumu günceller ve `GameEvent` listesi döndürür. Çekirdek animasyondan ve zamandan habersizdir.
- **Dil:** kod tanımlayıcıları İngilizce, oyuncuya görünen metinler Türkçe ve JSON'da.
- **Sabitler:** denge sayıları `DuelRules` / `MatchRules` içinde veya `data/*.json` içinde durur, koda dağılmaz.

## Çalışma şekli
- Değişiklik önce testle: TDD. Her görev sonunda `dotnet test` geçmeli.
- Denge değişikliğinden sonra simülatörle ölç, sonucu `docs/balance/` altına yaz.
- Commit mesajları İngilizce, kısa, önek kullanır: `core:`, `data:`, `sim:`, `balance:`, `unity:`, `docs:`.
