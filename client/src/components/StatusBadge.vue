<template>
  <span class="inline-flex items-center px-2 py-0.5 rounded-full text-xs font-semibold" :class="config.classes">
    {{ config.label }}
  </span>
</template>

<script setup lang="ts">
import { computed } from 'vue';

type ShipmentStatus =
  | 'Created' | 'AssignedToWarehouse' | 'Picking'
  | 'ReadyForDispatch' | 'AssignedToVehicle' | 'Delivered'
  | 'ReturnedToWarehouse' | 'Cancelled' | 'Passive';

type PurchaseOrderStatus =
  | 'Draft' | 'Approved' | 'PartiallyReceived' | 'FullyReceived' | 'Closed' | 'Cancelled';

type GoodsReceiptStatus = 'Draft' | 'Posted' | 'Cancelled';

type FloatingReturnStatus = 'Pending' | 'MatchedToShipment' | 'AddedToStock' | 'WrittenOff';

type StockCountStatus = 'Draft' | 'Completed';

interface StatusConfig { label: string; classes: string; }

const SHIPMENT_MAP: Record<ShipmentStatus, StatusConfig> = {
  Created:             { label: 'Taslak',         classes: 'bg-gray-100 text-gray-700' },
  AssignedToWarehouse: { label: 'Depoda',          classes: 'bg-yellow-100 text-yellow-800' },
  Picking:             { label: 'Toplanıyor',      classes: 'bg-blue-100 text-blue-800' },
  ReadyForDispatch:    { label: 'Sevke Hazır',     classes: 'bg-purple-100 text-purple-800' },
  AssignedToVehicle:   { label: 'Yolda',           classes: 'bg-indigo-100 text-indigo-800' },
  Delivered:           { label: 'Teslim Edildi',   classes: 'bg-green-100 text-green-800' },
  ReturnedToWarehouse: { label: 'İade',            classes: 'bg-orange-100 text-orange-800' },
  Cancelled:           { label: 'İptal',           classes: 'bg-red-100 text-red-700' },
  Passive:             { label: 'Pasif',           classes: 'bg-gray-200 text-gray-500' },
};

const PURCHASE_ORDER_MAP: Record<PurchaseOrderStatus, StatusConfig> = {
  Draft:             { label: 'Taslak',           classes: 'bg-gray-100 text-gray-700' },
  Approved:          { label: 'Onaylandı',        classes: 'bg-blue-100 text-blue-800' },
  PartiallyReceived: { label: 'Kısmen Alındı',    classes: 'bg-yellow-100 text-yellow-800' },
  FullyReceived:     { label: 'Tam Alındı',       classes: 'bg-green-100 text-green-800' },
  Closed:            { label: 'Kapatıldı',        classes: 'bg-gray-200 text-gray-600' },
  Cancelled:         { label: 'İptal',            classes: 'bg-red-100 text-red-700' },
};

const GOODS_RECEIPT_MAP: Record<GoodsReceiptStatus, StatusConfig> = {
  Draft:     { label: 'Taslak',   classes: 'bg-gray-100 text-gray-700' },
  Posted:    { label: 'Onaylandı',classes: 'bg-green-100 text-green-800' },
  Cancelled: { label: 'İptal',    classes: 'bg-red-100 text-red-700' },
};

const FLOATING_RETURN_MAP: Record<FloatingReturnStatus, StatusConfig> = {
  Pending:           { label: 'Beklemede',        classes: 'bg-orange-100 text-orange-800' },
  MatchedToShipment: { label: 'Eşleştirildi',    classes: 'bg-blue-100 text-blue-800' },
  AddedToStock:      { label: 'Stoğa Eklendi',   classes: 'bg-green-100 text-green-800' },
  WrittenOff:        { label: 'Hariç Tutuldu',   classes: 'bg-gray-200 text-gray-600' },
};

const STOCK_COUNT_MAP: Record<StockCountStatus, StatusConfig> = {
  Draft:     { label: 'Devam Ediyor', classes: 'bg-yellow-100 text-yellow-800' },
  Completed: { label: 'Tamamlandı',   classes: 'bg-green-100 text-green-800' },
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
