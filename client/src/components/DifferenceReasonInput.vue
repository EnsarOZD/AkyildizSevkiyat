<template>
  <div class="space-y-1.5">
    <select
      :value="isCustom ? OTHER : modelValue"
      @change="onSelect(($event.target as HTMLSelectElement).value)"
      class="w-full h-10 border rounded px-2 text-sm focus:ring-2 focus:ring-orange-400 focus:border-orange-400 outline-none bg-white dark:bg-gray-800 dark:text-gray-100"
      :class="modelValue ? 'border-gray-300 dark:border-gray-700' : 'border-orange-300 bg-orange-50 dark:bg-orange-950 dark:border-orange-700'"
      @click.stop
    >
      <option value="" disabled>Sebep seçiniz…</option>
      <option v-for="p in reasons" :key="p" :value="p">{{ p }}</option>
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
import { useDefinedReasons, ReasonCategory } from '../composables/useDefinedReasons';

const props = withDefaults(defineProps<{
  modelValue: string;
  /**
   * Geriye dönük uyumluluk için kabul edilir ama ARTIK OTOMATİK UYGULANMAZ.
   * Sebep boş başlar; depocu bilinçli olarak seçmek zorundadır (yanlışlıkla
   * varsayılan sebep girmeyi önlemek için).
   */
  defaultReason?: string;
  placeholder?: string;
}>(), {
  defaultReason: '',
  placeholder: 'Nedeni yazın…',
});

const emit = defineEmits<{ (e: 'update:modelValue', value: string): void }>();

// Sebepler artık DB'den yönetilir (Sebep Tanımları ekranı). "Diğer" seçilince
// serbest metin alanı açılır. API erişilemezse composable fallback listesi döner.
const { reasons, load } = useDefinedReasons(ReasonCategory.PickingDifference);
const OTHER = '__other__';

const isCustom = ref(false);
const customText = ref('');

function syncFromModel(v: string) {
  if (v && !reasons.value.includes(v)) {
    isCustom.value = true;
    customText.value = v;
  } else {
    isCustom.value = false;
    customText.value = '';
  }
}

syncFromModel(props.modelValue);

watch(() => props.modelValue, (v) => {
  const current = isCustom.value ? customText.value : props.modelValue;
  if (v !== current) syncFromModel(v);
});

// Liste geldikten sonra mevcut değeri yeniden değerlendir (preset mi serbest mi).
onMounted(async () => {
  await load();
  syncFromModel(props.modelValue);
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
