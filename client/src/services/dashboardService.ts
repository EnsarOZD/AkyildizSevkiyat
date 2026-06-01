import apiClient from './apiClient';

export interface RecentShipment {
    id: number;
    projectName: string;
    talepNo: string;
    status: string;
    deliveryDate: string;
    irsaliyeNo?: string | null;
}

export interface ImportBatchSummary {
    id: number;
    startedAt: string;
    completedAt?: string | null;
    status: string;
    newCount: number;
    needsMappingCount: number;
    failedCount: number;
}

export interface ActiveZonePreparation {
    id: number;
    zoneName: string;
    status: string;
    deliveryDate: string;
    shipmentCount: number;
    driverName?: string | null;
    plateNumber?: string | null;
}

export interface DashboardStats {
    // Shared
    totalActiveShipments: number;
    shipmentsToday: number;
    shipmentsOverdue: number;
    shipmentsDeliveredThisWeek: number;

    statusDraft: number;
    statusWarehouse: number;
    statusPicking: number;
    statusReady: number;
    statusOnRoute: number;

    pendingFloatingReturns: number;
    criticalStockCount: number;
    pendingGoodsReceiptsCount: number;

    pendingPOApprovalCount: number;
    todayShipmentsNotReadyCount: number;

    recentShipments: RecentShipment[];

    // Manager extras
    waitingForVehicleToday: RecentShipment[];
    waitingForVehicleTodayCount: number;

    // Accounting extras
    newIssOrdersTodayCount: number;
    pendingStockMappingCount: number;
    pendingNetsisTransferCount: number;
    todayDispatchedCount: number;
    missingItemsMailPendingCount: number;
    pendingNetsisShipments: RecentShipment[];
    missingItemsPendingShipments: RecentShipment[];
    lastIssImportBatch?: ImportBatchSummary | null;

    // Warehouse extras
    activeZonePreparations: ActiveZonePreparation[];
    todayPreparationNeededShipments: RecentShipment[];
}

export interface CriticalStockItem {
    stockMasterId: number;
    stockCode: string;
    stockName: string;
    unit: string;
    onHand: number;
    minStockQty: number;
    reorderPoint: number;
}

export const dashboardService = {
    getStats(): Promise<DashboardStats> {
        return apiClient.get('/dashboard/stats').then(r => r.data);
    },

    getCriticalStocks(): Promise<CriticalStockItem[]> {
        return apiClient.get('/dashboard/critical-stocks').then(r => r.data);
    },
};
