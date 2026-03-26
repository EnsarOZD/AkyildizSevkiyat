<template>
  <div class="min-h-screen relative flex items-center justify-center overflow-hidden">

    <!-- ── Background: warehouse photo + layered overlays ── -->
    <div class="absolute inset-0 z-0">
      <img
        src="https://images.unsplash.com/photo-1553413077-190dd305871c?q=90&w=2070&auto=format&fit=crop"
        alt="Warehouse"
        class="w-full h-full object-cover scale-105"
        style="filter: brightness(0.45) saturate(0.8);"
      />
      <!-- gradient: darker at top & bottom, center transparent -->
      <div class="absolute inset-0 bg-gradient-to-b from-black/60 via-black/20 to-black/70"></div>
      <!-- subtle blue tint -->
      <div class="absolute inset-0 bg-[#0c1a2e]/40"></div>
    </div>

    <!-- ── Top bar ── -->
    <div class="absolute top-0 left-0 right-0 z-20 px-8 py-5 flex items-center justify-between">
      <div class="flex items-center gap-3">
        <img src="/logo-icon.svg" alt="Akyıldız" class="w-8 h-8 brightness-0 invert" />
        <span class="text-white font-bold text-lg tracking-tight">
          Akyıldız <span class="text-orange-400">Sevkiyat</span>
        </span>
      </div>
      <span class="text-white/40 text-xs hidden sm:block">Lojistik Yönetim Sistemi</span>
    </div>

    <!-- ── Animated login card ── -->
    <Transition appear name="card">
      <div class="relative z-10 w-full max-w-md mx-4">

        <!-- Card -->
        <div class="bg-white/95 backdrop-blur-md rounded-2xl shadow-2xl overflow-hidden">

          <!-- Orange top stripe -->
          <div class="h-1 w-full bg-gradient-to-r from-orange-500 via-orange-400 to-orange-600"></div>

          <div class="px-8 py-10">

            <!-- Header -->
            <div class="text-center mb-8">
              <div class="inline-flex items-center justify-center w-14 h-14 bg-orange-50 rounded-2xl mb-4">
                <img src="/logo-icon.svg" alt="Logo" class="w-8 h-8" />
              </div>
              <h1 class="text-2xl font-bold text-gray-900">Sisteme Giriş</h1>
              <p class="text-gray-400 text-sm mt-1">Kimlik bilgilerinizi girin</p>
            </div>

            <!-- Form -->
            <form @submit.prevent="handleLogin" class="space-y-5">

              <!-- Email -->
              <div>
                <label class="block text-xs font-semibold text-gray-500 uppercase tracking-wider mb-1.5">
                  E-posta
                </label>
                <div class="relative">
                  <div class="absolute inset-y-0 left-0 pl-3.5 flex items-center pointer-events-none">
                    <svg class="h-4 w-4 text-gray-400" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 8l7.89 5.26a2 2 0 002.22 0L21 8M5 19h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z" />
                    </svg>
                  </div>
                  <input
                    v-model="email"
                    type="email"
                    required
                    autocomplete="email"
                    placeholder="ornek@akyildiz.com"
                    class="w-full pl-10 pr-4 py-3 bg-gray-50 border border-gray-200 rounded-xl text-sm text-gray-900 placeholder-gray-400 focus:ring-2 focus:ring-orange-500 focus:border-transparent focus:bg-white focus:outline-none transition-all"
                  />
                </div>
              </div>

              <!-- Password -->
              <div>
                <label class="block text-xs font-semibold text-gray-500 uppercase tracking-wider mb-1.5">
                  Şifre
                </label>
                <div class="relative">
                  <div class="absolute inset-y-0 left-0 pl-3.5 flex items-center pointer-events-none">
                    <svg class="h-4 w-4 text-gray-400" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 15v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z" />
                    </svg>
                  </div>
                  <input
                    v-model="password"
                    :type="showPassword ? 'text' : 'password'"
                    required
                    autocomplete="current-password"
                    placeholder="••••••••"
                    class="w-full pl-10 pr-10 py-3 bg-gray-50 border border-gray-200 rounded-xl text-sm text-gray-900 placeholder-gray-400 focus:ring-2 focus:ring-orange-500 focus:border-transparent focus:bg-white focus:outline-none transition-all"
                  />
                  <button type="button" @click="showPassword = !showPassword"
                    class="absolute inset-y-0 right-0 pr-3.5 flex items-center text-gray-400 hover:text-gray-600 transition-colors">
                    <svg v-if="!showPassword" class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z" />
                    </svg>
                    <svg v-else class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13.875 18.825A10.05 10.05 0 0112 19c-4.478 0-8.268-2.943-9.543-7a9.97 9.97 0 011.563-3.029m5.858.908a3 3 0 114.243 4.243M9.878 9.878l4.242 4.242M9.88 9.88l-3.29-3.29m7.532 7.532l3.29 3.29M3 3l3.59 3.59m0 0A9.953 9.953 0 0112 5c4.478 0 8.268 2.943 9.543 7a10.025 10.025 0 01-4.132 5.411m0 0L21 21" />
                    </svg>
                  </button>
                </div>
              </div>

              <!-- Error -->
              <Transition name="error">
                <div v-if="error" class="flex items-center gap-2.5 bg-red-50 border border-red-200 rounded-xl px-4 py-3">
                  <svg class="h-4 w-4 text-red-500 shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
                  </svg>
                  <p class="text-sm text-red-700">{{ error }}</p>
                </div>
              </Transition>

              <!-- Submit button -->
              <button
                type="submit"
                :disabled="loading"
                class="relative w-full py-3 px-4 bg-orange-500 hover:bg-orange-600 active:bg-orange-700 disabled:opacity-60 text-white font-semibold text-sm rounded-xl shadow-lg shadow-orange-500/30 transition-all hover:-translate-y-0.5 hover:shadow-xl hover:shadow-orange-500/40 focus:outline-none focus:ring-2 focus:ring-orange-500 focus:ring-offset-2 flex items-center justify-center gap-2 overflow-hidden"
              >
                <!-- Shimmer effect while loading -->
                <span v-if="loading" class="absolute inset-0 bg-gradient-to-r from-transparent via-white/20 to-transparent animate-shimmer"></span>
                <svg v-if="loading" class="animate-spin h-4 w-4" fill="none" viewBox="0 0 24 24">
                  <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"/>
                  <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z"/>
                </svg>
                <span>{{ loading ? 'Giriş Yapılıyor...' : 'Giriş Yap' }}</span>
                <svg v-if="!loading" class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M14 5l7 7m0 0l-7 7m7-7H3" />
                </svg>
              </button>

            </form>

          </div>

          <!-- Card footer -->
          <div class="px-8 py-4 bg-gray-50 border-t border-gray-100 flex items-center justify-between">
            <span class="text-xs text-gray-400">© 2026 Akyıldız</span>
            <span class="text-xs text-gray-400">v1.0</span>
          </div>
        </div>

        <!-- Below card hint -->
        <p class="text-center text-white/40 text-xs mt-6">
          Hesap erişimi için sistem yöneticinize başvurun.
        </p>

      </div>
    </Transition>

  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue';
import { useAuthStore } from '../stores/auth';
import { useRouter } from 'vue-router';
import { ApiErrorUtils } from '../utils/apiError';

const email = ref('');
const password = ref('');
const error = ref('');
const loading = ref(false);
const showPassword = ref(false);

const authStore = useAuthStore();
const router = useRouter();

const handleLogin = async () => {
  loading.value = true;
  error.value = '';
  try {
    await authStore.login(email.value, password.value);
    
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
/* Card entrance animation */
.card-enter-active {
  animation: cardIn 0.6s cubic-bezier(0.16, 1, 0.3, 1) both;
}
@keyframes cardIn {
  from {
    opacity: 0;
    transform: translateY(40px) scale(0.97);
  }
  to {
    opacity: 1;
    transform: translateY(0) scale(1);
  }
}

/* Error message animation */
.error-enter-active {
  animation: errorIn 0.3s ease both;
}
.error-leave-active {
  animation: errorIn 0.2s ease reverse;
}
@keyframes errorIn {
  from { opacity: 0; transform: translateY(-6px); }
  to   { opacity: 1; transform: translateY(0); }
}

/* Shimmer */
@keyframes shimmer {
  0%   { transform: translateX(-100%); }
  100% { transform: translateX(100%); }
}
.animate-shimmer {
  animation: shimmer 1.2s infinite;
}

/* Background image subtle zoom */
img.scale-105 {
  animation: bgZoom 20s ease-in-out infinite alternate;
}
@keyframes bgZoom {
  from { transform: scale(1.05); }
  to   { transform: scale(1.12); }
}
</style>
