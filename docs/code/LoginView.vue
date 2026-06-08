<template>
  <div
    class="login-root min-h-screen relative flex flex-col justify-center overflow-hidden px-7"
    style="font-family: 'Plus Jakarta Sans', system-ui, sans-serif;"
  >
    <!-- ── Background: navy radial gradient + faint route lines ── -->
    <div class="absolute inset-0 -z-10" style="background: radial-gradient(ellipse 100% 55% at 50% 0%, #1b3e74 0%, #0a1626 60%);"></div>
    <svg class="absolute inset-0 -z-10 w-full h-full opacity-50" viewBox="0 0 390 844" preserveAspectRatio="none">
      <path d="M-20 200 Q 120 150 220 220 T 420 200" fill="none" stroke="rgba(96,165,250,0.16)" stroke-width="1.5" />
      <path d="M-20 640 Q 140 600 240 660 T 420 630" fill="none" stroke="rgba(96,165,250,0.12)" stroke-width="1.5" />
      <circle cx="220" cy="220" r="3" fill="#60a5fa" opacity="0.7" />
      <circle cx="240" cy="660" r="3" fill="#60a5fa" opacity="0.5" />
    </svg>

    <!-- ── Brand + heading ── -->
    <Transition appear name="rise">
      <div class="relative z-10 mx-auto w-full max-w-md">
        <div class="mb-9">
          <!-- chevron mark -->
          <svg width="62" height="62" viewBox="0 0 100 100" aria-label="Akyıldız">
            <path d="M28 81 L50 58 L72 81" fill="none" stroke="#60a5fa" stroke-width="8.6" stroke-linecap="round" stroke-linejoin="round" />
            <path d="M18 58 L50 24 L82 58" fill="none" stroke="#ffffff" stroke-width="11" stroke-linecap="round" stroke-linejoin="round" />
          </svg>
          <h1 class="mt-6 text-[30px] leading-[1.1] font-extrabold text-white tracking-[-0.03em]">
            Sisteme<br />giriş yapın
          </h1>
          <p class="mt-2.5 text-sm text-white/55">Akyıldız Sevkiyat Yönetim Sistemi</p>
        </div>

        <!-- ── Form ── -->
        <form @submit.prevent="handleLogin" class="flex flex-col gap-4">

          <!-- Username -->
          <div>
            <label class="block text-[11px] font-bold uppercase tracking-wider text-white/55 mb-2">Kullanıcı Adı</label>
            <div
              class="flex items-center h-[50px] px-3.5 rounded-[14px] border transition-all"
              :class="focus === 'u'
                ? 'bg-blue-500/10 border-blue-400/65 ring-4 ring-blue-500/15'
                : 'bg-white/5 border-white/10'"
            >
              <svg class="h-[18px] w-[18px] shrink-0 transition-colors" :class="focus === 'u' ? 'text-blue-300' : 'text-white/35'" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                <path d="M20 21a8 8 0 1 0-16 0M16 7a4 4 0 1 1-8 0 4 4 0 0 1 8 0z" />
              </svg>
              <input
                v-model="username"
                type="text" required autocomplete="username" placeholder="kullaniciadi"
                @focus="focus = 'u'" @blur="focus = ''"
                class="flex-1 h-full min-w-0 bg-transparent border-none outline-none pl-3 text-[15px] font-medium text-white placeholder-white/35"
              />
            </div>
          </div>

          <!-- Password -->
          <div>
            <label class="block text-[11px] font-bold uppercase tracking-wider text-white/55 mb-2">Şifre</label>
            <div
              class="flex items-center h-[50px] px-3.5 rounded-[14px] border transition-all"
              :class="focus === 'p'
                ? 'bg-blue-500/10 border-blue-400/65 ring-4 ring-blue-500/15'
                : 'bg-white/5 border-white/10'"
            >
              <svg class="h-[18px] w-[18px] shrink-0 transition-colors" :class="focus === 'p' ? 'text-blue-300' : 'text-white/35'" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                <rect x="4" y="11" width="16" height="10" rx="2.5" /><path d="M8 11V7a4 4 0 0 1 8 0v4" />
              </svg>
              <input
                v-model="password"
                :type="showPassword ? 'text' : 'password'" required autocomplete="current-password" placeholder="••••••••"
                @focus="focus = 'p'" @blur="focus = ''"
                class="flex-1 h-full min-w-0 bg-transparent border-none outline-none pl-3 text-[15px] font-medium text-white placeholder-white/35"
              />
              <button type="button" @click="showPassword = !showPassword" class="shrink-0 p-1 text-white/35 hover:text-white/70 transition-colors">
                <svg v-if="!showPassword" class="h-[18px] w-[18px]" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                  <path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z" /><circle cx="12" cy="12" r="3" />
                </svg>
                <svg v-else class="h-[18px] w-[18px]" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                  <path d="M17.94 17.94A10 10 0 0 1 12 20c-7 0-11-8-11-8a18 18 0 0 1 5.06-5.94M9.9 4.24A9 9 0 0 1 12 4c7 0 11 8 11 8a18 18 0 0 1-2.16 3.19M1 1l22 22" />
                </svg>
              </button>
            </div>
          </div>

          <!-- Remember + forgot -->
          <div class="flex items-center justify-between mt-0.5 mb-1">
            <label class="flex items-center gap-2.5 cursor-pointer select-none">
              <input v-model="rememberMe" type="checkbox" class="peer sr-only" />
              <span class="w-[19px] h-[19px] rounded-md flex items-center justify-center border transition-colors"
                :class="rememberMe ? 'bg-blue-500 border-blue-500' : 'bg-white/5 border-white/20'">
                <svg v-if="rememberMe" class="w-3 h-3" fill="none" viewBox="0 0 24 24" stroke="#fff" stroke-width="3.2" stroke-linecap="round" stroke-linejoin="round"><path d="M5 13l4 4L19 7" /></svg>
              </span>
              <span class="text-[13.5px] font-medium text-white/55">Beni hatırla</span>
            </label>
            <span class="text-[13px] font-semibold text-blue-300 cursor-pointer hover:text-blue-200">Şifremi unuttum</span>
          </div>

          <!-- Error -->
          <Transition name="err">
            <div v-if="error" class="flex items-center gap-2.5 bg-red-500/10 border border-red-400/30 rounded-[14px] px-4 py-3">
              <svg class="h-4 w-4 text-red-400 shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
                <path stroke-linecap="round" stroke-linejoin="round" d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
              </svg>
              <p class="text-sm text-red-200">{{ error }}</p>
            </div>
          </Transition>

          <!-- Submit -->
          <button
            type="submit" :disabled="loading"
            class="relative overflow-hidden w-full h-[52px] rounded-[14px] text-white text-[15px] font-bold flex items-center justify-center gap-2.5 disabled:opacity-70 transition-all hover:-translate-y-px"
            style="background: linear-gradient(135deg,#3b82f6 0%,#2563eb 100%); box-shadow: 0 10px 26px rgba(37,99,235,0.45);"
          >
            <span v-if="loading" class="absolute inset-0 shimmer"></span>
            <svg v-if="loading" class="animate-spin h-[18px] w-[18px]" fill="none" viewBox="0 0 24 24">
              <circle cx="12" cy="12" r="9" fill="none" stroke="rgba(255,255,255,0.35)" stroke-width="3" />
              <path d="M12 3a9 9 0 0 1 9 9" fill="none" stroke="#fff" stroke-width="3" stroke-linecap="round" />
            </svg>
            <span>{{ loading ? 'Giriş Yapılıyor...' : 'Giriş Yap' }}</span>
            <svg v-if="!loading" class="h-[18px] w-[18px]" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round"><path d="M5 12h14M13 6l6 6-6 6" /></svg>
          </button>
        </form>
      </div>
    </Transition>

    <!-- ── Footer ── -->
    <p class="absolute bottom-[max(1.5rem,env(safe-area-inset-bottom))] left-0 right-0 text-center text-[11.5px] text-white/35">
      Hesap erişimi için sistem yöneticinize başvurun
    </p>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue';
import { useAuthStore } from '../stores/auth';
import { useRouter } from 'vue-router';
import { ApiErrorUtils } from '../utils/apiError';

const username = ref('');
const password = ref('');
const rememberMe = ref(false);
const error = ref('');
const loading = ref(false);
const showPassword = ref(false);
const focus = ref<'' | 'u' | 'p'>('');

const authStore = useAuthStore();
const router = useRouter();

const handleLogin = async () => {
  loading.value = true;
  error.value = '';
  try {
    await authStore.login(username.value, password.value, rememberMe.value);

    if (authStore.userRole === 'Driver') {
      router.push('/driver');
    } else if (authStore.userRole === 'Warehouse') {
      router.push('/warehouse');
    } else {
      router.push('/');
    }
  } catch (err: unknown) {
    error.value = ApiErrorUtils.getErrorMessage(err, 'Kullanıcı adı veya şifre hatalı.');
  } finally {
    loading.value = false;
  }
};
</script>

<style scoped>
/* Card / content entrance */
.rise-enter-active { animation: riseIn 0.6s cubic-bezier(0.16, 1, 0.3, 1) both; }
@keyframes riseIn {
  from { opacity: 0; transform: translateY(22px); }
  to   { opacity: 1; transform: translateY(0); }
}

/* Error */
.err-enter-active { animation: errIn 0.3s ease both; }
.err-leave-active { animation: errIn 0.2s ease reverse; }
@keyframes errIn {
  from { opacity: 0; transform: translateY(-6px); }
  to   { opacity: 1; transform: translateY(0); }
}

/* Button shimmer while loading */
.shimmer {
  background: linear-gradient(90deg, transparent, rgba(255,255,255,0.28), transparent);
  background-size: 200% 100%;
  animation: shim 1.1s linear infinite;
}
@keyframes shim {
  0%   { background-position: -150% 0; }
  100% { background-position: 250% 0; }
}
</style>
