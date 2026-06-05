<template>
  <div ref="triggerRef" class="relative inline-block">
    <button
      @click.stop="toggle"
      class="p-1.5 rounded-lg hover:bg-gray-100 dark:hover:bg-gray-700 text-gray-500 hover:text-gray-700 dark:hover:text-gray-200 transition-colors"
      title="İşlemler"
      :aria-expanded="open"
    >
      <EllipsisVerticalIcon class="w-4 h-4" />
    </button>

    <Teleport to="body">
      <Transition
        enter-active-class="transition ease-out duration-100"
        enter-from-class="opacity-0 scale-95"
        enter-to-class="opacity-100 scale-100"
        leave-active-class="transition ease-in duration-75"
        leave-from-class="opacity-100 scale-100"
        leave-to-class="opacity-0 scale-95"
      >
        <div
          v-if="open"
          ref="menuRef"
          :style="floatStyle"
          class="fixed z-[200] min-w-[176px] origin-top-right bg-white dark:bg-gray-800 rounded-xl shadow-xl border border-gray-200 dark:border-gray-700 py-1.5 overflow-hidden"
          @click.stop
        >
          <!-- Workflow actions -->
          <button
            v-if="can('assignToWarehouse')"
            @click="act('assignToWarehouse')"
            class="menu-item text-yellow-700 dark:text-yellow-400 hover:bg-yellow-50 dark:hover:bg-yellow-900/20"
          >
            <BuildingStorefrontIcon class="w-4 h-4" />
            Depoya Ata
          </button>

          <button
            v-if="can('startPicking')"
            @click="act('startPicking')"
            class="menu-item text-blue-700 dark:text-blue-400 hover:bg-blue-50 dark:hover:bg-blue-900/20"
          >
            <ArchiveBoxIcon class="w-4 h-4" />
            Toplamaya Başla
          </button>

          <button
            v-if="can('markReady')"
            @click="act('markReady')"
            class="menu-item text-purple-700 dark:text-purple-400 hover:bg-purple-50 dark:hover:bg-purple-900/20"
          >
            <CheckCircleIcon class="w-4 h-4" />
            Hazır İşaretle
          </button>

          <button
            v-if="can('exportToNetsis')"
            @click="act('exportToNetsis')"
            :disabled="exporting"
            class="menu-item text-orange-700 dark:text-orange-400 hover:bg-orange-50 dark:hover:bg-orange-900/20 disabled:opacity-50 disabled:cursor-not-allowed"
          >
            <span v-if="exporting" class="w-4 h-4 border-2 border-orange-500 border-t-transparent rounded-full animate-spin flex-shrink-0" />
            <ArrowUpTrayIcon v-else class="w-4 h-4" />
            {{ exporting ? 'Aktarılıyor...' : "Netsis'e Aktar" }}
          </button>

          <button
            v-if="can('markDelivered')"
            @click="act('markDelivered')"
            class="menu-item text-green-700 dark:text-green-400 hover:bg-green-50 dark:hover:bg-green-900/20"
          >
            <CheckBadgeIcon class="w-4 h-4" />
            Teslim Edildi
          </button>

          <router-link
            v-if="can('returnEntry')"
            :to="`/shipments/${shipment.id}?action=return`"
            @click="open = false"
            class="menu-item text-red-600 dark:text-red-400 hover:bg-red-50 dark:hover:bg-red-900/20"
          >
            <ArrowUturnLeftIcon class="w-4 h-4" />
            İade Gir
          </router-link>

          <!-- Separator before destructive -->
          <hr
            v-if="hasDestructiveActions"
            class="my-1 border-gray-100 dark:border-gray-700"
          />

          <button
            v-if="can('deleteDraft')"
            @click="act('deleteDraft')"
            class="menu-item text-red-600 dark:text-red-400 hover:bg-red-50 dark:hover:bg-red-900/20"
          >
            <TrashIcon class="w-4 h-4" />
            Sil
          </button>

          <button
            v-if="can('passiveOn')"
            @click="act('passiveOn')"
            class="menu-item text-amber-600 dark:text-amber-400 hover:bg-amber-50 dark:hover:bg-amber-900/20"
          >
            <ArchiveBoxXMarkIcon class="w-4 h-4" />
            Pasife Al
          </button>

          <button
            v-if="can('cancel')"
            @click="act('cancel')"
            class="menu-item text-red-600 dark:text-red-400 hover:bg-red-50 dark:hover:bg-red-900/20"
          >
            <XCircleIcon class="w-4 h-4" />
            İptal Et
          </button>

          <button
            v-if="can('passiveOff')"
            @click="act('passiveOff')"
            class="menu-item text-green-700 dark:text-green-400 hover:bg-green-50 dark:hover:bg-green-900/20"
          >
            <ArrowPathIcon class="w-4 h-4" />
            Aktife Al
          </button>

          <!-- Separator before Detay -->
          <hr class="my-1 border-gray-100 dark:border-gray-700" />

          <router-link
            :to="`/shipments/${shipment.id}`"
            @click="open = false"
            class="menu-item text-blue-600 dark:text-blue-400 hover:bg-blue-50 dark:hover:bg-blue-900/20 font-medium"
          >
            <ArrowTopRightOnSquareIcon class="w-4 h-4" />
            Detay
          </router-link>
        </div>
      </Transition>
    </Teleport>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted } from 'vue';
import {
  EllipsisVerticalIcon,
  BuildingStorefrontIcon,
  ArchiveBoxIcon,
  ArchiveBoxXMarkIcon,
  CheckCircleIcon,
  CheckBadgeIcon,
  ArrowUpTrayIcon,
  ArrowUturnLeftIcon,
  ArrowPathIcon,
  ArrowTopRightOnSquareIcon,
  TrashIcon,
  XCircleIcon,
} from '@heroicons/vue/24/outline';
import { useAuthStore } from '../stores/auth';

interface ShipmentRow {
  id: number;
  status: string;
  netsisTransferredAt?: string | null;
  operationTypeValue?: number;
}

const props = defineProps<{
  shipment: ShipmentRow;
  exporting?: boolean;
}>();

const emit = defineEmits<{
  assignToWarehouse: [];
  startPicking: [];
  markReady: [];
  exportToNetsis: [];
  markDelivered: [];
  deleteDraft: [];
  passiveOn: [];
  passiveOff: [];
  cancel: [];
}>();

const authStore = useAuthStore();

function hasRole(roles: string[]): boolean {
  return roles.includes(authStore.userRole);
}

const s = computed(() => props.shipment);

const can = (action: string): boolean => {
  const st = s.value.status;
  const nt = s.value.netsisTransferredAt;
  switch (action) {
    case 'assignToWarehouse':
      // Kıyafet operasyonu depo hazırlığa alınamaz
      return st === 'Created' && s.value.operationTypeValue !== 1 && hasRole(['Admin', 'Accounting']);
    case 'startPicking':
      return st === 'AssignedToWarehouse' && hasRole(['Admin', 'Warehouse']);
    case 'markReady':
      return st === 'Picking' && hasRole(['Admin', 'Warehouse']);
    case 'exportToNetsis':
      if (s.value.operationTypeValue === 1)
        // Kıyafet: Created veya ReadyForDispatch'ten aktarılabilir
        return (st === 'Created' || st === 'ReadyForDispatch') && !nt && hasRole(['Admin', 'Manager', 'Accounting']);
      return st === 'ReadyForDispatch' && !nt && hasRole(['Admin', 'Manager', 'Accounting']);
    case 'markDelivered':
      if (s.value.operationTypeValue === 1 && st === 'ReadyForDispatch' && nt)
        return hasRole(['Admin', 'Manager', 'Accounting']);
      return st === 'Dispatched' && hasRole(['Admin', 'Manager', 'Accounting']);
    case 'returnEntry':
      return st === 'Dispatched' && hasRole(['Admin', 'Manager', 'Accounting']);
    case 'deleteDraft':
      return st === 'Created' && hasRole(['Admin', 'Manager', 'Accounting']);
    case 'passiveOn':
      return st === 'Created' && hasRole(['Admin', 'Accounting']);
    case 'cancel':
      // Sebep girilerek iptal — Netsis'e aktarılmamış (nt yok) ön-sevk aşamaları.
      // Depo hazırlık aşamasında rezervasyon backend'de otomatik serbest bırakılır.
      return !nt
        && ['Created', 'AssignedToWarehouse', 'Picking', 'ReadyForDispatch'].includes(st)
        && hasRole(['Admin', 'Manager', 'Accounting']);
    case 'passiveOff':
      return st === 'Passive' && hasRole(['Admin', 'Accounting']);
    default:
      return false;
  }
};

const hasDestructiveActions = computed(() =>
  can('deleteDraft') || can('passiveOn') || can('cancel') || can('passiveOff')
);

// ── Dropdown state ────────────────────────────────────────────────────────────
const open = ref(false);
const triggerRef = ref<HTMLElement | null>(null);
const menuRef = ref<HTMLElement | null>(null);
const floatStyle = ref({ top: '0px', right: '0px' });

function toggle(e: MouseEvent) {
  if (!open.value) {
    const rect = (e.currentTarget as HTMLElement).getBoundingClientRect();
    const menuWidth = 180;
    const spaceBelow = window.innerHeight - rect.bottom;
    const spaceRight = window.innerWidth - rect.left;

    const top = spaceBelow > 200
      ? rect.bottom + 4
      : rect.top - 4; // will use transform to align bottom
    const right = spaceRight < menuWidth
      ? window.innerWidth - rect.right
      : window.innerWidth - rect.right;

    floatStyle.value = {
      top: `${top}px`,
      right: `${right}px`,
    };
  }
  open.value = !open.value;
}

function act(event: string) {
  open.value = false;
  emit(event as any);
}

function onClickOutside(e: MouseEvent) {
  const target = e.target as Node;
  if (!menuRef.value?.contains(target) && !triggerRef.value?.contains(target)) {
    open.value = false;
  }
}

function onKeydown(e: KeyboardEvent) {
  if (e.key === 'Escape') open.value = false;
}

onMounted(() => {
  document.addEventListener('click', onClickOutside);
  document.addEventListener('keydown', onKeydown);
});

onUnmounted(() => {
  document.removeEventListener('click', onClickOutside);
  document.removeEventListener('keydown', onKeydown);
});
</script>

<style scoped>
.menu-item {
  @apply w-full flex items-center gap-2.5 px-3.5 py-2 text-sm transition-colors text-left;
}
</style>
