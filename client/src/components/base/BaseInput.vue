<template>
  <div :class="wrapperClass">
    <label
      v-if="label"
      :for="inputId"
      class="block text-sm font-medium text-gray-700 dark:text-gray-300"
      :style="{ marginBottom: '0.375rem' }"
    >
      {{ label }}
      <span v-if="required" class="text-danger-500 ml-0.5">*</span>
    </label>

    <div class="relative">
      <!-- Left icon slot -->
      <div
        v-if="$slots.iconLeft"
        class="pointer-events-none absolute inset-y-0 left-0 flex items-center pl-3 text-gray-400"
      >
        <slot name="iconLeft" />
      </div>

      <input
        :id="inputId"
        ref="inputRef"
        v-bind="$attrs"
        :type="type"
        :value="modelValue"
        :placeholder="placeholder"
        :disabled="disabled"
        :readonly="readonly"
        :required="required"
        :aria-invalid="!!error"
        :aria-describedby="error ? `${inputId}-error` : hint ? `${inputId}-hint` : undefined"
        :class="inputClasses"
        @input="$emit('update:modelValue', ($event.target as HTMLInputElement).value)"
        @blur="$emit('blur', $event)"
        @focus="$emit('focus', $event)"
      />

      <!-- Right icon / error icon -->
      <div
        v-if="$slots.iconRight || error"
        class="absolute inset-y-0 right-0 flex items-center pr-3"
      >
        <svg v-if="error" class="h-5 w-5 text-danger-500" viewBox="0 0 20 20" fill="currentColor">
          <path fill-rule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-8-5a.75.75 0 01.75.75v4.5a.75.75 0 01-1.5 0v-4.5A.75.75 0 0110 5zm0 10a1 1 0 100-2 1 1 0 000 2z" clip-rule="evenodd" />
        </svg>
        <slot v-else name="iconRight" />
      </div>
    </div>

    <p v-if="error" :id="`${inputId}-error`" class="mt-1 text-sm text-danger-600 dark:text-danger-400" role="alert">
      {{ error }}
    </p>
    <p v-else-if="hint" :id="`${inputId}-hint`" class="mt-1 text-xs text-gray-500 dark:text-gray-400">
      {{ hint }}
    </p>
  </div>
</template>

<script setup lang="ts">
import { computed, ref, useId, useSlots } from 'vue'

defineOptions({ inheritAttrs: false })

const props = defineProps({
  modelValue:  { type: [String, Number], default: '' },
  label:       { type: String, default: '' },
  type:        { type: String, default: 'text' },
  placeholder: { type: String, default: '' },
  hint:        { type: String, default: '' },
  error:       { type: String, default: '' },
  disabled:    { type: Boolean, default: false },
  readonly:    { type: Boolean, default: false },
  required:    { type: Boolean, default: false },
  size:        { type: String as () => 'sm' | 'md' | 'lg', default: 'md' },
})

defineEmits<{
  'update:modelValue': [value: string]
  'blur': [event: FocusEvent]
  'focus': [event: FocusEvent]
}>()

const inputRef = ref<HTMLInputElement | null>(null)
const inputId = useId()
const slots = useSlots()

const sizeClasses: Record<string, string> = {
  sm: 'px-2.5 py-1.5 text-sm',
  md: 'px-3 py-2 text-sm',
  lg: 'px-4 py-3 text-base',
}

const inputClasses = computed(() => [
  'block w-full rounded-input border transition-colors duration-150',
  'bg-white dark:bg-gray-800',
  'text-gray-900 dark:text-gray-100',
  'placeholder:text-gray-400 dark:placeholder:text-gray-500',
  sizeClasses[props.size],
  props.error
    ? 'border-danger-300 dark:border-danger-600 focus:ring-danger-500 focus:border-danger-500'
    : 'border-gray-300 dark:border-gray-600 focus:ring-brand-500 focus:border-brand-500',
  'focus:outline-none focus:ring-2 focus:ring-offset-0',
  props.disabled && 'opacity-50 cursor-not-allowed bg-gray-50 dark:bg-gray-900',
  slots.iconLeft && 'pl-10',
  (slots.iconRight || props.error) && 'pr-10',
])

const wrapperClass = computed(() => props.size === 'lg' ? 'space-y-2' : undefined)

defineExpose({ focus: () => inputRef.value?.focus() })
</script>
