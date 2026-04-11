# Debate Feature Workflow

Bu workflow agent'ların birbirini eleştirdiği
multi-turn conversation modudur.

Aşamalar:
1. Manager: Görevi analiz et, agent'ları seç
2. Business Analyst: İş gereksinimlerini analiz et, eksiklikleri tespit et
3. Backend + Frontend: Paralel plan çıkar (BA analizini dikkate alarak)
4. Çapraz eleştiri: Backend/Frontend birbirini, BA implementasyonu eleştirsin
5. Architect: Çelişkileri çöz
6. QA: Final review
7. Manager: Final karar ver

## Manager Talimatları

Bu debate workflow'unda tüm agent'ları (Business Analyst, Backend, Frontend, Architect, QA) seçmelisin.
Business Analyst önce iş gereksinimlerini analiz edecek.
Backend ve Frontend BA analizini dikkate alarak paralel plan çıkaracak.
Ardından birbirlerinin planlarını eleştirecekler, BA ise implementasyonu iş perspektifinden değerlendirecek.
En sonunda sen tüm çıktıları değerlendirerek final implementation planını vereceksin.

Görev analizinde şunlara dikkat et:
- İş gereksinimlerinin eksiksiz tanımlanması (BA agent)
- Backend ve Frontend arasındaki potansiyel uyumsuzluklar
- API contract gereksinimleri
- Öngörülemeyen edge case'ler
- Mimari riskler
- Sahada/gerçek hayatta kullanılabilirlik (BA perspektifi)
