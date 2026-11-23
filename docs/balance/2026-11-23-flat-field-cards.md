# Sade saha kartları (2026-11-23)

## Değişiklik
Kurgu oturana kadar saha düellosu kartları sadeleştirildi. 30 saha kartının etkisi tek bir düz değere indi:
- **Hasar kartları:** "N hasar." Stat ölçeklenmesi, durum etkisi (Dengesiz, Momentum, Yorgun), kart çekme ve enerji etkisi yok.
- **Savunma kartları:** "N blok."
- Kart kimlikleri, isimleri, maliyetleri, rolleri ve mevki etiketleri değişmedi. Desteler aynı kaldı.
- Şut kartları (köşe hedefli kartlar ve `sert_sut`) ile kaleci kartları değişmedi.
- Eski tanımlar `data/archive/cards-v1.json` dosyasında saklanıyor. Oyun bu dosyayı okumaz.
- Motor değişmedi. Durum etkileri ve ölçeklenme kodda duruyor, sadece veride kullanılmıyor.

**Değer cetveli** (ilk denemede hasar değerleri 2 düşüktü, aşağıdaki ilk denemeye bakın):

| Maliyet | Hasar (sıradan / nadir+) | Blok |
|---|---|---|
| 0 | - | 4 |
| 1 | 7–8 / 9 | 5–7 |
| 2 | 12–14 / 15–16 | 12 |
| 3 | - | 17 |

## Ölçüm: tek düello, saha hücumu, GoodPolicy, 2000 seed

| Rakip | Kullanıcı | Hücum kazanır | Savunma kazanır | Pas başarılı | Pas başarısız |
|---|---|---|---|---|---|
| kirmizi | LW | %31 | %30 | %27 | %9 |
| kirmizi | CM | %60 | %39 | %0 | %0 |
| kirmizi | ST | %52 | %47 | %0 | %0 |
| kirmizi | LB | %0 | %41 | %44 | %13 |
| martilar | LW | %38 | %56 | %4 | %1 |
| martilar | ST | %46 | %53 | %0 | %0 |
| ahtapotlar | LW | %12 | %55 | %24 | %7 |
| ahtapotlar | ST | %45 | %54 | %0 | %0 |

## Ölçüm: maç, 1000 maç, seed 1

| Rakip | good: G / B / M | Gol (kullanıcı - rakip) |
|---|---|---|
| kirmizi | %81 / %20 / %0 | 1.86 - 0.00 |
| martilar | %28 / %57 / %15 | 0.45 - 0.23 |
| ahtapotlar | %9 / %44 / %47 | 0.16 - 0.89 |

Random politika kirmizi'ye karşı %3 kazanıyor. Hedef %10'un altıydı, sağlanıyor.

**Not:** maç başına gol sayısı spec hedefinin altında. Şut kartları eski haliyle duruyor ve maç akışı ızgara sistemiyle yeniden yazılacak. Bu yüzden maç seviyesi dengesi şimdilik ayarlanmadı.

## İlk deneme (geri alındı)
Hasar kartları maliyet 1 için 5–7, maliyet 2 için 9–13 değerindeydi. Sonuç: martilar ve ahtapotlar maçlarının %78–84'ü berabere bitti, maç başına 0.1 gol atıldı. Sebebi: eski kartlar statla ölçekleniyordu (ör. Çalım 3 + Çalım statı ≈ 8), düz değerler iyi hücumcular için eskisinden zayıf kaldı.
