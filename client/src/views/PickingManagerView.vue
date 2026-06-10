<template>
  <div class="p-4 max-w-6xl mx-auto space-y-4">
    <PageHeader title="Kıyafet Toplama — Yönetici Panosu">
      <template #description>Grup yönetimi, atama kuyruğu, sıralama ve toplama takibi.</template>
    </PageHeader>

    <!-- Üst bar: print-agent sağlık + yenile -->
    <div class="flex flex-wrap items-center gap-3">
      <span class="inline-flex items-center gap-1.5 px-3 py-1.5 rounded-lg text-sm font-semibold"
            :class="agentHealth.ok ? 'bg-emerald-50 text-emerald-700 dark:bg-emerald-900/30 dark:text-emerald-300'
                                   : 'bg-red-50 text-red-700 dark:bg-red-900/30 dark:text-red-300'">
        <span class="w-2 h-2 rounded-full" :class="agentHealth.ok ? 'bg-emerald-500' : 'bg-red-500'"></span>
        Yazıcı Ajanı: {{ agentHealth.text }}
      </span>
      <button @click="loadAll" class="px-3 py-1.5 text-sm font-medium bg-gray-100 dark:bg-gray-700 rounded-lg">Yenile</button>
    </div>

    <!-- Grup yönetimi -->
    <details class="bg-white dark:bg-gray-900 rounded-xl border border-gray-200 dark:border-gray-700">
      <summary class="px-4 py-3 cursor-pointer font-semibold text-gray-800 dark:text-gray-200">Toplama Grupları</summary>
      <div class="p-4 pt-0 space-y-2">
        <div class="flex flex-wrap items-end gap-2 border-b border-gray-100 dark:border-gray-800 pb-3">
          <input v-model="newGroup.name" placeholder="Grup adı (ör. Güvenlik)" class="flex-1 min-w-[160px] border rounded px-3 py-2 text-sm dark:bg-gray-800 dark:border-gray-700" />
          <input v-model.number="newGroup.sortOrder" type="number" placeholder="Sıra" class="w-20 border rounded px-2 py-2 text-sm dark:bg-gray-800 dark:border-gray-700" />
          <button @click="saveGroup(null)" :disabled="!newGroup.name.trim()" class="px-4 py-2 bg-indigo-600 hover:bg-indigo-700 disabled:opacity-50 text-white font-semibold rounded-lg text-sm">Ekle</button>
        </div>
        <div v-for="g in groups" :key="g.id" class="flex items-center gap-2" :class="{ 'opacity-50': !g.isActive }">
          <input v-model="g.name" class="flex-1 border rounded px-2 py-1.5 text-sm dark:bg-gray-800 dark:border-gray-700" />
          <input v-model.number="g.sortOrder" type="number" class="w-20 border rounded px-2 py-1.5 text-sm dark:bg-gray-800 dark:border-gray-700" />
          <button @click="saveGroup(g)" class="px-3 py-1.5 text-xs font-bold text-indigo-600 bg-indigo-50 dark:bg-indigo-900/30 rounded">Kaydet</button>
          <button v-if="g.isActive" @click="deactivateGroup(g)" class="px-2 py-1.5 text-xs font-bold text-red-600 bg-red-50 dark:bg-red-900/20 rounded">Pasifleştir</button>
          <span v-else class="text-[11px] text-gray-400">pasif</span>
        </div>
      </div>
    </details>

    <!-- Araba (Container) yönetimi -->
    <details class="bg-white dark:bg-gray-900 rounded-xl border border-gray-200 dark:border-gray-700">
      <summary class="px-4 py-3 cursor-pointer font-semibold text-gray-800 dark:text-gray-200">Arabalar / Paletler ({{ containers.length }})</summary>
      <div class="p-4 pt-0 space-y-2">
        <div class="flex flex-wrap items-end gap-2 border-b border-gray-100 dark:border-gray-800 pb-3">
          <input v-model="newContainer.code" placeholder="Kod (QR), ör. ARB-03" class="flex-1 min-w-[140px] border rounded px-3 py-2 text-sm dark:bg-gray-800 dark:border-gray-700" />
          <select v-model.number="newContainer.type" class="border rounded px-2 py-2 text-sm dark:bg-gray-800 dark:border-gray-700">
            <option :value="0">Araba</option>
            <option :value="1">Palet</option>
          </select>
          <button @click="saveContainer(null)" :disabled="!newContainer.code.trim()" class="px-4 py-2 bg-indigo-600 hover:bg-indigo-700 disabled:opacity-50 text-white font-semibold rounded-lg text-sm">Ekle</button>
        </div>
        <!-- Etiket baskı çubuğu -->
        <div class="flex flex-wrap items-center gap-2 pb-1">
          <button @click="printLabels" class="px-3 py-1.5 text-sm font-semibold bg-gray-900 hover:bg-gray-700 text-white rounded-lg">🏷 Etiketleri Yazdır</button>
          <span class="text-xs text-gray-400">{{ selectedContainers.size > 0 ? `${selectedContainers.size} seçili` : 'Seçim yoksa tüm aktif arabalar' }}</span>
        </div>
        <div v-for="c in containers" :key="c.id" class="flex items-center gap-2" :class="{ 'opacity-50': !c.isActive }">
          <input type="checkbox" :checked="selectedContainers.has(c.id)" :disabled="!c.isActive" @change="toggleSelect(c.id)" class="shrink-0 accent-indigo-600 disabled:opacity-30" title="Etikete dahil et" />
          <input v-model="c.code" class="flex-1 border rounded px-2 py-1.5 text-sm dark:bg-gray-800 dark:border-gray-700" />
          <select v-model.number="c.type" class="border rounded px-2 py-1.5 text-sm dark:bg-gray-800 dark:border-gray-700">
            <option :value="0">Araba</option>
            <option :value="1">Palet</option>
          </select>
          <button @click="saveContainer(c)" class="px-3 py-1.5 text-xs font-bold text-indigo-600 bg-indigo-50 dark:bg-indigo-900/30 rounded">Kaydet</button>
          <button v-if="c.isActive" @click="deactivateContainer(c)" class="px-2 py-1.5 text-xs font-bold text-red-600 bg-red-50 dark:bg-red-900/20 rounded">Pasifleştir</button>
          <span v-else class="text-[11px] text-gray-400">pasif</span>
        </div>
      </div>
    </details>

    <div v-if="loading" class="text-center py-12 text-gray-400 text-sm">Yükleniyor...</div>

    <!-- Board: gruplar + gruplandırılmamış -->
    <div v-else class="space-y-4">
      <div v-for="col in columns" :key="col.key" class="bg-white dark:bg-gray-900 rounded-xl border border-gray-200 dark:border-gray-700">
        <div class="px-4 py-2 border-b border-gray-100 dark:border-gray-800 font-semibold text-gray-800 dark:text-gray-200 flex items-center justify-between">
          <span>{{ col.name }} <span class="text-gray-400 font-normal">({{ col.items.length }})</span></span>
        </div>
        <div v-if="col.items.length === 0" class="px-4 py-3 text-xs text-gray-400">Boş.</div>
        <div v-else class="divide-y divide-gray-100 dark:divide-gray-800">
          <div v-for="(it, idx) in col.items" :key="it.shipmentId">
          <div class="p-3 flex flex-wrap items-center gap-2">
            <!-- sıralama -->
            <div class="flex flex-col">
              <button @click="move(col, idx, -1)" :disabled="idx === 0" class="text-gray-400 disabled:opacity-30 leading-none">▲</button>
              <button @click="move(col, idx, 1)" :disabled="idx === col.items.length - 1" class="text-gray-400 disabled:opacity-30 leading-none">▼</button>
            </div>
            <!-- detay aç/kapa -->
            <button @click="toggleDetail(it)" class="text-gray-400 hover:text-indigo-600 transition-transform" :class="{ 'rotate-90': expanded.has(it.shipmentId) }" title="Satır detayı">▶</button>
            <div class="flex-1 min-w-[200px] cursor-pointer" @click="toggleDetail(it)">
              <div class="flex flex-wrap items-center gap-1.5">
                <span class="font-semibold text-gray-900 dark:text-gray-100">{{ it.externalOrderNumber || ('#' + it.shipmentId) }}</span>
                <span v-if="it.talepNo" class="text-[10px] font-bold bg-indigo-50 dark:bg-indigo-900/30 text-indigo-700 dark:text-indigo-300 px-1.5 py-0.5 rounded">T:{{ it.talepNo }}</span>
                <span class="px-1.5 py-0.5 rounded text-[10px] font-bold" :class="it.status === 'Picking' ? 'bg-yellow-100 text-yellow-800' : 'bg-gray-100 text-gray-700'">{{ it.status === 'Picking' ? 'Hazırlanıyor' : 'Bekliyor' }}</span>
                <span v-if="it.pickingMode != null" class="px-1.5 py-0.5 rounded text-[10px] font-bold bg-purple-100 text-purple-700">{{ modeLabel(it.pickingMode) }}</span>
                <span v-if="it.claimedOutOfOrder" class="px-1.5 py-0.5 rounded text-[10px] font-bold bg-orange-100 text-orange-700" title="Sıra atlanarak alındı">⚠ sıra atladı</span>
                <span v-if="it.paused" class="px-1.5 py-0.5 rounded text-[10px] font-bold bg-amber-100 text-amber-800">⏸ duraklatıldı</span>
                <span v-if="it.pickingCompleted" class="px-1.5 py-0.5 rounded text-[10px] font-bold bg-emerald-100 text-emerald-700">toplama bitti</span>
                <span v-if="it.openContainerCount > 0" class="px-1.5 py-0.5 rounded text-[10px] font-bold bg-sky-100 text-sky-700">🛒 {{ it.openContainerCount }}</span>
              </div>
              <div class="text-xs text-gray-500 dark:text-gray-400 mt-0.5">
                {{ it.projectCode }} — {{ it.projectName }} · {{ it.lineCount }} kalem
                <span v-if="it.assignedPickerName" class="text-emerald-600 dark:text-emerald-400"> · 👷 {{ it.assignedPickerName }}</span>
                <span v-if="it.reservedForUserId" class="text-indigo-600 dark:text-indigo-400"> · 🔒 {{ userName(it.reservedForUserId) }}</span>
              </div>
            </div>

            <!-- aksiyonlar -->
            <div class="flex flex-wrap items-center gap-1.5">
              <select :value="it.pickingGroupId ?? ''" @change="onMoveGroup(it, $event)" class="text-xs border rounded px-1.5 py-1 dark:bg-gray-800 dark:border-gray-700" title="Gruba taşı">
                <option value="">Gruplandırılmamış</option>
                <option v-for="g in activeGroups" :key="g.id" :value="g.id">{{ g.name }}</option>
              </select>
              <select v-if="it.status !== 'Picking'" :value="it.reservedForUserId ?? ''" @change="onReserve(it, $event)" class="text-xs border rounded px-1.5 py-1 dark:bg-gray-800 dark:border-gray-700" title="Rezerve et">
                <option value="">Rezerve yok</option>
                <option v-for="u in users" :key="u.id" :value="u.id">{{ u.firstName }} {{ u.lastName }}</option>
              </select>
              <template v-if="it.status === 'Picking'">
                <button v-if="!it.paused" @click="doPause(it)" class="px-2 py-1 text-xs font-bold text-amber-700 bg-amber-50 rounded">Duraklat</button>
                <button v-else @click="doResume(it)" class="px-2 py-1 text-xs font-bold text-emerald-700 bg-emerald-50 rounded">Devam</button>
                <button @click="askUnclaim(it)" class="px-2 py-1 text-xs font-bold text-red-700 bg-red-50 rounded">Bırak</button>
              </template>
            </div>
          </div>

          <!-- Satır detayı (lazy-load) -->
          <div v-if="expanded.has(it.shipmentId)" class="px-3 pb-3 bg-gray-50 dark:bg-gray-800/40">
            <div v-if="!details[it.shipmentId]" class="py-3 text-xs text-gray-400">Yükleniyor...</div>
            <div v-else class="space-y-2">
              <!-- Toplayıcı / kapamacı / koli bilgisi -->
              <div class="flex flex-wrap gap-1.5 text-[11px]">
                <span v-if="details[it.shipmentId]!.assignedPickerName" class="px-1.5 py-0.5 rounded bg-emerald-50 dark:bg-emerald-900/30 text-emerald-700 dark:text-emerald-300 font-semibold">👷 Toplayıcı: {{ details[it.shipmentId]!.assignedPickerName }}</span>
                <span v-if="details[it.shipmentId]!.preparedByUserName" class="px-1.5 py-0.5 rounded bg-sky-50 dark:bg-sky-900/30 text-sky-700 dark:text-sky-300 font-semibold">✓ Toplamayı bitiren: {{ details[it.shipmentId]!.preparedByUserName }}</span>
                <span v-if="details[it.shipmentId]!.closedByUserName" class="px-1.5 py-0.5 rounded bg-violet-50 dark:bg-violet-900/30 text-violet-700 dark:text-violet-300 font-semibold">📦 Kapayan: {{ details[it.shipmentId]!.closedByUserName }}</span>
                <span v-if="details[it.shipmentId]!.boxCount != null" class="px-1.5 py-0.5 rounded bg-amber-50 dark:bg-amber-900/30 text-amber-800 dark:text-amber-300 font-semibold">{{ details[it.shipmentId]!.boxCount }} {{ packageLabel(details[it.shipmentId]!.packageType) }}</span>
                <span v-if="details[it.shipmentId]!.labelPrinted" class="px-1.5 py-0.5 rounded bg-gray-200 dark:bg-gray-700 text-gray-600 dark:text-gray-300 font-semibold">🏷 etiket basıldı</span>
                <span v-for="code in details[it.shipmentId]!.openContainerCodes" :key="code" class="px-1.5 py-0.5 rounded bg-sky-100 dark:bg-sky-900/30 text-sky-700 dark:text-sky-300 font-semibold">🛒 {{ code }}</span>
              </div>
              <!-- Satırlar -->
              <div class="overflow-x-auto rounded-lg border border-gray-200 dark:border-gray-700">
                <table class="w-full text-xs">
                  <thead class="bg-gray-100 dark:bg-gray-800 text-gray-500 dark:text-gray-400">
                    <tr>
                      <th class="text-left px-2 py-1.5 font-semibold">Stok</th>
                      <th class="text-left px-2 py-1.5 font-semibold">Tür</th>
                      <th class="text-right px-2 py-1.5 font-semibold">Sipariş</th>
                      <th class="text-right px-2 py-1.5 font-semibold">Toplanan</th>
                      <th class="text-right px-2 py-1.5 font-semibold">Fark</th>
                      <th class="text-left px-2 py-1.5 font-semibold">Sebep</th>
                    </tr>
                  </thead>
                  <tbody class="divide-y divide-gray-100 dark:divide-gray-800">
                    <tr v-for="ln in details[it.shipmentId]!.lines" :key="ln.lineId" class="bg-white dark:bg-gray-900">
                      <td class="px-2 py-1.5">
                        <span class="font-mono text-gray-500">{{ ln.stockCode }}</span> · {{ ln.stockName }}
                      </td>
                      <td class="px-2 py-1.5">
                        <span v-if="ln.clothingType != null" class="px-1.5 py-0.5 rounded text-[10px] font-bold"
                              :class="ln.clothingType === 1 ? 'bg-orange-100 text-orange-700' : 'bg-gray-100 text-gray-600'">{{ clothingLabel(ln.clothingType) }}</span>
                        <span v-else class="text-gray-300">—</span>
                      </td>
                      <td class="px-2 py-1.5 text-right tabular-nums">{{ ln.orderedQty }} {{ ln.unit }}</td>
                      <td class="px-2 py-1.5 text-right tabular-nums">{{ ln.deliveredQty }}</td>
                      <td class="px-2 py-1.5 text-right tabular-nums font-semibold"
                          :class="ln.differenceQty < 0 ? 'text-red-600' : ln.differenceQty > 0 ? 'text-amber-600' : 'text-gray-400'">
                        {{ ln.differenceQty > 0 ? '+' : '' }}{{ ln.differenceQty }}
                      </td>
                      <td class="px-2 py-1.5 text-gray-500">{{ ln.differenceReason || '—' }}</td>
                    </tr>
                  </tbody>
                </table>
              </div>
            </div>
          </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Bırakma (unclaim) sebep modalı -->
    <div v-if="unclaimTarget" class="fixed inset-0 bg-black/50 flex items-center justify-center p-4 z-50">
      <div class="bg-white dark:bg-gray-900 rounded-xl p-4 w-full max-w-sm space-y-3">
        <h3 class="font-bold text-gray-900 dark:text-gray-100">İşi Bırak</h3>
        <p class="text-xs text-gray-500">#{{ unclaimTarget.shipmentId }} — sebep zorunlu (kısmi toplama silinmez).</p>
        <textarea v-model="unclaimReason" rows="3" class="w-full border rounded px-2 py-1.5 text-sm dark:bg-gray-800 dark:border-gray-700" placeholder="Sebep..."></textarea>
        <div class="flex gap-2">
          <button @click="unclaimTarget = null" class="flex-1 py-2 bg-gray-100 dark:bg-gray-700 rounded-lg text-sm font-bold">Vazgeç</button>
          <button @click="confirmUnclaim" :disabled="!unclaimReason.trim()" class="flex-1 py-2 bg-red-600 disabled:opacity-50 text-white rounded-lg text-sm font-bold">Bırak</button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import { useRouter } from 'vue-router';
import PageHeader from '../components/PageHeader.vue';
import clothingPickingService, { type PickingOverviewItem, type PickingDetail, PickingModeLabels, ClothingTypeLabels, PackageTypeLabels } from '../services/clothingPickingService';
import pickingGroupService, { type PickingGroup } from '../services/pickingGroupService';
import containerService, { type Container } from '../services/containerService';
import printService from '../services/printService';
import userService, { type UserListItem } from '../services/userService';
import { useNotificationStore } from '../stores/notification';
import { ApiErrorUtils } from '../utils/apiError';

const notify = useNotificationStore();
const router = useRouter();
const loading = ref(false);
const groups = ref<PickingGroup[]>([]);
const items = ref<PickingOverviewItem[]>([]);
const users = ref<UserListItem[]>([]);
const newGroup = ref({ name: '', sortOrder: 0 });
const containers = ref<Container[]>([]);
const newContainer = ref({ code: '', type: 0 });
const agentHealth = ref<{ ok: boolean; text: string }>({ ok: false, text: '—' });

const unclaimTarget = ref<PickingOverviewItem | null>(null);
const unclaimReason = ref('');

// Satır detayı (lazy-load): açık satırlar + yüklenen detay önbelleği
const expanded = ref<Set<number>>(new Set());
const details = ref<Record<number, PickingDetail>>({});
const clothingLabel = (t: number) => ClothingTypeLabels[t] ?? '?';
const packageLabel = (t?: number | null) => (t != null ? PackageTypeLabels[t] : null) ?? 'Koli';

async function toggleDetail(it: PickingOverviewItem) {
  const id = it.shipmentId;
  const next = new Set(expanded.value);
  if (next.has(id)) {
    next.delete(id);
    expanded.value = next;
    return;
  }
  next.add(id);
  expanded.value = next;
  if (!details.value[id]) {
    try {
      details.value[id] = await clothingPickingService.detail(id);
    } catch (e) {
      expanded.value = new Set([...expanded.value].filter(x => x !== id));
      notify.add(ApiErrorUtils.getErrorMessage(e) || 'Detay yüklenemedi.', 'error');
    }
  }
}

const activeGroups = computed(() => groups.value.filter(g => g.isActive));
const modeLabel = (m: number) => PickingModeLabels[m] ?? '?';
const userName = (id: number) => { const u = users.value.find(x => x.id === id); return u ? `${u.firstName} ${u.lastName}` : '?'; };

const columns = computed(() => {
  const cols: { key: string; name: string; groupId: number | null; items: PickingOverviewItem[] }[] = [];
  for (const g of activeGroups.value)
    cols.push({ key: 'g' + g.id, name: g.name, groupId: g.id, items: items.value.filter(i => i.pickingGroupId === g.id) });
  cols.push({ key: 'none', name: 'Gruplandırılmamış', groupId: null, items: items.value.filter(i => i.pickingGroupId == null) });
  return cols;
});

async function loadAll() {
  loading.value = true;
  try {
    const [ov, us, cs] = await Promise.all([clothingPickingService.overview(), userService.getAll(), containerService.getAll(false)]);
    groups.value = ov.groups;
    items.value = ov.items;
    users.value = us.filter(u => u.isActive);
    containers.value = cs;
    await loadAgentHealth();
  } catch (e) {
    notify.add(ApiErrorUtils.getErrorMessage(e) || 'Yüklenemedi.', 'error');
  } finally {
    loading.value = false;
  }
}

async function loadAgentHealth() {
  try {
    const [agents, jobsRes] = await Promise.all([printService.agents(), printService.jobs(1, 20)]);
    const now = Date.now();
    const online = agents.filter(a => now - new Date(a.lastSeenAt).getTime() < 5 * 60 * 1000);
    const jobs = Array.isArray(jobsRes) ? jobsRes : (jobsRes.items ?? []);
    const failed = jobs.filter((j: any) => j.status === 3).length;
    if (online.length === 0) agentHealth.value = { ok: false, text: 'çevrimdışı' };
    else if (failed > 0) agentHealth.value = { ok: false, text: `${online.length} aktif · ${failed} başarısız iş` };
    else agentHealth.value = { ok: true, text: `${online.length} aktif` };
  } catch {
    agentHealth.value = { ok: false, text: 'bilinmiyor' };
  }
}

async function saveGroup(g: PickingGroup | null) {
  try {
    if (g) await pickingGroupService.save({ id: g.id, name: g.name.trim(), sortOrder: g.sortOrder, isActive: g.isActive });
    else {
      await pickingGroupService.save({ name: newGroup.value.name.trim(), sortOrder: newGroup.value.sortOrder, isActive: true });
      newGroup.value = { name: '', sortOrder: 0 };
    }
    await loadAll();
  } catch (e) { notify.add(ApiErrorUtils.getErrorMessage(e) || 'Kaydedilemedi.', 'error'); }
}

async function deactivateGroup(g: PickingGroup) {
  if (!await notify.promptConfirm({ title: 'Pasifleştir', message: `"${g.name}" pasifleştirilsin mi?`, confirmText: 'Pasifleştir' })) return;
  try { await pickingGroupService.deactivate(g.id); await loadAll(); }
  catch (e) { notify.add(ApiErrorUtils.getErrorMessage(e) || 'Hata.', 'error'); }
}

async function saveContainer(c: Container | null) {
  try {
    if (c) await containerService.save({ id: c.id, code: c.code.trim(), type: c.type, isActive: c.isActive });
    else {
      await containerService.save({ code: newContainer.value.code.trim(), type: newContainer.value.type, isActive: true });
      newContainer.value = { code: '', type: 0 };
    }
    await loadAll();
  } catch (e) { notify.add(ApiErrorUtils.getErrorMessage(e) || 'Kaydedilemedi.', 'error'); }
}

async function deactivateContainer(c: Container) {
  if (!await notify.promptConfirm({ title: 'Pasifleştir', message: `"${c.code}" pasifleştirilsin mi?`, confirmText: 'Pasifleştir' })) return;
  try { await containerService.deactivate(c.id); await loadAll(); }
  catch (e) { notify.add(ApiErrorUtils.getErrorMessage(e) || 'Hata.', 'error'); }
}

// Etiket baskısı — seçili (ya da tüm aktif) arabalar için yazdırılabilir sayfa
const selectedContainers = ref<Set<number>>(new Set());
function toggleSelect(id: number) {
  const next = new Set(selectedContainers.value);
  next.has(id) ? next.delete(id) : next.add(id);
  selectedContainers.value = next;
}
function printLabels() {
  const ids = [...selectedContainers.value];
  const route = router.resolve({ name: 'ContainerLabelPrint', query: ids.length ? { ids: ids.join(',') } : {} });
  window.open(route.href, '_blank');
}

async function onMoveGroup(it: PickingOverviewItem, ev: Event) {
  const val = (ev.target as HTMLSelectElement).value;
  const gid = val === '' ? null : Number(val);
  try { await clothingPickingService.assignGroup([it.shipmentId], gid); await loadAll(); }
  catch (e) { notify.add(ApiErrorUtils.getErrorMessage(e) || 'Taşınamadı.', 'error'); }
}

async function onReserve(it: PickingOverviewItem, ev: Event) {
  const val = (ev.target as HTMLSelectElement).value;
  const uid = val === '' ? null : Number(val);
  try { await clothingPickingService.reserve(it.shipmentId, uid); await loadAll(); }
  catch (e) { notify.add(ApiErrorUtils.getErrorMessage(e) || 'Rezerve edilemedi.', 'error'); }
}

async function move(col: { items: PickingOverviewItem[] }, idx: number, dir: number) {
  const arr = [...col.items];
  const j = idx + dir;
  if (j < 0 || j >= arr.length) return;
  const tmp = arr[idx]!; arr[idx] = arr[j]!; arr[j] = tmp;
  try { await clothingPickingService.reorder(arr.map(x => x.shipmentId)); await loadAll(); }
  catch (e) { notify.add(ApiErrorUtils.getErrorMessage(e) || 'Sıralanamadı.', 'error'); }
}

async function doPause(it: PickingOverviewItem) {
  try { await clothingPickingService.pause(it.shipmentId); await loadAll(); }
  catch (e) { notify.add(ApiErrorUtils.getErrorMessage(e) || 'Hata.', 'error'); }
}
async function doResume(it: PickingOverviewItem) {
  try { await clothingPickingService.resume(it.shipmentId); await loadAll(); }
  catch (e) { notify.add(ApiErrorUtils.getErrorMessage(e) || 'Hata.', 'error'); }
}

function askUnclaim(it: PickingOverviewItem) { unclaimTarget.value = it; unclaimReason.value = ''; }
async function confirmUnclaim() {
  if (!unclaimTarget.value || !unclaimReason.value.trim()) return;
  try {
    await clothingPickingService.unclaim(unclaimTarget.value.shipmentId, unclaimReason.value.trim());
    unclaimTarget.value = null;
    await loadAll();
  } catch (e) { notify.add(ApiErrorUtils.getErrorMessage(e) || 'Bırakılamadı.', 'error'); }
}

onMounted(loadAll);
</script>
