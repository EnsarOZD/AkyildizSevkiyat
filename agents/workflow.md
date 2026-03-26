## Görev Tier Sınıflandırması

| Tier | Kapsam | Pipeline |
|------|--------|----------|
| S (Küçük) | Tek dosya, bugfix, UI tweak, küçük düzeltme | Manager → Backend/Frontend → Done |
| M (Orta) | Multi-file, yeni endpoint, yeni component, mevcut akış değişikliği | Manager → Backend → Frontend → QA |
| L (Büyük) | Yeni entity/modül, migration, breaking change, yeni feature alanı | Manager → Architect → Backend → Frontend → QA → Manager |

Architect ve Operations **yalnızca Tier L veya gerçekten gerekli olduğunda** devreye girer.

---

## Genel Akış

```
Kullanıcı Direktifi
  → Manager: analiz et, tier belirle, gerekirse Architect/Operations'a danış
  → Kullanıcı Onayı (Tier M/L için zorunlu)
  → Backend (önce)
  → Frontend (backend contract netleşince)
  → QA (Tier M/L için)
  → Manager: özet rapor
```

---

## Zorunlu Danışma Koşulları

**Architect** — aşağıdakilerden biri varsa:
yeni entity/aggregate/tablo · yeni modül · migration · mevcut pattern değişikliği · authorization modeli etkisi · dış entegrasyon · breaking API change · cross-cutting concern

**Operations** — aşağıdakilerden biri varsa:
yeni kullanıcı akışı/ekranı · picking/warehouse/shipment/delivery/goods receipt etkisi · rol bazlı davranış değişikliği · kullanıcıdan yeni veri girişi

---

## Agent Spawn Formatı

```
AGENT: [Manager / Backend / Frontend / Architect / QA / Operations]
TIER: [S / M / L]

GÖREV:
- [Madde 1]
- [Madde 2]

BAĞLAM:
- [Agent dosyasında olmayan, bu göreve özgü domain/teknik bağlam]

SINIR:
- [Bu göreve dahil olmayan şeyler]
- [Kabul kriterleri]
```

Genel kurallar her agent'ın kendi dosyasında tanımlıdır; spawn'da tekrarlanmaz.

---

## Onay Seviyeleri

| Durum | Kim Onaylar |
|-------|-------------|
| Feature planı | Kullanıcı |
| Yeni component/view listesi | Kullanıcı (plan aşamasında) |
| Yeni store / router / layout | Kullanıcıya Manager üzerinden sunulur |
| Migration | Yalnızca plan kapsamındaysa uygulanır |
| Mimari karar | Architect |
| Operasyonel akış | Operations |
| Plan dışı genişleme | Kullanıcıya geri dönülür |

---

## Geri Dönüş Döngüleri

**Architect:** Uygun → devam · Koşullu → plan revize · Uygun Değil → yeni yaklaşım

**Operations:** Uygun → devam · Koşullu → risk notu eklenir · Uygun Değil → süreç yeniden tasarlanır

**Execution sırasında** (Backend/Frontend) plan dışı ihtiyaç / mimari engel / contract uyuşmazlığı / yeni migration gereksinimi çıkarsa → agent durur, Manager'a rapor döner.

**QA:** Geçer → kapanış · Koşullu Geçer → dikkat notuyla teslim · Geçmez → ilgili agent'a geri dönüş

---

## Sistem Geneli — Hiçbir Agent Şunları Yapamaz

- Plan dışı değişiklik yapamaz
- Sessizce kapsam genişletemez
- Hata veya riski saklayamaz
- Build kırık teslim yapamaz
- QA fail sonucunu görmezden gelemez
- Kullanıcıdan onay alınmamış şeyi uygulamaya sokamaz
