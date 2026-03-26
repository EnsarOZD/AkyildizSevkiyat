<template>
  <div class="relative">
    <div class="flex gap-2">
      <div class="relative flex-grow">
        <input
          type="text"
          class="w-full bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-700 rounded-md shadow-sm pl-3 pr-10 py-2 text-left cursor-default focus:outline-none focus:ring-1 focus:ring-indigo-500 focus:border-indigo-500 sm:text-sm dark:text-gray-100"
          :placeholder="placeholder || 'Tedarikçi Ara...'"
          v-model="searchTerm"
          @focus="isOpen = true"
          @input="onInput"
        >
        <div class="absolute inset-y-0 right-0 flex items-center pr-2 pointer-events-none">
          <svg class="h-5 w-5 text-gray-400" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" aria-hidden="true">
            <path fill-rule="evenodd" d="M10 3a1 1 0 01.707.293l3 3a1 1 0 01-1.414 1.414L10 5.414 7.707 7.707a1 1 0 01-1.414-1.414l3-3A1 1 0 0110 3zm-3.707 9.293a1 1 0 011.414 0L10 14.586l2.293-2.293a1 1 0 011.414 1.414l-3 3a1 1 0 01-1.414 0l-3-3a1 1 0 010-1.414z" clip-rule="evenodd" />
          </svg>
        </div>

        <ul v-if="isOpen && filteredSuppliers.length > 0" class="absolute z-10 mt-1 w-full bg-white dark:bg-gray-800 shadow-lg max-h-60 rounded-md py-1 text-base ring-1 ring-black ring-opacity-5 overflow-auto focus:outline-none sm:text-sm">
          <li
            v-for="supplier in filteredSuppliers"
            :key="supplier.id"
            class="cursor-pointer select-none relative py-2 pl-3 pr-9 hover:bg-indigo-600 hover:text-white text-gray-900 dark:text-gray-100"
            @click="selectSupplier(supplier)"
          >
            <span class="block truncate" :class="{ 'font-semibold': modelValue === supplier.id }">
              {{ supplier.name }} <span v-if="supplier.supplierCode" class="text-xs text-gray-500 dark:text-gray-400 ml-1">({{ supplier.supplierCode }})</span>
            </span>
            <span v-if="modelValue === supplier.id" class="absolute inset-y-0 right-0 flex items-center pr-4 text-indigo-600 hover:text-white">
              <svg class="h-5 w-5" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" aria-hidden="true">
                <path fill-rule="evenodd" d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z" clip-rule="evenodd" />
              </svg>
            </span>
          </li>
        </ul>
      </div>

      <button
        type="button"
        class="inline-flex items-center px-3 py-2 border border-gray-300 dark:border-gray-700 shadow-sm text-sm leading-4 font-medium rounded-md text-gray-700 dark:text-gray-300 bg-white dark:bg-gray-800 hover:bg-gray-50 dark:hover:bg-gray-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500"
        @click="showCreateModal = true"
      >
        <svg xmlns="http://www.w3.org/2000/svg" class="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
        </svg>
      </button>
    </div>

    <!-- Create Modal -->
    <CreateSupplierModal
        :isOpen="showCreateModal"
        @close="showCreateModal = false"
        @saved="onSupplierCreated"
    />
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, computed, watch } from 'vue';
import { supplierService } from '../services/supplierService';
import type { Supplier } from '../services/supplierService';
import CreateSupplierModal from './CreateSupplierModal.vue';

const props = defineProps(['modelValue', 'placeholder']); // modelValue is the Supplier ID
const emit = defineEmits(['update:modelValue', 'change']);

const searchTerm = ref('');
const isOpen = ref(false);
const suppliers = ref<Supplier[]>([]);
const showCreateModal = ref(false);
let searchTimeout: any = null;

const loadSuppliers = async (query = '') => {
  try {
    suppliers.value = await supplierService.getAll(query);
  } catch (e) {
    console.error(e);
  }
};

const filteredSuppliers = computed(() => {
    return suppliers.value;
});

const onInput = () => {
    if (searchTimeout) clearTimeout(searchTimeout);
    searchTimeout = setTimeout(() => {
        loadSuppliers(searchTerm.value);
    }, 300);
};

const selectSupplier = (supplier: Supplier) => {
    emit('update:modelValue', supplier.id);
    emit('change', supplier);
    searchTerm.value = supplier.name;
    isOpen.value = false;
};

const onSupplierCreated = (newSupplier: Supplier) => {
    suppliers.value.push(newSupplier);
    selectSupplier(newSupplier);
};

onMounted(() => {
   loadSuppliers();
});

// Watch modelValue to sync searchTerm if externally set?
watch(() => props.modelValue, (newVal) => {
    if (!newVal) {
        searchTerm.value = '';
    } else {
        const found = suppliers.value.find(s => s.id === newVal);
        if (found) searchTerm.value = found.name;
        // else maybe load detail? For now assume it's in list or we handle it
    }
});

// Close click outside logic (simplified using blur delay or global click listener)
// For now, strict 'isOpen' toggle on focus/selection.
</script>
