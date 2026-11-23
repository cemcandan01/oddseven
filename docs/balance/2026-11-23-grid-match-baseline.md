# Izgara maçı taban ölçümü (2026-11-23)

## Komutlar
```
cd core
dotnet run --project WeirdLand.Sim -- --mode grid --matches 1000 --seed 1 --policy good --opp all
dotnet run --project WeirdLand.Sim -- --mode grid --matches 1000 --seed 1 --policy random --opp kirmizi
```
`--mode grid`: `good` = `GreedyGridPolicy` + `GoodPolicy`, `random` = `RandomGridPolicy` + `RandomPolicy`. Kullanıcı mavi. Her maçta seed `seed + i`, politika akışı `seed ^ 0x5bd1e995` (klasik modla aynı). Pas başarısı = BallPassed / (BallPassed + PassIntercepted). Düello serisi: aynı iki oyuncu arasında üst üste düello.

## Ayar sonrası sonuç (GridMatchMoves = 25, StunMoves = 2)

| Rakip | Politika | G / B / M | Gol (kul. - rakip) | Hamle | Düello/maç | Pas/maç | Pas başarısı | En uzun seri |
|---|---|---|---|---|---|---|---|---|
| kirmizi | good | %48 / %52 / %0 | 0.52 - 0.00 | 25 | 22.7 | 9.5 | %83 | 3 |
| martilar | good | %8 / %58 / %34 | 0.09 - 0.41 | 25 | 20.9 | 8.2 | %82 | 3 |
| ahtapotlar | good | %9 / %53 / %38 | 0.13 - 0.53 | 25 | 21.6 | 6.8 | %83 | 3 |
| kirmizi | random | %0 / %19 / %81 | 0.00 - 1.13 | 25 | 21.9 | 7.7 | %77 | 3 |

Kullanıcı hücumdayken ve savunmadayken düelloyu savunan tarafın kazanma payı (good):

| Rakip | Kullanıcı hücumda: savunan kazanır | Kullanıcı savunmada: savunan (kullanıcı) kazanır |
|---|---|---|
| kirmizi | %47 | %100 |
| martilar | %65 | %62 |
| ahtapotlar | %62 | %60 |
| kirmizi (random) | %76 | %51 |


## Ayar turları
Hedef: ortalama düello 10-25, iyi ile rastgele arasında belirgin fark. Tüm satırlar 1000 maç, seed 1, good politika, mavi. Hücre: G / B / M, gol (kul. - rakip), düello/maç.

| Tur | Değişiklik | Hamle | kirmizi | martilar | ahtapotlar |
|---|---|---|---|---|---|
| 0 | GridMatchMoves 90, StunMoves 2 | 90 | %97/%3/%0, 2.44-0.00, 79.3 | %17/%28/%55, 0.59-1.44, 71.7 | %17/%28/%55, 0.66-1.60, 70.9 |
| 1 | StunMoves 5 (90 hamle), geri alındı | 90 | %94/%6/%0, 1.99-0.00, 61.0 | %28/%46/%26, 0.44-0.44, 57.0 | %23/%35/%42, 0.65-1.01, 57.8 |
| 2 | GridMatchMoves 30, StunMoves 2 | 30 | %59/%41/%0, 0.67-0.00, 27.0 | %11/%53/%37, 0.12-0.49, 24.9 | %11/%49/%41, 0.17-0.61, 25.4 |
| 3 | GridMatchMoves 25, StunMoves 2 (kabul) | 25 | %48/%52/%0, 0.52-0.00, 22.7 | %8/%58/%34, 0.09-0.41, 20.9 | %9/%53/%38, 0.13-0.53, 21.6 |

Altın ızgara parmak izi `21123685` -> `2c2ff2a7` olarak yeniden kaydedildi (maç 90 -> 25 hamle olduğu için olay listesi değişti). Klasik altın (`3b407571`) değişmedi. `Movement_IsDeterministic` testi maç bitince döngüden çıkacak şekilde düzeltildi ve boş olmayan log doğrulaması eklendi.

## Sonuçlar
- **Düello hedefi sadece maçı kısaltarak tutturuldu.** Hamle başına düello oranı değişmedi: tur 0'da 79.3/90 (%88), tur 3'te 22.7/25 (%91) kirmizi için; martilar %80 -> %84, ahtapotlar %79 -> %86. Yani hedef (10-25) maç temposu hakkında bir şey söylemiyor. **Açık tasarım sorunu:** neredeyse her hamlede düello açılıyor; pas ve ilerleme seçimiyle düelloyu belirleyen "satranç" hissi bu haliyle zayıf. Düello sıklığını azaltan bir değişiklik (baskı/yerleşim kuralları) ayrı ele alınmalı.
- Ölçülen etkiler: StunMoves 2 -> 5 düelloyu yaklaşık %23 azalttı (79.3 -> 61.0) ve skoru dengeledi, ama hedefe yetmedi. Pas katsayıları (Pass*) ve ShiftFactor hiç değiştirilmedi, etkileri ölçülmedi.
- Iyi politika rastgeleye belirgin üstün: kirmizi'ye karşı good %48 G / %0 M, random %0 G / %81 M.
- **Gol sayısı:** 90 hamlede kirmizi'ye karşı 2.44 gol, martilar'a 0.59-1.44 idi. Maçı 25 hamleye indirmek golleri yaklaşık orantılı düşürdü (0.52 / 0.09-0.41), bu düşüşün asıl sebebi büyük olasılıkla maç kısalığı. Düello gücü veya `EnemyPowerScalePercent` suçlanamaz, çünkü ayrı bir karşılaştırma yapılmadı. 90 hamlede bile martilar/ahtapotlar'a karşı good kaybediyordu (%55 M), bu ayrı bir güç dengesi konusu.
- En uzun aynı ikili serisi hep 3, spec sınırı sağlanıyor.

## Kullanıcı savunma / hücum düello payları (tur 3)
Düellolar kullanıcının hücum veya savunma tarafında olmasına göre ayrıldı (toplam, "turn-1 KO" tek tek incelenmedi):
kirmizi'ye karşı rakip hücumdayken kullanıcı savunmasının kazanma oranı %100 (kirmizi hücumda hiçbir düello kazanmıyor). Güçlü rakiplerde kullanıcı savunmada %60-62 kazanıyor, hücumda ise %35-38. "Savunmacı ilk turda kolay KO oluyor" endişesi sadece bu toplu kazanma oranlarıyla kontrol edildi, tur bazlı bir ölçüm yapılmadı; toplu oranlarda savunma tarafında sorun görünmüyor.

## Öneri
Düello sıklığı (açık tasarım sorunu), gol sayısı ve güçlü rakibe karşı hücum dengesi ayrı denge işlerinde ele alınmalı. `EnemyPowerScalePercent`, kart verisi ve düello kuralları bu görevde değiştirilmedi.
