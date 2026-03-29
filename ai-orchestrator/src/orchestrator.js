import Anthropic from "@anthropic-ai/sdk";
import fs from "fs/promises";
import path from "path";

const client = new Anthropic({ apiKey: process.env.ANTHROPIC_API_KEY });

const MODEL = "claude-opus-4-6";
const AGENTS_DIR = "./agents";
const WORKFLOWS_DIR = "./workflows";
const OUTPUTS_DIR = "./outputs";

// ─── Prompt loader ────────────────────────────────────────────────────────────

async function loadPrompt(folder, name) {
  const filePath = path.join(folder, `${name}.md`);
  try {
    return await fs.readFile(filePath, "utf-8");
  } catch {
    throw new Error(`Prompt dosyası bulunamadı: ${filePath}`);
  }
}

// ─── Single agent call ────────────────────────────────────────────────────────

async function callAgent({ agentName, systemPrompt, userMessage, context = "" }) {
  console.log(`\n  ▸ ${agentName} çalışıyor...`);

  const fullUserMessage = context
    ? `## Bağlam\n${context}\n\n## Görev\n${userMessage}`
    : userMessage;

  const response = await client.messages.create({
    model: MODEL,
    max_tokens: 4096,
    system: systemPrompt,
    messages: [{ role: "user", content: fullUserMessage }],
  });

  const output = response.content[0].text;
  console.log(`  ✓ ${agentName} tamamlandı (${output.length} karakter)`);
  return output;
}

// ─── Manager — görev analizi ──────────────────────────────────────────────────

async function runManager(task, workflowPrompt) {
  const managerPrompt = await loadPrompt(AGENTS_DIR, "manager");

  const systemPrompt = `${managerPrompt}\n\n---\n\n## Workflow Talimatları\n${workflowPrompt}`;

  return callAgent({
    agentName: "Manager",
    systemPrompt,
    userMessage: task,
  });
}

// ─── Agent seçimi ve çalıştırma ───────────────────────────────────────────────

async function runSelectedAgents(managerOutput, task) {
  const results = { manager: managerOutput };

  // Manager çıktısından hangi agent'ların seçildiğini tespit et
  const needsBackend =
    /backend agent/i.test(managerOutput) || /backend.*seçildi/i.test(managerOutput);
  const needsFrontend =
    /frontend agent/i.test(managerOutput) || /frontend.*seçildi/i.test(managerOutput);
  const needsArchitect =
    /architect agent/i.test(managerOutput) || /architect.*seçildi/i.test(managerOutput);
  const needsQA =
    /qa.reviewer agent/i.test(managerOutput) ||
    /qa.*seçildi/i.test(managerOutput) ||
    /quality/i.test(managerOutput);

  const context = `## Manager Analizi\n${managerOutput}\n\n## Orijinal Görev\n${task}`;

  // Backend ve Frontend paralel çalışabilir
  const parallelTasks = [];

  if (needsBackend) {
    const backendPrompt = await loadPrompt(AGENTS_DIR, "backend");
    parallelTasks.push(
      callAgent({
        agentName: "Backend Agent",
        systemPrompt: backendPrompt,
        userMessage: task,
        context,
      }).then((output) => {
        results.backend = output;
      })
    );
  }

  if (needsFrontend) {
    const frontendPrompt = await loadPrompt(AGENTS_DIR, "frontend");
    parallelTasks.push(
      callAgent({
        agentName: "Frontend Agent",
        systemPrompt: frontendPrompt,
        userMessage: task,
        context,
      }).then((output) => {
        results.frontend = output;
      })
    );
  }

  // Paralel agent'ları bekle
  if (parallelTasks.length > 0) {
    await Promise.all(parallelTasks);
  }

  // Architect — backend/frontend çıktılarına bakarak karar verir (sequential)
  if (needsArchitect) {
    const architectPrompt = await loadPrompt(AGENTS_DIR, "architect");
    const architectContext = [
      context,
      results.backend ? `## Backend Analizi\n${results.backend}` : "",
      results.frontend ? `## Frontend Analizi\n${results.frontend}` : "",
    ]
      .filter(Boolean)
      .join("\n\n");

    results.architect = await callAgent({
      agentName: "Architect Agent",
      systemPrompt: architectPrompt,
      userMessage: task,
      context: architectContext,
    });
  }

  // QA — her şeyin sonunda çalışır
  if (needsQA) {
    const qaPrompt = await loadPrompt(AGENTS_DIR, "qa-reviewer");
    const qaContext = [
      context,
      results.backend ? `## Backend Analizi\n${results.backend}` : "",
      results.frontend ? `## Frontend Analizi\n${results.frontend}` : "",
      results.architect ? `## Architect Değerlendirmesi\n${results.architect}` : "",
    ]
      .filter(Boolean)
      .join("\n\n");

    results.qa = await callAgent({
      agentName: "QA Reviewer",
      systemPrompt: qaPrompt,
      userMessage: task,
      context: qaContext,
    });
  }

  return results;
}

// ─── Sonuç birleştirme ────────────────────────────────────────────────────────

function mergeResults(results) {
  const sections = [];

  sections.push("# Multi-Agent Sonuç Raporu");
  sections.push(`*Oluşturulma: ${new Date().toLocaleString("tr-TR")}*\n`);

  if (results.manager) {
    sections.push("---\n## 📋 Manager Analizi\n");
    sections.push(results.manager);
  }

  if (results.backend) {
    sections.push("---\n## ⚙️ Backend Agent\n");
    sections.push(results.backend);
  }

  if (results.frontend) {
    sections.push("---\n## 🖥️ Frontend Agent\n");
    sections.push(results.frontend);
  }

  if (results.architect) {
    sections.push("---\n## 🏗️ Architect Agent\n");
    sections.push(results.architect);
  }

  if (results.qa) {
    sections.push("---\n## ✅ QA Reviewer\n");
    sections.push(results.qa);
  }

  return sections.join("\n\n");
}

// ─── Çıktı kaydetme ───────────────────────────────────────────────────────────

async function saveOutput(content, workflow) {
  await fs.mkdir(OUTPUTS_DIR, { recursive: true });

  const timestamp = new Date()
    .toISOString()
    .replace(/[:.]/g, "-")
    .slice(0, 19);
  const fileName = `${workflow}_${timestamp}.md`;
  const filePath = path.join(OUTPUTS_DIR, fileName);

  await fs.writeFile(filePath, content, "utf-8");
  return filePath;
}

// ─── Ana fonksiyon ────────────────────────────────────────────────────────────

export async function run(task, workflowName = "plan-feature") {
  console.log("\n╔══════════════════════════════════════╗");
  console.log("║     AI Orchestrator — Başlatılıyor    ║");
  console.log("╚══════════════════════════════════════╝");
  console.log(`\nWorkflow : ${workflowName}`);
  console.log(`Görev    : ${task.slice(0, 80)}${task.length > 80 ? "..." : ""}\n`);

  // Workflow prompt'unu yükle
  const workflowPrompt = await loadPrompt(WORKFLOWS_DIR, workflowName);

  // 1. Manager çalışır
  console.log("── Aşama 1: Manager Analizi ──");
  const managerOutput = await runManager(task, workflowPrompt);

  // 2. Seçilen agent'lar çalışır
  console.log("\n── Aşama 2: Agent Çalıştırma ──");
  const results = await runSelectedAgents(managerOutput, task);

  // 3. Sonuçları birleştir
  const merged = mergeResults(results);

  // 4. Kaydet
  const outputPath = await saveOutput(merged, workflowName);

  console.log("\n╔══════════════════════════════════════╗");
  console.log("║            Tamamlandı ✓               ║");
  console.log("╚══════════════════════════════════════╝");
  console.log(`\nRapor kaydedildi: ${outputPath}\n`);

  return { results, merged, outputPath };
}
