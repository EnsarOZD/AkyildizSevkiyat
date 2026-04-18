<template>
  <div class="bg-white dark:bg-gray-900 border border-gray-200 dark:border-gray-700 rounded-xl overflow-hidden max-w-full w-full">
    <div v-for="(group, groupIdx) in groupedLines" :key="groupIdx" class="border-b last:border-b-0">
      <div
        class="px-5 py-2 font-bold text-sm flex justify-between items-center"
        :class="group.zoneName === 'Tanımsız' || group.zoneName === 'No Zone'
          ? 'bg-red-100 dark:bg-red-900/30 text-red-800 dark:text-red-400'
          : 'bg-gray-100 dark:bg-gray-800 text-gray-700 dark:text-gray-300'"
      >
        <span class="break-words mr-2 text-left">{{ group.zoneName }}</span>
        <span class="text-xs font-normal bg-white dark:bg-gray-900 px-2 py-1 rounded border dark:border-gray-700 whitespace-nowrap">
          {{ group.lines.length }} Kalem
        </span>
      </div>
      <!-- Table View (Desktop/Tablet) -->
      <div class="hidden md:block overflow-x-auto">
        <table class="min-w-full divide-y divide-gray-200 dark:divide-gray-700">
          <thead v-if="groupIdx === 0" class="bg-gray-50 dark:bg-gray-800">
            <tr>
              <th class="px-5 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Stok Kodu</th>
              <th class="px-5 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider min-w-[200px]">Stok Adı</th>
              <th class="px-5 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Birim</th>
              <th class="px-5 py-3 text-right text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Sipariş</th>
              <th class="px-5 py-3 text-right text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Teslim</th>
            </tr>
          </thead>
          <tbody class="bg-white dark:bg-gray-900 divide-y divide-gray-200 dark:divide-gray-700">
            <tr v-for="line in group.lines" :key="line.id">
              <td class="px-5 py-3 whitespace-nowrap text-sm font-mono text-gray-900 dark:text-gray-100">
                {{ line.localStockCode || line.stockCode }}
                <div v-if="line.localStockCode && line.localStockCode !== line.stockCode" class="text-xs text-gray-400">
                  ISS: {{ line.stockCode }}
                </div>
              </td>
              <td class="px-5 py-3 text-sm text-gray-900 dark:text-gray-100">{{ line.stockName }}</td>
              <td class="px-5 py-3 whitespace-nowrap text-sm text-gray-500 dark:text-gray-400">{{ line.unit || '-' }}</td>
              <td class="px-5 py-3 whitespace-nowrap text-sm font-bold text-gray-900 dark:text-gray-100 text-right">{{ line.orderedQty }}</td>
              <td
                class="px-5 py-3 whitespace-nowrap text-sm text-right"
                :class="line.deliveredQty > 0 && line.deliveredQty !== line.orderedQty
                  ? 'text-red-600 font-bold'
                  : 'text-gray-900 dark:text-gray-100'"
              >
                {{ line.deliveredQty > 0 ? line.deliveredQty : '-' }}
              </td>
            </tr>
          </tbody>
        </table>
      </div>

      <!-- Card View (Mobile) -->
      <div class="md:hidden divide-y divide-gray-200 dark:divide-gray-700">
        <div v-for="line in group.lines" :key="line.id" class="px-5 py-4 space-y-2">
          <div class="flex justify-between items-start gap-4 text-left">
            <div class="min-w-0 flex-1">
              <div class="text-[10px] font-mono text-gray-500 dark:text-gray-400">
                {{ line.localStockCode || line.stockCode }}
              </div>
              <div class="text-sm font-bold text-gray-900 dark:text-gray-100 mt-0.5 leading-tight">
                {{ line.stockName }}
              </div>
            </div>
            <div class="flex-shrink-0">
              <span class="text-[10px] font-bold bg-gray-100 dark:bg-gray-800 text-gray-600 dark:text-gray-400 px-1.5 py-0.5 rounded border border-gray-200 dark:border-gray-700 uppercase">
                {{ line.unit || '-' }}
              </span>
            </div>
          </div>
          <div class="flex justify-between items-center pt-1 border-t border-gray-50 dark:border-gray-800/50 mt-2">
            <div class="text-xs text-gray-500 dark:text-gray-400 font-medium">
              Sipariş: <span class="font-bold text-gray-900 dark:text-gray-100 ml-1">{{ line.orderedQty }}</span>
            </div>
            <div v-if="line.deliveredQty > 0" class="text-xs">
              <span class="text-gray-500 dark:text-gray-400 font-medium">Teslimat: </span>
              <span 
                class="ml-1 px-1.5 py-0.5 rounded-full"
                :class="line.deliveredQty !== line.orderedQty 
                  ? 'bg-red-50 dark:bg-red-900/20 text-red-600 dark:text-red-400 font-bold' 
                  : 'bg-green-50 dark:bg-green-900/20 text-green-600 dark:text-green-400 font-bold'"
              >
                {{ line.deliveredQty }}
              </span>
            </div>
            <div v-else class="text-xs text-gray-500 dark:text-gray-400 italic font-medium">Teslim edilmedi</div>
          </div>
        </div>
      </div>
    </div>
    <div v-if="groupedLines.length === 0" class="px-5 py-10 text-center text-sm text-gray-400">
      Ürün kaydı bulunamadı.
    </div>
  </div>
</template>

<script setup lang="ts">
interface ShipmentLine {
  id: number
  stockCode: string
  localStockCode?: string
  stockName: string
  unit?: string
  orderedQty: number
  deliveredQty: number
}

interface LineGroup {
  zoneName: string
  lines: ShipmentLine[]
}

defineProps<{ groupedLines: LineGroup[] }>()
</script>
