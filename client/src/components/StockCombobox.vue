<template>
  <div class="relative w-full">
    <div class="relative">
        <input
            ref="inputRef"
            type="text"
            class="w-full border rounded px-2 py-1 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 dark:bg-gray-800 dark:border-gray-700 dark:text-gray-100"
            :placeholder="placeholder"
            v-model="searchTerm"
            @input="onInput"
            @focus="onFocus"
            @blur="delayedHide"
        />
        <div v-if="loading" class="absolute right-2 top-1.5">
            <svg class="animate-spin h-4 w-4 text-gray-400" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
                <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
            </svg>
        </div>
    </div>

    <!-- Dropdown teleported to body to escape overflow constraints -->
    <Teleport to="body">
      <div v-if="showDropdown && (results.length > 0)"
           :style="dropdownStyle"
           class="fixed z-[9999] bg-white dark:bg-gray-800 border dark:border-gray-700 rounded shadow-lg max-h-60 overflow-y-auto text-sm">
          <div
              v-for="item in results"
              :key="item.id || item.Id"
              class="px-3 py-2 cursor-pointer hover:bg-blue-100 dark:hover:bg-blue-800/50 border-b dark:border-gray-700 last:border-b-0"
              @mousedown.prevent="selectItem(item)"
          >
              <div class="font-bold text-gray-800 dark:text-gray-200">{{ item.stockCode || item.StockCode }}</div>
              <div class="text-xs text-gray-600 dark:text-gray-400 break-words whitespace-normal">{{ item.stockName || item.StockName }}</div>
          </div>
      </div>

      <div v-if="showDropdown && results.length === 0 && searchTerm.length > 1 && !loading"
           :style="dropdownStyle"
           class="fixed z-[9999] bg-white dark:bg-gray-800 border dark:border-gray-700 rounded shadow-lg p-2 text-xs text-gray-500 dark:text-gray-400">
           No matches found.
      </div>
    </Teleport>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, onUnmounted, watch, type CSSProperties } from 'vue';
import { stockService } from '../services/stockService';

const props = defineProps<{
    modelValue?: any;
    placeholder?: string;
    initialCode?: string;
    /** 0 = Catering context (excludes Kıyafet), 1 = Clothing context (only Kıyafet), undefined = all */
    operationType?: number;
}>();

const emit = defineEmits(['update:modelValue', 'search', 'select']);

const searchTerm = ref('');
const results = ref<any[]>([]);
const loading = ref(false);
const showDropdown = ref(false);
let debounceTimer: any = null;
let isInternalChange = false;

// Keep search term in sync with model value (for clearing/etc)
watch(() => props.modelValue, (newVal) => {
    if (!newVal || newVal === 0) {
        if (!isInternalChange) searchTerm.value = '';
    }
});

// Use watch for more reliable reactivity than @input
watch(searchTerm, (newVal) => {
    if (isInternalChange) {
        isInternalChange = false;
        return;
    }

    emit('search', newVal);

    if (debounceTimer) clearTimeout(debounceTimer);

    if (!newVal || newVal.length < 1) {
        results.value = [];
        showDropdown.value = false;
        return;
    }

    debounceTimer = setTimeout(() => {
        search();
    }, 400);
});

const onInput = (_e: Event) => {
    showDropdown.value = true;
};

const search = async () => {
    loading.value = true;
    try {
        const categoryFilter = props.operationType === 0
            ? { excludeCategoryId: 3 }
            : props.operationType === 1
              ? { categoryId: 3 }
              : {};
        const data = await stockService.getAll({
            search: searchTerm.value,
            page: 1,
            size: 100,
            ...categoryFilter,
        });

        results.value = data.items;
        updateDropdownPosition();
        showDropdown.value = true;
    } catch (error) {
        console.error('[StockCombobox] Search error:', error);
    } finally {
        loading.value = false;
    }
};

const selectItem = (item: any) => {
    const code = item.stockCode || item.StockCode;
    const name = item.stockName || item.StockName;
    isInternalChange = true;
    searchTerm.value = `${code} - ${name}`;

    const id = item.id || item.Id;
    emit('update:modelValue', id);
    emit('select', item);
    showDropdown.value = false;
};

const delayedHide = () => {
    setTimeout(() => {
        showDropdown.value = false;
    }, 250);
};

const inputRef = ref<HTMLInputElement | null>(null);
const dropdownStyle = ref<CSSProperties>({});

const updateDropdownPosition = () => {
    if (!inputRef.value) return;
    const rect = inputRef.value.getBoundingClientRect();
    dropdownStyle.value = {
        top: `${rect.bottom + 4}px`,
        left: `${rect.left}px`,
        width: `${rect.width}px`,
    };
};

const onFocus = () => {
    updateDropdownPosition();
    showDropdown.value = true;
};

const focus = () => {
    inputRef.value?.focus();
};

defineExpose({ focus });

onMounted(() => {
    if (props.initialCode) {
        isInternalChange = true;
        searchTerm.value = props.initialCode;
    }
    window.addEventListener('scroll', updateDropdownPosition, true);
    window.addEventListener('resize', updateDropdownPosition);
});

onUnmounted(() => {
    window.removeEventListener('scroll', updateDropdownPosition, true);
    window.removeEventListener('resize', updateDropdownPosition);
});
</script>
