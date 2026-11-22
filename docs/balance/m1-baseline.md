# M1 temel denge ayarı (Kademe 1, Görev 10 + son düzeltme turu)

Tarih: 2026-11-22 · Dal: `m1-core-sim` · Kullanıcı takımı: `mavi` (başlangıç takımı)

Komutlar (`core/` klasöründen):

```
dotnet run --project WeirdLand.Sim -- --matches 2000 --seed 1 --policy random --opp kirmizi
dotnet run --project WeirdLand.Sim -- --matches 2000 --seed 1 --policy good --opp all
```

Süre tahmini simülatörün formülüdür: `tur × 15 sn + düello × 5 sn`.

## 1. Özet

Bu belge iki ölçümü birlikte anlatır:

- **Görev 10 ayarı** (commit `ebda27b`). Ayardan önce rakip neredeyse hiç gol atamıyordu: iyi model her kademeyi %97-100 yeniyordu, rastgele model 1. kademeyi %41 yeniyordu. Ayar dört kaldıraçla yapıldı (ayrıntı bölüm 5 ve 6):
  1. **Rakip güç çarpanı** %100'den %260'a çıktı. Bu, rakibin turda tek kart oynamasını telafi eder.
  2. **Can formülleri** değişti. Top taşıyan kırılgan (Kontrol düşük), savunan dayanıklı (Denge yüksek). Kaleci (Kale) biraz güçlendi.
  3. **Yeni kural:** köşe (şut) kartı kaleci olmayan bir rakibe yarım hasar verir.
  4. **Deste değişiklikleri:** kullanıcının orta saha ve kanat oyuncularına top kapma kartı verildi. Rakip takımların kanat kartları, kademe sırasını tutturacak ve takım tarzına uyacak şekilde değiştirildi.
- **Son düzeltme turu** (bu belge). Görev 10 ölçülürken bir motor hatası vardı: rakibin uyguladığı 1 yığınlık süreli durumlar etki etmeden siliniyordu ve rakip kalecinin köşeye yatma niyeti kozmetikti. Hata düzeltildi (bölüm 4), ölçümler yeniden alındı.

**Sonuç:** düzeltmeden sonra beş hedefin hepsi hâlâ tutuyor. Hiçbir denge değeri değişmedi. Sayılar küçük oranda oynadı (bölüm 2). En görünür değişiklik: kullanıcı şutları kaleciye karşı biraz daha sık ve daha hızlı gol oluyor.

## 2. Hedef ve sonuç (2000 maç, seed 1)

| Ölçüm | Hedef | Görev 10 öncesi | Görev 10 (`ebda27b`) | Şimdi | Durum |
|---|---|---|---|---|---|
| random vs 1. kademe (kirmizi), kullanıcı galibiyeti | < %10 | %41 | %1 | **%1** | tuttu |
| good vs 1. kademe (kirmizi), galibiyet | ≥ %55 | %100 | %80 | **%81** | tuttu |
| good vs 3. kademe (ahtapotlar), galibiyet | %25 – %50 | %97 | %28 | **%30** | tuttu |
| good, ortalama tahmini süre (dk) | 5 – 10 | 10.2 / 10.1 / 8.9 | 8.0 / 9.2 / 9.7 | **7.9 / 9.2 / 9.7** | tuttu (3. kademe üst sınıra yakın) |
| good, maç başı toplam gol | 1.5 – 4.5 | 6.54 / 5.32 / 4.32 | 3.25 / 2.00 / 2.43 | **3.27 / 2.01 / 2.50** | tuttu |

Süre ve gol sütunlarında sıra: kirmizi / martilar / ahtapotlar.

Seed 5000 ile tekrar ölçüldü (2000 maç). Sonuçlar seed 1 ile aynı bantta kaldı:

| Eşleşme | Görev 10 | Şimdi |
|---|---|---|
| good vs kirmizi | %80, gol 2.81-0.47, 8.1 dk | %80, gol 2.82-0.47, 8.0 dk |
| good vs martilar | %45, gol 1.16-0.82, 9.2 dk | %45, gol 1.17-0.82, 9.1 dk |
| good vs ahtapotlar | %27, gol 0.87-1.49, 9.8 dk | %30, gol 0.95-1.48, 9.7 dk |
| random vs kirmizi | – | %1, gol 0.08-3.48, 10.2 dk |

## 3. Son ölçümler (2000 maç, seed 1)

Parantez içi değer Görev 10 ölçümüdür.

| Eşleşme | G / B / M | Gol (kullanıcı-rakip) | Düello | Tur | Pas | Süre ort. / %90 |
|---|---|---|---|---|---|---|
| good vs kirmizi | %81 / %11 / %8 (80/12/8) | 2.80 - 0.47 (2.78 - 0.47) | 19.0 (19.0) | 25.4 (25.7) | 5.8 (5.8) | 7.9 / 10.1 (8.0 / 10.1) |
| good vs martilar | %46 / %27 / %27 (45/28/27) | 1.20 - 0.81 (1.19 - 0.81) | 20.2 (20.2) | 29.9 (30.0) | 8.7 (8.7) | 9.2 / 11.4 (9.2 / 11.4) |
| good vs ahtapotlar | %30 / %22 / %48 (28/22/50) | 0.97 - 1.53 (0.90 - 1.53) | 21.9 (21.8) | 31.6 (31.6) | 13.8 (13.9) | 9.7 / 12.2 (9.7 / 12.2) |
| random vs kirmizi | %1 / %7 / %93 (1/8/91) | 0.07 - 3.50 (0.07 - 3.35) | 21.8 (21.8) | 33.5 (33.5) | 5.7 (5.9) | 10.2 / 12.8 (10.2 / 12.9) |
| random vs martilar | %0 / %5 / %95 (0/4/95) | 0.03 - 3.99 (0.02 - 4.04) | 21.9 | 34.3 | 10.2 | 10.4 / 13.1 (10.4 / 12.9) |
| random vs ahtapotlar | %0 / %6 / %94 (0/6/94) | 0.01 - 3.72 (0.01 - 3.69) | 22.5 | 33.8 | 10.9 | 10.3 / 12.8 (10.3 / 12.8) |

Not: rastgele modelin sayıları iki nedenle oynadı: durum zamanlaması düzeltmesi ve simülatörün politika seed'inin artık ayrı türetilmesi (bölüm 4.3). İyi model zar kullanmadığı için onun sayılarını sadece düzeltme etkiler.

En sık skorlar:

- **good vs kirmizi:** 3-0 (%15), 2-0 (%13), 4-0 (%11), 1-0 (%8), 5-0 (%8), 2-1 (%7), 1-1 (%6), 3-1 (%5)
- **good vs martilar:** 1-0 (%14), 0-0 (%14), 0-1 (%12), 1-1 (%12), 2-0 (%11), 2-1 (%7), 0-2 (%5), 3-0 (%5)
- **good vs ahtapotlar:** 0-1 (%12), 1-1 (%10), 1-0 (%10), 0-2 (%9), 0-0 (%8), 1-2 (%6), 2-0 (%6), 2-1 (%5)

### Aşama kırılımı (good, 2000 maç)

Bu tablo, `MatchRunner` ile aynı döngüyü kuran ve depoya eklenmeyen geçici bir betikle ölçüldü. Her hücrede üç sayı var: maç başına düello sayısı × hücum edenin kazanma oranı / ortalama tur sayısı. Pasla kazanılan düello, kazanılmış sayılır. Görev 10'dan farklı olan hücreler kalın.

| Eşleşme | Kullanıcı hücumu: orta saha | son üçte bir | şut | Rakip hücumu: orta saha | son üçte bir | şut |
|---|---|---|---|---|---|---|
| kirmizi | 6.0 × %72 / 1.1 | 4.3 × %66 / 1.1 | **2.8 × %99 / 1.4** | 4.0 × %29 / 1.7 | 1.2 × %61 / 1.6 | 0.7 × %66 / 1.6 |
| martilar | 4.6 × %86 / 1.1 | 4.0 × %42 / 1.1 | **1.7 × %72 / 1.3** | 5.3 × %67 / 1.8 | 3.6 × %27 / 2.1 | **1.0** × %85 / 1.5 |
| ahtapotlar | **4.1** × %82 / 1.2 | 3.3 × **%52** / 1.2 | **1.7 × %56 / 1.5** | 5.9 × %73 / 1.6 | 4.3 × **%57** / 1.3 | 2.5 × **%62** / 1.8 |

### Şut dönüşümü (kullanıcı şutu → gol)

| Rakip | Görev 10 | Şimdi | Şut düellosu ort. tur (önce → şimdi) |
|---|---|---|---|
| kirmizi | %98 | **%99** | 1.52 → 1.42 |
| martilar | %71 | **%72** | 1.36 → 1.31 |
| ahtapotlar | %53 | **%56** | 1.54 → 1.49 |

Rakip şutları (kullanıcı kalecisine karşı) neredeyse aynı kaldı: %66 / %85 / %61'den %66 / %85 / %62'ye.

## 4. Durum zamanlaması düzeltmesi ve etkisi

### 4.1 Hata

Rakip, `EndTurn` içinde niyetini uyguluyordu, hemen ardından iki tarafın süreli durumları 1 azalıyordu. Verideki tüm süreli durum kartları 1 yığın uygular. Bu yüzden:

- Rakibin kullanıcıya verdiği **Yorgun** ve **Dengesiz** aynı anda 1'den 0'a iniyor, hiç etki etmiyordu.
- Rakip kalecinin **"sağa yatıyor"** niyeti kozmetikti. Kapatma kullanıcı şut çekmeden önce siliniyordu, "kalecinin yatmadığı köşeye çift hasar" (spec §5.4) hiç oluşmuyordu.

Görev 10'un bütün sayıları bu hatayla, yani rakip durum etkileri fiilen kapalıyken ölçüldü.

### 4.2 Düzeltme (karar R14)

1. **Yeni uygulanan yığınlar o tur sayılmaz.** Rakip hamlesinden önce var olan yığınlar tur sonunda 1 azalır. Rakip hamlesi sırasında eklenen yığınlar bir sonraki tur saymaya başlar. Sonuç: rakibin verdiği Yorgun kullanıcının bir sonraki turunda hasarı düşürür. Rakibin verdiği Dengesiz rakibin bir sonraki vuruşunu büyütür. Kullanıcının uyguladığı durumlar eskisi gibi çalışır.
2. **Kaleci niyeti açıklanınca yatar.** Rakip niyet kartındaki kendine Yakın/Uzak köşe kapatma, niyet açıklanır açıklanmaz uygulanır (`StatusApplied` olayı). Kartın geri kalanı (ör. Koruma) `EndTurn`'de çözülür, kapatma ikinci kez uygulanmaz. Gösterilen yatış, kullanıcının o tur şut çektiği andaki yatıştır.
3. **İyi model kaleciyi okur.** Şutçuyken, kalecinin kapatmadığı köşeye giden kartı önce oynar. Bu bir ölçüm aracı düzeltmesi: mekanik ancak böyle ölçülür.
4. **Yeni olaylar** (Unity için): `TurnStarted`, `BlockReset`, `StatusChanged` (yığın azalınca; 0 = silindi).

### 4.3 Simülatör seed'i

Simülatör eskiden motoru ve politikayı aynı seed ile başlatıyordu. Artık politika seed'i `s ^ 0x5bd1e995` olarak türetiliyor. Bu sadece rastgele modelin sayılarını değiştirir. Düzeltmenin etkisini ayırmak için rastgele model eski seed düzeniyle de ölçüldü: kirmizi'ye karşı %1.6 galibiyet, gol 0.09-3.50 (Görev 10: 0.07-3.35). Yani rakibin gol artışı (3.35 → 3.50) düzeltmeden geliyor: artık işleyen Yorgun/Dengesiz, bloklamayan rastgele modele karşı rakibin hücumunu güçlendiriyor.

### 4.4 Etki neden küçük?

Geçici betikle sayıldı (good, 2000 maç):

- **Kaleci yatış niyeti** kullanıcı şut turlarının kirmizi'de %49'unda, martilar'da %25'inde, ahtapotlar'da %11'inde var. Ahtapotlar kalecisi ayrıca turların %18'inde `ahtapot_eller` ile iki köşeyi birden kapatıyor, bu durumda açık köşe yok. Kalan niyetler hasar (`kafa_yumruk`, `cik_ve_kap`) ya da blok (`refleks`, `uzanis`).
- **Kirmizi'ye karşı şutlar zaten %98 gol oluyordu.** Çift hasar sadece şutu hızlandırdı (1.52 → 1.42 tur).
- **Rakibin kullanıcıya verdiği Yorgun/Dengesiz nadir.** Kullanıcı saha turlarının sadece %0.1-2.6'sı bu durumlardan biriyle başlıyor. Rakip niyeti çoğu zaman öldürücü ya da en yüksek hasarlı kart. `kolunu_uzat` ve `kayarak_mudahale` sadece rakip savunurken kullanılabiliyor.
- **Kanatların sadece `yakin_kose` kartı var** (mavi). Kaleci Yakın köşeye yatınca kanat oyuncusunun açık köşeye kartı olmuyor. Şutçunun desteğinde iki köşe kartı olması gerçek bir karar.

### 4.5 Yeniden ayar

Gerekmedi. Beş hedef düzeltmeden sonra da tutuyor ve seed 5000'de aynı bantta kalıyor. R13 kaldıraçlarından hiçbiri değiştirilmedi.

## 5. Değiştirilen kurallar ve sabitler (Görev 10)

| Yer | Sabit / kural | Önce | Sonra | Neden |
|---|---|---|---|---|
| `MatchRules` | `PowerScalePercent(tier)` | `100 + 10·(tier−1)` | `EnemyPowerBasePercent + TierPowerPercent·(tier−1)` = `260 + 0·(tier−1)` | Rakip turda tek kart oynuyor. Çarpan bunu telafi eder. Kademe farkı zaten statlar ve destelerden geliyor: yüksek stat hem canı hem hasarı büyütüyor. Eğim kaldıraç olarak duruyor (`TierPowerPercent`), şimdilik 0. |
| `DuelRules` | `KontrolMax` tabanı (`KontrolBase`) | 10 | **5** | Taşıyıcı kırılgan olur. Rakibin müdahale niyeti çoğu zaman öldürücüdür. Kullanıcı ya bloklar, ya pas verir, ya da bu tur bitirir. Düellolar kısalır. |
| `DuelRules` | `DengeMax` tabanı (`DengeBase`) | 10 | **14** | İyi model saha düellosunu tek turda bitiremesin, rakip niyeti önem kazansın. |
| `DuelRules` | `ShooterKontrolMax` tabanı (`ShooterKontrolBase`) | 8 | **10** | Ölçeklenmiş kaleci vuruşu şutçuyu hemen düşürmesin. |
| `DuelRules` | `KaleMax` tabanı (`KaleBase`) | 8 | **12** | Ölçeklenmiş rakip şutları tek vuruşta gol olmasın. |
| `DuelRules` | `EnemyPassThreshold` | 0.6 | **0.65** | 0.6'da Pas 5-6 olan rakipler (ahtapotlar kanatları ve orta sahası) her düelloya 1. turda %60-66'lık pasla başlıyordu. Savunmanın hiçbir etkisi kalmıyordu (saf zar). 0.65'te kanatlar önce oynuyor, pası hasar verdikten sonra deniyor. 0.7 3. kademe maç süresini 11 dk'nın üstüne çıkardı. |
| `DuelRules` + `EffectResolver` | **Yeni kural:** köşe kartı vs kaleci olmayan hedef (`CornerVsFieldMult`) | tam hasar | **×0.5** | `golcunun_icgudusu` (12 + Şut) gibi şut kartları saha düellosunda en iyi hasar kartlarıydı. Artık kanat ve forvet destesinde saha kartı ile şut kartı ayrı bir karar. Kural, kalecinin köşesiz şuta ×0.5 uygulamasının aynası. |

Formül değerleri adlandırılmış sabit: `KontrolBase`, `DengeBase`, `ShooterKontrolBase`, `KaleBase`, `EnemyPowerBasePercent`.

Son düzeltme turunda bu tablodaki hiçbir değer değişmedi.

## 6. Değiştirilen veri (`data/teams.json`, Görev 10)

`cards.json` içinde hiçbir kart değeri değişmedi. Son düzeltme turunda veri değişmedi. Yeni sıkı veri doğrulaması (zorunlu alanlar, negatif stat/maliyet, yinelenen oyuncu kimliği vb.) gerçek veride hata bulmadı.

| Oyuncu | Önce | Sonra | Neden |
|---|---|---|---|
| `mavi_cm` | `pas_yolunu_kapat` | `kayarak_mudahale` | Kullanıcının orta saha ve kanat savunucularında hiç hasar kartı yoktu. Rakip hücumunda 3 tur bloklayıp süre dolmasını beklemek zorundaydı. Artık top kapma (taşıyıcıyı düşürme) bir seçenek. Pas niyetine karşı tek cevap da bu, çünkü niyetteki pas şansı sabitlenmiş durumda. İyi model vs kirmizi süresi 9.8'den 8.2 dk'ya indi (500 maç). |
| `mavi_lw`, `mavi_rw` | `kolunu_uzat` | `kayarak_mudahale` | Aynı neden. |
| `kirmizi_lw`, `kirmizi_rw` | `ortala` | `ici_kes` | Tarzı "Kanat ağırlıklı": kanattan içeri kat etme tehdidi. Bu olmadan good %94 kazanıyor, kirmizi neredeyse hiç gol atamıyor. |
| `kirmizi_lw`, `kirmizi_rw` | `topu_sakla` | `kayarak_mudahale` | Tarzı "sert savunma": orta sahada müdahale tehdidi. Bu olmadan good %91 kazanıyor. |
| `martilar_lw`, `martilar_rw` | `ici_kes` | `ortala` | Tarzı "Hava topu ve hızlı kanatlar": orta ve hız. `ici_kes` ile 2. kademe 3. kademeden zor çıkıyordu (good %27). Nedeni 8. bölümde (1. madde). |
| `ahtapotlar_lw`, `ahtapotlar_rw` | `ici_kes` | `calim` | Tarzı "Kale önünde duvar, ölümcül santrafor": tehdit kale ve forvetten gelir. `ici_kes` ile good %19 kazanıyordu (hedef %25-50). |

Rol kuralları korundu: her saha oyuncusunda en az 2 hücum/ikisi de ve 2 savunma/ikisi de kartı var, kanat ve forvetlerde köşe kartı var. Deste boyları değişmedi (6-8).

## 7. Deneme günlüğü ve sınırdaki hedefler

### 7.1 Görev 10 deneme günlüğü (kısa)

Ara ölçümler 400-500 maçla geçici betikle yapıldı. Son sayılar 2000 maçla resmi simülatörden alındı. Bu denemeler durum zamanlaması hatası varken yapıldı.

1. **Sadece köşe kuralı:** etkisi küçük (good %97-100, random %35).
2. **Çarpan `200 + 30·(t−1)`, canlar K14/D18/SK10/Kale14:** random %7, good 99/42/30. Kademe sıçraması çok büyük, süre 11.2 dk.
3. **Düz çarpan 250-300:** kirmizi %91'den %67'ye indi. Ama 2. ve 3. kademe %16-31'e düştü ve süre 10.8-11 dk oldu.
4. **Pas eşiği taraması:** 0.7'de ahtapotlar oynamaya başlıyor, en zor takım oluyor ama süresi 11-12.6 dk. 0.5'te düellolar kısa ama sonuç çoğunlukla zar.
5. **K/D/Kale/eşik ızgarası:** iyi model vs kirmizi süresi 10-11.5 dk'da takıldı. Neden: kullanıcı zincirleri uzun, kullanıcının savunduğu saha düellosu 2.1-2.7 tur sürüyor.
6. **Kirmizi orta sahasına müdahale + K8/D22:** kirmizi %60-83, süre 9.3-10.2. Bu sefer 2. kademe en zor oldu (`sert_mudahale`/`ust_kose` sıçramaları).
7. **Kirmizi stat artışı:** neredeyse etkisiz, geri alındı. Kirmizi kanatlarına `ici_kes`: %90'dan %78'e.
8. **K=5:** düellolar kısaldı. Mavi orta saha ve kanatlarına müdahale kartı: kirmizi maçı 9.8'den 8.2 dk'ya.
9. **Eşik 0.65 seçildi.**
10. **Ayıklama:** `kirmizi_st` için `plase`, `martilar` kanatlarında `ust_kose`→`uzak_kose`, `martilar` beklerinde `sert_mudahale`→`topu_sakla`, kirmizi stat artışları ve `kirmizi_cm` müdahalesi gereksiz çıktı, geri alındı.
11. **Kart düzeyinde düzeltme denendi:** `ici_kes` önce hasar, sonra Dengesiz. Sonuç: good vs kirmizi %93, 2000 maçta kirmizi good'a maç başı 0.01 gol attı (hiç kaybetme yok). Geri alındı, deste düzeyinde değişiklik tercih edildi.

### 7.2 Son düzeltme turu

1. Düzeltmeden sonra resmi simülatörle ölçüldü (2000 maç, seed 1 ve 5000). Beş hedef tuttu, ayar yapılmadı.

### 7.3 Sınırdaki hedefler

Ulaşılamayan hedef yok. İki hedef sınıra yakın:

- **3. kademe süresi 9.7 dk** (üst sınır 10).
- **3. kademe galibiyeti %30** (alt sınır %25). Düzeltmeyle %28'den biraz uzaklaştı.

Bunlar kademe 2 değişikliklerine karşı hassas. Ayrıca %90'lık dilim süreleri 10.1-12.2 dk. Yani maçların kabaca %10-40'ı "10 dakikayı aşan maç" anti-hedefine giriyor. Ortalama hedefte, kuyruk değil.

## 8. Kademe 2 için gözlemler

1. **Tek kart × 2.6 sıçramalar yaratıyor.** Rakip kartın maliyetini ödemiyor. Bu yüzden pahalı ve kendini güçlendiren kartlar rakip elinde orantısız güçlü:
   - `golcunun_icgudusu` (3 enerji, 12 + Şut, üst köşe) ≈ 46 hasar.
   - `ust_kose` ≈ 31-33 hasar.
   - Orijinal `ici_kes` (önce Dengesiz, sonra hasar ×1.5) ≈ 39-41 hasar.

   Bunlar kullanıcının bir turda kurabileceği en yüksek bloğu (≈ 12-17) aşıyor. Çekildiklerinde neredeyse kesin gol ya da düello kaybı oluyor (şans). Öneri: rakip bir enerji bütçesine kadar kart oynasın, ya da spec §8.4'teki desen tablosu kullanılsın. Ardından çarpan 100'e doğru indirilsin.
2. **Durum sayacı zamanlaması düzeltildi** (bölüm 4). Rakip durum etkileri ve kaleci yatışı artık işliyor. Etkileri bu kadrolarla küçük. Kademe 2'de rakip destelerine daha çok durum kartı ve kaleci yatış kartı eklenirse yeniden ölçülmeli.
3. **Rakip pas şansı niyet anında sabitleniyor.** Kullanıcının tek cevabı taşıyıcıyı aynı turda düşürmek. Pas 6 olan orta sahalar (martilar, ahtapotlar) 0.66 ≥ 0.65 ile her zaman 1. turda pas veriyor (saf zar). Pas şansını kullanıcı hamlelerine bağlamak ya da eşiği stata göre ayarlamak düşünülmeli.
4. **Tam sayı uçurumları.** Örnek: çarpan 250'de kirmizi kanadının müdahalesi 6 × 2.5 = 15. Bu, `mavi_lw` Kontrolü (11) + `topu_sakla` (4) toplamına tam eşit. Sonuç: good vs kirmizi %88'den %75'e düşüyor. Değerler 255-265 arası bu uçurumdan uzak seçildi. Kademe 2'de stat ve kart değişiklikleri öldürücülük eşiklerini kolayca çevirebilir. Her değişiklikten sonra ölçülmeli.
5. **Kontrol çok düşük** (5 + Güç + Çalım ≈ 10-15). Rakibin hemen her hasar niyeti taşıyıcı için öldürücü. Kural okunaklı ("müdahale görürsen blokla, pas ver ya da bitir") ama sert. Arayüz öldürücü niyeti çok açık göstermeli.
6. **Süre.** Kullanıcının savunduğu saha düellosu hâlâ 1.6-2.1 tur. Savunanın süre dolunca kazanması ve iyi modelin sadece öldürücü hasara karşı bloklaması bunu uzatıyor. Ortalama hedefte ama %90'lık dilim değil. Gerçek süre Kademe 3 oyun testinde ölçülmeli.
7. **Rastgele model ezici biçimde kaybediyor** (%93 mağlubiyet, maç başı 0.07 gol). İyi model ile rastgele arasındaki fark çok büyük. Spec §6 "ortalama model" ister. Sadece öldürücü hasara karşı bloklayan iyi model ile rastgele arasında bir "açgözlü" model ölçüme eklenmeli.
8. **Kirmizi kalecisi tehdit değil.** Kullanıcı şutlarının %99'u gol oluyor (düzeltmeden önce %98). Kaleci yatışı artık çift hasar açtığı için şutlar daha da kısa. 1. kademenin zorluğu orta sahadaki müdahalelerden ve kanat patlamalarından geliyor.
9. **0 maliyetli kartlar** (`tempo_belirle`, `soguk_kanlilik`) rakip için ölü kart. Enerji rakip için anlamsız, puanları 0. Rakip başka seçeneği yoksa bunları "bekle" niyeti olarak seçiyor. Ahtapotlar orta sahasında ikisi birden var. Kullanıcı için bedava değer sağlıyorlar ve başlangıç destesinde yoklar. Mağaza gelince izlenmeli.
10. **Köşe kuralının yan etkisi.** Kanatların `yakin_kose` kartı sahada yarım hasar veriyor. Kanat destesinde saha kartı ile şut kartı oranı artık gerçek bir deste kurma kararı. Ödül havuzu bu kararı besleyecek kartlar içermeli.
11. **Kaleci yatışı ve köşe kartları.** Mavi kanatlarında sadece Yakın köşe kartı var. Kaleci Yakın'a yatınca kanadın açık köşeye kartı olmuyor. Ödül havuzunda Uzak köşe kartı ve kalecinin yatışını değiştiren kartlar bu kararı derinleştirir.
