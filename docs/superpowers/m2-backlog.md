# Kademe 2'ye devreden işler (Kademe 1 incelemelerinden)

Kaynak: Kademe 1 görev incelemeleri ve tüm dal son incelemesi (2026-11-22).

## Kademe 2'nin ilk görevinde
- **Blok hesabı tek yerde değil.** `EffectResolver` içindeki blok hesabı `EnemyAi` ve `GoodPolicy`'de tekrar yazılmış. `EffectResolver.ComputeBlock(EffectDef, Combatant)` olarak açılmalı. Unity kart önizlemesi ("Koruma 7") de buna ihtiyaç duyacak.
- **Unity sözleşmesi yazılmalı.** Hangi bilgi doğrudan durumdan okunur (el, Hp), hangisi olaylardan öğrenilir? `Combatant` için salt okunur bir yüzey gerekiyor. Şu an alanlar ve listeler herkese açık ve değiştirilebilir.
- **Unity uyumluluğu:**
  - IL2CPP ile Newtonsoft'un yansıma kullandığı veri tipleri kırpılabilir, `link.xml` gerekiyor.
  - Ondalıklı sayı hesabının (float) farklı çalışma ortamlarında aynı sonucu verip vermediği bir EditMode testiyle kontrol edilmeli.
  - Çekirdeğin Unity'ye nasıl bağlanacağı seçilmeli: derlenmiş DLL mi, yerel UPM paketi mi?

## Oynanış ve denge
- **Rakibin tek kartı çok sert vuruyor.** Rakip her tur tek kart oynuyor ve hasarı ×2,6 ile ölçekleniyor. Bazı kartlar oyuncunun bir turda bloklayabileceğinden fazla vuruyor. Çözüm: rakibe bir enerji bütçesi ya da spec §8.4'teki desen tablosu.
- **Rakibin pas ihtimali canlı değil.** Niyet gösterildiği anda sabitleniyor, taşıyıcıya vurmak onu düşürmüyor. Spec §5.4 bunun "hazırlık sonucu" olmasını istiyor.
- **Taşıyıcılar kırılgan.** `KontrolBase 5` nedeniyle rakibin hemen her hasar niyeti öldürücü olabiliyor. Oyun testinde bakılmalı.
- **Uzun maçlar.** 3. kademe rakipte ortalama süre 9,7 dk, maçların en yavaş %10'u 10 dakikayı aşıyor. Anti-hedefe yakın.
- **Kademe farkı sadece veriye dayanıyor.** `TierPowerPercent = 0`, kademe zorluğu tamamen deste değişimlerinden geliyor.
- **Ara bir oyuncu modeli yok.** "Ortalama" bir oyuncu modeli olsaydı iyi ve rastgele oyun arasındaki fark daha iyi ölçülürdü.
- **Kalecinin yatışı az kullanılıyor.** Rakip kaleci şut turlarının sadece %49/25/11'inde yatıyor. Kaleci desteleri gözden geçirilmeli ki köşe kararı daha sık önemli olsun.
- **Yığın yenilemesi yazılmalı.** Rakip var olan bir duruma yığın eklediğinde sonuç `eski − 1 + yeni` oluyor. Unity yığın animasyonu için bu kural belgelenmeli.

## Testler ve sağlamlık
- **Rng:** `Rng.Next(<=0)` korumasız. mulberry32 için sabit değerli bir test yok.
- **Veri arama:** `GameData.Card/Team` bilinmeyen kimlikte `KeyNotFoundException` atıyor. `TryGet` ya da `DataException` olmalı.
- **Eksik stat:** tek tek eksik bir stat 0 sayılıyor. Stats alanlarının zorunlu olup olmayacağına karar verilmeli.
- **Eksik test doğrulamaları:**
  - `DamageDealt.Blocked` ve `HpAfter` değerleri doğrulanmıyor. Unity bağlanınca eklenmeli.
  - Rakip yapay zekasının eşitlik bozma ve karıştırma testleri yok.
  - `PolicyTests` içinde değiştirilebilir bir static alan var.
- **RealDataTests:**
  - `../../../../../data` yolu kırılgan.
  - §5.5 kart adları, deste boyutu ve kademe artışı doğrulanmıyor.

## Kademe 4'te
- Penaltı düellosu testleri.
- 0 maliyetli bedava kazanç kartları (`tempo_belirle`, `soguk_kanlilik`) mağaza gelince yeniden değerlendirilmeli.
