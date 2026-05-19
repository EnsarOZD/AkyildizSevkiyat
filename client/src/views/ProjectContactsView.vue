<template>
  <div class="max-w-5xl mx-auto space-y-6">
    <div class="flex items-center justify-between">
      <div>
        <h1 class="text-xl font-bold text-gray-900 dark:text-white">Proje İletişim Bilgileri</h1>
        <p class="text-sm text-gray-500 dark:text-gray-400 mt-0.5">
          ISS'ten teslim alacak kişi bilgisi gelmediğinde kullanılacak varsayılan iletişim bilgileri.
        </p>
      </div>
    </div>

    <!-- Search -->
    <div class="relative">
      <MagnifyingGlassIcon class="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-gray-400" />
      <input
        v-model="search"
        type="text"
        placeholder="Proje ara..."
        class="w-full pl-9 pr-4 py-2.5 rounded-lg border border-gray-300 dark:border-white/20 bg-white dark:bg-white/5 text-gray-900 dark:text-white placeholder-gray-400 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
      />
    </div>

    <!-- Loading -->
    <div v-if="loading" class="flex justify-center py-16">
      <div class="w-8 h-8 border-4 border-blue-600 border-t-transparent rounded-full animate-spin"></div>
    </div>

    <!-- Table -->
    <div v-else class="bg-white dark:bg-[#0f2744] rounded-xl shadow-sm border border-gray-200 dark:border-white/10 overflow-hidden">
      <table class="w-full text-sm">
        <thead>
          <tr class="border-b border-gray-200 dark:border-white/10 bg-gray-50 dark:bg-white/5">
            <th class="px-4 py-3 text-left font-medium text-gray-500 dark:text-gray-400">Proje</th>
            <th class="px-4 py-3 text-left font-medium text-gray-500 dark:text-gray-400">Bölge</th>
            <th class="px-4 py-3 text-left font-medium text-gray-500 dark:text-gray-400">Teslim Alacak Kişi</th>
            <th class="px-4 py-3 text-left font-medium text-gray-500 dark:text-gray-400">İletişim No</th>
            <th class="px-4 py-3 w-20"></th>
          </tr>
        </thead>
        <tbody>
          <tr v-if="filteredProjects.length === 0">
            <td colspan="5" class="px-4 py-8 text-center text-gray-400 text-sm">Sonuç bulunamadı.</td>
          </tr>
          <tr
            v-for="p in filteredProjects"
            :key="p.id"
            class="border-b border-gray-100 dark:border-white/5 hover:bg-gray-50 dark:hover:bg-white/5 transition-colors"
          >
            <td class="px-4 py-3">
              <p class="font-medium text-gray-900 dark:text-white">{{ p.name }}</p>
              <p class="text-xs text-gray-400">{{ p.code }}</p>
            </td>
            <td class="px-4 py-3 text-gray-500 dark:text-gray-400">{{ p.zone || '—' }}</td>
            <td class="px-4 py-3">
              <input
                v-model="edits[p.id]!.contactName"
                type="text"
                placeholder="İsim Soyisim"
                class="w-full px-2.5 py-1.5 text-sm rounded-lg border border-gray-200 dark:border-white/20 bg-white dark:bg-white/5 text-gray-900 dark:text-white placeholder-gray-400 focus:outline-none focus:ring-2 focus:ring-blue-500"
              />
            </td>
            <td class="px-4 py-3">
              <input
                v-model="edits[p.id]!.contactPhone"
                type="tel"
                placeholder="0532 xxx xx xx"
                class="w-full px-2.5 py-1.5 text-sm rounded-lg border border-gray-200 dark:border-white/20 bg-white dark:bg-white/5 text-gray-900 dark:text-white placeholder-gray-400 focus:outline-none focus:ring-2 focus:ring-blue-500"
              />
            </td>
            <td class="px-4 py-3">
              <button
                @click="save(p.id)"
                :disabled="saving === p.id"
                class="px-3 py-1.5 bg-blue-600 hover:bg-blue-700 disabled:bg-blue-400 text-white text-xs font-medium rounded-lg transition-colors flex items-center gap-1"
              >
                <span v-if="saving === p.id" class="w-3 h-3 border-2 border-white border-t-transparent rounded-full animate-spin"></span>
                <CheckIcon v-else class="w-3 h-3" />
                Kaydet
              </button>
            </td>
          </tr>
        </tbody>
      </table>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, computed, onMounted } from 'vue';
import { MagnifyingGlassIcon, CheckIcon } from '@heroicons/vue/24/outline';
import projectService from '../services/projectService';
import { useNotificationStore } from '../stores/notification';

interface ProjectContact {
  id: number;
  code: string;
  name: string;
  zone?: string;
  defaultContactName?: string;
  defaultContactPhone?: string;
}

const notify = useNotificationStore();
const loading = ref(false);
const saving = ref<number | null>(null);
const search = ref('');
const projects = ref<ProjectContact[]>([]);
const edits = reactive<Record<number, { contactName: string; contactPhone: string } | undefined>>({});

const filteredProjects = computed(() => {
  const q = search.value.toLowerCase().trim();
  if (!q) return projects.value;
  return projects.value.filter(p =>
    p.name.toLowerCase().includes(q) ||
    p.code.toLowerCase().includes(q) ||
    (p.zone ?? '').toLowerCase().includes(q)
  );
});

async function load() {
  loading.value = true;
  try {
    const data = await projectService.getContacts();
    projects.value = data;
    for (const p of data) {
      edits[p.id] = {
        contactName: p.defaultContactName ?? '',
        contactPhone: p.defaultContactPhone ?? '',
      };
    }
  } catch {
    notify.add('Proje listesi yüklenemedi.', 'error');
  } finally {
    loading.value = false;
  }
}

async function save(projectId: number) {
  saving.value = projectId;
  try {
    const e = edits[projectId];
    if (!e) return;
    await projectService.updateContact(projectId, e.contactName || null, e.contactPhone || null);
    const p = projects.value.find(x => x.id === projectId);
    if (p) {
      p.defaultContactName = e.contactName || undefined;
      p.defaultContactPhone = e.contactPhone || undefined;
    }
    notify.add('Kaydedildi.', 'success');
  } catch {
    notify.add('Kaydedilemedi.', 'error');
  } finally {
    saving.value = null;
  }
}

onMounted(load);
</script>
