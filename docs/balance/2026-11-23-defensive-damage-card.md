# Savunma destelerine hasar kartı (2026-11-23)

## Değişiklik
Her saha oyuncusunun hem hücum hem savunma destesinde en az bir hasar kartı olmalı. Veri testi: `RealData_EveryFieldPlayerHasDamageCardInBothRoles`.

Kuralı sağlamayan 10 oyuncunun destesine `kayarak_mudahale` eklendi:
mavi_st, kirmizi_cm, kirmizi_st, martilar_cm, martilar_lw, martilar_rw, martilar_st, ahtapotlar_cm, ahtapotlar_lw, ahtapotlar_rw.

**Sebep:** kirmizi_cm'nin savunma destesi sadece blok kartlarından oluşuyordu. Ona karşı hücum eden oyuncu hiç hasar almıyordu, yani düelloyu kaybedemiyordu.

## Ölçüm: tek düello, saha hücumu, GoodPolicy, 2000 seed
Kullanıcı mavi. Rakip savunmacı varsayılan eşleşmeden geliyor (ST'ye karşı CM).

| Rakip | Kullanıcı | Önce: hücum kazanır | Sonra: hücum kazanır | Sonra: savunma kazanır |
|---|---|---|---|---|
| kirmizi | ST | %100 | %82 | %17 |
| martilar | ST | %99 | %78 | %21 |
| ahtapotlar | ST | %97 | %60 | %38 |

LW, CM ve LB senaryolarında hücumcunun kazanma oranı ±%5 içinde kaldı. Bu senaryolarda rakip savunmacının zaten hasar kartı vardı ya da sonuç pasla belirleniyordu.

## Ölçüm: maç, `--policy good --opp all --seed 1`
Önce 500 maç, sonra 1000 maç oynatıldı.

| Rakip | Önce G/B/M | Sonra G/B/M |
|---|---|---|
| kirmizi | %80 / %13 / %7 | %76 / %13 / %11 |
| martilar | %48 / %25 / %27 | %36 / %30 / %34 |
| ahtapotlar | %30 / %20 / %49 | %25 / %21 / %55 |

Rakipler beklendiği gibi biraz güçlendi. Tier sıralaması korunuyor.
