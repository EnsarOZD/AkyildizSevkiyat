import { useNotificationStore } from '../stores/notification';

/**
 * Google Haritalar bağlantısını açmadan önce kullanıcıya onay sorar.
 * Rota / Yol Tarifi butonlarının tamamında ortak kullanılır.
 */
export function useOpenMaps() {
  const notif = useNotificationStore();

  async function openMaps(url: string | null | undefined) {
    if (!url) return;
    const ok = await notif.promptConfirm({
      title: 'Google Haritalar',
      message: 'Google Haritalar uygulamasında açılsın mı?',
      confirmText: 'Aç',
      cancelText: 'Vazgeç',
      type: 'info',
    });
    if (ok) {
      window.open(url, '_blank', 'noopener');
    }
  }

  return { openMaps };
}
