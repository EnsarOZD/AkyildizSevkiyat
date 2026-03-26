# Netsis Wings (NetOpenX REST) Entegrasyon Talep Dokümanı

**Konu:** Akyıldız Sevkiyat Sistemi - Netsis Wings Entegrasyonu

Merhaba,

Mevcut yapımızda siparişler kendi geliştirdiğimiz web tabanlı sistemimiz üzerinden hazırlanmakta olup, bu siparişlerin Netsis Wings’e **"Müşteri Siparişi"** olarak aktarılması planlanmaktadır.

### ⚠️ Önemli Not:
Mevcut entegrasyon yapımızda olduğu gibi, bizim sistemimizde üretilen sipariş numarası Netsis içerisindeki **Müşteri Siparişi Numarası (Belge No)** ile birebir aynı olacaktır. Netsis tarafında sipariş numarasının sistemimizden gelen numara ile oluşturulmasını, otomatik artan numara kullanılmamasını hedefliyoruz.

Netsis tarafında irsaliye kesme işlemleri mevcut operasyon akışımızda olduğu gibi Netsis üzerinden yapılmaya devam edecektir. Ayrıca, kesilen irsaliyelerden bağımsız olarak stokların güncel bakiye (mevcut stok / serbest stok) bilgilerinin tarafımıza anlık olarak okunabilmesi gerekmektedir.

Bu kapsamda **NetOpenX REST** entegrasyonu için aşağıdaki bilgileri ve yönlendirmeleri tarafınızdan rica ederiz:

---

## 1️⃣ Entegrasyon Altyapısı
**Kullanılacak Yöntem:** NetOpenX REST (Aktif ve desteklenen sürüm)

Lütfen aşağıdaki bilgileri iletiniz:
- **REST Servis Base URL:** (Örn: `http://server_ip:port/api/v2`)
- **Login / Token Alma Endpoint Bilgisi:**

## 2️⃣ Kimlik Doğrulama (Authentication) Bilgileri
Entegrasyon servislerinde kullanılacak yetkili kullanıcı bilgileri:
- **Netsis Kullanıcı Adı:**
- **Şifre:**
- **Firma Kodu (DB Adı):**
- **Şube Kodu:**
- **İşletme Kodu (Varsa):**

*> **Not:** Mümkünse operasyonel kullanıcılardan bağımsız, sadece API işlemleri için yetkilendirilmiş bir "Entegrasyon Kullanıcısı" tanımlanmasını tercih ederiz.*

## 3️⃣ Müşteri Siparişi Oluşturma (Create Sales Order)
Siparişlerin sisteme "Kaydedildi" statüsünde aktarılabilmesi için:
- **Endpoint / Metod:** (Örn: `POST /api/v2/sales/orders`)
- **Sipariş Numarası Yönetimi:** Belge numarasının (Fatura/Sipariş No) dış sistemden gönderdiğimiz ID ile oluşması için gerekli parametre veya yöntem nedir?
- **Zorunlu Alanlar Listesi:** (Cari Kod, Stok Kodu, Miktar, Depo Kodu, Teslim Tarihi, Belge Tipi vb.)
- **Proje Kodu Kullanımı:** Proje kodu alanının hangi parametre ile gönderileceği.
- **Örnek JSON Payload:** Başarılı bir sipariş oluşturma isteği için örnek gövde verisi.

## 4️⃣ Stok Bakiye Bilgilerinin Okunması
Stokların anlık durumunu sorgulamak için:
- **Endpoint / Metod:** (Örn: `POST /api/v2/items/balance` ?)
- **Sorgu Parametreleri:** Stok kodu veya Depo kodu bazlı filtreleme imkanı var mı?
- **Dönen Veri Seti:**
    - Stok Kodu
    - Depo Kodu
    - Toplam Mevcut Stok
    - Rezerve Miktar / Serbest Stok
- **Örnek Response:** Dönen JSON cevabının örneği.

## 5️⃣ Yetki & Güvenlik
- **IP Kısıtlaması:** Sunucunuza erişim için IP whitelist tanımlamamız gerekiyor mu?
- **SSL:** Servisler HTTPS üzerinden mi çalışıyor? Sertifika gerekliliği var mı?
- **Test Ortamı:** Geliştirme sürecinde Canlı veriyi etkilememek için kullanabileceğimiz bir Test/Sandbox ortamı var mı? Varsa erişim bilgileri.

---

Talep edilen bilgiler doğrultusunda entegrasyon geliştirme ve test süreçlerine başlayacağız.
Geri dönüşlerinizi rica eder, iyi çalışmalar dileriz.
