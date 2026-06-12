<template>
  <div class="space-y-2">
    <div class="flex gap-2">
      <input v-model="code" @keyup.enter="emitCode" type="text" inputmode="text"
             :placeholder="placeholder"
             class="flex-1 px-3 py-2.5 rounded-lg border border-gray-300 dark:border-gray-700 bg-white dark:bg-gray-900 text-gray-900 dark:text-gray-100 focus:outline-none focus:ring-2 focus:ring-violet-400" />
      <button @click="emitCode" :disabled="!code.trim()"
              class="px-4 py-2.5 bg-violet-600 hover:bg-violet-700 disabled:opacity-50 text-white font-semibold rounded-lg text-sm whitespace-nowrap">Onayla</button>
      <button @click="toggleCamera" class="px-3 py-2.5 bg-gray-100 dark:bg-gray-700 rounded-lg text-sm" :title="scanning ? 'Kamerayı kapat' : 'Kamerayı aç'">
        {{ scanning ? '✕' : '📷' }}
      </button>
    </div>
    <div v-if="scanning" class="relative rounded-xl overflow-hidden bg-black aspect-video">
      <video ref="videoEl" class="w-full h-full object-cover" autoplay playsinline muted></video>
      <canvas ref="canvasEl" class="hidden"></canvas>
      <div class="absolute inset-0 border-4 border-violet-400/60 m-8 rounded-lg pointer-events-none"></div>
    </div>
    <p v-if="error" class="text-xs text-red-500">{{ error }}</p>
  </div>
</template>

<script setup lang="ts">
import { ref, onUnmounted } from 'vue';
import jsQR from 'jsqr';

withDefaults(defineProps<{ placeholder?: string }>(), { placeholder: 'Araba kodu / QR' });
const emit = defineEmits<{ (e: 'scan', code: string): void }>();

const code = ref('');
const scanning = ref(false);
const error = ref('');
const videoEl = ref<HTMLVideoElement | null>(null);
const canvasEl = ref<HTMLCanvasElement | null>(null);
let stream: MediaStream | null = null;
let raf = 0;

function emitCode() {
  const c = code.value.trim();
  if (!c) return;
  emit('scan', c);
  code.value = '';
}

async function toggleCamera() {
  if (scanning.value) { stopCamera(); return; }
  error.value = '';
  try {
    stream = await navigator.mediaDevices.getUserMedia({ video: { facingMode: 'environment' } });
    scanning.value = true;
    await nextFrameSetup();
  } catch {
    error.value = 'Kamera açılamadı. Kod girişini kullanın.';
    scanning.value = false;
  }
}

async function nextFrameSetup() {
  // video el render edilene kadar bekle
  await new Promise(r => setTimeout(r, 50));
  if (videoEl.value && stream) {
    videoEl.value.srcObject = stream;
    await videoEl.value.play().catch(() => {});
    raf = requestAnimationFrame(tick);
  }
}

function tick() {
  if (!scanning.value || !videoEl.value || !canvasEl.value) return;
  const video = videoEl.value;
  const canvas = canvasEl.value;
  if (video.readyState === video.HAVE_ENOUGH_DATA) {
    canvas.width = video.videoWidth;
    canvas.height = video.videoHeight;
    const ctx = canvas.getContext('2d');
    if (ctx) {
      ctx.drawImage(video, 0, 0, canvas.width, canvas.height);
      const img = ctx.getImageData(0, 0, canvas.width, canvas.height);
      const result = jsQR(img.data, img.width, img.height);
      if (result && result.data) {
        emit('scan', result.data.trim());
        stopCamera();
        return;
      }
    }
  }
  raf = requestAnimationFrame(tick);
}

function stopCamera() {
  scanning.value = false;
  if (raf) cancelAnimationFrame(raf);
  stream?.getTracks().forEach(t => t.stop());
  stream = null;
}

onUnmounted(stopCamera);
</script>
