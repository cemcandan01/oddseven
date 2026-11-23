# Izgara maçı taban ölçümü (2026-11-23)

## Komutlar
```
cd core
dotnet run --project WeirdLand.Sim -- --mode grid --matches 1000 --seed 1 --policy good --opp all
dotnet run --project WeirdLand.Sim -- --mode grid --matches 1000 --seed 1 --policy random --opp kirmizi
```
`--mode grid`: `good` = `GreedyGridPolicy` + `GoodPolicy`, `random` = `RandomGridPolicy` + `RandomPolicy`. Kullanıcı mavi. Her maçta seed `seed + i`, politika akışı `seed ^ 0x5bd1e995` (klasik modla aynı). Pas başarısı = BallPassed / (BallPassed + PassIntercepted). Düello serisi: aynı iki oyuncu arasında üst üste düello.

## Ayar sonrası sonuç (GridMatchMoves = 25, StunMoves = 2)

| Rakip | Politika | G / B / M | Gol (kul. - rakip) | Düello/maç | Pas/maç | Pas başarısı | En uzun seri |
|---|---|---|---|---|---|---|---|
| kirmizi | good | %48 / %52 / %0 | 0.52 - 0.00 | 22.7 | 9.5 | %83 | 3 |
| martilar | good | %8 / %58 / %34 | 0.09 - 0.41 | 20.9 | 8.2 | %82 | 3 |
| ahtapotlar | good | %9 / %53 / %38 | 0.13 - 0.53 | 21.6 | 6.8 | %83 | 3 |
| kirmizi | random | %0 / %19 / %81 | 0.00 - 1.13 | 21.9 | 7.7 | %77 | 3 |

Kullanıcı hücumdayken ve savunmadayken düelloyu savunan tarafın kazanma payı (good):

| Rakip | Kullanıcı hücumda: savunan kazanır | Kullanıcı savunmada: savunan (kullanıcı) kazanır |
|---|---|---|
| kirmizi | %47 | %100 |
| martilar | %65 | %62 |
| ahtapotlar | %62 | %60 |
| kirmizi (random) | %76 | %51 |

Kullanıcı savunmada düelloların çoğunu kazanıyor, ilk turda kolay KO endişesi (EnemyPowerScalePercent 260) bu ölçümde doğrulanmadı. Güçlü rakiplerde kullanıcı savunmada %60-62 kazanıyor, hücumda %35-38 (savunan %62-65). Yani sorun savunma değil hücum tarafı: güçlü rakibe karşı hücum düellosu zor. Rakip savunmada, kirmizi'ye karşı rakip hücumda hiç kazanamıyor (%100).

## Ayar turları
Hedef: ortalama düello 10-25, iyi ile rastgele arasında belirgin fark.

| Tur | Değişiklik | Düello/maç (kirmizi, good) | Sonuç |
|---|---|---|---|
| 0 | GridMatchMoves 90, StunMoves 2 | 79.3 | Hedefin çok üstünde, her hamle neredeyse düello |
| 1 | StunMoves 5 (90 hamle) | 61.0 | Sersemletme zayıf kaldıraç, geri alındı |
| 2 | GridMatchMoves 30, StunMoves 2 | 27.0 (martilar 24.9) | Hedefin biraz üstünde |
| 3 | GridMatchMoves 25, StunMoves 2 | 22.7 (martilar 20.9) | Hedefte, kabul edildi |

Altın ızgara parmak izi `21123685` -> `2c2ff2a7` olarak yeniden kaydedildi (maç süresi 90 -> 25 hamle olduğu için olay listesi değişti). Klasik altın parmak izi (`3b407571`) değişmedi. `Movement_IsDeterministic` testi, maç bitince döngüden çıkacak şekilde düzeltildi (kısalan maç sonrası Advance çağırıyordu).

## Sonuçlar
- Düello sayısı hedefte (21-23). Düello sıklığı hamlelerin ~%85'i: savunmacı baskısı her hamlede düello açıyor. Düello sayısını sadece maç uzunluğu belirliyor, pas/sersemletme katsayıları zayıf etkili.
- Iyi politika rastgeleye belirgin üstün: kirmizi'ye karşı %48 galibiyet / %0 mağlubiyet, random %0 / %81.
- Gol sayısı düşük (maç başına 0.1-0.5 kullanıcı golü). Martilar ve ahtapotlar'a karşı iyi politika bile galip gelmiyor (%8-9 G, %34-38 M). Bunun sebebi düello gücü (tier ölçeği), `EnemyPowerScalePercent` ve kart verisi değiştirilmedi.
- En uzun aynı ikili serisi hep 3, spec sınırı (3'ten fazla olmayacak) sağlanıyor.

## Öneri
Gol sayısı ve güçlü rakiplere karşı hücum dengesi için `EnemyPowerScalePercent`, kart verisi veya düello kuralları ayrı bir denge işinde ele alınmalı.
