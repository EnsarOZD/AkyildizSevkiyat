<template>
  <Teleport to="body">
    <Transition
      enter-active-class="transition duration-300 ease-out"
      enter-from-class="opacity-0"
      enter-to-class="opacity-100"
      leave-active-class="transition duration-200 ease-in pointer-events-none"
      leave-from-class="opacity-100"
      leave-to-class="opacity-0"
    >
      <div v-if="show" class="fixed inset-0 z-[100] overflow-y-auto">
        <!-- Backdrop -->
        <div
          class="fixed inset-0 bg-black/50 backdrop-blur-sm"
          @click="onBackdropClick"
        ></div>

        <!-- Modal Content -->
        <div class="flex min-h-screen items-center justify-center p-4">
          <Transition
            enter-active-class="transition duration-300 ease-out"
            enter-from-class="opacity-0 scale-95 translate-y-4"
            enter-to-class="opacity-100 scale-100 translate-y-0"
            leave-active-class="transition duration-200 ease-in"
            leave-from-class="opacity-100 scale-100 translate-y-0"
            leave-to-class="opacity-0 scale-95 translate-y-4"
          >
            <div
              class="relative bg-white dark:bg-gray-900 rounded-xl shadow-2xl w-full overflow-hidden flex flex-col"
              :class="maxWidthClass"
              role="dialog"
              aria-modal="true"
              :aria-labelledby="titleId"
              @keydown.esc.prevent="close"
              tabindex="-1"
              ref="dialogRef"
            >
              <!-- Header -->
              <div class="px-6 py-4 border-b border-gray-100 dark:border-gray-700 flex items-center justify-between bg-gray-50/50 dark:bg-gray-800/50">
                <h3 class="text-xl font-bold text-gray-900 dark:text-gray-100" :id="titleId">
                  <slot name="title">{{ title }}</slot>
                </h3>
                <button
                  type="button"
                  @click="close"
                  class="p-2 text-gray-400 hover:text-gray-600 hover:bg-gray-200 dark:hover:bg-gray-700 rounded-lg transition-colors"
                  aria-label="Kapat"
                >
                  <svg xmlns="http://www.w3.org/2000/svg" class="h-6 w-6" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
                  </svg>
                </button>
              </div>

              <!-- Body -->
              <div class="px-6 py-6 overflow-y-auto max-h-[75vh]">
                <slot></slot>
              </div>

              <!-- Footer -->
              <div v-if="hasFooter" class="px-6 py-4 border-t border-gray-100 dark:border-gray-700 bg-gray-50/50 dark:bg-gray-800/50 flex flex-wrap items-center justify-end gap-3 [&>button]:min-h-[44px] [&>button]:py-3 [&>button]:px-4 [&>button]:w-full [&>button]:sm:w-auto">
                <slot name="footer"></slot>
              </div>
            </div>
          </Transition>
        </div>
      </div>
    </Transition>
  </Teleport>
</template>

<script setup lang="ts">
import { computed, useSlots, onUnmounted, watch, ref } from 'vue';

interface Props {
  show: boolean;
  title?: string;
  maxWidth?: 'sm' | 'md' | 'lg' | 'xl' | '2xl' | '3xl' | '4xl' | '5xl';
  closeOnBackdrop?: boolean;
}

const props = withDefaults(defineProps<Props>(), {
  show: false,
  title: '',
  maxWidth: 'md',
  closeOnBackdrop: true,
});

const emit = defineEmits<{
  (e: 'close'): void;
}>();

const slots = useSlots();
const hasFooter = computed(() => !!slots.footer);

const dialogRef = ref<HTMLElement | null>(null);
const titleId = `modal-title-${Math.random().toString(36).slice(2)}`;

const close = () => emit('close');

const onBackdropClick = () => {
  if (!props.closeOnBackdrop) return;
  close();
};

const maxWidthClass = computed(() => {
  const classes: Record<string, string> = {
    sm: 'max-w-sm',
    md: 'max-w-md',
    lg: 'max-w-lg',
    xl: 'max-w-xl',
    '2xl': 'max-w-2xl',
    '3xl': 'max-w-3xl',
    '4xl': 'max-w-4xl',
    '5xl': 'max-w-5xl',
  };
  return classes[props.maxWidth] ?? classes.md;
});

// body scroll lock + focus
watch(
  () => props.show,
  (isOpen) => {
    if (isOpen) {
      document.body.style.overflow = 'hidden';
      // next tick not required in most cases; still safe:
      setTimeout(() => dialogRef.value?.focus(), 0);
    } else {
      document.body.style.overflow = '';
    }
  },
  { immediate: true }
);

onUnmounted(() => {
  document.body.style.overflow = '';
});
</script>
