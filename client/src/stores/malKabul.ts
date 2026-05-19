import { defineStore } from 'pinia';
import { ref, computed } from 'vue';

export interface POAllocation {
    purchaseOrderLineId: string;
    purchaseOrderId: string;
    purchaseOrderNumber: string;
    supplierNameSnapshot: string;
    supplierId: string;
    orderedQty: number;
    remainingQty: number;
    receivedQty: number;
    rejectedQty: number;
    rejectReason: string;
}

export interface SessionEntry {
    stockMasterId: number;
    stockName: string;
    stockCode: string;
    unit: string;
    allocations: POAllocation[];
}

export const useMalKabulStore = defineStore('malKabul', () => {
    const entries = ref<SessionEntry[]>([]);

    const totalEntryCount = computed(() => entries.value.length);

    const totalReceivedCount = computed(() =>
        entries.value.reduce((sum, e) =>
            sum + e.allocations.reduce((s, a) => s + (a.receivedQty || 0), 0), 0)
    );

    function hasEntry(stockMasterId: number): boolean {
        return entries.value.some(e => e.stockMasterId === stockMasterId);
    }

    function getEntry(stockMasterId: number): SessionEntry | undefined {
        return entries.value.find(e => e.stockMasterId === stockMasterId);
    }

    function upsertEntry(entry: SessionEntry): void {
        const idx = entries.value.findIndex(e => e.stockMasterId === entry.stockMasterId);
        if (idx >= 0) {
            entries.value[idx] = entry;
        } else {
            entries.value.push(entry);
        }
    }

    function removeEntry(stockMasterId: number): void {
        const idx = entries.value.findIndex(e => e.stockMasterId === stockMasterId);
        if (idx >= 0) entries.value.splice(idx, 1);
    }

    function clearSession(): void {
        entries.value = [];
    }

    // All PO IDs that have at least one allocation with qty > 0
    const involvedPoIds = computed((): string[] => {
        const ids = new Set<string>();
        entries.value.forEach(e =>
            e.allocations.forEach(a => {
                if ((a.receivedQty || 0) > 0 || (a.rejectedQty || 0) > 0) {
                    ids.add(a.purchaseOrderId);
                }
            })
        );
        return Array.from(ids);
    });

    // Supplier derived from the first allocation with qty
    const supplierInfo = computed((): { supplierId: string; supplierName: string } | null => {
        for (const entry of entries.value) {
            for (const alloc of entry.allocations) {
                if ((alloc.receivedQty || 0) > 0) {
                    return { supplierId: alloc.supplierId, supplierName: alloc.supplierNameSnapshot };
                }
            }
        }
        return null;
    });

    return {
        entries,
        totalEntryCount,
        totalReceivedCount,
        hasEntry,
        getEntry,
        upsertEntry,
        removeEntry,
        clearSession,
        involvedPoIds,
        supplierInfo,
    };
});
