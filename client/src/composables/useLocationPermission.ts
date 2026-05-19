import { ref, onMounted } from 'vue';

export type LocationPermissionState = 'unknown' | 'granted' | 'denied' | 'prompt' | 'unsupported';

export function useLocationPermission() {
  const permissionState = ref<LocationPermissionState>('unknown');
  const isIos = /iphone|ipad|ipod/i.test(navigator.userAgent);

  async function checkPermission() {
    if (!navigator.geolocation) {
      permissionState.value = 'unsupported';
      return;
    }
    try {
      const result = await navigator.permissions.query({ name: 'geolocation' });
      permissionState.value = result.state as LocationPermissionState;
      result.addEventListener('change', () => {
        permissionState.value = result.state as LocationPermissionState;
      });
    } catch {
      permissionState.value = 'unknown';
    }
  }

  const settingsInstructions = isIos
    ? 'Ayarlar → Safari → Konum seçeneğini "İzin Ver" yapın.'
    : 'Tarayıcı adres çubuğundaki kilit simgesine dokunun → Konum → İzin Ver.';

  onMounted(checkPermission);

  return { permissionState, settingsInstructions, checkPermission };
}
