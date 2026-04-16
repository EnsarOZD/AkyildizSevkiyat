<template>
  <div>
    <label
      v-if="label"
      :for="selectId"
      class="block text-sm font-medium text-gray-700 dark:text-gray-300"
      :style="{ marginBottom: '0.375rem' }"
    >
      {{ label }}
      <span v-if="required" class="text-danger-500 ml-0.5">*</span>
    </label>

    <select
      :id="selectId"
      v-bind="$attrs"
      :value="modelValue"
      :disabled="disabled"
      :required="required"
      :aria-invalid="!!error"
      :class="selectClasses"
      @change="$emit('update:modelValue', ($event.target as HTMLSelectElement).value)"
      @blur="$emit('blur', $event)"
    >
      <slot />
    </select>

    <p v-if="error" class="mt-1 text-sm text-danger-600 dark:text-danger-400" role="alert">
      {{ error }}
    </p>
    <p v-else-if="hint" class="mt-1 text-xs text-gray-500 dark:text-gray-400">
      {{ hint }}
    </p>
  </div>
</template>

<script setup lang="ts">
import { computed, useId } from 'vue'

defineOptions({ inheritAttrs: false })

const props = defineProps({
  modelValue: { type: [String, Number, null] as any, default: null },
  label:      { type: String, default: '' },
  hint:       { type: String, default: '' },
  error:      { type: String, default: '' },
  disabled:   { type: Boolean, default: false },
  required:   { type: Boolean, default: false },
  size:       { type: String as () => 'sm' | 'md' | 'lg', default: 'md' },
})

defineEmits<{
  'update:modelValue': [value: string]
  'blur': [event: FocusEvent]
}>()

const selectId = useId()

const sizeClasses: Record<string, string> = {
  sm: 'px-2.5 py-1.5 text-sm',
  md: 'px-3 py-2 text-sm',
  lg: 'px-4 py-3 text-base',
}

const selectClasses = computed(() => [
  'block w-full rounded-input border transition-colors duration-150',
  'bg-white dark:bg-gray-800',
  'text-gray-900 dark:text-gray-100',
  sizeClasses[props.size],
  props.error
    ? 'border-danger-300 dark:border-danger-600 focus:ring-danger-500 focus:border-danger-500'
    : 'border-gray-300 dark:border-gray-600 focus:ring-brand-500 focus:border-brand-500',
  'focus:outline-none focus:ring-2 focus:ring-offset-0',
  props.disabled && 'opacity-50 cursor-not-allowed bg-gray-50 dark:bg-gray-900',
])
</script>
