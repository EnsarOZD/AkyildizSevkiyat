import apiClient from './apiClient';

export interface RecentShipment {
    id: number;
    projectName: string;
    talepNo: string;
    status: string;
    deliveryDate: string;
}

export interface DashboardStats {
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
