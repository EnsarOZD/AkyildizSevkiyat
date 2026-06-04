<template>
  <div class="space-y-4">

    <!-- Header row -->
    <div class="flex items-center justify-between">
      <p class="text-sm text-gray-500 dark:text-gray-400">
        Açık şoför seferleri ve takılı kalmış sevkiyatlar
      </p>
      <button
        @click="load"
        :disabled="loading"
        class="flex items-center gap-1.5 px-3 py-1.5 text-xs font-medium text-blue-600 dark:text-blue-400 bg-blue-50 dark:bg-blue-900/20 hover:bg-blue-100 dark:hover:bg-blue-900/30 rounded-lg transition-colors disabled:opacity-50"
      >
        <ArrowPathIcon class="w-3.5 h-3.5" :class="loading ? 'animate-spin' : ''" />
        Yenile
      </button>
    </div>

    <!-- Loading -->
    <div v-if="loading && sessions.length === 0" class="flex justify-center py-16">
      <div class="w-7 h-7 border-4 border-blue-600 border-t-transparent rounded-full animate-spin"></div>
    </div>

    <!-- Error -->
    <div
      v-else-if="error"
      class="bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-700 rounded-xl p-4 text-red-700 dark:text-red-400 text-sm"
    >{{ error }}</div>

    <!-- Empty state -->
    <div
      v-else-if="!loading && sessions.length === 0"
      class="text-center py-20"
    >
      <CheckCircleIcon class="w-14 h-14 mx-auto text-green-300 dark:text-green-700 mb-3" />
      <p class="font-semibold text-gray-600 dark:text-gray-300">Açık sefer yok</p>
      <p class="text-sm text-gray-400 dark:text-gray-500 mt-1">Şu an aktif sürüş yapan şoför bulunmuyor.</p>
    </div>

    <!-- Session cards -->
    <div v-else class="space-y-4">
      <div
        v-for="session in sessions"
        :key="session.sessionId"
        class="bg-white dark:bg-[#0f2744] rounded-2xl border border-gray-100 dark:border-white/10 shadow-sm overflow-hidden"
      >
        <!-- Session header (accordion toggle) -->
        <div
          @click="toggle(session.sessionId)"
          class="px-4 pt-4 pb-3 border-b border-gray-100 dark:border-white/10 flex items-start justify-between gap-3 cursor-pointer select-none hover:bg-gray-50 dark:hover:bg-white/5 transition-colors"
        >
          <div class="flex items-start gap-2 min-w-0">
            <ChevronDownIcon
              class="w-4 h-4 mt-1 text-gray-400 transition-transform flex-shrink-0"
              :class="{ 'rotate-180': isExpanded(session.sessionId) }"
            />
            <div class="min-w-0">
              <div class="flex items-center gap-2">
                <span class="w-2 h-2 rounded-full bg-green-400 animate-pulse flex-shrink-0"></span>
                <p class="font-semibold text-gray-900 dark:text-white truncate">{{ session.driverFullName }}</p>
              </div>
              <div class="flex items-center flex-wrap gap-3 mt-1 text-xs text-gray-500 dark:text-gray-400">
                <span class="flex items-center gap-1">
                  <TruckIcon class="w-3.5 h-3.5" />
                  {{ session.plateNumber }}
                </span>
                <span class="flex items-center gap-1">
                  <ClockIcon class="w-3.5 h-3.5" />
                  {{ formatTime(session.startTime) }}
                </span>
                <span
                  class="px-1.5 py-0.5 rounded font-mono"
                  :class="session.elapsedMinutes > 480
                    ? 'bg-red-100 dark:bg-red-900/30 text-red-600 dark:text-red-400'
                    : 'bg-gray-100 dark:bg-white/10 text-gray-600 dark:text-gray-400'"
                >{{ formatElapsed(session.elapsedMinutes) }}</span>
                <span class="px-1.5 py-0.5 rounded-full bg-green-100 dark:bg-green-900/30 text-green-700 dark:text-green-400 font-medium">
                  {{ session.deliveredProjects }}/{{ session.totalProjects }} teslim
                </span>
              </div>
            </div>
          </div>
          <button
            @click.stop="openForceClose(session)"
            class="shrink-0 px-3 py-1.5 text-xs font-medium text-red-600 dark:text-red-400 bg-red-50 dark:bg-red-900/20 hover:bg-red-100 dark:hover:bg-red-900/30 rounded-lg transition-colors border border-red-200 dark:border-red-700/50"
          >
            Seferi Kapat
          </button>
        </div>

        <!-- Shipments section (genişletilince) -->
        <div v-show="isExpanded(session.sessionId)" class="p-4">
          <div v-if="session.shipments.length === 0" class="text-sm text-gray-400 dark:text-gray-500 italic">
            Bu şoföre ait takılı sevkiyat yok.
          </div>
          <template v-else>
            <!-- Select all row -->
            <div class="flex items-center justify-between mb-3">
              <label class="flex items-center gap-2 text-xs font-medium text-gray-600 dark:text-gray-400 cursor-pointer select-none">
                <input
                  type="checkbox"
                  class="rounded border-gray-300 dark:border-gray-600 text-blue-600"
                  :checked="isAllSelected(session)"
                  :indeterminate="isSomeSelected(session) && !isAllSelected(session)"
                  @change="toggleAll(session)"
                />
                Tümünü seç ({{ points(session).length }} nokta · {{ session.shipments.length }} irsaliye)
              </label>
              <button
                v-if="selectedForSession(session.sessionId).length > 0"
                @click="openBulkDeliver(session)"
                class="px-3 py-1.5 text-xs font-semibold text-white bg-green-600 hover:bg-green-700 active:bg-green-800 rounded-lg transition-colors"
              >
                {{ selectedForSession(session.sessionId).length }} İrsaliyeyi Teslim Et
              </button>
            </div>

            <!-- Teslim noktası (proje) satırları -->
            <div class="space-y-1.5">
              <label
                v-for="pt in points(session)"
                :key="pt.projectId"
                class="flex items-center gap-3 p-2.5 rounded-xl border cursor-pointer select-none transition-colors"
                :class="isPointSelected(session.sessionId, pt)
                  ? 'bg-blue-50 dark:bg-blue-900/20 border-blue-200 dark:border-blue-700'
                  : 'bg-gray-50 dark:bg-white/5 border-gray-100 dark:border-white/5 hover:bg-gray-100 dark:hover:bg-white/10'"
              >
                <input
                  type="checkbox"
                  :checked="isPointSelected(session.sessionId, pt)"
                  :indeterminate="isPointPartiallySelected(session.sessionId, pt)"
                  @change="togglePoint(session.sessionId, pt)"
                  class="rounded border-gray-300 dark:border-gray-600 text-blue-600 flex-shrink-0"
                />
                <div class="flex-1 min-w-0">
                  <p class="text-sm font-medium text-gray-800 dark:text-gray-100 truncate">{{ pt.projectName }}</p>
                  <div class="flex items-center gap-2 mt-0.5">
                    <span class="text-xs text-gray-500 dark:text-gray-400">{{ pt.shipmentIds.length }} irsaliye</span>
                    <span class="text-xs text-gray-400">{{ pt.totalLines }} kalem</span>
                  </div>
                </div>
                <span
                  class="shrink-0 text-[11px] font-semibold px-2 py-0.5 rounded-full"
                  :class="pt.allDispatched
                    ? 'bg-blue-100 dark:bg-blue-900/30 text-blue-700 dark:text-blue-400'
                    : 'bg-amber-100 dark:bg-amber-900/30 text-amber-700 dark:text-amber-400'"
                >{{ pt.allDispatched ? 'Yolda' : 'Araçta' }}</span>
              </label>
            </div>
          </template>
        </div>
      </div>
    </div>

    <!-- Bulk Deliver Modal -->
    <Teleport to="body">
      <Transition
        enter-active-class="transition ease-out duration-200"
        enter-from-class="opacity-0"
        enter-to-class="opacity-100"
        leave-active-class="transition ease-in duration-150"
        leave-from-class="opacity-100"
        leave-to-class="opacity-0"
      >
        <div
          v-if="deliverModal.open"
          class="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4"
          @click.self="deliverModal.open = false"
        >
          <div class="w-full max-w-md bg-white dark:bg-[#0f2744] rounded-2xl shadow-2xl overflow-hidden">
            <div class="px-5 py-4 border-b border-gray-100 dark:border-white/10">
              <h3 class="font-bold text-gray-900 dark:text-white">Toplu Teslim Et</h3>
              <p class="text-xs text-gray-500 dark:text-gray-400 mt-0.5">
                {{ deliverModal.shipmentIds.length }} irsaliye teslim edildi olarak işaretlenecek.
              </p>
            </div>
            <div class="px-5 py-4 space-y-3">
              <div>
                <label class="block text-xs font-semibold text-gray-700 dark:text-gray-300 mb-1">
                  Teslim Alan <span class="text-red-500">*</span>
                </label>
                <input
                  v-model="deliverModal.recipient"
                  type="text"
                  placeholder="Teslim alan kişi / güvenlik adı"
                  class="w-full px-3 py-2 text-sm rounded-xl border border-gray-200 dark:border-white/20 bg-white dark:bg-white/5 text-gray-900 dark:text-white placeholder-gray-400 focus:outline-none focus:ring-2 focus:ring-blue-500"
                />
              </div>
              <div>
                <label class="block text-xs font-semibold text-gray-700 dark:text-gray-300 mb-1">
                  Açıklama / Not <span class="text-red-500">*</span>
                </label>
                <textarea
                  v-model="deliverModal.note"
                  rows="2"
                  placeholder="Yönetici müdahalesinin nedeni (ör: şoför paneli kullanılmadı)"
                  class="w-full px-3 py-2 text-sm rounded-xl border border-gray-200 dark:border-white/20 bg-white dark:bg-white/5 text-gray-900 dark:text-white placeholder-gray-400 focus:outline-none focus:ring-2 focus:ring-blue-500 resize-none"
                ></textarea>
              </div>
              <p v-if="deliverModal.error" class="text-xs text-red-500">{{ deliverModal.error }}</p>
            </div>
            <div class="px-5 pb-5 flex justify-end gap-2">
              <button
                @click="deliverModal.open = false"
                class="px-4 py-2 text-sm text-gray-600 dark:text-gray-400 hover:bg-gray-100 dark:hover:bg-white/10 rounded-xl transition-colors"
              >İptal</button>
              <button
                @click="confirmBulkDeliver"
                :disabled="deliverModal.submitting"
                class="px-4 py-2 text-sm font-semibold text-white bg-green-600 hover:bg-green-700 disabled:opacity-50 rounded-xl transition-colors flex items-center gap-2"
              >
                <span v-if="deliverModal.submitting" class="w-3.5 h-3.5 border-2 border-white border-t-transparent rounded-full animate-spin"></span>
                {{ deliverModal.submitting ? 'İşleniyor...' : 'Teslim Et' }}
              </button>
            </div>
          </div>
        </div>
      </Transition>
    </Teleport>

    <!-- Force Close Modal -->
    <Teleport to="body">
      <Transition
        enter-active-class="transition ease-out duration-200"
        enter-from-class="opacity-0"
        enter-to-class="opacity-100"
        leave-active-class="transition ease-in duration-150"
        leave-from-class="opacity-100"
        leave-to-class="opacity-0"
      >
        <div
          v-if="forceCloseModal.open"
          class="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4"
          @click.self="forceCloseModal.open = false"
        >
          <div class="w-full max-w-md bg-white dark:bg-[#0f2744] rounded-2xl shadow-2xl overflow-hidden">
            <div class="px-5 py-4 border-b border-gray-100 dark:border-white/10">
              <h3 class="font-bold text-gray-900 dark:text-white">Seferi Zorla Kapat</h3>
              <p class="text-xs text-gray-500 dark:text-gray-400 mt-0.5">
                {{ forceCloseModal.driverName }} — {{ forceCloseModal.plateNumber }}
              </p>
            </div>
            <div class="px-5 py-4">
              <label class="block text-xs font-semibold text-gray-700 dark:text-gray-300 mb-1">Not (opsiyonel)</label>
              <textarea
                v-model="forceCloseModal.notes"
                rows="2"
                placeholder="Kapatma nedeni..."
                class="w-full px-3 py-2 text-sm rounded-xl border border-gray-200 dark:border-white/20 bg-white dark:bg-white/5 text-gray-900 dark:text-white placeholder-gray-400 focus:outline-none focus:ring-2 focus:ring-blue-500 resize-none"
              ></textarea>
            </div>
            <div class="px-5 pb-5 flex justify-end gap-2">
              <button
                @click="forceCloseModal.open = false"
                class="px-4 py-2 text-sm text-gray-600 dark:text-gray-400 hover:bg-gray-100 dark:hover:bg-white/10 rounded-xl transition-colors"
              >İptal</button>
              <button
                @click="confirmForceClose"
                :disabled="forceCloseModal.submitting"
                class="px-4 py-2 text-sm font-semibold text-white bg-red-600 hover:bg-red-700 disabled:opacity-50 rounded-xl transition-colors flex items-center gap-2"
              >
                <span v-if="forceCloseModal.submitting" class="w-3.5 h-3.5 border-2 border-white border-t-transparent rounded-full animate-spin"></span>
                {{ forceCloseModal.submitting ? 'Kapatılıyor...' : 'Zorla Kapat' }}
              </button>
            </div>
          </div>
        </div>
      </Transition>
    </Teleport>

  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue';
import {
  ArrowPathIcon,
  TruckIcon,
  ClockIcon,
  CheckCircleIcon,
  ChevronDownIcon,
} from '@heroicons/vue/24/outline';
import adminService, { type ActiveSessionWithShipmentsDto } from '../services/adminService';
import { useNotificationStore } from '../stores/notification';

const notify = useNotificationStore();

const sessions = ref<ActiveSessionWithShipmentsDto[]>([]);
const loading = ref(false);
const error = ref('');

// Accordion: açık session id'leri
const expandedSessions = reactive(new Set<string>());
const isExpanded = (id: string) => expandedSessions.has(id);
const toggle = (id: string) => {
  if (expandedSessions.has(id)) expandedSessions.delete(id);
  else expandedSessions.add(id);
};

// sessionId → Set<shipmentId>
const selections = reactive<Record<string, Set<number>>>({});

const deliverModal = reactive({
  open: false,
  sessionId: '',
  shipmentIds: [] as number[],
  recipient: '',
  note: '',
  error: '',
  submitting: false,
});

const forceCloseModal = reactive({
  open: false,
  sessionId: '',
  driverName: '',
  plateNumber: '',
  notes: '',
  submitting: false,
});

// ── Helpers ─────────────────────────────────────────────────────────────────

function formatTime(iso: string): string {
  return new Date(iso).toLocaleTimeString('tr-TR', { hour: '2-digit', minute: '2-digit' });
}

function formatElapsed(minutes: number): string {
  const h = Math.floor(minutes / 60);
  const m = minutes % 60;
  return h > 0 ? `${h}s ${m}d` : `${m}d`;
}

// ── Teslim noktaları (proje bazlı gruplama) ───────────────────────────────────

interface DeliveryPoint {
  projectId: number;
  projectName: string;
  shipmentIds: number[];
  totalLines: number;
  allDispatched: boolean;
}

function points(session: ActiveSessionWithShipmentsDto): DeliveryPoint[] {
  const map = new Map<number, DeliveryPoint>();
  for (const s of session.shipments) {
    let p = map.get(s.projectId);
    if (!p) {
      p = { projectId: s.projectId, projectName: s.projectName, shipmentIds: [], totalLines: 0, allDispatched: true };
      map.set(s.projectId, p);
    }
    p.shipmentIds.push(s.id);
    p.totalLines += s.lineCount;
    if (s.status !== 'Dispatched') p.allDispatched = false;
  }
  return Array.from(map.values());
}

function isPointSelected(sessionId: string, pt: DeliveryPoint): boolean {
  return pt.shipmentIds.length > 0 && pt.shipmentIds.every(id => isSelected(sessionId, id));
}

function isPointPartiallySelected(sessionId: string, pt: DeliveryPoint): boolean {
  const sel = pt.shipmentIds.filter(id => isSelected(sessionId, id)).length;
  return sel > 0 && sel < pt.shipmentIds.length;
}

function togglePoint(sessionId: string, pt: DeliveryPoint) {
  ensureSet(sessionId);
  const set = selections[sessionId]!;
  if (isPointSelected(sessionId, pt)) {
    pt.shipmentIds.forEach(id => set.delete(id));
  } else {
    pt.shipmentIds.forEach(id => set.add(id));
  }
}

// ── Selection ────────────────────────────────────────────────────────────────

function ensureSet(sessionId: string) {
  if (!selections[sessionId]) selections[sessionId] = new Set();
}

function isSelected(sessionId: string, shipmentId: number): boolean {
  return selections[sessionId]?.has(shipmentId) ?? false;
}

function isAllSelected(session: ActiveSessionWithShipmentsDto): boolean {
  if (session.shipments.length === 0) return false;
  return session.shipments.every(s => isSelected(session.sessionId, s.id));
}

function isSomeSelected(session: ActiveSessionWithShipmentsDto): boolean {
  return session.shipments.some(s => isSelected(session.sessionId, s.id));
}

function toggleAll(session: ActiveSessionWithShipmentsDto) {
  ensureSet(session.sessionId);
  const set = selections[session.sessionId]!;
  if (isAllSelected(session)) {
    set.clear();
  } else {
    session.shipments.forEach(s => set.add(s.id));
  }
}

function selectedForSession(sessionId: string): number[] {
  return Array.from(selections[sessionId] ?? []);
}

// ── Bulk Deliver ──────────────────────────────────────────────────────────────

function openBulkDeliver(session: ActiveSessionWithShipmentsDto) {
  deliverModal.sessionId = session.sessionId;
  deliverModal.shipmentIds = selectedForSession(session.sessionId);
  deliverModal.recipient = '';
  deliverModal.note = '';
  deliverModal.error = '';
  deliverModal.submitting = false;
  deliverModal.open = true;
}

async function confirmBulkDeliver() {
  if (!deliverModal.recipient.trim()) {
    deliverModal.error = 'Teslim alan bilgisi zorunludur.';
    return;
  }
  if (!deliverModal.note.trim()) {
    deliverModal.error = 'Açıklama / not zorunludur.';
    return;
  }

  deliverModal.error = '';
  deliverModal.submitting = true;

  const results = await Promise.allSettled(
    deliverModal.shipmentIds.map(id =>
      adminService.markShipmentDelivered(id, {
        deliveryRecipient: deliverModal.recipient,
        overrideNote: deliverModal.note,
      })
    )
  );

  const failed = results.filter(r => r.status === 'rejected').length;
  const ok = results.length - failed;

  deliverModal.submitting = false;
  deliverModal.open = false;

  if (ok > 0) notify.add(`${ok} irsaliye teslim edildi olarak işaretlendi.`, 'success');
  if (failed > 0) notify.add(`${failed} irsaliye işlenemedi. Lütfen kontrol edin.`, 'error');

  await load();
}

// ── Force Close ───────────────────────────────────────────────────────────────

function openForceClose(session: ActiveSessionWithShipmentsDto) {
  forceCloseModal.sessionId = session.sessionId;
  forceCloseModal.driverName = session.driverFullName;
  forceCloseModal.plateNumber = session.plateNumber;
  forceCloseModal.notes = '';
  forceCloseModal.submitting = false;
  forceCloseModal.open = true;
}

async function confirmForceClose() {
  forceCloseModal.submitting = true;
  try {
    await adminService.forceCloseSession(forceCloseModal.sessionId, forceCloseModal.notes || null);
    notify.add('Sefer zorla kapatıldı.', 'success');
    forceCloseModal.open = false;
    await load();
  } catch {
    notify.add('Sefer kapatılamadı. Lütfen tekrar deneyin.', 'error');
  } finally {
    forceCloseModal.submitting = false;
  }
}

// ── Data ──────────────────────────────────────────────────────────────────────

async function load() {
  loading.value = true;
  error.value = '';
  try {
    sessions.value = await adminService.getActiveOperations();
    // Reset selections for sessions that no longer exist
    const ids = new Set(sessions.value.map(s => s.sessionId));
    for (const key of Object.keys(selections)) {
      if (!ids.has(key)) delete selections[key];
    }
    // Tek sefer varsa otomatik aç; ilk yüklemede hiç açık yoksa ilkini aç
    if (sessions.value.length === 1) {
      expandedSessions.add(sessions.value[0]!.sessionId);
    } else if (expandedSessions.size === 0 && sessions.value.length > 0) {
      expandedSessions.add(sessions.value[0]!.sessionId);
    }
  } catch {
    error.value = 'Veriler yüklenemedi. Lütfen tekrar deneyin.';
  } finally {
    loading.value = false;
  }
}

onMounted(load);
</script>
