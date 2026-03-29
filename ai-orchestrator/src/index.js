import "dotenv/config";
import { run } from "./orchestrator.js";

// ─── CLI argümanları ──────────────────────────────────────────────────────────

const args = process.argv.slice(2);

if (args.length === 0) {
  console.log(`
Kullanım:
  node src/index.js "<görev>" [workflow]

Workflow seçenekleri:
  plan-feature    → Planlama (kod üretmez)  [varsayılan]
  build-feature   → Uygulama (kod üretir)
  review-feature  → İnceleme / Review

Örnekler:
  node src/index.js "Kullanıcı girişi sayfası eklenecek"
  node src/index.js "Sipariş listesi endpoint'i oluştur" build-feature
  node src/index.js "Auth modülü gözden geçirilsin" review-feature
`);
  process.exit(0);
}

const task = args[0];
const workflow = args[1] || "plan-feature";

const validWorkflows = ["plan-feature", "build-feature", "review-feature"];
if (!validWorkflows.includes(workflow)) {
  console.error(`Geçersiz workflow: "${workflow}"`);
  console.error(`Geçerli seçenekler: ${validWorkflows.join(", ")}`);
  process.exit(1);
}

if (!process.env.ANTHROPIC_API_KEY) {
  console.error("Hata: ANTHROPIC_API_KEY ortam değişkeni tanımlı değil.");
  console.error(".env dosyasını kontrol et.");
  process.exit(1);
}

// ─── Çalıştır ─────────────────────────────────────────────────────────────────

run(task, workflow).catch((err) => {
  console.error("\nHata:", err.message);
  process.exit(1);
});
