# Izgara maç akışı: tasarım

Tarih: 2026-11-23. Durum: taslak, onay bekliyor. Uygulayıcı kontrolünden geçti; çıkan sorular bölüm 6.4.1, 6.8, 6.9 ve 6.3'te cevaplandı.

## 1. Amaç ve neden

Şu anki maç motoru atağı soyut üç basamakta yürütüyor: orta saha → son üçte bir → şut. Konum yok. Pas, hedef kim olursa olsun atağı bir basamak ilerletiyor. Topu savunmacına geri versen bile atak ileri gidiyor, bu da anlamsız. Oyuncu da pasın neden tuttuğunu ya da tutmadığını okuyamıyor.

Kullanıcının istediği şey bir **satranç hissi**: sahayı yukarıdan görmek, rakiplerin konumundan hangi pasın tutacağını tahmin etmek, risk alıp uzun top denemek, boş oyuncuyla ilerlemek. Her hamlenin ardından herkes yer değiştirir, düello da gerçekten bir rakiple karşılaşınca açılır.

Bu belge düellonun **sonrasını** kapsar: düello kazanılınca ne olur, top nasıl ilerler, düello nerede ve kime karşı açılır.

## 2. Hedefler, hedef olmayanlar, anti-hedefler

**Hedefler**
- 12 × 8 kutuluk ızgara üzerinde iki takımın 7'şer oyuncusu.
- Topa sahip oyuncu için iki seçenek: **Pas at** ve **İlerle**. Bir de hamle harcamayan **Saha** butonu.
- Her hamlenin ardından bütün oyuncular hareket eder.
- Rakip, topa sahip oyuncunun bitişik kutusuna gelince düello açılır. Kazanırsan seçim ekranı tekrar gelir. Kaybedersen top rakibe geçer.
- Pas ihtimali ekranda yazmaz. Konumlardan okunur. Arkada belirlenimci bir formülle hesaplanır.
- Rakip topa sahipken aynı kurallarla hücum eder.

**Hedef olmayanlar (bu sürümde yok)**
- Şut düellosu, kaleci, gol akışı. Şut bölgesine giriş şimdilik geçici bir kuralla çözülür (bölüm 6.8).
- Taktik penceresi, oyun anlayışları, ödüller, Hype, Alev, Kondisyon. Bunlar [docs/ideas.md](../../ideas.md) dosyasında bekliyor.
- Eski maç motorunun silinmesi.

**Anti-hedefler (çalışsa bile başarısızlık sayılır)**
- Pasın sonucu şans gibi hissettiriyorsa. Oyuncu çizgideki rakipleri görüp riski tahmin edebilmeli.
- Aynı iki oyuncu arasında sonsuz düello döngüsü oluşuyorsa.
- Bir atak oyuncu kararı olmadan kendiliğinden bitiyorsa.

## 3. Kısıtlar
- Çekirdek kuralları geçerli: `netstandard2.1`, C# 9, `UnityEngine` yok. Tüm rastgelelik `Rng` üzerinden gelir. Komut girer, `GameEvent` listesi çıkar.
- Sayılar `MatchRules` içinde ya da `data/*.json` içinde durur.
- Düello motoru (`Duel`) olduğu gibi yeniden kullanılır.

## 4. Değerlendirilen yaklaşımlar

| Yaklaşım | Artısı | Eksisi | Karar |
|---|---|---|---|
| **Bölge ızgarası 12 × 8** | Konum net okunur, belirlenimci, test edilebilir, yukarıdan çizmesi kolay | Hareket kutu kutu, tam gerçekçi değil | **Seçildi (kullanıcı)** |
| Serbest koordinatlar | Daha gerçekçi görünür | Oyuncu riski tahmin edemez, dengesi zor | Elendi |
| Üç basamaklı modeli koruyup konumları sadece görsel yapmak | En ucuzu | Konum hiçbir şeyi değiştirmez, kullanıcının istediğinin tersi | Elendi |

Izgara boyutu: kullanıcı 6 × 4'ü çok küçük buldu. 12 × 8 önerildi ve kabul edildi.

## 5. Kullanıcının verdiği kararlar
- Topa sahip oyuncu için seçenekler: Pas at ve İlerle. Taktik seçeneği şimdilik yok.
- Her hamle sonucunda bütün oyuncular hareket eder.
- Pas yüzdesi gösterilmez. Matematik arkada kalır. Oyuncu konumdan ve maçın durumundan tahmin eder.
- Düşük ihtimalli uzun top denenebilir.
- "Saha" butonu sahanın tamamını yukarıdan açılı gösterir, destedeki kartları görüntüleme butonu gibi çalışır.
- İlerlerken bir rakibin gelip düello açması aranızdaki mesafeye bağlıdır.
- Düello kazanılınca seçenek ekranı açılır: Pas at, İlerle, Saha.
- Hücumda 3 turda rakibi yenemezsen topu kaptırırsın (mevcut düello kuralı, korunuyor).
- Şut, kaleci ve gol akışı bu sistemden sonra yapılacak.

## 6. Tasarım

### 6.1 Izgara
- 12 sütun (x = 0..11) ve 8 şerit (y = 0..7).
- Kullanıcı her zaman x = 11 yönüne hücum eder. Rakip x = 0 yönüne.
- Kutu başına en fazla bir oyuncu.
- **Bitişik:** 8 komşu kutu, çaprazlar dahil (Chebyshev mesafesi 1).
- **Şut bölgesi:** kullanıcı için x ≥ 10 ve 2 ≤ y ≤ 5. Rakip için simetriği (x ≤ 1).

### 6.2 Başlangıç dizilişi
Her mevkinin bir **ev kutusu** var. Kullanıcı tarafı (rakip için x ayna):

| Mevki | Ev kutusu (x, y) |
|---|---|
| GK | (0, 3) |
| LB | (2, 1) |
| RB | (2, 6) |
| CM | (4, 3) |
| LW | (5, 0) |
| RW | (5, 7) |
| ST | (6, 4) |

Maç ve top kaybından sonraki her yeni atak, topa sahip olan takımın CM'sinde başlar. Herkes ev kutusundan başlar. Maç başında topu kimin alacağı `Rng` ile belirlenir.

### 6.3 Hamle döngüsü
Maç, sırayla oynanan **hamlelerden** oluşur. Bir hamle:

1. **Karar:** topa sahip oyuncu bir eylem seçer. Kullanıcıysa kullanıcı seçer, rakipse rakip yapay zekâsı seçer.
   - **İlerle(hedef kutu):** taşıyıcı, Hız statına göre 1 ya da 2 kutu uzaklıktaki boş bir kutuya geçer. Hız ≥ 4 ise 2 kutu, değilse 1 kutu. 2 kutuluk ilerlemede arada en az bir boş ara kutu olmalı, yani taşıyıcı bir rakibin üstünden atlayamaz. Rakibin bitişiğinden geçmek serbesttir, düello kontrolü sadece varış kutusunda yapılır.
   - **Pas(hedef oyuncu):** top bir takım arkadaşına atılır (bölüm 6.5).
2. **Hareket:** topa sahip olan dışındaki bütün oyuncular hareket eder (bölüm 6.6). Hareket sırası belirlenimcidir: önce savunan takım, sonra hücum eden takım, her takım içinde mevki sırasına göre (GK, LB, RB, CM, LW, RW, ST).
3. **Düello kontrolü:** taşıyıcının bitişiğinde, sersemlemiş olmayan bir rakip varsa düello açılır. Birden fazla rakip varsa en yakın olan seçilir. Eşitlikte mevki sırasına bakılır.
4. **Düello yoksa:** sıra yine topa sahip olana geçer, döngü 1. adımdan devam eder.

### 6.4 Düello sonrası
- **Hücum kazanırsa:** taşıyıcı topu korur. Yenilen savunmacı **2 hamle sersemler**: o süre boyunca hareket etmez ve düello açamaz. Bu kural sonsuz düello döngüsünü önler. Ardından seçenek ekranı gelir.
- **Savunma kazanırsa:** top savunmacıya geçer. Atak yön değiştirir. Yenilen eski taşıyıcı 2 hamle sersemler. Yeni taşıyıcı hemen bir eylem seçer.
- **Düello içinde pas yok.** Izgara modunda düello ekranındaki Pas butonu kapalıdır. Pas sahada, seçenek ekranından atılır. Sebep: hedef seçimi ızgara görmeden anlamsız.

### 6.4.1 Düellonun kurulumu
- Her iki taraf da **kendi oyuncusunun destesi ve statlarıyla** girer: taşıyıcı ve düelloyu açan savunmacı. Bu, eski maç motorunun yaptığıyla aynı.
- `DuelSetup` alanları: `Kind = Field`, `UserIsAttacker` = topa kullanıcı mı sahip, `EnemyPowerScalePercent` = rakip takımın tier'ına göre (eski motordaki gibi). Pas hedefi listesi boş bırakılır, böylece düello içinde pas kapanır.
- Momentum, Alev, Kondisyon ve taktik bonusları bu sürümde verilmez.
- 3 tur kuralı her iki yönde de geçerli: kullanıcı savunurken rakip hücumcu 3 turda kullanıcının savunmacısını yenemezse topu kaybeder.
- `Duel` değişmez. Pas hedefi listesi boş olunca düellodaki Pas zaten kapalı. *(Uygulayıcı kararı: bunu doğrulayan bir test eklenir.)*

### 6.5 Pas
- Atanın Pas statı, mesafe, pas çizgisi ve alıcının çevresi bir **başarı ihtimali** verir. İhtimal ekranda gösterilmez.
- **Pas çizgisi:** atanın kutusundan alıcının kutusuna Bresenham çizgisi. Çizginin uç noktaları hariç her kutu için o kutuda ya da bitişiğinde bir rakip varsa, o rakip **kesme adayıdır**.
- **Formül (başlangıç değerleri, `MatchRules` içinde):**
  - temel = 0.95 − 0.04 × mesafe (Chebyshev)
  - \+ 0.03 × atanın Pas statı
  - − 0.20 × her kesme adayı
  - − 0.10 × alıcının bitişiğindeki her rakip
  - sonuç 0.05 ile 0.95 arasına sıkıştırılır
- **Başarılı pas:** alıcı taşıyıcı olur. Ardından hareket ve düello kontrolü normal şekilde yapılır.
- **Başarısız pas:** top, kesme adaylarından alıcıya en yakın olana geçer. Aday yoksa alıcının bitişiğindeki en yakın rakibe geçer. O da yoksa alıcıya en yakın rakibe geçer. Atak yön değiştirir.
- **Okunabilirlik:** pas seçerken her alıcıya çizilen çizgi, kesme adayı varsa kesik gösterilir. Yüzde yazmaz.

### 6.6 Hareket kuralları (yapay zekâ)
Her oyuncu her hamlede en fazla 1 kutu hareket eder. Taşıyıcının 2 kutusu bu kurala dahil değil, o İlerle eyleminden gelir.

**Savunan takım:**
- **Baskıcı:** taşıyıcıya en yakın, sersemlemiş olmayan saha oyuncusu (kaleci hariç) taşıyıcıya doğru 1 kutu gider. Her hamlede tek baskıcı olur. Eşitlikte mevki sırasına bakılır.
- **Diğerleri:** ev kutularının, taşıyıcının bulunduğu sütuna göre kaydırılmış haline doğru 1 kutu gider. Savunma hattı topa göre geri ya da ileri kayar. Kaydırma şöyle: hedef x = ev x + (taşıyıcı x − 5.5) × 0.5, yuvarlanır ve sınırlar içinde tutulur.

**Hücum eden takım (topsuz oyuncular):**
- Ev kutularının topa göre ileri kaydırılmış haline doğru 1 kutu gider. Formül savunmadakiyle aynı, ama yön hücum yönü.
- Aynı mesafede birden çok kutu varsa, bitişiğinde en az rakip olan kutu seçilir. Eşitlik bozma sırası sabittir: önce x, sonra y.

**Kaleciler:** kendi ceza alanında, topun y koordinatını izleyerek kalır. Düello açmaz, pas alabilir.

### 6.7 Rakip taşıyıcının kararı
Rakip topa sahipken her hamlede şunları puanlar:
- her alıcı için bir pas puanı: başarı ihtimali × alıcının kaleye yakınlığı
- her ulaşılabilir kutu için bir ilerleme puanı: kaleye yakınlık − bitişikteki kullanıcı oyuncusu sayısı

En yüksek puanlı eylemi seçer. Eşitlikte sabit bir sıra kullanılır.

*(Not, 2026-11-23: uygulamada ilerleme puanı kaleye yakınlık − 0,25 × bitişikteki rakip oyuncu sayısıdır; bu katsayı planda onaylandı.)*

### 6.8 Şut bölgesi (geçici kural)
Taşıyıcı şut bölgesine girdiğinde atak biter. Bölgede duran bir takım arkadaşına başarılı pas da "girmek" sayılır. Bölgeye giriş ancak hamlenin düello kontrolünden sonra sayılır, yani bölgeye giren taşıyıcının bitişiğinde rakip varsa önce düello oynanır *(benim kararım: savunmaya bölgede son bir şans vermek için)*. Kazanılırsa, bir `ShotChance` olayı üretilir ve **geçici olarak gol sayılır**. Ardından yeni atak, gol yiyen takımın CM'sinde başlar. Bu kural, şut düellosu tasarlandığında değişecek. Amaç şimdilik maçların bir skoru olması ve simülatörün ölçüm yapabilmesi.

### 6.9 Hamle ve süre tanımı
- **Bir hamle** = taşıyıcının bir eylemi (İlerle ya da Pas) + herkesin hareketi + düello kontrolü + varsa düello. Düello, onu tetikleyen hamlenin parçasıdır.
- Düellodan sonra kazananın seçtiği eylem yeni bir hamledir.
- **Sersemlik sayacı:** sersemleyen oyuncu, sersemlediği hamleden sonraki 2 hamlenin hareket adımında hareket etmez ve o 2 hamlenin düello kontrolünde düello açamaz. Sayaç her hamlenin sonunda 1 azalır.
- **Top el değiştirince** (savunma düelloyu kazanınca ya da pas kesilince) yeni taşıyıcı için hemen bir düello kontrolü yapılır. Bitişiğinde sersemlemiş olmayan bir rakip varsa düello açılır. Eski taşıyıcı zaten sersemlemiş olduğu için aynı ikili arasında hemen tekrar düello olmaz.

### 6.9.1 Maç süresi
- Maç sabit sayıda hamleden oluşur. Başlangıç değeri 90 hamle (`MatchRules.GridMatchMoves`).
- Düello içindeki turlar hamle sayılmaz.
- Devre arası bu sürümde yok.
- *(Not, 2026-11-23: Görev 9'da `GridMatchMoves` 90'dan 25'e ayarlandı, bkz. [ızgara maç ölçümü](../../balance/2026-11-23-grid-match-baseline.md). Bu açık bir tasarım sorunu: hamlelerin yaklaşık %90'ında düello açılıyor, bu yüzden hamle sayısı maç süresini iyi temsil etmiyor.)*

### 6.10 Çekirdek yapısı
- Yeni sınıf: `WeirdLand.Core.Match.GridMatch`. Eski `MatchEngine` ve simülatördeki eski mod dokunulmadan kalır.
- **Komutlar:** `Advance(x, y)`, `Pass(playerId)`, düello komutları (mevcut `Duel` üzerinden), `ResolveOpponentMove()`.
- **Olaylar (yeni):** `PlayerMoved`, `BallPassed`, `PassIntercepted`, `DuelTriggered`, `PlayerStunned`, `PossessionChanged`, `ShotChance`, `MatchMoveAdvanced`.
- **Sorgular:** her oyuncunun konumu, sersemlik süresi, taşıyıcı, ulaşılabilir kutular, pas çizgisindeki kesme adayları. Sonuncusu sadece görsel ipucu için, ihtimal değil.
- **Simülatör:** `--mode grid` seçeneği. Kullanıcı tarafı için basit bir politika (rakip yapay zekâsıyla aynı puanlama) ve rastgele politika.

### 6.11 Unity
- Yeni sahne: `GridMatch`. Yukarıdan açılı tek ekranda bütün saha görünür. Yaklaşma ya da kaydırma yok.
- Oyuncular yer tutucu çöp adamlarla ya da dairelerle gösterilir. Sersemleyen oyuncu soluk görünür.
- **Seçenek ekranı:** Pas at, İlerle, Saha.
  - **İlerle:** ulaşılabilir kutular vurgulanır, tıklanınca hamle yapılır.
  - **Pas at:** takım arkadaşlarına çizgiler çizilir. Kesme adayı olan çizgiler kesik görünür. Alıcıya tıklanınca hamle yapılır.
  - **Saha:** seçenek ekranını gizleyip sahayı tam gösterir. Tekrar basınca geri döner.
- Düello açılınca mevcut düello ekranı açılır, düello bitince sahaya dönülür.
- Rakip hamleleri kısa animasyonlarla sırayla oynatılır. Kullanıcı rakip atağında karar vermez, sadece düellolara girer.

## 7. Uygulayıcıya bırakılanlar
- Sınıf ve dosya bölümlemesi, olayların tam alan listesi.
- Bresenham ve yol bulmanın gerçeklenmesi. Engelli kutu varsa en yakın boş kutuya gitmek yeterli.
- Unity'deki animasyon süreleri, renkler, düzen.
- Formül katsayıları başlangıç değeridir. Simülatörle ayarlanır, sonuçlar `docs/balance/` altına yazılır.

## 8. Başarı ölçütleri
- Belirlenimcilik: aynı seed ve aynı komutlar aynı olay listesini verir. Altın parmak izi testi eklenir.
- Simülatörde iyi politika ile rastgele politika arasında belirgin fark olur.
- Maç başına ortalama düello sayısı 10–25 arasında kalır.
- Aynı iki oyuncu arasında üst üste 3'ten fazla düello olmaz. Bu, testle doğrulanır.
- Kullanıcı Unity'de bir maçı baştan sona oynayabilir.
