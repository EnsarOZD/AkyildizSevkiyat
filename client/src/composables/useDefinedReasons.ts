import { ref } from 'vue';
import apiClient from '../services/apiClient';

// Backend ReasonCategory enum ile birebir (numeric).
export const ReasonCategory = {
  PickingDifference: 0,
  GoodsReceiptReject: 1,
} as const;

// API erişilemezse eski koda gömülü listeler — üretimde güvenli bozulma.
const FALLBACK: Record<number, string[]> = {
  0: ['Stokta yok', 'Kısmi stok', 'Koli tamamlaması'],
  1: ['Hasarlı', 'Eksik / Kırık', 'Yanlış Ürün', 'Kalite Sorunu'],
};

/**
 * Bir kategorinin aktif sebep etiketlerini yükler.
 * Hata/boş durumunda FALLBACK döner; çağıran taraf load() çağırmalıdır.
 */
export function useDefinedReasons(category: number) {
  const reasons = ref<string[]>([]);
  const loaded = ref(false);

  async function load() {
    if (loaded.value) return;
    try {
      const res = await apiClient.get('/defined-reasons', { params: { category, activeOnly: true } });
      const labels = (res.data || []).map((r: any) => r.label).filter(Boolean);
      reasons.value = labels.length ? labels : (FALLBACK[category] ?? []);
    } catch {
      reasons.value = FALLBACK[category] ?? [];
    } finally {
      loaded.value = true;
    }
  }

  return { reasons, loaded, load };
}
