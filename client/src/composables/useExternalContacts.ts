import { ref } from 'vue';
import apiClient from '../services/apiClient';

export interface ExternalContact {
  id: number;
  name: string;
  email: string;
  note?: string;
}

/**
 * Harici e-posta kişilerini (aktif) tek seferlik yükleyip, seçim (CC) yönetimini sağlar.
 * Eksik-bildirim, iptal ve erteleme mail modallarında ortak kullanılır.
 */
export function useExternalContacts() {
  const contacts = ref<ExternalContact[]>([]);
  const loading = ref(false);
  const loaded = ref(false);
  const selectedIds = ref<Set<number>>(new Set());

  async function load() {
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
  }

  function toggle(id: number) {
    const next = new Set(selectedIds.value);
    next.has(id) ? next.delete(id) : next.add(id);
    selectedIds.value = next;
  }

  function reset() {
    selectedIds.value = new Set();
  }

  function selectedEmails(): string[] {
    return contacts.value.filter(c => selectedIds.value.has(c.id)).map(c => c.email);
  }

  return { contacts, loading, selectedIds, load, toggle, reset, selectedEmails };
}
