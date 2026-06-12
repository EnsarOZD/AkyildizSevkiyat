<template>
  <span class="inline-flex items-center gap-1.5 px-2.5 py-0.5 rounded-full text-xs font-semibold" :class="config.classes">
    <span class="w-1.5 h-1.5 rounded-full bg-current opacity-70"></span>
    {{ config.label }}
  </span>
</template>

<script setup lang="ts">
import { computed } from 'vue';

type ShipmentStatus =
  | 'Created' | 'AssignedToWarehouse' | 'Picking'
  | 'ReadyForDispatch' | 'AssignedToVehicle' | 'Dispatched'
  | 'Delivered' | 'ReturnedToWarehouse' | 'Cancelled' | 'Passive';

type PurchaseOrderStatus =
  | 'Draft' | 'Approved' | 'PartiallyReceived' | 'FullyReceived' | 'Closed' | 'Cancelled';

type GoodsReceiptStatus = 'Draft' | 'Posted' | 'Cancelled';

type FloatingReturnStatus = 'Pending' | 'MatchedToShipment' | 'AddedToStock' | 'WrittenOff';

type StockCountStatus = 'Draft' | 'Completed';

interface StatusConfig { label: string; classes: string; }

const SHIPMENT_MAP: Record<ShipmentStatus, StatusConfig> = {
  Created:             { label: 'Taslak',         classes: 'bg-gray-100 dark:bg-gray-800 text-gray-700 dark:text-gray-400' },
  AssignedToWarehouse: { label: 'Depoda',          classes: 'bg-yellow-100 dark:bg-yellow-900/30 text-yellow-800 dark:text-yellow-400' },
  Picking:             { label: 'Toplanıyor',      classes: 'bg-blue-100 dark:bg-blue-900/30 text-blue-800 dark:text-blue-400' },
  ReadyForDispatch:    { label: 'Sevke Hazır',     classes: 'bg-blue-100 dark:bg-blue-900/30 text-blue-800 dark:text-blue-400 transition-all' },
  AssignedToVehicle:   { label: 'Araçta',          classes: 'bg-violet-100 dark:bg-violet-900/30 text-violet-800 dark:text-violet-400' },
  Dispatched:          { label: 'Yolda',           classes: 'bg-orange-100 dark:bg-orange-900/30 text-orange-800 dark:text-orange-400' },
  Delivered:           { label: 'Teslim Edildi',   classes: 'bg-green-100 dark:bg-green-900/30 text-green-800 dark:text-green-400' },
  ReturnedToWarehouse: { label: 'İade',            classes: 'bg-orange-100 dark:bg-orange-900/30 text-orange-800 dark:text-orange-400' },
  Cancelled:           { label: 'İptal',           classes: 'bg-red-100 dark:bg-red-900/30 text-red-700 dark:text-red-400' },
  Passive:             { label: 'Pasif',           classes: 'bg-gray-200 dark:bg-gray-800 text-gray-500 dark:text-gray-500' },
};

const PURCHASE_ORDER_MAP: Record<PurchaseOrderStatus, StatusConfig> = {
  Draft:             { label: 'Taslak',           classes: 'bg-gray-100 dark:bg-gray-800 text-gray-700 dark:text-gray-400' },
  Approved:          { label: 'Onaylandı',        classes: 'bg-blue-100 dark:bg-blue-900/30 text-blue-800 dark:text-blue-400' },
  PartiallyReceived: { label: 'Kısmen Alındı',    classes: 'bg-yellow-100 dark:bg-yellow-900/30 text-yellow-800 dark:text-yellow-400' },
  FullyReceived:     { label: 'Tam Alındı',       classes: 'bg-green-100 dark:bg-green-900/30 text-green-800 dark:text-green-400' },
  Closed:            { label: 'Kapatıldı',        classes: 'bg-gray-200 dark:bg-gray-800 text-gray-600 dark:text-gray-500' },
  Cancelled:         { label: 'İptal',            classes: 'bg-red-100 dark:bg-red-900/30 text-red-700 dark:text-red-400' },
};

const GOODS_RECEIPT_MAP: Record<GoodsReceiptStatus, StatusConfig> = {
  Draft:     { label: 'Taslak',   classes: 'bg-gray-100 dark:bg-gray-800 text-gray-700 dark:text-gray-400' },
  Posted:    { label: 'Onaylandı',classes: 'bg-green-100 dark:bg-green-900/30 text-green-800 dark:text-green-400' },
  Cancelled: { label: 'İptal',    classes: 'bg-red-100 dark:bg-red-900/30 text-red-700 dark:text-red-400' },
};

const FLOATING_RETURN_MAP: Record<FloatingReturnStatus, StatusConfig> = {
  Pending:           { label: 'Beklemede',        classes: 'bg-orange-100 dark:bg-orange-900/30 text-orange-800 dark:text-orange-400' },
  MatchedToShipment: { label: 'Eşleştirildi',    classes: 'bg-blue-100 dark:bg-blue-900/30 text-blue-800 dark:text-blue-400' },
  AddedToStock:      { label: 'Stoğa Eklendi',   classes: 'bg-green-100 dark:bg-green-900/30 text-green-800 dark:text-green-400' },
  WrittenOff:        { label: 'Hariç Tutuldu',   classes: 'bg-gray-200 dark:bg-gray-800 text-gray-600 dark:text-gray-500' },
};

const STOCK_COUNT_MAP: Record<StockCountStatus, StatusConfig> = {
  Draft:     { label: 'Devam Ediyor', classes: 'bg-yellow-100 dark:bg-yellow-900/30 text-yellow-800 dark:text-yellow-400' },
  Completed: { label: 'Tamamlandı',   classes: 'bg-green-100 dark:bg-green-900/30 text-green-800 dark:text-green-400' },
};

const props = withDefaults(defineProps<{
  status: string;
  type?: 'shipment' | 'purchaseOrder' | 'goodsReceipt' | 'floatingReturn' | 'stockCount';
}>(), { type: 'shipment' });

const config = computed((): StatusConfig => {
  const fallback: StatusConfig = { label: props.status, classes: 'bg-gray-100 text-gray-700' };

  switch (props.type) {
    case 'shipment':
      return SHIPMENT_MAP[props.status as ShipmentStatus] ?? fallback;
    case 'purchaseOrder':
      return PURCHASE_ORDER_MAP[props.status as PurchaseOrderStatus] ?? fallback;
    case 'goodsReceipt':
      return GOODS_RECEIPT_MAP[props.status as GoodsReceiptStatus] ?? fallback;
    case 'floatingReturn':
      return FLOATING_RETURN_MAP[props.status as FloatingReturnStatus] ?? fallback;
    case 'stockCount':
      return STOCK_COUNT_MAP[props.status as StockCountStatus] ?? fallback;
    default:
      return fallback;
  }
});
</script>
