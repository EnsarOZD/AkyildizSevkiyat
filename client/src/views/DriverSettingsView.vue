<template>
  <div class="space-y-4 pb-8">
    <p class="text-xs text-gray-500 dark:text-gray-400">
      Uygulamanın düzgün çalışması için aşağıdaki izinlerin verilmesi gerekir.
      İzin reddedilmişse tarayıcı ayarlarından manuel olarak açmanız gerekir.
    </p>

    <!-- Location -->
    <PermissionCard
      title="Konum"
      description="Sefer başlatma/bitirme ve teslimat konumu kaydı için gereklidir."
      :status="locationStatus"
      :instructions="locationInstructions"
      @request="requestLocation"
    />

    <!-- Camera -->
    <PermissionCard
      title="Kamera"
      description="QR kod okutarak sefer başlatmak/bitirmek için gereklidir."
      :status="cameraStatus"
      :instructions="platformInstructions"
      @request="requestCamera"
    />

    <!-- Notifications -->
    <PermissionCard
      title="Bildirimler"
      description="Yeni görev ve sistem bildirimleri almak için gereklidir."
      :status="notifStatus"
      :instructions="platformInstructions"
      @request="requestNotifications"
    />

    <!-- Platform note -->
    <p class="text-[11px] text-gray-400 dark:text-gray-500 text-center leading-relaxed">
      {{ platformInstructions }}
    </p>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import PermissionCard from '../components/driver/PermissionCard.vue';

type PermStatus = 'unknown' | 'granted' | 'denied' | 'prompt' | 'unsupported';

const isIos = /iphone|ipad|ipod/i.test(navigator.userAgent);

const locationStatus = ref<PermStatus>('unknown');
const cameraStatus   = ref<PermStatus>('unknown');
const notifStatus    = ref<PermStatus>('unknown');

const locationInstructions = isIos
  ? 'iPhone: Ayarlar → Safari → Konum → "Sormayı İste" veya "İzin Ver" seçin.'
  : 'Android/Masaüstü: Tarayıcı adres çubuğundaki kilit simgesi → Konum → İzin Ver.';

const platformInstructions = isIos
  ? 'iPhone: Tarayıcı adres çubuğundaki "AA" simgesi → Web Sitesi Ayarları → ilgili izni açın.'
  : 'Android/Masaüstü: Tarayıcı adres çubuğundaki kilit simgesi → ilgili izni İzin Ver yapın.';

async function queryPerm(name: PermissionName): Promise<PermStatus> {
  try {
    const result = await navigator.permissions.query({ name });
    result.addEventListener('change', () => {
      if (name === 'geolocation') locationStatus.value = result.state as PermStatus;
      if (name === ('camera' as PermissionName)) cameraStatus.value = result.state as PermStatus;
      if (name === 'notifications') notifStatus.value = result.state as PermStatus;
    });
    return result.state as PermStatus;
  } catch {
    return 'unknown';
  }
}

async function requestLocation() {
  if (!navigator.geolocation) { locationStatus.value = 'unsupported'; return; }
  navigator.geolocation.getCurrentPosition(
    () => { locationStatus.value = 'granted'; },
    () => { locationStatus.value = 'denied'; },
    { enableHighAccuracy: true, timeout: 10000 }
  );
}

async function requestCamera() {
  try {
    const stream = await navigator.mediaDevices.getUserMedia({ video: true });
    stream.getTracks().forEach(t => t.stop());
    cameraStatus.value = 'granted';
  } catch {
    cameraStatus.value = 'denied';
  }
}

async function requestNotifications() {
  if (!('Notification' in window)) { notifStatus.value = 'unsupported'; return; }
  const result = await Notification.requestPermission();
  notifStatus.value = result === 'granted' ? 'granted' : 'denied';
}

onMounted(async () => {
  locationStatus.value = await queryPerm('geolocation');
  cameraStatus.value   = await queryPerm('camera' as PermissionName);

  if ('Notification' in window) {
    notifStatus.value = Notification.permission === 'granted' ? 'granted'
      : Notification.permission === 'denied' ? 'denied' : 'prompt';
  } else {
    notifStatus.value = 'unsupported';
  }
});
</script>
