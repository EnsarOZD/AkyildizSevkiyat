#!/usr/bin/env node
/**
 * Akyıldız — Marka Renk Codemod'u
 * ================================
 * Tüm `client/src` ağacındaki Tailwind aksan renklerini yeni kurumsal kimliğe çeker.
 * Bir kez çalıştır → bütün sayfalar/bileşenler/modallar tek seferde hizalanır.
 *
 * Kurallar (yalnızca AKSAN renkleri; SEMANTİK renkler korunur):
 *   indigo-*  → blue-*     (saf aksan → marka mavisi #2563eb)
 *   teal-*    → blue-*     (Nakliye/şehir-dışı aksanı → marka mavisi)
 *   purple-*  → violet-*   (Kıyafet kategorisi → daha yumuşak, ama hâlâ ayırt edilir)
 *   fuchsia-* → violet-*
 *
 * KORUNANLAR (dokunulmaz — anlam taşır):
 *   green/emerald = başarı/teslim · red = yıkıcı/iptal · amber/yellow = uyarı/bekliyor
 *   orange = iade/gecikme · sky/blue = bilgi/birincil · gray/slate = nötr
 *
 * Kullanım:
 *   node recolor.mjs            # client/src altını günceller (varsayılan kök)
 *   node recolor.mjs ./src      # özel kök
 *   node recolor.mjs --dry      # yazmadan sadece raporla
 *
 * Idempotent: ikinci çalıştırma güvenli (no-op).
 */

import { readdir, readFile, writeFile } from 'node:fs/promises';
import { join, extname } from 'node:path';

const args = process.argv.slice(2);
const DRY = args.includes('--dry');
const ROOT = args.find(a => !a.startsWith('--')) ?? 'client/src';

// Sıra: \b...-(\d{2,3}) tüm tonları (50..950) yakalar; /opacity son eki korunur
// (örn. indigo-900/40 → blue-900/40).
const RULES = [
  [/\bpurple-(\d{2,3})\b/g,  'violet-'],
  [/\bfuchsia-(\d{2,3})\b/g, 'violet-'],
  [/\bindigo-(\d{2,3})\b/g,  'blue-'],
  [/\bteal-(\d{2,3})\b/g,    'blue-'],
];

const EXTS = new Set(['.vue', '.ts', '.tsx', '.js', '.jsx', '.css']);
const SKIP_DIRS = new Set(['node_modules', 'dist', '.git', '.nuxt', '.output']);

async function* walk(dir) {
  for (const entry of await readdir(dir, { withFileTypes: true })) {
    const p = join(dir, entry.name);
    if (entry.isDirectory()) {
      if (!SKIP_DIRS.has(entry.name)) yield* walk(p);
    } else if (EXTS.has(extname(entry.name))) {
      yield p;
    }
  }
}

let filesChanged = 0, totalReplacements = 0;
const report = [];

for await (const file of walk(ROOT)) {
  const src = await readFile(file, 'utf8');
  let out = src, count = 0;
  for (const [re, prefix] of RULES) {
    out = out.replace(re, (_full, shade) => { count++; return prefix + shade; });
  }
  if (out !== src) {
    filesChanged++;
    totalReplacements += count;
    report.push(`  ${file}  (${count})`);
    if (!DRY) await writeFile(file, out, 'utf8');
  }
}

console.log(`\nAkyıldız renk codemod'u ${DRY ? '(DRY RUN) ' : ''}— kök: ${ROOT}`);
console.log(report.join('\n') || '  (değişiklik yok)');
console.log(`\n${filesChanged} dosya · ${totalReplacements} değişiklik${DRY ? ' (yazılmadı)' : ' yazıldı'}.\n`);
