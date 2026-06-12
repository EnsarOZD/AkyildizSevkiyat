<template>
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
        v-if="modelValue"
        class="fixed inset-0 z-[60] bg-[#0a1626] flex flex-col dark text-white"
        style="font-family: 'Plus Jakarta Sans', system-ui, sans-serif;"
      >
        <!-- Üst bar: adım sayacı + atla -->
        <div class="flex items-center justify-between px-5 pt-4 max-w-2xl mx-auto w-full"
          :style="{ paddingTop: 'max(1rem, env(safe-area-inset-top))' }">
          <span class="text-xs font-semibold text-white/40">
            {{ current + 1 }} / {{ slides.length }}
          </span>
          <button
            v-if="!isLast"
            @click="finish"
            class="text-xs font-semibold text-white/50 px-3 py-1.5 rounded-lg hover:bg-white/[0.06]"
          >
            Atla
          </button>
        </div>

        <!-- Slayt içeriği (dokunarak kaydırılabilir) -->
        <div
          class="flex-1 flex flex-col items-center justify-center px-7 max-w-2xl mx-auto w-full select-none"
          @touchstart="onTouchStart"
          @touchend="onTouchEnd"
        >
          <Transition :name="dir >= 0 ? 'slide-next' : 'slide-prev'" mode="out-in">
            <div :key="current" class="flex flex-col items-center text-center w-full">
              <!-- Ekran görüntüsü (varsa) -->
              <div
                v-if="screenUrl(slide.image)"
                class="mb-7 rounded-[22px] overflow-hidden border border-white/[0.1] shadow-2xl shadow-black/40 max-h-[46vh]"
              >
                <img
                  :src="screenUrl(slide.image)"
                  :alt="slide.title"
                  class="block max-h-[46vh] w-auto object-contain"
                />
              </div>
              <!-- İkon rozeti (görsel yoksa) -->
              <div
                v-else
                class="w-24 h-24 rounded-[28px] flex items-center justify-center mb-8 shadow-lg"
                :class="slide.iconBg"
              >
                <component :is="slide.icon" class="w-12 h-12" :class="slide.iconColor" />
              </div>

              <h2 class="text-2xl font-extrabold text-white leading-tight mb-3">
                {{ slide.title }}
              </h2>
              <p class="text-[15px] leading-relaxed text-white/60 max-w-sm">
                {{ slide.body }}
              </p>

              <!-- İpucu satırı (opsiyonel) -->
              <div
                v-if="slide.hint"
                class="mt-6 flex items-center gap-2.5 px-4 py-2.5 rounded-2xl bg-white/[0.05] border border-white/[0.07] max-w-sm"
              >
                <component :is="slide.hintIcon ?? LightBulbIcon" class="w-5 h-5 flex-shrink-0 text-amber-300" />
                <span class="text-[13px] text-white/70 text-left leading-snug">{{ slide.hint }}</span>
              </div>
            </div>
          </Transition>
        </div>

        <!-- Nokta göstergeleri -->
        <div class="flex items-center justify-center gap-2 pb-5">
          <button
            v-for="(_, i) in slides"
            :key="i"
            @click="goTo(i)"
            :aria-label="`Adım ${i + 1}`"
            class="h-2 rounded-full transition-all"
            :class="i === current ? 'w-6 bg-blue-400' : 'w-2 bg-white/20'"
          />
        </div>

        <!-- Alt butonlar -->
        <div
          class="px-6 pb-6 max-w-2xl mx-auto w-full flex items-center gap-3"
          :style="{ paddingBottom: 'max(1.5rem, env(safe-area-inset-bottom))' }"
        >
          <button
            v-if="current > 0"
            @click="prev"
            class="h-13 px-5 py-3.5 rounded-2xl font-semibold text-sm text-white/70 bg-white/[0.06] hover:bg-white/[0.1] transition-colors"
          >
            Geri
          </button>
          <button
            @click="isLast ? finish() : next()"
            class="flex-1 h-13 py-3.5 rounded-2xl font-bold text-base text-white bg-gradient-to-br from-blue-500 to-blue-600 shadow-lg shadow-blue-600/30 active:scale-[0.98] transition-transform"
          >
            {{ isLast ? 'Başla' : 'Devam' }}
          </button>
        </div>
      </div>
    </Transition>
  </Teleport>
</template>

<script setup lang="ts">
import { ref, computed, type Component } from 'vue';
import {
  HandRaisedIcon,
  QrCodeIcon,
  MapIcon,
  CheckCircleIcon,
  ArrowUturnLeftIcon,
  SignalSlashIcon,
  LightBulbIcon,
  MapPinIcon,
} from '@heroicons/vue/24/outline';

interface Slide {
  icon: Component;
  iconBg: string;
  iconColor: string;
  title: string;
  body: string;
  hint?: string;
  hintIcon?: Component;
  /** assets/driver-onboarding/{image}.png varsa kartta ekran görüntüsü gösterilir */
  image?: string;
}

// Ekran görüntüleri — dosya eklendiğinde otomatik algılanır, yoksa ikon gösterilir.
const screenModules = import.meta.glob(
  '../../assets/driver-onboarding/*.{png,jpg,jpeg,webp}',
  { eager: true, query: '?url', import: 'default' },
) as Record<string, string>;

function screenUrl(name?: string): string | undefined {
  if (!name) return undefined;
  const match = Object.entries(screenModules).find(([path]) =>
    /([^/]+)\.(png|jpe?g|webp)$/i.exec(path)?.[1] === name,
  );
  return match?.[1];
}

defineProps<{ modelValue: boolean }>();
const emit = defineEmits<{
  (e: 'update:modelValue', v: boolean): void;
  (e: 'done'): void;
}>();

const slides: Slide[] = [
  {
    icon: HandRaisedIcon,
    iconBg: 'bg-gradient-to-br from-blue-500/25 to-indigo-600/25',
    iconColor: 'text-blue-300',
    title: 'Hoş geldin! 👋',
    body: 'Bu panel günlük seferlerini yönetmen için tasarlandı. Birkaç adımda nasıl kullanacağını gösterelim. İstediğin an "Diğer › Nasıl kullanılır?" menüsünden tekrar açabilirsin.',
  },
  {
    icon: QrCodeIcon,
    iconBg: 'bg-gradient-to-br from-blue-500/25 to-blue-600/25',
    iconColor: 'text-blue-300',
    title: '1. Seferi Başlat',
    body: 'Aracına bindiğinde alttaki ortadaki mavi QR butonuna bas ve araç üstündeki QR kodu okut. Sefer başlayınca o günün durakları yüklenir.',
    hint: 'Konum izni açık olmalı — yoksa QR okutma çalışmaz. Ayarlar\'dan açabilirsin.',
    hintIcon: MapPinIcon,
    image: 'sefer-baslat',
  },
  {
    icon: MapIcon,
    iconBg: 'bg-gradient-to-br from-emerald-500/25 to-green-600/25',
    iconColor: 'text-emerald-300',
    title: '2. Rotayı Takip Et',
    body: '"Ana" sekmesinde duraklarını sırayla görürsün. "Rota" butonu tüm durakları Google Maps\'te açar; navigasyonu oradan kullan.',
    hint: 'Bir durağa dokununca o teslimatın detayını ve müşteri bilgisini görürsün.',
    image: 'rota',
  },
  {
    icon: CheckCircleIcon,
    iconBg: 'bg-gradient-to-br from-emerald-500/25 to-green-600/25',
    iconColor: 'text-emerald-300',
    title: '3. Teslimatı Tamamla',
    body: 'Her durakta teslimatı yaptıktan sonra ekrandaki adımları izleyerek "Teslim Edildi" olarak işaretle. Tamamladıkların "Tamamlanan" sekmesine geçer.',
    image: 'teslimat',
  },
  {
    icon: ArrowUturnLeftIcon,
    iconBg: 'bg-gradient-to-br from-amber-500/25 to-orange-600/25',
    iconColor: 'text-amber-300',
    title: '4. Seferi Kapat & İade',
    body: 'Tüm teslimatlar bitince QR butonu yeşile döner — okutarak seferi kapat. Geri getirdiğin ürün varsa kapatmadan önce iade olarak işle.',
    image: 'seferi-kapat',
  },
  {
    icon: SignalSlashIcon,
    iconBg: 'bg-gradient-to-br from-slate-500/25 to-slate-600/25',
    iconColor: 'text-white/70',
    title: 'İnternet Yoksa Sorun Değil',
    body: 'Kapsama dışında kalsan bile teslimatların cihazda kaydedilir ve bağlantı gelince otomatik gönderilir. Üstteki "Aktif / Çevrimdışı" göstergesinden durumu takip edebilirsin.',
    hint: 'Hazırsın! İyi yolculuklar. 🚚',
    image: 'cevrimdisi',
  },
];

const current = ref(0);
const dir = ref(1); // geçiş yönü: 1 ileri, -1 geri
const slide = computed(() => slides[current.value]!);
const isLast = computed(() => current.value === slides.length - 1);

function next() {
  if (current.value < slides.length - 1) {
    dir.value = 1;
    current.value++;
  }
}
function prev() {
  if (current.value > 0) {
    dir.value = -1;
    current.value--;
  }
}
function goTo(i: number) {
  dir.value = i > current.value ? 1 : -1;
  current.value = i;
}
function finish() {
  emit('done');
  emit('update:modelValue', false);
  // bir sonraki açılış için başa sar
  setTimeout(() => { current.value = 0; }, 200);
}

// Dokunarak kaydırma
let touchX = 0;
function onTouchStart(e: TouchEvent) {
  touchX = e.changedTouches[0]?.clientX ?? 0;
}
function onTouchEnd(e: TouchEvent) {
  const dx = (e.changedTouches[0]?.clientX ?? 0) - touchX;
  if (Math.abs(dx) < 50) return;
  if (dx < 0) next();
  else prev();
}
</script>

<style scoped>
.slide-next-enter-active,
.slide-next-leave-active,
.slide-prev-enter-active,
.slide-prev-leave-active {
  transition: all 0.25s ease;
}
.slide-next-enter-from {
  opacity: 0;
  transform: translateX(40px);
}
.slide-next-leave-to {
  opacity: 0;
  transform: translateX(-40px);
}
.slide-prev-enter-from {
  opacity: 0;
  transform: translateX(-40px);
}
.slide-prev-leave-to {
  opacity: 0;
  transform: translateX(40px);
}
.h-13 {
  height: 3.25rem;
}
</style>
