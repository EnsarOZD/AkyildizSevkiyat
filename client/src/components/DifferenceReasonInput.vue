<template>
  <div class="space-y-1.5">
    <select
      :value="isCustom ? OTHER : modelValue"
      @change="onSelect(($event.target as HTMLSelectElement).value)"
      class="w-full h-10 border rounded px-2 text-sm focus:ring-2 focus:ring-orange-400 focus:border-orange-400 outline-none bg-white dark:bg-gray-800 dark:text-gray-100"
      :class="modelValue ? 'border-gray-300 dark:border-gray-700' : 'border-orange-300 bg-orange-50 dark:bg-orange-950 dark:border-orange-700'"
      @click.stop
    >
      <option v-for="p in PRESETS" :key="p" :value="p">{{ p }}</option>
      <option :value="OTHER">Diğer…</option>
    </select>

    <input
      v-if="isCustom"
      type="text"
      :value="customText"
      @input="onCustomInput(($event.target as HTMLInputElement).value)"
      :placeholder="placeholder"
      class="w-full h-10 border rounded px-3 text-sm focus:ring-2 focus:ring-orange-400 focus:border-orange-400 outline-none border-gray-300 dark:border-gray-700 dark:bg-gray-800 dark:text-gray-100"
      @click.stop
    />
  </div>
</template>

<script setup lang="ts">
import { ref, watch, onMounted } from 'vue';

const props = withDefaults(defineProps<{
  modelValue: string;
  /** Boş geldiğinde otomatik seçilecek varsayılan neden. */
  defaultReason?: string;
  placeholder?: string;
}>(), {
  defaultReason: 'Stokta yok',
  placeholder: 'Nedeni yazın…',
});

const emit = defineEmits<{ (e: 'update:modelValue', value: string): void }>();

// Hızlı seçim için hazır nedenler. "Diğer" seçilince serbest metin alanı açılır.
const PRESETS = ['Stokta yok', 'Kısmi stok', 'Hasarlı / Bozuk', 'Yanlış ürün', 'Fazla geldi'];
const OTHER = '__other__';

const isCustom = ref(false);
const customText = ref('');

function syncFromModel(v: string) {
  if (v && !PRESETS.includes(v)) {
    isCustom.value = true;
    customText.value = v;
  } else {
    isCustom.value = false;
    customText.value = '';
  }
}

syncFromModel(props.modelValue);

// Alan ilk göründüğünde değer boşsa varsayılan nedeni uygula.
onMounted(() => {
  if (!props.modelValue) emit('update:modelValue', props.defaultReason);
});

watch(() => props.modelValue, (v) => {
  const current = isCustom.value ? customText.value : props.modelValue;
  if (v !== current) syncFromModel(v);
});

function onSelect(value: string) {
  if (value === OTHER) {
    isCustom.value = true;
    emit('update:modelValue', customText.value);
  } else {
    isCustom.value = false;
    customText.value = '';
    emit('update:modelValue', value);
  }
}

function onCustomInput(value: string) {
  customText.value = value;
  emit('update:modelValue', value);
}
</script>
