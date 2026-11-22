# WeirdLand — Tasarım Belgesi

Tarih: 2026-11-22 · Durum: gözden geçirildi, kullanıcı onayı bekliyor

## 1. Amaç ve neden

WeirdLand, futbol temalı, sıra tabanlı bir **deckbuilder roguelike** (Slay the Spire'ın futbol versiyonu). Maç sahada otomatik akar. İki oyuncu karşı karşıya geldiğinde oyun bir **düello ekranına** geçer ve o düello, o anda düelloya giren oyuncunun kartlarıyla oynanan kısa bir kart savaşıdır.

HTML prototipinde (`prototype/index.html`) oyun mantığı denendi. Şu sonuçlar çıktı:
- Her anı tek bir zar atışına bağlamak kararları anlamsız hissettiriyor.
- Rakibin hamlesini tamamen gizlemek oyunu taş-kağıt-makasa çeviriyor.
- Rakibin niyetini göstermek ancak oyuncunun seçenekleri kısıtlıysa (el ve enerji) düşündürücü oluyor.

Bu belge o prototipin yerine geçecek gerçek oyunu tanımlar. Prototip sadece referanstır.

Kullanıcıyı çeken şey:
- elindeki kartlarla kombo tutturmak,
- içinde bulunduğu durumdan çıkan aksiyonlarla düelloyu kazanmak,
- ilerideki karşılaşmaları düşünerek takımını ve destelerini geliştirmek.

## 2. Hedefler, hedef olmayanlar, anti-hedefler

**Hedefler**
- Bir maç 5-10 dakika sürer. Bir kupa koşusu 1-1,5 saatte biter.
- Her düelloda elin ve enerjin kısıtlı. Rakibin niyeti görünür ve onu elindekilerle çözmeye çalışırsın.
- Maçlar arası gelişim gerçek strateji kararları içerir: kimi geliştireyim, hangi kartı alayım, hangisini atayım.
- Maçın gidişatı sabit değil. Bazen rakibi ezersin, bazen ezilirsin (Üstünlük sistemi).
- Kurallar Unity'den bağımsız test edilebilir ve binlerce maçla simüle edilebilir.

**Hedef olmayanlar (şimdilik)**
- Çok oyunculu mod, çevrimiçi özellikler.
- Gerçek zamanlı futbol simülasyonu. Saha katmanı soyut, sadece kritik anlar oynanır.
- Mobil ve konsol. İlk hedef Windows PC.
- Türkçe dışında dil. Metinler yine de merkezi bir tabloda tutulur ki sonra eklenebilsin.
- Ücretli eklenti.

**Anti-hedefler (çalışsa bile başarısızlık sayılır)**
- Rastgele tıklayarak düzenli kazanılabilen bir oyun.
- "En yüksek yüzdeyi seç" ile çözülen kararlar.
- Okunamayan, saf tahmine dayalı rakip davranışı.
- 10 dakikayı aşan maçlar.
- Kuralları sadece oynayarak test edilebilen bir kod yapısı.

## 3. Kısıtlar

- **Motor:** Unity 6000.3.9f1 (Unity 6.3, makinede kurulu). Dil C#. Makinede .NET 10 SDK var.
- **Ekran üzerinden çalışma:** Claude oyun ekranını göremez. Oynayan ve ekran görüntüsü paylaşan kullanıcıdır. Bu yüzden:
  - Kurallar komut satırından test edilebilir olmalı.
  - Unity varlıkları (prefab, sahne) mümkün olduğunca editör script'leriyle üretilmeli.
- **Görsel:** başlangıçta yer tutucu çöp adamlar ve basit şekiller kullanılır, gerçek çizimler sonra takılır. Görsel varlıklar tutucu-bağımsız bağlanmalı.
- **Eklentiler:** ücretsiz olanlar kullanılır (bölüm 8.6).

## 4. Yaklaşımlar

| Yaklaşım | Kazancı | Bedeli | Karar |
|---|---|---|---|
| Düz HTML/CSS/JS | Prototipe yakın, kurulum yok | Büyüdükçe dağılır, "gerçek proje" değil | Elendi |
| Phaser (canvas) | Hareketli sahneler kolay | Kart ve menü ağırlıklı arayüz zahmetli | Elendi |
| TypeScript + Vite, HTML arayüz | Test ve simülasyon kolay, arayüz kolay | Kullanıcı gerçek bir oyun motoru istiyor | Elendi |
| Godot 4 | Metin tabanlı sahneler, hafif | Kurulu değil, kart oyunu ekosistemi daha dar | Elendi |
| **Unity 6 + saf C# çekirdek** | Gerçek oyun altyapısı, geniş ekosistem, kurulu. Çekirdek Unity'siz test edilir. | Editör tarafı Claude için kör, prefab ve sahneler kodla üretilmeli | **Seçildi (kullanıcı)** |

## 5. Oyun yapısı

### 5.1 Kupa koşusu (roguelike + küçük kalıcı ilerleme)

- **Format:** 6 maç: 3 grup maçı, çeyrek final, yarı final, final.
  - *Karar:* grup aşamasından çıkmak için 3 maçtan en az 4 puan gerekir (galibiyet 3, beraberlik 1).
  - *Karar:* eleme maçlarında beraberlik olursa uzatma yok, doğrudan penaltı serisi oynanır. Penaltı serisi kısa kaleci düellolarından oluşur (bölüm 5.4).
- **Elenirsen koşu biter.** Takım, desteler, seviyeler, para ve meyveler sıfırlanır.
- **Kalıcı kazanımlar (koşular arası).** Her koşu, ulaşılan tura göre kalıcı bir puan verir. Bu puanla yeni kartlar ve yeni meyveler mağaza havuzuna açılır. Takımı doğrudan güçlendiren kalıcı stat artışı **yok**, çünkü roguelike gerilimini bozar.
  - *Karar:* ileride açılabilir başlangıç takımları (farklı başlangıç desteleri) eklenebilir. Bu ilk sürümde zorunlu değil.
- **Sıradaki rakip önceden görülür.** Rakibin oyun tarzı (ör. "Kanat ağırlıklı, sert savunma") ve öne çıkan oyuncuları gösterilir. Oyuncu maç öncesi hazırlığını buna göre yapar.

### 5.2 Kadro

- **7 kişilik takım** (prototipteki gibi): Kaleci, Sol Savunma, Sağ Savunma, Orta Saha, Sol Kanat, Sağ Kanat, Santrafor.
  - *Karar:* 7 kişi, desteleri yönetilebilir tutar.
- **Her oyuncunun statları:** Hız, Güç, Pas, Çalım, Şut, Savunma, Hava. Kalecide bunlara ek olarak Yakın ve Uzak.
  - Statlar kart etkilerini ölçekler: "Çalım: 4 + Çalım statı hasar" gibi.
- **Her oyuncunun tek bir destesi var**, 6-10 kart. Her kart bir **rol etiketi** taşır: Hücum, Savunma ya da İkisi de.
  - Düelloda sadece o anki role uyan kartlar karıştırılır. Top taşıyan, Hücum ve İkisi de kartlarını kullanır. Savunan, Savunma ve İkisi de kartlarını kullanır.
  - Mevki, rol oranını belirler: kanat ve santraforda hücum ağırlıklı, savunmacılarda savunma ağırlıklı. Mevkinin zayıf olduğu rolde de birkaç kart olur.
  - Başlangıç destesi her mevkiye iki rolü de kapsayacak şekilde verilir. Bir rolde hiç kart yoksa oyuncu o rolde sadece temel hamlelerle oynar (bölüm 5.4).
- **Her oyuncu en fazla 1 meyve taşır** (*karar*: başlangıçta 1, ileride ikinci yuva açılabilir).

### 5.3 Maç akışı

- **Saha görünümü:** küçük bir saha haritası. Top otomatik dolaşır, dakika akar.
- **Kritik anlar:** maçta 8-12 kritik an olur. Bu üst sınır süreyi tutturmak için.
  - Her kritik an bir atak zinciri başlatır: atak yapan takımın taşıyıcısı ile karşısındaki savunmacı arasında bir düello.
- **Üstünlük göstergesi:** her kritik anın hangi takıma düşeceğini belirler. -100 ile +100 arasında sallanır ve şunlardan etkilenir:
  - Orta saha statları
  - Takım hype'ı
  - Taktik
  - Son düelloların sonuçları
  - Rastgelelik
- **Atak zinciri:** 1-3 düellodan oluşur.
  - Taşıyıcı rakibi geçerse ilerler. Bir sonraki düello ya bir sonraki savunmacıya karşıdır, ya da ceza sahasındaysa kaleciye karşı şut düellosudur.
  - Pas kartı oynanırsa saha görünümüne geçilir ve pasın hedefi seçilir. Hedef oyuncu kendi destesiyle sıradaki düelloya girer.
  - Herhangi bir düello kaybedilirse atak biter.
- **Rakip atağında** senin savunmacın düelloya girer.
- **Taktik:** maç öncesi seçilir, sadece devre arasında değiştirilebilir. Taktikler:
  - Üstünlüğü kaydırır.
  - Atakların hangi kulvardan geldiğini belirler.
  - Düello kaybedildiğinde kontra yeme riskini değiştirir.
  - Prototipteki hücum ve savunma taktikleri başlangıç noktasıdır.
- **Kondisyon:** her oyuncunun maç boyunca taşınan bir kondisyonu var.
  - Düellolar kondisyon harcar, bazı kartlar ek kondisyon harcar.
  - *Karar:* kondisyon %30'un altındaysa oyuncu düelloya 1 eksik enerjiyle başlar.
  - Kondisyon maçlar arasında tamamen dolar.

### 5.4 Düello

İki tarafın değerleri:
- **Taşıyıcı:** Kontrol (can). Kontrolü sıfırlanırsa topu kaybeder.
- **Savunmacı:** Denge (can). Dengesi sıfırlanırsa geçilir.

Akış:
- **Tur yapısı:** oyuncu her tur 3 enerji alır ve kendi oyuncusunun destesinden, role uyan kartlardan **4 kart** çeker. Kartları oynar, turu bitirir. Ardından rakip, gösterdiği niyeti uygular.
- **Düellolar arası taşınanlar:**
  - Her düello taze karıştırılmış bir desteyle ve tam Kontrol/Denge ile başlar. Çekme ve atma yığınları düelloya özeldir.
  - Momentum aynı atak zinciri içinde, bir sonraki düelloya giren oyuncuya taşınır. Zincir bitince sıfırlanır.
  - Diğer durum etkileri düello bitince silinir.
  - Kondisyon, hype ve Alev maç boyunca taşınır.
  - 4 kart çekmenin sebebi: küçük destede deste kompozisyonu ve inceltme gerçek karar olsun.
- **Niyet:** rakibin bir sonraki hamlesi her zaman görünür (ör. "Kayarak müdahale: 7"). Rakibin hamleleri kendi destesinden ya da desen tablosundan gelir (bölüm 8.4).
- **Zaman sınırı:** en fazla 3 tur. Sonunda kimse kazanmamışsa savunmaya yardım gelir ve **savunan taraf kazanır**. Bu, hücum edeni risk almaya zorlar. Oyuncu savunmadayken aynı sınır onun lehine çalışır.
- **Pas: her zaman açık temel hamle.** Desteden çekilmez, 1 enerji tutar, düelloyu her an bitirebilir. Topu seçilen bir takım arkadaşına verir.
  - Başarı ihtimali görünür ve o anki duruma göre değişir. Rakip Dengesizse ve Denge'si düşükse artar. Taşıyıcının Kontrolü düşükse azalır. Pas statı ve hedefin konumu etkiler.
  - Başarı ihtimali düello içinde yapılan hamlelerle değiştiği için saf bir zar değil, bir hazırlık sonucudur.
  - **Başarısız pas düello kaybı sayılır:** atak biter, Alev söner, rakip kontraya çıkabilir.
  - Destede ayrıca daha güçlü özel pas kartları olabilir (ör. Ara pası, Uzun top). Bunlar daha yüksek ihtimal ya da ek etki verir.
  - *Karar:* savunan tarafın da benzer bir temel hamlesi olabilir (ör. "Uzaklaştır"). İnşa eden, gerekirse ekler.
- **Şut düellosu (kaleciye karşı):** aynı düello motoru kullanılır, en fazla **2 tur** sürer.
  - Şutçu, kartlarıyla kalecinin **Kale** değerine hasar verir. Kale sıfırlanırsa gol olur.
  - Kalecinin niyeti görünür, ör. "Sağ köşeye yatıyor" ya da "Çıkıp topu kapacak".
  - Şut kartları köşe hedefler (Yakın köşe, Uzak köşe, Üst köşe). Kalecinin yatmadığı köşeye giden şut **çift hasar** verir. Kalecinin Yakın/Uzak statları, ilgili köşedeki savunmasını belirler (prototipteki köşe sisteminden).
  - Kaleci "Çık ve kap" gibi hamlelerle şutçunun Kontrolüne saldırır. Kontrol sıfırlanırsa kaleci topu alır.
  - 2 tur sonunda gol olmazsa kurtarış sayılır.
  - **Oyuncunun kalecisi savunurken** aynı düello oynanır, oyuncu kaleci kartlarını kullanır, rakip şutçunun niyeti görünür.
- **Penaltı:** tek turluk şut düellosu.

### 5.5 Kartlar ve durum etkileri

- **Kart bileşenleri:** ad, enerji maliyeti, hedef, etki listesi, rol etiketi (Hücum/Savunma/İkisi de), mevki etiketleri, nadirlik, yükseltilmiş (+) versiyonu.
- **Mevki etiketleri:** her kartın hangi mevkilerin destesine girebileceğini belirler. Etiketler: Kaleci, Savunma, Orta Saha, Kanat, Santrafor.
- **Nadirlik:** Sıradan, Nadir, Efsane.
- **Örnek kartlar:**
  - Topu sakla (+Koruma)
  - Sırtını koy (+Koruma, sonraki Çalım güçlenir)
  - Kolunu uzat (rakibi Dengesiz yapar)
  - Çalım (hasar, Dengesiz rakibe ×2)
  - Kayarak müdahale
  - Pas
  - Şut (köşe hedefli)
- **İlk durum etkileri:**
  - **Koruma:** gelen hasarı emer, tur sonunda sıfırlanır.
  - **Dengesiz:** alınan hasar %50 artar, süreli.
  - **Momentum:** verilen hasar artar, düello boyunca kalır.
  - **Yorgun:** verilen hasar azalır.
  - Liste genişletilebilir olmalı.
- **İlk sürüm hedefi:** yaklaşık 40 kart. Her mevkiye başlangıç destesi olarak 6 temel kart, kalanı mağaza ve ödül havuzu.

### 5.6 Hype, Alev ve meyveler

- **Hype:** düello kazanmak, özellikle zor durumdan dönmek, hype verir. 3 hype olunca oyuncu **Alev** alır ve bir sonraki düelloya +1 enerjiyle başlar.
  - Alevli oyuncu düello kaybederse alev söner. Onu durduran rakip doğrudan Alev alır (prototipten).
  - Gol, atan takımın tüm oyuncularına +1 hype verir.
  - Takımın toplam hype'ı Üstünlüğü besler.
- **Meyveler** Slay the Spire'daki relic'lerin karşılığıdır: oyuncuya takılan, **her zaman çalışan** pasif güçler.
  - Bazı meyvelerin ek bir **Alev bonusu** olur, oyuncu alevliyken güçlenirler. Bu, Alev'i değerli tutar.
  - Prototipteki dört meyve başlangıç setidir ve kart sistemine göre yeniden yorumlanır: Ahtapot, Kral Yengeç, Kılıç Balığı, Uçan Balık.
  - Ahtapot ve Kral Yengeç **kaleciye özeldir** ve köşe savunmasını etkiler. Diğer meyvelerin hangi mevkilere takılabileceği meyvenin tanımında yazar.
  - Kupa yolunda yenileri kazanılır veya satın alınır.

### 5.7 Maç sonu: EXP, seviye, para, mağaza

- **EXP:** oyunculara düelloya girdikleri ve kazandıkları oranda dağılır. Seviye atlayan oyuncunun bir kartı yükseltilir (+ versiyonuna geçer).
- **Para:** maç sonucuna ve maç içi başarılara göre verilir.
- **Maçlar arası mağaza:**
  - Yeni kart: mevki etiketine uygun bir oyuncunun destesine eklenir.
  - Meyve.
  - **Kart çıkarma**, ücretli. *Karar:* deste inceltme StS'nin temel stratejilerinden biri olduğu için eklendi.
- **Maç sonu ödülü:** her maç sonunda ayrıca 3 karttan 1'ini seçme hakkı. İsteyen almayabilir.

## 6. Denge ilkeleri

- Rastgele oynayan oyuncu düzenli kazanamamalı. Simülasyonla ölçülür: hedef olarak, rastgele oynayan bir model ilk grup maçını %10'dan az kazanmalı.
- İyi oynayan model ile ortalama model arasındaki fark belirgin olmalı.
- Ortalama maç süresi 5-10 dakika. Düello sayısı ve tur süreleri simülasyonla ölçülür. Ekranda harcanan süre oyun testinde ölçülür.
- Skorlar çoğunlukla 0-0 ile 3-2 arasında. Arada farklı skorlar mümkün.

## 7. Kademeler

Her kademe oynanabilir ya da çalıştırılabilir bir sürümle biter.

1. **Çekirdek ve simülasyon:** saf C# kurallar motoru (düello, kartlar, durum etkileri, maç akışı, Üstünlük), JSON veri yükleme, komut satırı simülatörü, birim testleri. Unity gerekmez.
2. **Düello ekranı (Unity):** tek bir düello baştan sona oynanır. El, enerji, niyet, çubuklar, durum etkileri, kart animasyonları, yer tutucu çöp adamlar.
3. **Maç akışı:** saha haritası, kritik anlar, atak zinciri, pas hedefi seçimi, şut düellosu, devre arası taktik, kondisyon, maç sonu ekranı.
4. **Kupa, mağaza ve kalıcı kazanımlar:** kupa haritası, rakip önizleme, mağaza, EXP ve seviye, kart yükseltme, meyveler, penaltı, kayıt ve yükleme, kalıcı kazanımlar.
5. **Cila:** gerçek görseller, ses, efektler, ayarlar menüsü, denge turu.

## 8. Teknik mimari

### 8.1 Depo düzeni

```
WeirdLand/                     (git deposu)
  prototype/index.html         HTML prototip (referans)
  docs/                        tasarım belgeleri ve planlar
  core/                        saf C# çekirdek (.NET, Unity'den bağımsız)
    WeirdLand.Core/            kurallar motoru (netstandard2.1)
    WeirdLand.Core.Tests/      NUnit testleri (dotnet test)
    WeirdLand.Sim/             komut satırı simülatörü
  data/                        oyun verisi (JSON): kartlar, oyuncular, takımlar, meyveler
  unity/                       Unity projesi (6000.3.9f1)
```

- **Çekirdek kaynak kodunun tek bir yeri var.** Unity, çekirdeği derlenmiş bir DLL olarak alır: `dotnet build` çıktısı Unity'nin `Plugins` klasörüne kopyalanır. Alternatifi, çekirdek klasörünü Unity'de `noEngineReferences: true` olan bir asmdef ile UPM yerel paketi olarak bağlamak.
  - *Karar inşa edene ait,* ama kod iki yerde kopyalanmamalı.
- **Çekirdek `netstandard2.1` hedefler.** Unity ile uyumlu C# sürümü kullanılır, Unity'nin desteklemediği yeni dil özelliklerinden kaçınılır.

### 8.2 Çekirdek ilkeleri

- **Deterministik:** tüm rastgelelik, seed alan kendi RNG sınıfından gelir. `UnityEngine.Random` ve `System.Random` doğrudan kullanılmaz. Aynı seed ve aynı girdiler aynı sonucu verir. Hata ayıklama ve simülasyon buna dayanır.
- **Komut girer, olay çıkar.** Arayüz çekirdeğe komut gönderir (ör. `PlayCard`, `EndTurn`, `ChoosePassTarget`). Çekirdek durumu günceller ve bir olay listesi döndürür (ör. `CardPlayed`, `DamageDealt`, `StatusApplied`, `DuelEnded`). Unity bu olayları sırayla canlandırır. Çekirdek animasyondan ve zamandan habersizdir.
- **Veri odaklı kart etkileri:** kart etkileri küçük bir etki sözlüğüyle JSON'da tanımlanır (hasar, koruma, durum ekle, kart çek, enerji kazan, koşullu çarpan…). Sözlüğe sığmayan özel kartlar adlandırılmış C# etki sınıflarıyla yazılır.
- **Rakip yapay zekası çekirdekte olur.** Simülasyonda iki tarafı da oynayan "oyuncu modelleri" (rastgele, açgözlü, iyi) çekirdekte veya simülatörde bulunur.

### 8.3 Veri

- **Tek kaynak `data/` altındaki JSON dosyalarıdır.** Hem çekirdek testleri ve simülatör hem de Unity aynı dosyaları okur.
- *Karar (konuşmada ScriptableObject denmişti, değişiklik):* JSON seçildi. Simülatör Unity'siz çalışacağı için verinin Unity'den bağımsız okunabilmesi gerekiyor. JSON ayrıca metin olduğu için kolay incelenir ve değiştirilir. İleride Unity içinde kart düzenlemeyi kolaylaştıran bir editör penceresi eklenebilir.
- **Veri dosyaları bir şema sürümü taşır.** Geçersiz veri test sırasında yakalanır.

### 8.4 Rakip davranışı

- **Rakip oyuncular da desteyle oynar.** Niyetleri her tur görünür. Rakibin seçimi, o oyuncunun destesi ve basit öncelik kurallarıyla belirlenir (StS'deki düşman desenleri gibi).
- **Rakip takımların zorluğu tura göre artar.** Grup maçlarında zayıf, finalde güçlü desteler ve meyveler.
- **Rakip takım tanımları JSON'dadır:** oyun tarzı, oyuncular, desteler, meyveler.

### 8.5 Unity sunum katmanı

- **Sahneler:** Boot, Ana Menü, Kupa Haritası, Maç (saha + düello katmanı), Mağaza/Geliştirme, Sonuç.
- **Arayüz:** uGUI + TextMeshPro. Animasyonlar DOTween ile, akış UniTask ile. Kartlar, oyuncu figürleri ve ekran parçaları prefab'dır. Prefab'lar ve sahne iskeletleri mümkün olduğunca editör script'leriyle üretilir ki tekrar üretilebilir olsunlar.
- **Görsel varlıklar:** yer tutucular (çöp adam, düz renkli kart çerçevesi) ilk aşamada kullanılır. Varlıklar bir katalog üzerinden bağlanır ki sonra değiştirilebilsin.
- **Çözünürlük:** 1920×1080 temel alınır, farklı en-boy oranlarına göre ölçeklenir.
- **Yerelleştirme:** tüm metinler tek bir Türkçe metin tablosundan gelir.

### 8.6 Paketler

| Paket | Kaynak | Amaç |
|---|---|---|
| DOTween (ücretsiz) | Asset Store / OpenUPM | Animasyon |
| TextMeshPro | Unity 6 ile gelir (uGUI) | Yazı |
| Unity Test Framework | Unity paketi | Unity tarafı testleri |
| Newtonsoft JSON (`com.unity.nuget.newtonsoft-json`) | Unity paketi | JSON okuma, kayıt |
| UniTask | OpenUPM | Asenkron akış |
| Input System | Unity paketi | Girdi |
| 2D Animation / 2D Sprite | Unity paketi | Kademe 5 karakter animasyonu |

Çekirdek tarafında JSON okumak için System.Text.Json (netstandard2.1 için NuGet paketi) veya Newtonsoft kullanılabilir. İki tarafta aynı kütüphaneyi kullanmak tercih edilir.

### 8.7 Kayıt

- **İki dosya:** koşu kaydı ve kalıcı ilerleme kaydı. JSON formatında, `Application.persistentDataPath` altında tutulur.
- **Ne zaman kaydedilir:** koşu her maç sınırında kaydedilir. Maç ortasında çıkılırsa maç baştan başlar.
- **Kayıt dosyaları sürüm numarası taşır.**

### 8.8 Test ve kalite

- **Çekirdek:** NUnit ile `dotnet test`. Her kart etkisi, durum etkisi ve düello sonucu kuralı testlenir.
- **Simülatör:** `dotnet run` ile N maç veya N koşu çalıştırır, oyuncu modelleri arasında kazanma oranı, skor dağılımı, düello sayısı ve tahmini süre raporlar. Bölüm 6'daki hedefler buradan ölçülür.
- **Unity:** veri yükleme ve sahne açılışı için EditMode/PlayMode duman testleri. Komut satırından `Unity.exe -batchmode -runTests` ile çalıştırılır.
- **Git:** Unity için `.gitignore`. Görseller ve sesler için Git LFS. Her kademe için ayrı dal, kademe bitince ana dala birleştirilir.

## 9. İnşa edene bırakılanlar

- Tüm sayısal değerler: Kontrol, Denge ve Kale değerleri, hasar miktarları, Üstünlük formülü, EXP eğrisi, fiyatlar. Bunlar simülasyonla ayarlanır.
- İlk ~40 kartın tam listesi ve etkileri. Bölüm 5.5'teki örnekler ve mevki dağılımı esas alınır.
- Rakip takım içerikleri ve rakip yapay zekasının ayrıntıları.
- Çekirdeğin Unity'ye nasıl bağlanacağı (DLL kopyalama veya UPM yerel paket).
- Saha haritasının görsel tasarımı ve yer tutucu sanat tarzı.
- Kademe içi iş sırası.
