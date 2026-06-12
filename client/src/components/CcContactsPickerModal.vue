<template>
  <BaseModal :show="show" :title="title" maxWidth="sm" @close="emit('close')">
    <div class="space-y-3">
      <div v-if="headerHtml || $slots.header" class="text-sm bg-gray-50 dark:bg-gray-800 rounded p-3 border dark:border-gray-700">
        <slot name="header">
          <div v-html="headerHtml"></div>
        </slot>
      </div>

      <div>
        <p class="text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
          CC'ye eklenecek kişiler <span class="text-gray-400 font-normal">(opsiyonel)</span>
        </p>
        <div v-if="loading" class="text-sm text-gray-400 py-2">Yükleniyor...</div>
        <div v-else-if="contacts.length === 0" class="text-sm text-gray-400 py-2 italic">Harici mail adresi tanımlı değil.</div>
        <div v-else class="space-y-1 max-h-56 overflow-y-auto border dark:border-gray-700 rounded p-2">
          <label
            v-for="c in contacts"
            :key="c.id"
            class="flex items-center gap-3 px-2 py-2 rounded cursor-pointer hover:bg-gray-50 dark:hover:bg-gray-800"
          >
            <input
              type="checkbox"
              :checked="selectedIds.has(c.id)"
              @change="toggle(c.id)"
              class="h-4 w-4 rounded border-gray-300 text-blue-600"
            />
            <div class="flex-1 min-w-0">
              <div class="text-sm font-medium text-gray-800 dark:text-gray-100">{{ c.name }}</div>
              <div class="text-xs text-gray-500 dark:text-gray-400">{{ c.email }}</div>
            </div>
          </label>
        </div>
      </div>
    </div>

    <template #footer>
      <button
        @click="emit('close')"
        :disabled="submitting"
        class="px-4 py-2 text-gray-600 dark:text-gray-400 hover:bg-gray-100 dark:hover:bg-gray-700 rounded text-sm disabled:opacity-50"
      >
        İptal
      </button>
      <button
        @click="submit"
        :disabled="submitting"
        class="px-4 py-2 bg-blue-600 text-white rounded hover:bg-blue-700 font-semibold text-sm disabled:opacity-50"
      >
        {{ submitting ? 'Gönderiliyor...' : submitLabel }}
      </button>
    </template>
  </BaseModal>
</template>

<script setup lang="ts">
import { ref, watch } from 'vue';
import BaseModal from './BaseModal.vue';
import apiClient from '../services/apiClient';

interface ExternalContact { id: number; name: string; email: string; note?: string }

const props = withDefaults(defineProps<{
  show: boolean;
  title?: string;
  submitLabel?: string;
  submitting?: boolean;
  headerHtml?: string;
}>(), {
  title: 'Mail Gönder — CC Seç',
  submitLabel: 'Gönder',
  submitting: false,
  headerHtml: '',
});

const emit = defineEmits<{
  (e: 'close'): void;
  (e: 'submit', ccEmails: string[]): void;
}>();

const contacts = ref<ExternalContact[]>([]);
const loading = ref(false);
const loaded = ref(false);
const selectedIds = ref<Set<number>>(new Set());

const loadContacts = async () => {
  if (loaded.value) return;
  loading.value = true;
  try {
    const res = await apiClient.get('/external-email-contacts', { params: { activeOnly: true } });
    contacts.value = (res.data || []).filter((c: ExternalContact) => c.email);
    loaded.value = true;
  } catch {
    contacts.value = [];
  } finally {
    loading.value = false;
  }
};

watch(() => props.show, (open) => {
  if (open) {
    selectedIds.value = new Set();
    loadContacts();
  }
});

const toggle = (id: number) => {
  const next = new Set(selectedIds.value);
  if (next.has(id)) next.delete(id); else next.add(id);
  selectedIds.value = next;
};

const submit = () => {
  const ccEmails = contacts.value
    .filter(c => selectedIds.value.has(c.id))
    .map(c => c.email);
  emit('submit', ccEmails);
};
</script>
