# Şoför Tanıtım Turu — Ekran Görüntüleri

Bu klasöre eklediğiniz PNG/JPG/WEBP görseller, `DriverOnboarding.vue` tanıtım
turundaki ilgili kartta otomatik olarak gösterilir. Dosya yoksa o kartta panelin
ikonu gösterilmeye devam eder (build bozulmaz).

## Beklenen dosya adları

Her kartın `image` anahtarı ile eşleşir:

| Dosya adı            | Kart                          |
|----------------------|-------------------------------|
| `sefer-baslat.png`   | 1. Seferi Başlat (QR)         |
| `rota.png`           | 2. Rotayı Takip Et            |
| `teslimat.png`       | 3. Teslimatı Tamamla          |
| `seferi-kapat.png`   | 4. Seferi Kapat & İade        |
| `cevrimdisi.png`     | İnternet Yoksa Sorun Değil    |

## Öneriler

- **Boyut:** Dikey (telefon) ekran görüntüsü ideal. Genişlik ~600–800px yeterli.
- **Format:** `.webp` en küçük dosya boyutunu verir; `.png` de olur.
- İlgili ekranı şoför hesabıyla açıp cihazdan/ tarayıcıdan ekran görüntüsü alın,
  hassas müşteri verisi varsa karartın/örnek veriyle alın.
- Görsel ekledikten sonra `npm run build` veya dev sunucusu otomatik algılar.
