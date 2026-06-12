# SPEC: Kıyafet Deposu Toplama & Kapama Akışı (Picking/Packing)

**Proje:** AkyildizSevkiyat (.NET 10 Clean Architecture + Vue 3)
**Tarih:** 2026-06-09
**Kaynak:** Depo müdürü toplantı notları + soru-cevap netleştirmesi
**Durum:** İş analizi tamamlandı, teknik açık sorular bölüm 7'de

---

## 1. Mevcut Manuel Akış (As-Is)

1. İrsaliye / sipariş formu depo yöneticisine gelir.
2. Yönetici siparişleri kriterlere göre (beklenen koli boyutu, proje türü: güvenlik/temizlik vb.) toplayıcılara böler. Küçük talepleri öne alabilir, gerekirse devam eden işi kestirip araya sipariş sokar.
3. Toplayıcılar toplama arabalarıyla sahada ürün toplar. Bir sipariş bir veya birden çok arabaya yayılabilir.
4. Kapamacılar arabalardaki ürünleri koliler, tüm koliler bitince etikete proje kodu/adı/koli adedi basılır (Zebra yazıcı, "ykbarkod" yazılımı) veya elle yazılır. Etiket örneği: `Hiltown Avm-TM-34340-4 Koli`.
5. Hazırlanan talep muhasebeye döner, koli adedi talebin üzerine yazılır, koli adetli irsaliye oluşturulur.
6. Dağıtım zamanı geldiğinde sahaya geri gelir.
7. Düşük dönemde kapamacı çalışmaz; toplayıcı kendi kolisini kapatır.

## 2. Temel İş Kuralları (Netleştirilmiş)

| # | Kural | Kaynak |
|---|-------|--------|
| K1 | Tek arabada tek sipariş. İstisna: poşete sığan 2-3 kalemlik küçük siparişler toplu (batch) hazırlanabilir. | Müdür Q&A |
| K2 | "Toplama bitti" kararını **toplayıcı** verir (listesindeki tüm satırlar işlendiğinde). | Müdür Q&A |
| K3 | Eksik ürünler rapora düşer; stok tamamlanınca **manuel kararla** ("gönderilmesi istenirse") gönderilir. Otomatik tetik yok. | Müdür Q&A |
| K4 | Koli adedinin **tek doğruluk noktası kapamacıdır**. Muhasebe yeniden girmez, sistemden okur. | Müdür Q&A |
| K5 | Atama sipariş bazındadır; kriter (koli boyutu, proje türü) yöneticinin insiyatifidir — sistem kriteri otomatikleştirmez, sadece filtreleme/gruplama kolaylığı sağlar. | Müdür Q&A |
| K6 | Kapamacı bir **görev tipidir, rol değildir**. Toplayıcı kendi kapamasını yapabilir. | Toplantı notu |
| K7 | Öncelik = kuyruk sıralaması + devam eden toplama görevini **duraklatıp araya iş sokabilme**. Yarım görev durumu korunmalı. | Müdür Q&A |
| K8 | Etiket: kapamada "Etiket Bas" butonu → Zebra'dan otomatik basım. Basılmak istenmezse atlanır, elle yazılır (poşet ve tek koliler için tipik). | Müdür Q&A |

## 3. Domain Modeli

### Entity'ler

**PickingJob (ToplamaGörevi)**
- `Id`, `Type` (Single | Batch), `AssignedPickerId`, `Status`, `QueueOrder` (öncelik sırası)
- Single → tam olarak 1 sipariş; Batch → N küçük sipariş (join: `PickingJobOrders`)
- `CartAssignments`: 1..n araba (Single), 1 araba (Batch)
- Status: `Assigned → InProgress → Paused ↔ InProgress → Completed`
- `Completed` alt durumu: `Full` (eksiksiz) | `WithShortage` (eksikli)

**PickingJobLine**
- `OrderLineId`, `RequestedQty`, `PickedQty`, `ShortageQty`
- Duraklatma/devam ve eksik raporu için satır bazlı miktar takibi **zorunlu**.

**Cart (ToplamaArabası)**
- `Code` (barkod), `CurrentPickingJobId` (nullable)
- Açık soru: duraklatılan görevde araba kilitli mi kalır? (Bkz. 7.4)

**ClosingJob (KapamaGörevi)**
- `OrderId`, `AssignedToId` (herhangi bir personel — K6), `Status` (Pending → InProgress → Closed)
- `BoxCount` (int, tek doğruluk noktası — K4)
- `PackageType` (Koli | Poşet)
- `LabelPrinted` (bool), `LabelPrintedAt`

**ShortageRecord (EksikÜrünKaydı)**
- `OrderId`, `OrderLineId`, `Qty`
- Status: `Pending → DispatchRequested → Picked → Shipped` veya `Cancelled`
- "Gönder" aksiyonu yeni bir mini `PickingJob` (Single, sadece eksik satırlar) üretir.

**LabelPrintJob** (Print/Baskı subsystem)
- ZPL şablonu, içerik: `{ProjeKodu}-{ProjeAdı}-{Birim}-{N} Koli` (mevcut ykbarkod formatıyla birebir uyumlu)
- Kuyruk + retry; yazıcı erişilemezse kapama bloklanmaz (elle yazma fallback'i zaten iş kuralı — K8).

### Sipariş depo durumu (state machine)

```
Pending → Assigned → Picking → (Paused ↔ Picking) → Picked[Full|WithShortage]
       → Closing → Closed(BoxCount girildi) → InvoiceReady → ReadyForDispatch
```

- `Picked[WithShortage]` siparişin ilerlemesini ENGELLEMEZ — eksikli kapanıp sevk edilebilir (K3). Eksikler ShortageRecord kuyruğunda bağımsız yaşar.
- `ReadyForDispatch` mevcut kıyafet export akışına bağlanır — **dikkat:** mevcut kıyafet flow'u "depo hazırlığını atlayan direkt export" olarak kurulu. Bu spec ile kıyafet siparişleri artık depo hazırlığından geçecek; export tetikleyicisinin yeri değişmeli. Implementasyonda mevcut akış envanteri şart (bkz. 6).

## 4. Ekranlar

1. **Yönetici Atama Paneli:** Bekleyen siparişler listesi; proje türü / tahmini boyut filtreleri; toplayıcıya atama; batch oluşturma (küçük siparişleri seçip tek görevde birleştirme); kuyruk sıralama (öncelik); aktif görevi duraklat + araya görev sok.
2. **Toplayıcı Ekranı (mobil/tablet):** Görev kuyruğum; satır bazlı toplama (PickedQty girişi; depo adresleme/barkod sistemi entegrasyonu — Mayıs 2026 tasarımı `[Koridor+Yön]-[Sıra]-[Seviye]-[Pozisyon]` ile lokasyon gösterimi); satırı "eksik" işaretleme + eksik adet; "Toplamayı Bitir" (K2); duraklatılan görevde kaldığı yerden devam.
3. **Kapama Ekranı:** Kapamaya hazır siparişler; koli adedi girişi (K4); paket tipi (koli/poşet); "Etiket Bas" butonu (K8) + "Elle Yazıldı" atlama seçeneği.
4. **Eksik Ürün Kuyruğu:** Bekleyen ShortageRecord listesi; stok durumu görünümü; "Gönder" aksiyonu → mini PickingJob oluşturur (K3).
5. **Muhasebe Görünümü:** Kapatılmış siparişler + sistemdeki koli adedi (salt okunur); koli adetli irsaliye oluşturma — mevcut Netsis entegrasyonu/irsaliye akışına bağlanır.

## 5. Kapsam Dışı (Bu Fazda Yok)

- Koli içi ürün takibi (hangi koli hangi ürünü içeriyor) — sadece toplam koli adedi tutulur.
- Atama kriterlerinin otomatikleştirilmesi (öneri motoru vb.).
- Toplama rotası optimizasyonu.

## 6. Claude Code İmplementasyon Planı

**Faz 0 — Read-only envanter (hiçbir değişiklik yok):**
> "Mevcut Order/Shipment entity'lerini, kıyafet (clothing) export akışını ve status enum'larını listele. Kıyafet siparişlerinin şu an hangi noktada export edildiğini ve hangi handler'ların tetiklediğini raporla. HİÇBİR DOSYAYI DEĞİŞTİRME. Rapor bitince STOP."

**Faz 1 — Gap analizi:**
> "Ekteki SPEC dokümanını Faz 0 raporuyla karşılaştır. Yeni entity'ler, değişecek mevcut entity'ler, kıyafet export tetikleyicisinin taşınacağı yer ve migration ihtiyaçlarını listele. Riskli/geri dönüşsüz değişiklikleri ayrıca işaretle. Kod yazma. STOP."

**Faz 2+ — Atomik implementasyon (her commit tek konu):**
1. Domain entity'ler + migration
2. PickingJob CQRS command/query'leri + state machine
3. ClosingJob + ShortageRecord
4. Print subsystem (ZPL) — teknik açık sorular (7.1) çözüldükten sonra
5. Vue ekranları (yönetici → toplayıcı → kapama → eksik kuyruğu sırasıyla)
6. Kıyafet export tetikleyicisinin ReadyForDispatch'e taşınması (en riskli adım, en sona)

## 7. Açık Teknik Sorular (İmplementasyon Öncesi Cevaplanmalı)

1. **Zebra bağlantısı:** ykbarkod masaüstü yazılımı mı? Web uygulamasından basım için seçenekler: ağ üzerinden raw ZPL (port 9100), Zebra BrowserPrint, veya lokal print-agent servisi. Yazıcının IP'si var mı?
2. **Koli serisi:** Etikete `1/4, 2/4...` serisi basılsın mı? Teslimat noktasında eksik koli kontrolünü kolaylaştırır; mevcut formatta yok.
3. **Batch etiketleri:** Toplu hazırlanan poşet siparişlerinde her sipariş kendi poşet etiketini alıyor mu, yoksa elle mi yazılıyor?
4. **Duraklatma + araba:** Görev duraklatıldığında yarım dolu araba kilitli mi bekler (araba sayısı kısıtsa darboğaz olur), yoksa boşaltılır mı?
