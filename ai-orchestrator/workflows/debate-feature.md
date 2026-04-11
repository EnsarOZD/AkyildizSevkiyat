# Debate Feature Workflow

Bu workflow agent'ların birbirini eleştirdiği
multi-turn conversation modudur.

Aşamalar:
1. Manager: Görevi analiz et, agent'ları seç
2. Backend + Frontend: Paralel plan çıkar
3. Çapraz eleştiri: Birbirini eleştir
4. Architect: Çelişkileri çöz
5. QA: Final review
6. Manager: Final karar ver

## Manager Talimatları

Bu debate workflow'unda tüm agent'ları (Backend, Frontend, Architect, QA) seçmelisin.
Her agent önce kendi planını çıkaracak, ardından birbirlerinin planlarını eleştirecek.
En sonunda sen tüm çıktıları değerlendirerek final implementation planını vereceksin.

Görev analizinde şunlara dikkat et:
- Backend ve Frontend arasındaki potansiyel uyumsuzluklar
- API contract gereksinimleri
- Öngörülemeyen edge case'ler
- Mimari riskler
